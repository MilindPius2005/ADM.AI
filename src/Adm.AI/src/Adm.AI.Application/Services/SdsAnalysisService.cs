using System.Diagnostics;
using System.Collections.Concurrent;
using System.Text.Json;
using Adm.AI.Application.Configuration;
using Adm.AI.Application.DTOs;
using Adm.AI.Application.DTOs.Internal;
using Adm.AI.Application.Exceptions;
using Adm.AI.Application.Interfaces;
using Adm.AI.Domain.Entities;
using Adm.AI.Domain.Enums;
using Adm.AI.Domain.Models;
using Adm.AI.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adm.AI.Application.Services;

public class SdsAnalysisService : ISdsAnalysisService
{
    private readonly IFileValidationService _fileValidation;
    private readonly IHashService _hashService;
    private readonly IPdfExtractionService _pdfExtraction;
    private readonly IPdfRenderingService _pdfRendering;
    private readonly ITextCleaningService _textCleaning;
    private readonly IPromptService _promptService;
    private readonly ISdsAiProcessingService _aiProcessing;
    private readonly IAnalysisRepository _repository;
    private readonly IAiConcurrencyLimiter _concurrencyLimiter;
    private readonly OpenAiSettings _openAiSettings;
    private readonly ILogger<SdsAnalysisService> _logger;
    private readonly ISdsSectionParser _sectionParser;
    private readonly SdsChunkingSettings _chunkingSettings;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public SdsAnalysisService(
        IFileValidationService fileValidation,
        IHashService hashService,
        IPdfExtractionService pdfExtraction,
        IPdfRenderingService pdfRendering,
        ITextCleaningService textCleaning,
        IPromptService promptService,
        ISdsAiProcessingService aiProcessing,
        IAnalysisRepository repository,
        IAiConcurrencyLimiter concurrencyLimiter,
        IOptions<OpenAiSettings> openAiSettings,
        ILogger<SdsAnalysisService> logger,
        ISdsSectionParser sectionParser,
        IOptions<SdsChunkingSettings> chunkingSettings)
    {
        _fileValidation = fileValidation;
        _hashService = hashService;
        _pdfExtraction = pdfExtraction;
        _pdfRendering = pdfRendering;
        _textCleaning = textCleaning;
        _promptService = promptService;
        _aiProcessing = aiProcessing;
        _repository = repository;
        _concurrencyLimiter = concurrencyLimiter;
        _openAiSettings = openAiSettings.Value;
        _logger = logger;
        _sectionParser = sectionParser;
        _chunkingSettings = chunkingSettings.Value;
    }

    public async Task<ApiResponse<SdsAnalysisResponse>> AnalyzeSdsAsync(
        string fileName, byte[] fileBytes, string? contentType, CancellationToken ct = default)
    {
        return await AnalyzeSingleDocumentAsync(fileName, fileBytes, contentType, ct);
    }

