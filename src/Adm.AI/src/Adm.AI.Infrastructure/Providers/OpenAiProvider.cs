using System.Diagnostics;
using Adm.AI.Application.Exceptions;
using Adm.AI.Application.Interfaces;
using Adm.AI.Application.DTOs.Internal;
using Adm.AI.Application.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.ClientModel;
using System.Threading;

namespace Adm.AI.Infrastructure.Providers;

public class OpenAiProvider : IOpenAiProvider
{
    private readonly OpenAiSettings _settings;
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenAiProvider> _logger;
    private readonly SemaphoreSlim _rateLimiter;

    public OpenAiProvider(IOptions<OpenAiSettings> settings, IOptions<AiProcessingSettings> aiSettings, ILogger<OpenAiProvider> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _rateLimiter = new SemaphoreSlim(Math.Max(1, aiSettings.Value.MaxConcurrentAiCalls));
        
        if (!string.IsNullOrWhiteSpace(_settings.EndpointUrl))
        {
            var options = new OpenAI.OpenAIClientOptions
            {
                Endpoint = new Uri(_settings.EndpointUrl)
            };
            _chatClient = new ChatClient(_settings.Model, new ApiKeyCredential(_settings.ApiKey), options);
        }
        else
        {
            _chatClient = new ChatClient(_settings.Model, _settings.ApiKey);
        }
    }

    public async Task<AiCompletionResult> GetStructuredCompletionAsync(
        string systemPrompt,
        string userPrompt,
        string jsonSchemaName,
        string jsonSchema,
        CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        return await ExecuteCompletionAsync(messages, jsonSchemaName, jsonSchema, ct);
    }

    public async Task<AiCompletionResult> GetStructuredVisionCompletionAsync(
        string systemPrompt,
        string userPrompt,
        List<byte[]> imageBytesList,
        string mimeType,
        string jsonSchemaName,
        string jsonSchema,
        CancellationToken ct = default)
    {
        var userContentParts = new List<ChatMessageContentPart> { ChatMessageContentPart.CreateTextPart(userPrompt) };
        foreach (var imageBytes in imageBytesList)
        {
            userContentParts.Add(ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageBytes), mimeType));
        }

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userContentParts)
        };

        return await ExecuteCompletionAsync(messages, jsonSchemaName, jsonSchema, ct);
    }

    private async Task<AiCompletionResult> ExecuteCompletionAsync(
        List<ChatMessage> messages,
        string jsonSchemaName,
        string jsonSchema,
        CancellationToken ct)
    {
        var chatOptions = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: jsonSchemaName,
                jsonSchema: BinaryData.FromString(jsonSchema),
                jsonSchemaIsStrict: false
            ),
            Temperature = _settings.Temperature,
            MaxOutputTokenCount = _settings.MaxTokens
        };

        var stopwatch = Stopwatch.StartNew();
        int maxRetries = _settings.MaxRetries > 0 ? _settings.MaxRetries : 5;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            var wait = Stopwatch.StartNew();
            await _rateLimiter.WaitAsync(ct);
            _logger.LogInformation("AI slot wait {WaitMs}ms; attempt {Attempt}/{Max}", wait.ElapsedMilliseconds, attempt, maxRetries);
            int retryDelayMs = 0;
            try
            {
                try 
                {
                    var call = Stopwatch.StartNew();
                    var completion = await _chatClient.CompleteChatAsync(messages, chatOptions, ct);
                    _logger.LogInformation("AI call {CallMs}ms; input tokens {InputTokens}; output tokens {OutputTokens}",
                        call.ElapsedMilliseconds, completion.Value.Usage.InputTokenCount, completion.Value.Usage.OutputTokenCount);
                    stopwatch.Stop();

                    var resultText = completion.Value.Content[0].Text;
                    var promptTokens = completion.Value.Usage.InputTokenCount;
                    var completionTokens = completion.Value.Usage.OutputTokenCount;
                    var totalTokens = completion.Value.Usage.TotalTokenCount;

                    var cost = (promptTokens * _settings.InputTokenPricePerMillion / 1_000_000m)
                             + (completionTokens * _settings.OutputTokenPricePerMillion / 1_000_000m);

                    return new AiCompletionResult(
                        JsonContent: resultText,
                        PromptTokens: promptTokens,
                        CompletionTokens: completionTokens,
                        TotalTokens: totalTokens,
                        EstimatedCostUsd: cost,
                        DurationMs: stopwatch.ElapsedMilliseconds);
                }
                catch (ClientResultException ex) when ((ex.Status == 429 || ex.Status == 503 || ex.Status == 500) && attempt < maxRetries)
                {
                    retryDelayMs = attempt * 2500;
                    _logger.LogWarning("Service error ({Status}) by AI provider. Retrying in {Delay}ms (Attempt {Attempt}/{Max})", ex.Status, retryDelayMs, attempt, maxRetries);
                }
                catch (ClientResultException ex)
                {
                    _logger.LogError(ex, "OpenAI API returned an error status.");
                    var code = ex.Status switch
                    {
                        429 => "AI_RATE_LIMITED",
                        400 => "AI_BAD_REQUEST",
                        401 or 403 => "AI_AUTH_ERROR",
                        _ => "AI_SERVICE_ERROR"
                    };
                    throw new AiProcessingException(code, "The AI service returned an error.", ex);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error calling OpenAI API.");
                    throw new AiProcessingException("AI_SERVICE_ERROR", "Failed to call AI service.", ex);
                }
            }
            finally
            {
                _rateLimiter.Release();
            }
            if (retryDelayMs > 0) await Task.Delay(retryDelayMs, ct);
        }
        
        throw new AiProcessingException("AI_TIMEOUT", "Exceeded max retries for AI service.");
    }
}
