namespace StudyApp.Api.Models;
public class GenerationRun
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public Module Module { get; set; } = null!;
    public GenerationStatus Status { get; set; } = GenerationStatus.Queued;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
