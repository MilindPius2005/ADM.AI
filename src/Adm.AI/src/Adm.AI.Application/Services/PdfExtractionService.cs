using System.Text;
using Adm.AI.Application.DTOs.Internal;
using Adm.AI.Application.Exceptions;
using Adm.AI.Application.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Adm.AI.Application.Services;

public class PdfExtractionService : IPdfExtractionService
{
    public PdfExtractionResult Extract(byte[] pdfBytes)
    {
        try
        {
            using var document = PdfDocument.Open(pdfBytes);
            var textBuilder = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                var text = ContentOrderTextExtractor.GetText(page);
                textBuilder.AppendLine(text);
            }

            return new PdfExtractionResult(textBuilder.ToString(), document.NumberOfPages);
        }
        catch (Exception ex)
        {
            throw new PdfExtractionException("PDF_EXTRACTION_FAILED", "Failed to extract text from the PDF document.", ex);
        }
    }
}
