namespace StudyApp.Api.Models;

public class Section
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public Module Module { get; set; } = null!;
    public int HeadingLevel { get; set; }
    public string HeadingText { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? SourcePageRefsJson { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
