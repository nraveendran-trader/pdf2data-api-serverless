namespace pdf2data.Services;
public interface IPdfParsingService
{
    Task<string> ConvertPdfToText(byte[] pdfBytes);
}