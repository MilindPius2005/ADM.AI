using Adm.AI.Application.DTOs.Internal;
using Adm.AI.Application.Exceptions;
using Adm.AI.Application.Interfaces;
using Adm.AI.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Adm.AI.UnitTests.Services;

public class SdsAiProcessingServiceTests
{
    private readonly Mock<IOpenAiProvider> _openAiMock = new();
    private readonly Mock<ILogger<SdsAiProcessingService>> _loggerMock = new();

    [Fact]
    public async Task ProcessAsync_SuccessfulCall_ReturnsDeserializedResult()
    {
        var service = new SdsAiProcessingService(_openAiMock.Object, _loggerMock.Object);
        var prompt = new PromptTemplate("system", "user {DOCUMENT_TEXT}", "{}", "1.0");

        _openAiMock.Setup(x => x.GetStructuredCompletionAsync("system", "user docText", "sds_extraction", "{}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiCompletionResult(
                "{\"chemicalName\": \"TestChem\"}", 10, 20, 30, 0.01m, 100));

        var result = await service.ProcessAsync(prompt, "docText");

        Assert.NotNull(result);
        Assert.Equal("TestChem", result.ExtractionResult.ChemicalName);
        Assert.Equal(10, result.PromptTokens);
        Assert.Equal(30, result.TotalTokens);
    }

    [Fact]
    public async Task ProcessAsync_DeserializationFails_RetriesOnceAndThrows()
    {
        var service = new SdsAiProcessingService(_openAiMock.Object, _loggerMock.Object);
        var prompt = new PromptTemplate("system", "user {DOCUMENT_TEXT}", "{}", "1.0");

        _openAiMock.Setup(x => x.GetStructuredCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiCompletionResult("invalid_json", 10, 20, 30, 0.01m, 100));

        await Assert.ThrowsAsync<AiProcessingException>(() => service.ProcessAsync(prompt, "docText"));

        _openAiMock.Verify(x => x.GetStructuredCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
