namespace StudyApp.Api.Models;
public class QuizQuestion
{
    public Guid Id { get; set; }
    public Guid SectionId { get; set; }
    public Section Section { get; set; } = null!;
    public string QuestionText { get; set; } = string.Empty;
    public string ChoicesJson { get; set; } = "[]";
    public string CorrectAnswer { get; set; } = string.Empty;
    public string SourceRef { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
