namespace Adm.AI.Application.DTOs.Internal;

/// <summary>
/// Returned by IPdfExtractionService. Contains raw extracted text and page count.
/// </summary>
public record PdfExtractionResult(
    string Text,
    int PageCount
);
