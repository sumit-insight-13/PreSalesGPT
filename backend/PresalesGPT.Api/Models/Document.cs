namespace PresalesGPT.Api.Models;

public class Document
{
    public int Id { get; set; }

    public int OpportunityId { get; set; }

    public string FileName { get; set; } = "";

    public string ContentType { get; set; } = "";

    public long FileSizeBytes { get; set; }

    public string StoredPath { get; set; } = "";

    public string ExtractedText { get; set; } = "";

    public int PageCount { get; set; }

    public string Status { get; set; } = "Uploaded";

    public string? ErrorMessage { get; set; }

    public DateTime UploadedOn { get; set; }
}
