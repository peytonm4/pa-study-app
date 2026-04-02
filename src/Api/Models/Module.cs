namespace StudyApp.Api.Models;

public class Module
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Document> Documents { get; set; } = [];
    public ICollection<Section> Sections { get; set; } = [];
    public ICollection<ExtractionRun> ExtractionRuns { get; set; } = [];
    public ICollection<GenerationRun> GenerationRuns { get; set; } = [];
}
