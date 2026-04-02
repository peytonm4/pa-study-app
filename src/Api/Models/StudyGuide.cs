namespace StudyApp.Api.Models;
public class StudyGuide
{
    public Guid Id { get; set; }
    public Guid SectionId { get; set; }
    public Section Section { get; set; } = null!;
    public string DirectAnswer { get; set; } = string.Empty;
    public string HighYieldDetailsJson { get; set; } = "[]";
    public string KeyTablesJson { get; set; } = "[]";
    public string MustKnowNumbersJson { get; set; } = "[]";
    public string SourcesJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
