namespace Adm.AI.Application.Interfaces;

public interface IFileValidationService
{
    /// <summary>
    /// Validates file metadata (extension, size, content type) and PDF signature bytes.
    /// Throws FileValidationException on any validation failure.
    /// </summary>
    /// <param name="fileName">Original filename</param>
    /// <param name="fileSizeBytes">File size in bytes</param>
    /// <param name="contentType">MIME content type (may be null)</param>
    /// <param name="fileBytes">Raw file bytes (first 5 bytes checked for %PDF- signature)</param>
    void Validate(string fileName, long fileSizeBytes, string? contentType, byte[] fileBytes);
}
