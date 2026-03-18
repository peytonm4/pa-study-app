namespace StudyApp.Api.Models;

public class Document
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public Module Module { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string S3Key { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploading;
    public int PendingVisionJobs { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Chunk> Chunks { get; set; } = [];
}
