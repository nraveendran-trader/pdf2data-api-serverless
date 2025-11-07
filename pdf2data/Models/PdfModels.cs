namespace pdf2data.Models;

public class PdfExtractionRequest
{
    public IFormFile File { get; set; } = null!;
    public string? ExtractionType { get; set; }
    public bool IncludeMetadata { get; set; } = true;
    public bool IncludeImages { get; set; } = false;
}

public class PdfExtractionResponse
{
    public string JobId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ExtractedText { get; set; }
    public PdfMetadata? Metadata { get; set; }
    public List<string>? ExtractedImages { get; set; }
}

public class PdfMetadata
{
    public int PageCount { get; set; }
    public string? Author { get; set; }
    public string? Title { get; set; }
    public string? Subject { get; set; }
    public string? Creator { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

public class JobStatus
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Progress { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? CompletedTime { get; set; }
    public string? ResultUrl { get; set; }
    public string? ErrorMessage { get; set; }
}