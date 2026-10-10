using Adm.AI.Application.Configuration;
using Adm.AI.Application.Exceptions;
using Adm.AI.Application.Services;
using Microsoft.Extensions.Options;

namespace Adm.AI.UnitTests.Services;

public class FileValidationServiceTests
{
    private readonly FileValidationService _sut;
    private readonly FileUploadSettings _settings;
    
    public FileValidationServiceTests()
    {
        _settings = new FileUploadSettings
        {
            MaxFileSizeBytes = 1024 * 1024, // 1MB
            AllowedExtensions = [".pdf"],
            AllowedContentTypes = ["application/pdf"]
        };
        var options = Options.Create(_settings);
        _sut = new FileValidationService(options);
    }

    [Fact]
    public void Validate_ValidPdf_DoesNotThrow()
    {
        // Arrange
        var pdfBytes = new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-', 0x31, 0x30 };
        
        // Act & Assert (Should not throw)
        _sut.Validate("test.pdf", pdfBytes.Length, "application/pdf", pdfBytes);
    }

    [Fact]
    public void Validate_EmptyFile_ThrowsFileValidationException()
    {
        // Arrange
        var emptyBytes = Array.Empty<byte>();

        // Act & Assert
        var ex = Assert.Throws<FileValidationException>(() => 
            _sut.Validate("test.pdf", 0, "application/pdf", emptyBytes));
        Assert.Equal("FILE_EMPTY", ex.ErrorCode);
    }

    [Fact]
    public void Validate_OversizedFile_ThrowsFileValidationException()
    {
        // Arrange
        var pdfBytes = new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-' };
        var oversizedSize = _settings.MaxFileSizeBytes + 1;

        // Act & Assert
        var ex = Assert.Throws<FileValidationException>(() => 
            _sut.Validate("test.pdf", oversizedSize, "application/pdf", pdfBytes));
        Assert.Equal("FILE_TOO_LARGE", ex.ErrorCode);
    }

    [Fact]
    public void Validate_InvalidExtension_ThrowsFileValidationException()
    {
        // Arrange
        var pdfBytes = new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-' };

        // Act & Assert
        var ex = Assert.Throws<FileValidationException>(() => 
            _sut.Validate("test.txt", pdfBytes.Length, "application/pdf", pdfBytes));
        Assert.Equal("INVALID_FILE_TYPE", ex.ErrorCode);
    }

    [Fact]
    public void Validate_InvalidContentType_ThrowsFileValidationException()
    {
        // Arrange
        var pdfBytes = new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-' };

        // Act & Assert
        var ex = Assert.Throws<FileValidationException>(() => 
            _sut.Validate("test.pdf", pdfBytes.Length, "text/plain", pdfBytes));
        Assert.Equal("INVALID_FILE_TYPE", ex.ErrorCode);
    }

    [Fact]
    public void Validate_InvalidSignature_ThrowsFileValidationException()
    {
        // Arrange
        var invalidBytes = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 };

        // Act & Assert
        var ex = Assert.Throws<FileValidationException>(() => 
            _sut.Validate("test.pdf", invalidBytes.Length, "application/pdf", invalidBytes));
        Assert.Equal("UNREADABLE_PDF", ex.ErrorCode);
    }
}
