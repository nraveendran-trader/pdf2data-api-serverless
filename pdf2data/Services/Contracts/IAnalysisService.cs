using pdf2data.Models.Common;

namespace pdf2data.Services;

public interface IAnalysisService
{
    Task<AnalysisResponse> AnalyzeTextAsync(string prompt);
    Task<AnalysisResponse> AnalyzePdfAsync(MemoryStream stream, string prompt, string documentName);
}