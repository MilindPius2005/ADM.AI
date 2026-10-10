namespace Adm.AI.Application.Interfaces;

public interface IPdfRenderingService
{
    /// <summary>
    /// Renders the specified pages of a PDF to JPEG images.
    /// Returns a list of byte arrays representing the JPEGs.
    /// </summary>
    List<byte[]> RenderPagesToImages(byte[] pdfBytes, int startPage = 1, int maxPages = 2);
}
