namespace Adm.AI.Application.Configuration;

public class FileUploadSettings
{
    public const string SectionName = "FileUpload";

    public long MaxFileSizeBytes { get; set; } = 104_857_600; // 100 MB
    public int MaxExtractedTextLength { get; set; } = 4_000_000; // ~1M tokens
    public string[] AllowedExtensions { get; set; } = [".pdf"];
    public string[] AllowedContentTypes { get; set; } = ["application/pdf"];
}
