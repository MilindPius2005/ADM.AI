using System.Text;
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

public class SdsAnalysisBatchTests
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
    public async Task AnalyzeBatchAsync_ValidFiles_ReturnsBatchResponse()
    {
        var service = CreateService();
        var files = new List<(string FileName, byte[] FileBytes, string? ContentType)>
        {
            ("test1.pdf", Encoding.UTF8.GetBytes("content1"), "application/pdf"),
            ("test2.pdf", Encoding.UTF8.GetBytes("content2"), "application/pdf"),
            ("test3.pdf", Encoding.UTF8.GetBytes("content3"), "application/pdf")
        };

        _concurrencyLimiter.Setup(x => x.ExecuteAsync(It.IsAny<Func<Task<int>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<int>>, CancellationToken>((func, ct) => func());

        _hashService.Setup(x => x.ComputeHash(It.IsAny<byte[]>())).Returns(Guid.NewGuid().ToString());
        _repository.Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SdsAnalysis?)null);
        _repository.Setup(x => x.CreateAsync(It.IsAny<SdsAnalysis>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SdsAnalysis a, CancellationToken _) => a);
        
        _pdfExtraction.Setup(x => x.Extract(It.IsAny<byte[]>())).Returns(new PdfExtractionResult("text", 2));
        _pdfRendering.Setup(x => x.RenderPagesToImages(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new List<byte[]>());
        _textCleaning.Setup(x => x.Clean(It.IsAny<string>())).Returns("clean");
        _promptService.Setup(x => x.GetPromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromptTemplate("sys", "user {DOCUMENT_TEXT}", "{}", "1.0"));
        _aiProcessing.Setup(x => x.DetectPictogramsAsync(It.IsAny<PromptTemplate>(), It.IsAny<List<byte[]>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GhsPictogram>());

        _aiProcessing.Setup(x => x.ProcessAsync(It.IsAny<PromptTemplate>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SdsAiResult(new SdsExtractionResult(), 10, 20, 30, 0.01m, 100));
        _aiProcessing.Setup(x => x.ProcessMultimodalAsync(It.IsAny<PromptTemplate>(), It.IsAny<string>(), It.IsAny<List<byte[]>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SdsAiResult(new SdsExtractionResult(), 10, 20, 30, 0.01m, 100));

        var response = await service.AnalyzeBatchAsync(files);
        
        Assert.NotNull(response);
        Assert.Equal(3, response.Total);
        Assert.Equal(3, response.Successful);
        Assert.Equal(0, response.Failed);
        _aiProcessing.Verify(x => x.ProcessMultimodalAsync(It.IsAny<PromptTemplate>(), It.IsAny<string>(), It.IsAny<List<byte[]>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _aiProcessing.Verify(x => x.ProcessAsync(It.IsAny<PromptTemplate>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
