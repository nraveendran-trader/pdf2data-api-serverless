namespace pdf2data.Models.Common;
public class PdfAnalysisRequest{
    public required string Prompt { get; set; }
    public required IFormFile File { get; set; }
}