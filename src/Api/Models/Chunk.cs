namespace StudyApp.Api.Models;

public class Chunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsVisionExtracted { get; set; } = false;
}
