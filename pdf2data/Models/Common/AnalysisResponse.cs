
namespace pdf2data.Models.Common;
public class AnalysisResponse
{
    public required string RequestId { get; set; }
    public string? Prompt { get; set; }
    public string? Response { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public string? AttachmentName { get; set; }
    public DateTime ProcessedAtUtc { get; set; }

}