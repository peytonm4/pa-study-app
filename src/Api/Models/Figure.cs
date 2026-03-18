namespace StudyApp.Api.Models;

public class Figure
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public string S3Key { get; set; } = string.Empty;
    public bool Keep { get; set; } = false;
    public int PageNumber { get; set; }
    public string? Caption { get; set; }
    public string? LabelType { get; set; }
    public string? ManifestMetadataJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
