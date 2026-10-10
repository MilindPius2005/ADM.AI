using Adm.AI.Application.Configuration;
using Adm.AI.Application.Exceptions;
using Adm.AI.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Adm.AI.Application.Services;

public class FileValidationService : IFileValidationService
{
    private readonly FileUploadSettings _settings;
    
    // PDF magic bytes: %PDF- (hex: 25 50 44 46 2D)
    private static readonly byte[] PdfSignature = [(byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-'];

    public FileValidationService(IOptions<FileUploadSettings> settings)
    {
        _settings = settings.Value;
    }

    public void Validate(string fileName, long fileSizeBytes, string? contentType, byte[] fileBytes)
    {
        if (fileSizeBytes == 0 || fileBytes.Length == 0)
        {
            throw new FileValidationException("FILE_EMPTY", $"File '{fileName}' is empty.");
        }

        if (fileSizeBytes > _settings.MaxFileSizeBytes)
        {
            throw new FileValidationException("FILE_TOO_LARGE", $"File '{fileName}' exceeds maximum allowed size of {_settings.MaxFileSizeBytes / 1024 / 1024} MB.");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_settings.AllowedExtensions.Contains(extension))
        {
            throw new FileValidationException("INVALID_FILE_TYPE", $"File extension '{extension}' is not allowed.");
        }

        if (contentType != null && !_settings.AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new FileValidationException("INVALID_FILE_TYPE", $"Content type '{contentType}' is not allowed.");
        }

        if (fileBytes.Length < PdfSignature.Length || !fileBytes.Take(PdfSignature.Length).SequenceEqual(PdfSignature))
        {
            throw new FileValidationException("UNREADABLE_PDF", $"File '{fileName}' does not have a valid PDF signature.");
        }
    }
}
