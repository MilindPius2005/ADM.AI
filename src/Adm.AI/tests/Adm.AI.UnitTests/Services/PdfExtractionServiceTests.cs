using Adm.AI.Application.Exceptions;
using Adm.AI.Application.Services;

namespace Adm.AI.UnitTests.Services;

public class PdfExtractionServiceTests
{
    private readonly PdfExtractionService _sut;

    public PdfExtractionServiceTests()
    {
        _sut = new PdfExtractionService();
    }

    [Fact]
    public void Extract_CorruptPdfBytes_ThrowsPdfExtractionException()
    {
        // Arrange
        var corruptBytes = new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-', 0x01, 0x02, 0x03 };

        // Act & Assert
        var ex = Assert.Throws<PdfExtractionException>(() => _sut.Extract(corruptBytes));
        Assert.Equal("PDF_EXTRACTION_FAILED", ex.ErrorCode);
    }
}
