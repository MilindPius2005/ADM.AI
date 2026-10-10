using Adm.AI.Application.Configuration;
using Adm.AI.Application.DTOs;
using Adm.AI.Application.DTOs.Internal;
using Adm.AI.Application.Interfaces;
using Adm.AI.Application.Services;
using Adm.AI.Domain.Entities;
using Adm.AI.Domain.Models;
using Adm.AI.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Adm.AI.UnitTests.Services;

public class SdsAnalysisServiceTests
{
    private readonly Mock<IFileValidationService> _fileValidation = new();
    private readonly Mock<IHashService> _hashService = new();
    private readonly Mock<IPdfExtractionService> _pdfExtraction = new();
    private readonly Mock<IPdfRenderingService> _pdfRendering = new();
    private readonly Mock<ITextCleaningService> _textCleaning = new();
    private readonly Mock<IPromptService> _promptService = new();
    private readonly Mock<ISdsAiProcessingService> _aiProcessing = new();
    private readonly Mock<IAnalysisRepository> _repository = new();
    private readonly Mock<IAiConcurrencyLimiter> _concurrencyLimiter = new();
    private readonly Mock<IOptions<OpenAiSettings>> _openAiSettings = new();
    private readonly Mock<ILogger<SdsAnalysisService>> _logger = new();
    private readonly Mock<ISdsSectionParser> _sectionParser = new();
    private readonly Mock<IOptions<SdsChunkingSettings>> _chunkingSettings = new();

    private SdsAnalysisService CreateService()
    {
        _openAiSettings.Setup(x => x.Value).Returns(new OpenAiSettings { Model = "gpt-4" });
        _chunkingSettings.Setup(x => x.Value).Returns(new SdsChunkingSettings { Enabled = false });

        return new SdsAnalysisService(
            _fileValidation.Object,
            _hashService.Object,
            _pdfExtraction.Object,
            _pdfRendering.Object,
            _textCleaning.Object,
            _promptService.Object,
            _aiProcessing.Object,
            _repository.Object,
            _concurrencyLimiter.Object,
            _openAiSettings.Object,
            _logger.Object,
            _sectionParser.Object,
            _chunkingSettings.Object
        );
    }

    [Fact]
    public async Task AnalyzeBatchAsync_EmptyList_ReturnsEmptyResponse()
    {
        var service = CreateService();
        var response = await service.AnalyzeBatchAsync(new List<(string, byte[], string?)>());
        Assert.Equal(0, response.Total);
    }
}
