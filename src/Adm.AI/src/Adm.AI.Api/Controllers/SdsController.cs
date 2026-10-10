using Adm.AI.Application.Configuration;
using Adm.AI.Application.DTOs;
using Adm.AI.Application.Exceptions;
using Adm.AI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
namespace Adm.AI.Api.Controllers;
[ApiController]
[Route("api/sds")]
public class SdsController : ControllerBase
{
    private readonly ISdsAnalysisService _analysisService;
    private readonly ILogger<SdsController> _logger;
    private readonly AiProcessingSettings _aiSettings;
    public SdsController(ISdsAnalysisService analysisService, ILogger<SdsController> logger, IOptions<AiProcessingSettings> aiSettings)
    {
        _analysisService = analysisService;
        _logger = logger;
        _aiSettings = aiSettings.Value;
    }
    /// <summary>
    /// Analyze an SDS PDF document or a batch of documents.
    /// </summary>
    [HttpPost("analyze")]
    [RequestSizeLimit(104_857_600L * 10)] // 100 MB * 10 max
    public async Task<IActionResult> AnalyzeSds(List<IFormFile> files, CancellationToken ct)
    {
        // Try fallback if someone sent a single file using the old parameter name 'file'
        // If files is empty but Request.Form.Files has it, let's grab it.
        if ((files == null || files.Count == 0) && Request.HasFormContentType)
        {
            files = Request.Form.Files.ToList();
        }
        if (files == null || files.Count == 0)
        {
            return BadRequest(ApiResponse<SdsAnalysisResponse>.Fail("EMPTY_FILE", "No files were uploaded."));
        }
        if (files.Count > _aiSettings.MaxBatchSize)
        {
            return BadRequest(ApiResponse<SdsAnalysisResponse>.Fail("BATCH_TOO_LARGE", $"Maximum batch size is {_aiSettings.MaxBatchSize} files."));
        }
        if (files.Count == 1)
        {
            var file = files[0];
            if (file.Length == 0)
            {
                return BadRequest(ApiResponse<SdsAnalysisResponse>.Fail("EMPTY_FILE", "The file is empty."));
            }
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms, ct);
                fileBytes = ms.ToArray();
            }
            try
            {
                var result = await _analysisService.AnalyzeSdsAsync(
                    file.FileName, fileBytes, file.ContentType, ct);

                return Ok(result);
            }
            catch (FileValidationException ex)
            {
                _logger.LogWarning(ex, "File validation failed: {ErrorCode}", ex.ErrorCode);
                return BadRequest(ApiResponse<SdsAnalysisResponse>.Fail(ex.ErrorCode, ex.Message));
            }
            catch (PdfExtractionException ex)
            {
                _logger.LogWarning(ex, "PDF extraction failed: {ErrorCode}", ex.ErrorCode);
                return UnprocessableEntity(ApiResponse<SdsAnalysisResponse>.Fail(ex.ErrorCode, ex.Message));
            }
            catch (AiProcessingException ex)
            {
                _logger.LogError(ex, "AI processing failed: {ErrorCode}", ex.ErrorCode);
                return StatusCode(502, ApiResponse<SdsAnalysisResponse>.Fail(ex.ErrorCode, ex.Message));
            }
        }
        else
        {
            var fileDataList = new List<(string FileName, byte[] FileBytes, string? ContentType)>();

            foreach (var file in files)
            {
                if (file.Length == 0) continue;
                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms, ct);
                    fileBytes = ms.ToArray();
                }
                fileDataList.Add((file.FileName, fileBytes, file.ContentType));
            }

            if (fileDataList.Count == 0)
            {
                return BadRequest(ApiResponse<SdsAnalysisResponse>.Fail("EMPTY_FILE", "All uploaded files were empty."));
            }

            var batchResult = await _analysisService.AnalyzeBatchAsync(fileDataList, ct);
            return Ok(batchResult);
        }
    }

    /// <summary>
    /// Retrieve a previously stored SDS analysis by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAnalysis(Guid id, CancellationToken ct)
    {
        var result = await _analysisService.GetAnalysisAsync(id, ct);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
