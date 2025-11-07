namespace pdf2data.Services;
public interface IPdfParsingService
{
    Task<string> ConvertPdfToXmlAsync(byte[] pdfBytes);
}