    public async Task<BatchAnalysisResponse> AnalyzeBatchAsync(
        List<(string FileName, byte[] FileBytes, string? ContentType)> files,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new ConcurrentBag<BatchItemResult>();

        var tasks = files.Select(async file =>
        {
            return await _concurrencyLimiter.ExecuteAsync(async () =>
            {
                var itemStopwatch = Stopwatch.StartNew();
                try
                {
                    var response = await AnalyzeSingleDocumentAsync(file.FileName, file.FileBytes, file.ContentType, ct);
                    itemStopwatch.Stop();
                    
                    if (response.Success)
                    {
                        results.Add(new BatchItemResult
                        {
                            FileName = file.FileName,
                            Success = true,
                            FromCache = response.FromCache,
                            AnalysisId = response.AnalysisId,
                            ProcessingTimeMs = response.ProcessingTimeMs,
                            Data = response.Data,
                            Error = null
                        });
                    }
                    else
                    {
                        results.Add(new BatchItemResult
                        {
                            FileName = file.FileName,
                            Success = false,
                            FromCache = false,
                            AnalysisId = null,
                            ProcessingTimeMs = itemStopwatch.ElapsedMilliseconds,
                            Data = null,
                            Error = response.Error
                        });
                    }
                }
                catch (Exception ex)
                {
                    itemStopwatch.Stop();
                    _logger.LogError(ex, "Unexpected error processing file {FileName}", file.FileName);
                    
                    var errorCode = ex switch 
                    {
                        FileValidationException => ((FileValidationException)ex).ErrorCode,
                        PdfExtractionException => ((PdfExtractionException)ex).ErrorCode,
                        AiProcessingException => ((AiProcessingException)ex).ErrorCode,
                        _ => "UNKNOWN_ERROR"
                    };

                    results.Add(new BatchItemResult
                    {
                        FileName = file.FileName,
                        Success = false,
                        FromCache = false,
                        AnalysisId = null,
                        ProcessingTimeMs = itemStopwatch.ElapsedMilliseconds,
                        Data = null,
                        Error = new ErrorDetail(errorCode, ex.Message)
                    });
                }
                return 0; // dummy
            }, ct);
        });

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var resultList = results.ToList();

        _logger.LogInformation("Batch processing completed. Total files: {Total}, Successful: {Successful}, Failed: {Failed}, Total time: {TotalTimeMs}ms",
            files.Count, resultList.Count(x => x.Success), resultList.Count(x => !x.Success), stopwatch.ElapsedMilliseconds);

        return new BatchAnalysisResponse
        {
            Total = resultList.Count,
            Successful = resultList.Count(x => x.Success),
            Failed = resultList.Count(x => !x.Success),
            TotalProcessingTimeMs = stopwatch.ElapsedMilliseconds,
            Results = resultList
        };
    }

    
    private async Task<ApiResponse<SdsAnalysisResponse>> AnalyzeSingleDocumentAsync(
        string fileName, byte[] fileBytes, string? contentType, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // 1. Validate file
        _fileValidation.Validate(fileName, fileBytes.Length, contentType, fileBytes);

        // 2. Compute SHA-256 hash
        var hash = _hashService.ComputeHash(fileBytes);

        // 3. Check for existing analysis (duplicate detection)
        var existing = await _repository.GetByHashAsync(hash, ct);
        if (existing != null)
        {
            stopwatch.Stop();
            _logger.LogInformation("Duplicate SDS detected for hash {Hash}, returning cached result", hash);
            return ApiResponse<SdsAnalysisResponse>.Ok(
                MapToResponse(existing),
                existing.Id.ToString(),
                stopwatch.ElapsedMilliseconds,
                fromCache: true);
        }

        // 4. Extract PDF text
        var preparation = Stopwatch.StartNew();
        var pdfResult = _pdfExtraction.Extract(fileBytes);
        var cleanedText = _textCleaning.Clean(pdfResult.Text);
        _logger.LogInformation("PDF text prepared for {FileName} in {ElapsedMs}ms", fileName, preparation.ElapsedMilliseconds);
        preparation.Restart();
        var images = _pdfRendering.RenderPagesToImages(fileBytes, 1, 2);
        _logger.LogInformation("PDF images rendered for {FileName} in {ElapsedMs}ms", fileName, preparation.ElapsedMilliseconds);

        SdsAiResult aiResult;
        bool chunkingFailed = false;
        var promptVersion = "single-pass";

        try
        {
            var prompt = await _promptService.GetPromptAsync("sds", ct);
            aiResult = await _aiProcessing.ProcessMultimodalAsync(prompt, cleanedText, images, ct);
            _logger.LogInformation("Single-pass extraction for {FileName}: AI time {AiMs}ms, tokens {Tokens}",
                fileName, aiResult.AiProcessingTimeMs, aiResult.TotalTokens);
        }
        catch (AiProcessingException singlePassError) when (!ct.IsCancellationRequested &&
            (singlePassError.ErrorCode == "AI_INVALID_RESPONSE" || singlePassError.ErrorCode == "AI_BAD_REQUEST"))
        {
            _logger.LogWarning(singlePassError, "Single-pass extraction failed for {FileName}; using section fallback", fileName);
            promptVersion = "fallback";
            var visionPrompt = await _promptService.GetPromptAsync("vision", ct);
            var visionAiTask = _aiProcessing.DetectPictogramsAsync(visionPrompt, images, ct);

        if (_chunkingSettings.Enabled && _sectionParser.ParseSections(cleanedText).Count > 0)
        {
            var sections = _sectionParser.ParseSections(cleanedText);
            _logger.LogInformation("SDS Parser found {Count} sections: [{Sections}]", 
                sections.Count, 
                string.Join(", ", sections.Select(s => $"S{s.SectionNumber}:{s.Title}")));
            
            var chunkTasks = new Dictionary<string, Task<SdsAiResult>>();

            foreach (var group in _chunkingSettings.Groups)
            {
                var groupSections = sections.Where(s => group.Sections.Contains(s.SectionNumber)).ToList();
                var chunkText = string.Join("\n\n", groupSections.Select(s => $"SECTION {s.SectionNumber}: {s.Title}\n{s.Content}"));
                
                // A missing group has no useful input; never send the whole PDF four times.
                if (string.IsNullOrWhiteSpace(chunkText))
                {
                    _logger.LogWarning("Chunk {Group} found no sections; skipping", group.Name);
                    chunkingFailed = true;
                    continue;
                }
                else
                {
                    _logger.LogInformation("Chunk {Group}: {SectionCount} sections, {Len} chars", group.Name, groupSections.Count, chunkText.Length);
                }

                var chunkPrompt = await _promptService.GetPromptAsync($"sds/chunks/{group.Name}", ct);
                
                // Start task and store it
                chunkTasks[group.Name] = _aiProcessing.ProcessAsync(chunkPrompt, chunkText, $"sds_chunk_{group.Name}", ct);
            }

            var results = new Dictionary<string, SdsExtractionResult>();
            int totalPromptTokens = 0, totalCompletionTokens = 0, totalTokens = 0;
            decimal totalCost = 0;
            long maxProcessingTime = 0;

            foreach (var kvp in chunkTasks)
            {
                try
                {
                    var chunkRes = await kvp.Value;
                    results[kvp.Key] = chunkRes.ExtractionResult;
                    
                    _logger.LogInformation("Chunk {ChunkName} succeeded: tokens={Tokens}, chemicalName={Chem}, signalWord={Signal}, physProps={Phys}",
                        kvp.Key, chunkRes.TotalTokens,
                        chunkRes.ExtractionResult.ChemicalName ?? "(null)",
                        chunkRes.ExtractionResult.SignalWord ?? "(null)",
                        chunkRes.ExtractionResult.PhysicalProperties != null ? "present" : "(null)");
                    
                    totalPromptTokens += chunkRes.PromptTokens;
                    totalCompletionTokens += chunkRes.CompletionTokens;
                    totalTokens += chunkRes.TotalTokens;
                    totalCost += chunkRes.EstimatedCostUsd;
                    maxProcessingTime = Math.Max(maxProcessingTime, chunkRes.AiProcessingTimeMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Chunk {ChunkName} failed.", kvp.Key);
                    chunkingFailed = true;
                }
            }

            var mergedResult = MergeChunkResults(results);
            aiResult = new SdsAiResult(mergedResult, totalPromptTokens, totalCompletionTokens, totalTokens, totalCost, maxProcessingTime);
        }
        else
        {
            var prompt = await _promptService.GetPromptAsync("sds", ct);
            var textAiTask = _aiProcessing.ProcessAsync(prompt, cleanedText, "sds_extraction", ct);
            await Task.WhenAll(textAiTask, visionAiTask);
            aiResult = textAiTask.Result;
        }

        var pictograms = await visionAiTask;
        if (pictograms != null && pictograms.Count > 0)
        {
            var mergedExtractionResult = aiResult.ExtractionResult with { Pictograms = pictograms };
            aiResult = aiResult with { ExtractionResult = mergedExtractionResult };
        }
        }

        var analysisResultJson = JsonSerializer.Serialize(aiResult.ExtractionResult, _jsonOptions);

        var analysis = new SdsAnalysis
        {
            Id = Guid.NewGuid(),
            FileHash = hash,
            FileName = fileName,
            FileSizeBytes = fileBytes.Length,
            ExtractedText = cleanedText,
            AnalysisResultJson = analysisResultJson,
            Status = chunkingFailed ? AnalysisStatus.PartiallyCompleted : AnalysisStatus.Completed,
            PromptVersion = chunkingFailed ? "fallback-partial" : promptVersion,
            OpenAiModel = _openAiSettings.Model,
            PromptTokens = aiResult.PromptTokens,
            CompletionTokens = aiResult.CompletionTokens,
            TotalTokens = aiResult.TotalTokens,
            EstimatedCostUsd = aiResult.EstimatedCostUsd,
            ProcessingTimeMs = aiResult.AiProcessingTimeMs,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var saved = await _repository.CreateAsync(analysis, ct);
        stopwatch.Stop();
        
        _logger.LogInformation("SDS Pipeline: {FileName} Total: {TotalTime}s", fileName, stopwatch.Elapsed.TotalSeconds.ToString("0.00"));

        return ApiResponse<SdsAnalysisResponse>.Ok(
            MapToResponse(saved),
            saved.Id.ToString(),
            stopwatch.ElapsedMilliseconds,
            fromCache: false);
    }

    private SdsExtractionResult MergeChunkResults(Dictionary<string, SdsExtractionResult> chunkResults)
    {
        var finalResult = new SdsExtractionResult();
        var allConfidences = new List<FieldConfidence>();

        foreach (var kvp in chunkResults)
        {
            var chunk = kvp.Value;
            if (chunk == null) continue;

            if (kvp.Key == "group_1_4")
            {
                finalResult = finalResult with {
                    ChemicalName = chunk.ChemicalName,
                    ProductIdentifier = chunk.ProductIdentifier,
                    CasNumber = chunk.CasNumber,
                    UnNumber = chunk.UnNumber,
                    RecommendedUse = chunk.RecommendedUse,
                    Manufacturer = chunk.Manufacturer,
                    Supplier = chunk.Supplier,
                    EmergencyPhone = chunk.EmergencyPhone,
                    SignalWord = chunk.SignalWord,
                    HazardClassification = chunk.HazardClassification,
                    HazardStatements = chunk.HazardStatements,
                    PrecautionaryStatements = chunk.PrecautionaryStatements,
                    Composition = chunk.Composition,
                    FirstAidMeasures = chunk.FirstAidMeasures
                };
            }
            else if (kvp.Key == "group_5_8")
            {
                finalResult = finalResult with {
                    FireFightingMeasures = chunk.FireFightingMeasures,
                    AccidentalReleaseMeasures = chunk.AccidentalReleaseMeasures,
                    HandlingStorage = chunk.HandlingStorage,
                    ExposureControls = chunk.ExposureControls,
                    Ppe = chunk.Ppe
                };
            }
            else if (kvp.Key == "group_9_12")
            {
                finalResult = finalResult with {
                    PhysicalProperties = chunk.PhysicalProperties,
                    StabilityReactivity = chunk.StabilityReactivity,
                    ToxicologicalInformation = chunk.ToxicologicalInformation,
                    EcologicalInformation = chunk.EcologicalInformation
                };
            }
            else if (kvp.Key == "group_13_16")
            {
                finalResult = finalResult with {
                    DisposalConsiderations = chunk.DisposalConsiderations,
                    TransportInformation = chunk.TransportInformation,
                    RegulatoryInformation = chunk.RegulatoryInformation
                };
            }

            if (chunk.Confidence != null && chunk.Confidence.FieldConfidences != null)
            {
                allConfidences.AddRange(chunk.Confidence.FieldConfidences);
            }
        }

        // Aggregate confidence
        string overallConfidence = "High";
        if (allConfidences.Any(c => c.Level == "Low")) overallConfidence = "Low";
        else if (allConfidences.Any(c => c.Level == "Medium")) overallConfidence = "Medium";

        finalResult = finalResult with {
            Confidence = new ConfidenceInfo(overallConfidence, "Merged from chunks.", allConfidences)
        };

        return finalResult;
    }

    
    private SdsAnalysisResponse MapToResponse(SdsAnalysis entity, SdsExtractionResult? extractionResult = null)
    {
        var result = extractionResult ??
            JsonSerializer.Deserialize<SdsExtractionResult>(entity.AnalysisResultJson, _jsonOptions)!;

        return new SdsAnalysisResponse
        {
            AnalysisId = entity.Id.ToString(),
            FileName = entity.FileName,
            FileHash = entity.FileHash,
            Status = entity.Status.ToString(),
            PromptVersion = entity.PromptVersion,
            OpenAiModel = entity.OpenAiModel,
            PromptTokens = entity.PromptTokens,
            CompletionTokens = entity.CompletionTokens,
            TotalTokens = entity.TotalTokens,
            EstimatedCostUsd = entity.EstimatedCostUsd,
            ProcessingTimeMs = entity.ProcessingTimeMs,
            CreatedAt = entity.CreatedAt,
            ExtractionResult = result
        };
    }
    public async Task<ApiResponse<SdsAnalysisResponse>> GetAnalysisAsync(Guid id, CancellationToken ct = default)
    {
        var analysis = await _repository.GetByIdAsync(id, ct);
        if (analysis == null)
        {
            return ApiResponse<SdsAnalysisResponse>.Fail("NOT_FOUND", "Analysis not found.");
        }

        return ApiResponse<SdsAnalysisResponse>.Ok(MapToResponse(analysis), id.ToString(), 0, true);
    }
}
