namespace Adm.AI.UnitTests.Services;

using Adm.AI.Application.Configuration;
using Adm.AI.Application.DTOs;
using Adm.AI.Application.DTOs.Internal;
using Adm.AI.Application.Exceptions;
using Adm.AI.Domain.Models;

/// <summary>
/// Phase 1 verification tests — validates that Application layer DTOs,
/// configuration classes, and exceptions compile and behave correctly.
/// </summary>
public class ApplicationContractTests
{
    [Fact]
    public void ApiResponse_Ok_SetsCorrectProperties()
    {
        var response = ApiResponse<string>.Ok("test-data", "analysis-123", 5000, fromCache: false);

        Assert.True(response.Success);
        Assert.False(response.FromCache);
        Assert.Equal("analysis-123", response.AnalysisId);
        Assert.Equal(5000, response.ProcessingTimeMs);
        Assert.Equal("test-data", response.Data);
        Assert.Null(response.Error);
    }

    [Fact]
    public void ApiResponse_Ok_FromCache_SetsFlag()
    {
        var response = ApiResponse<string>.Ok("cached-data", "analysis-456", 12, fromCache: true);

        Assert.True(response.Success);
        Assert.True(response.FromCache);
    }

    [Fact]
    public void ApiResponse_Fail_SetsErrorProperties()
    {
        var response = ApiResponse<string>.Fail("INVALID_FILE_TYPE", "Only PDF files are accepted.");

        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("INVALID_FILE_TYPE", response.Error!.Code);
        Assert.Equal("Only PDF files are accepted.", response.Error.Message);
        Assert.Null(response.Data);
        Assert.Null(response.AnalysisId);
    }

    [Fact]
    public void SdsAnalysisResponse_CanBeCreated()
    {
        var response = new SdsAnalysisResponse
        {
            AnalysisId = Guid.NewGuid().ToString(),
            FileName = "test.pdf",
            FileHash = "abc123",
            Status = "Completed",
            PromptVersion = "1.0.0",
            OpenAiModel = "gpt-4o",
            PromptTokens = 100,
            CompletionTokens = 200,
            TotalTokens = 300,
            EstimatedCostUsd = 0.005m,
            ProcessingTimeMs = 10000,
            CreatedAt = DateTimeOffset.UtcNow,
            ExtractionResult = new SdsExtractionResult { ChemicalName = "Test" }
        };

        Assert.Equal("Completed", response.Status);
        Assert.Equal("Test", response.ExtractionResult.ChemicalName);
    }

    [Fact]
    public void InternalDTOs_CanBeConstructed()
    {
        var pdfResult = new PdfExtractionResult("extracted text", 10);
        Assert.Equal("extracted text", pdfResult.Text);
        Assert.Equal(10, pdfResult.PageCount);

        var prompt = new PromptTemplate("system", "user {DOCUMENT_TEXT}", "{}", "1.0.0");
        Assert.Equal("1.0.0", prompt.Version);

        var aiResult = new AiCompletionResult("{}", 100, 200, 300, 0.005m, 5000);
        Assert.Equal(300, aiResult.TotalTokens);
        Assert.Equal(0.005m, aiResult.EstimatedCostUsd);

        var sdsResult = new SdsAiResult(
            new SdsExtractionResult { ChemicalName = "Test" },
            100, 200, 300, 0.005m, 5000);
        Assert.Equal("Test", sdsResult.ExtractionResult.ChemicalName);
    }

    [Fact]
    public void OpenAiSettings_HasCorrectDefaults()
    {
        var settings = new OpenAiSettings();

        Assert.Equal("gpt-4o", settings.Model);
        Assert.Equal(16384, settings.MaxTokens);
        Assert.Equal(0f, settings.Temperature);
        Assert.Equal(120, settings.TimeoutSeconds);
        Assert.Equal(2, settings.MaxRetries);
        Assert.Equal(2.50m, settings.InputTokenPricePerMillion);
        Assert.Equal(10.00m, settings.OutputTokenPricePerMillion);
    }

    [Fact]
    public void FileUploadSettings_HasCorrectDefaults()
    {
        var settings = new FileUploadSettings();

        Assert.Equal(104857600, settings.MaxFileSizeBytes);
        Assert.Equal(4000000, settings.MaxExtractedTextLength);
        Assert.Single(settings.AllowedExtensions);
        Assert.Equal(".pdf", settings.AllowedExtensions[0]);
    }

    [Fact]
    public void PromptSettings_HasCorrectDefaults()
    {
        var settings = new PromptSettings();
        Assert.Equal("prompts", settings.BasePath);
    }

    [Fact]
    public void FileValidationException_CarriesErrorCode()
    {
        var ex = new FileValidationException("FILE_TOO_LARGE", "File exceeds 25MB limit.");
        Assert.Equal("FILE_TOO_LARGE", ex.ErrorCode);
        Assert.Equal("File exceeds 25MB limit.", ex.Message);
    }

    [Fact]
    public void PdfExtractionException_CarriesErrorCode_And_InnerException()
    {
        var inner = new InvalidOperationException("corrupted");
        var ex = new PdfExtractionException("PDF_EXTRACTION_FAILED", "Cannot parse PDF.", inner);
        Assert.Equal("PDF_EXTRACTION_FAILED", ex.ErrorCode);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void AiProcessingException_CarriesErrorCode_And_InnerException()
    {
        var inner = new TimeoutException("timed out");
        var ex = new AiProcessingException("AI_SERVICE_TIMEOUT", "OpenAI timed out.", inner);
        Assert.Equal("AI_SERVICE_TIMEOUT", ex.ErrorCode);
        Assert.Same(inner, ex.InnerException);
    }
}
