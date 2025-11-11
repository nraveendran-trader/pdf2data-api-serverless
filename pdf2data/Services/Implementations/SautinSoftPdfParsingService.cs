using pdf2data.Providers;
using SautinSoft;

namespace pdf2data.Services;
public class SautinSoftPdfParsingService : IPdfParsingService
{    
    public async Task<string> ConvertPdfToXmlAsync(byte[] pdfBytes)
    {
        var pdfFocus = new PdfFocus();
        pdfFocus.Serial = await ConfigProvider.GetPdfFocusKeyAsync();

        // Implementation for converting PDF to HTML using SautinSoft library
        await Task.Delay(100); // Simulate async work
        return "<html><body>Converted PDF Content</body></html>";
    }
}