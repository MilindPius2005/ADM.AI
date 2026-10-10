namespace Adm.AI.Application.Interfaces;

using Adm.AI.Application.DTOs.Internal;

public interface IPdfExtractionService
{
    /// <summary>
    /// Extracts text from PDF bytes using PdfPig. Returns raw text + page count.
    /// Throws PdfExtractionException if PDF is corrupted or unreadable.
    /// Synchronous — PdfPig is a synchronous library.
    /// </summary>
    PdfExtractionResult Extract(byte[] pdfBytes);
}
