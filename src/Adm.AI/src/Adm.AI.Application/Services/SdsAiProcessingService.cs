using System.Text.Json;
using Adm.AI.Application.DTOs.Internal;
using Adm.AI.Application.Exceptions;
using Adm.AI.Application.Interfaces;
using Adm.AI.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Adm.AI.Application.Services;

public class SdsAiProcessingService : ISdsAiProcessingService
{
    private readonly IOpenAiProvider _openAiProvider;
    private readonly ILogger<SdsAiProcessingService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SdsAiProcessingService(IOpenAiProvider openAiProvider, ILogger<SdsAiProcessingService> logger)
    {
        _openAiProvider = openAiProvider;
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<SdsAiResult> ProcessAsync(PromptTemplate prompt, string documentText, string schemaName = "sds_extraction", CancellationToken ct = default)
    {
        var userPrompt = prompt.UserPromptTemplate.Replace("{DOCUMENT_TEXT}", documentText);
        
        int attempt = 1;
        while (attempt <= 2)
        {
            try
            {
                var completionResult = await _openAiProvider.GetStructuredCompletionAsync(
                    systemPrompt: prompt.SystemPrompt,
                    userPrompt: userPrompt,
                    jsonSchemaName: schemaName,
                    jsonSchema: prompt.JsonSchema,
                    ct: ct);

                try
                {
                    var extractionResult = JsonSerializer.Deserialize<SdsExtractionResult>(
                        completionResult.JsonContent, _jsonOptions);

                    if (extractionResult == null)
                    {
                        throw new JsonException("Deserialized result was null.");
                    }

                    return new SdsAiResult(
                        ExtractionResult: extractionResult,
                        PromptTokens: completionResult.PromptTokens,
                        CompletionTokens: completionResult.CompletionTokens,
                        TotalTokens: completionResult.TotalTokens,
                        EstimatedCostUsd: completionResult.EstimatedCostUsd,
                        AiProcessingTimeMs: completionResult.DurationMs);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize OpenAI response on attempt {Attempt}. Raw JSON: {RawJson}", attempt, completionResult.JsonContent);
                    
                    if (attempt == 2)
                    {
                        throw new AiProcessingException("AI_INVALID_RESPONSE", "Failed to deserialize the AI response after 2 attempts.", ex);
                    }
                }
            }
            catch (Exception ex) when (ex is not AiProcessingException)
            {
                _logger.LogError(ex, "OpenAI API call failed on attempt {Attempt}", attempt);
                
                if (attempt == 2)
                {
                    throw new AiProcessingException("AI_SERVICE_ERROR", "The AI service encountered an error after 2 attempts.", ex);
                }
            }
            
            attempt++;
        }

        throw new AiProcessingException("AI_SERVICE_ERROR", "Unknown error during AI processing.");
    }

    public async Task<List<GhsPictogram>> DetectPictogramsAsync(PromptTemplate prompt, List<byte[]> images, CancellationToken ct = default)
    {
        if (images == null || images.Count == 0) return new List<GhsPictogram>();

        var userPrompt = prompt.UserPromptTemplate;
        
        try
        {
            var completionResult = await _openAiProvider.GetStructuredVisionCompletionAsync(
                systemPrompt: prompt.SystemPrompt,
                userPrompt: userPrompt,
                imageBytesList: images,
                mimeType: "image/jpeg",
                jsonSchemaName: "ghs_pictograms",
                jsonSchema: prompt.JsonSchema,
                ct: ct);

            var root = JsonDocument.Parse(completionResult.JsonContent).RootElement;
            var pictograms = new List<GhsPictogram>();

            if (root.TryGetProperty("pictograms", out var picsArray) && picsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var pic in picsArray.EnumerateArray())
                {
                    if (pic.TryGetProperty("type", out var typeProp))
                    {
                        var typeStr = typeProp.GetString();
                        if (!string.IsNullOrWhiteSpace(typeStr))
                        {
                            var ghsCode = typeStr.ToLowerInvariant() switch
                            {
                                "exploding bomb" => "GHS01",
                                "flame" => "GHS02",
                                "flame over circle" => "GHS03",
                                "gas cylinder" => "GHS04",
                                "corrosion" => "GHS05",
                                "skull and crossbones" => "GHS06",
                                "exclamation mark" => "GHS07",
                                "health hazard" => "GHS08",
                                "environment" => "GHS09",
                                _ => null
                            };
                            
                            if (ghsCode != null)
                            {
                                pictograms.Add(new GhsPictogram { Name = typeStr, GhsCode = ghsCode });
                            }
                        }
                    }
                }
            }

            return pictograms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect pictograms via vision API.");
            return new List<GhsPictogram>(); // Degrade gracefully
        }
    }

}
