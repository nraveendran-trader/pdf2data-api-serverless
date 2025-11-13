using System.Text;
using pdf2data.Providers;
using SautinSoft;
using UglyToad.PdfPig;

namespace pdf2data.Services;
public class PdfPigParsingService : IPdfParsingService
{    
    public async Task<string> ConvertPdfToText(byte[] pdfBytes)
    {
        var stringBuilder = new StringBuilder();
        using (var doc = PdfDocument.Open(pdfBytes))
        {
            doc.GetPages().ToList().ForEach(page =>
            {
                stringBuilder.AppendLine(page.Text);
            });
        }
        return stringBuilder.ToString();
    }
}