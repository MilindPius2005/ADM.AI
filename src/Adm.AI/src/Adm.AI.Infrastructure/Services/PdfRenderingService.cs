using Adm.AI.Application.Interfaces;
using Docnet.Core;
using Docnet.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Runtime.InteropServices;

namespace Adm.AI.Infrastructure.Services;

public class PdfRenderingService : IPdfRenderingService
{
    public List<byte[]> RenderPagesToImages(byte[] pdfBytes, int startPage = 1, int maxPages = 2)
    {
        var images = new List<byte[]>();

        using var docReader = DocLib.Instance.GetDocReader(pdfBytes, new PageDimensions(2.0));
        var pageCount = docReader.GetPageCount();

        int endPage = Math.Min(startPage + maxPages - 1, pageCount);

        for (int i = startPage - 1; i < endPage; i++)
        {
            using var pageReader = docReader.GetPageReader(i);
            var rawBytes = pageReader.GetImage();
            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();

            using var image = Image.LoadPixelData<Bgra32>(rawBytes, width, height);
            using var ms = new MemoryStream();
            image.Save(ms, new JpegEncoder { Quality = 85 });
            images.Add(ms.ToArray());
        }

        return images;
    }
}
