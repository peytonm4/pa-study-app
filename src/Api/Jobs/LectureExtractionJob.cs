using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StudyApp.Api.Data;
using StudyApp.Api.Models;
using StudyApp.Api.Services;
using StudyApp.Api.Skills;

namespace StudyApp.Api.Jobs;

[AutomaticRetry(Attempts = 1)]
public class LectureExtractionJob(
    AppDbContext db,
    ISkillRunner skillRunner,
    IStorageService storage,
    IConfiguration config)
{
    public async Task Execute(Guid moduleId, CancellationToken ct = default)
    {
        var module = await db.Modules.FindAsync([moduleId], ct)
            ?? throw new InvalidOperationException($"Module {moduleId} not found");

        try
        {
            // Step 1: Mark as Processing
            module.ExtractionStatus = ExtractionStatus.Processing;
            await db.SaveChangesAsync(ct);

            // Step 2: Fetch Keep=true Figures for this module (via Document FK)
            var figures = await db.Figures
                .Include(f => f.Document)
                .Where(f => f.Document.ModuleId == moduleId && f.Keep)
                .ToListAsync(ct);

            // Step 3: Fetch all Chunks for this module (via Document FK)
            var chunks = await db.Chunks
                .Include(c => c.Document)
                .Where(c => c.Document.ModuleId == moduleId)
                .ToListAsync(ct);

            // Step 4: Build input JSON for the skill
            var inputObj = new
            {
                module_id = moduleId.ToString(),
                chunks = chunks.Select(c => new
                {
                    id = c.Id.ToString(),
                    file_name = c.FileName,
                    page_number = c.PageNumber,
                    content = c.Content
                }),
                figures = figures.Select(f => new
                {
                    id = f.Id.ToString(),
                    s3_key = f.S3Key,
                    page_number = f.PageNumber,
                    caption = f.Caption
                })
            };
            var inputJson = JsonSerializer.Serialize(inputObj);

            // Step 5: Determine script path
            var basePath = config["Skills:BasePath"] ?? "/skills";
            var scriptPath = Path.Combine(basePath, "lecture-extractor-extracted", "lecture_extractor.py");

            // Step 6: Call skill
            var resultJson = await skillRunner.RunAsync(scriptPath, inputJson, ct);

            // Step 7: Deserialize result
            var lectureResult = JsonSerializer.Deserialize<LectureResult>(resultJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Skill returned null or invalid JSON");

            // Step 8: Map to Section entities
            var sections = lectureResult.Sections.Select((dto, index) => new Section
            {
                Id = Guid.NewGuid(),
                ModuleId = moduleId,
                HeadingLevel = dto.Level,
                HeadingText = dto.Heading,
                Content = dto.Content,
                SourcePageRefsJson = JsonSerializer.Serialize(dto.Pages),
                SortOrder = index,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            // Step 9: Bulk insert sections
            db.Sections.AddRange(sections);
            await db.SaveChangesAsync(ct);

            // Step 10: Build .docx in memory
            using var ms = new MemoryStream();
            using (var wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new Body());

                // Add StyleDefinitionsPart with Heading1/Heading2/Heading3 styles
                var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
                stylesPart.Styles = BuildHeadingStyles();
                stylesPart.Styles.Save();

                var body = mainPart.Document.Body!;

                foreach (var section in sections)
                {
                    // Heading paragraph
                    var headingStyleId = section.HeadingLevel switch
                    {
                        1 => "Heading1",
                        2 => "Heading2",
                        _ => "Heading3"
                    };

                    var headingPara = new Paragraph(
                        new ParagraphProperties(
                            new ParagraphStyleId { Val = headingStyleId }),
                        new Run(new Text(section.HeadingText)));
                    body.AppendChild(headingPara);

                    // Content paragraph
                    if (!string.IsNullOrWhiteSpace(section.Content))
                    {
                        var contentPara = new Paragraph(
                            new Run(new Text(section.Content)));
                        body.AppendChild(contentPara);
                    }
                }

                mainPart.Document.Save();
            }

            // Step 11: Upload .docx to S3
            ms.Position = 0;
            var s3Key = $"modules/{moduleId}/lecture.docx";
            await storage.UploadAsync(ms, s3Key,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ct);

            // Step 12: Update module status
            module.DocxS3Key = s3Key;
            module.ExtractionStatus = ExtractionStatus.Ready;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            module.ExtractionStatus = ExtractionStatus.Failed;
            module.ExtractionError = ex.Message;
            await db.SaveChangesAsync(ct);
            throw;
        }
    }

    private static Styles BuildHeadingStyles()
    {
        var styles = new Styles();

        styles.AppendChild(BuildHeadingStyle("Heading1", "heading 1", "1"));
        styles.AppendChild(BuildHeadingStyle("Heading2", "heading 2", "2"));
        styles.AppendChild(BuildHeadingStyle("Heading3", "heading 3", "3"));

        return styles;
    }

    private static Style BuildHeadingStyle(string styleId, string styleName, string outlineLevel)
    {
        var style = new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = styleId
        };
        style.AppendChild(new StyleName { Val = styleName });
        style.AppendChild(new BasedOn { Val = "Normal" });
        style.AppendChild(new NextParagraphStyle { Val = "Normal" });
        style.AppendChild(new ParagraphProperties(
            new OutlineLevel { Val = int.Parse(outlineLevel) - 1 }));

        return style;
    }
}

// Internal DTOs for JSON deserialization
internal record LectureResult(
    [property: JsonPropertyName("sections")] List<SectionDto> Sections,
    [property: JsonPropertyName("docx_filename")] string? DocxFilename = null);

internal record SectionDto(
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("heading")] string Heading,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("pages")] List<int> Pages,
    [property: JsonPropertyName("figures")] List<string> Figures);
