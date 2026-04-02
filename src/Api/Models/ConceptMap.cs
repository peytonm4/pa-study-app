namespace StudyApp.Api.Models;
public class ConceptMap
{
    public Guid Id { get; set; }
    public Guid SectionId { get; set; }
    public Section Section { get; set; } = null!;
    public string MermaidSyntax { get; set; } = string.Empty;
    public string SourceNodeRefsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
