using System.Text.Json;
using System.Text.Json.Serialization;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StudyApp.Api.Data;
using StudyApp.Api.Models;
using StudyApp.Api.Providers;
using StudyApp.Api.Services;
using StudyApp.Api.Skills;

namespace StudyApp.Api.Jobs;

[AutomaticRetry(Attempts = 2)]
public class FigureExtractionJob(
    AppDbContext db,
    ISkillRunner skillRunner,
    IStorageService storage,
    IVisionProvider visionProvider,
    IConfiguration config)
{
    public async Task Execute(Guid documentId, CancellationToken ct = default)
    {
        var document = await db.Documents
            .Include(d => d.Module)
            .FirstOrDefaultAsync(d => d.Id == documentId, ct)
            ?? throw new InvalidOperationException($"Document {documentId} not found");

        // Download source file from S3 into a temp file (Python script needs a real file path)
        var downloadStream = await storage.DownloadAsync(document.S3Key, ct);
        var ext = Path.GetExtension(document.FileName).ToLowerInvariant();
        var tmpPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{ext}");

        try
        {
            using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write))
            {
                await downloadStream.CopyToAsync(fs, ct);
            }

            // Determine file_type for the Python script
            var fileType = ext == ".pdf" ? "pdf" : "pptx";

            // Build input JSON
            var inputObj = new
            {
                document_id = documentId.ToString(),
                file_path = tmpPath,
                file_type = fileType
            };
            var inputJson = JsonSerializer.Serialize(inputObj);

            // Determine script path from configuration
            var basePath = config["Skills:BasePath"] ?? "src/skills";
            var scriptPath = Path.Combine(basePath, "lecture-extractor-extracted", "extract_images.py");

            // Call the skill
            var manifestJson = await skillRunner.RunAsync(scriptPath, inputJson, ct);

            // Deserialize the manifest
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var manifest = JsonSerializer.Deserialize<FigureManifest>(manifestJson, options)
                ?? throw new InvalidOperationException("extract_images.py returned null or invalid JSON");

            // Create Figure entities
            var figures = manifest.Figures.Select(entry => new Figure
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                S3Key = entry.S3Key,
                Keep = entry.HasCaption,
                PageNumber = entry.Page,
                LabelType = entry.LabelType,
                ManifestMetadataJson = JsonSerializer.Serialize(entry)
            }).ToList();

            db.Figures.AddRange(figures);
            await db.SaveChangesAsync(ct);

            // For kept figures, populate Caption via IVisionProvider
            // Skip stub keys (stub/ prefix) — they don't exist in S3
            foreach (var figure in figures.Where(f => f.Keep && !f.S3Key.StartsWith("stub/")))
            {
                try
                {
                    var imageStream = await storage.DownloadAsync(figure.S3Key, ct);
                    using var ms = new MemoryStream();
                    await imageStream.CopyToAsync(ms, ct);
                    var imageBytes = ms.ToArray();

                    figure.Caption = await visionProvider.ExtractTextAsync(imageBytes, "image/png");
                }
                catch (Exception)
                {
                    // Caption is optional — skip if image unavailable
                }
            }

            if (figures.Any(f => f.Keep))
            {
                await db.SaveChangesAsync(ct);
            }
        }
        finally
        {
            if (File.Exists(tmpPath))
                File.Delete(tmpPath);
        }
    }
}

// Internal manifest DTOs for JSON deserialization
internal record FigureManifest(
    [property: JsonPropertyName("figures")] List<FigureEntry> Figures);

internal record FigureEntry(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("s3_key")] string S3Key,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("has_caption")] bool HasCaption,
    [property: JsonPropertyName("label_type")] string? LabelType);
