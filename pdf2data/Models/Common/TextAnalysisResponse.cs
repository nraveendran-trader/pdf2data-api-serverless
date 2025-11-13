
namespace pdf2data.Models.Common;
public class TextAnalysisResponse
{
    public required string Prompt { get; set; }
    public DateTime ProcessedAt { get; internal set; }
    public string Analysis { get; internal set; }
    public string Status { get; internal set; }
}