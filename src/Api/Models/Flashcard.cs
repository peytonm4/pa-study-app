namespace StudyApp.Api.Models;
public class Flashcard
{
    public Guid Id { get; set; }
    public Guid SectionId { get; set; }
    public Section Section { get; set; } = null!;
    public string Front { get; set; } = string.Empty;
    public string Back { get; set; } = string.Empty;
    public string CardType { get; set; } = "qa";   // "cloze" | "qa"
    public string SourceRefsJson { get; set; } = "[]";
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
