namespace StudyApp.Api.Models;

public class ExtractionRun
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public Module Module { get; set; } = null!;
    public ExtractionStatus Status { get; set; } = ExtractionStatus.Queued;
    public string? DocxS3Key { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
