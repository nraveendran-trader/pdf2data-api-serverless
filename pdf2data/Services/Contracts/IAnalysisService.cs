using pdf2data.Models.Common;

namespace pdf2data.Services;

public interface IAnalysisService
{
    Task<string> AnalyzeTextAsync(string prompt);
    Task<string> AnalyzePdfAsync(MemoryStream stream, string prompt);
}