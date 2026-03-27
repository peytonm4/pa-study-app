using System.Text.Json;
using System.Text.Json.Serialization;
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
    public async Task Execute(Guid moduleId, Guid runId, CancellationToken ct = default)
    {
        var run = await db.ExtractionRuns.FindAsync([runId], ct)
            ?? throw new InvalidOperationException($"ExtractionRun {runId} not found");

        try
        {
            // Step 1: Mark as Processing
            run.Status = ExtractionStatus.Processing;
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

            // Step 8: Replace existing sections for this module
            var existing = await db.Sections.Where(s => s.ModuleId == moduleId).ToListAsync(ct);
            db.Sections.RemoveRange(existing);

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

            db.Sections.AddRange(sections);
            await db.SaveChangesAsync(ct);

            // Step 9: Build .docx in memory
            using var ms = BuildDocx(sections);

            // Step 10: Upload .docx to S3
            ms.Position = 0;
            var s3Key = $"modules/{moduleId}/runs/{runId}/lecture.docx";
            await storage.UploadAsync(ms, s3Key,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ct);

            // Step 11: Update run status
            run.DocxS3Key = s3Key;
            run.Status = ExtractionStatus.Ready;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            run.Status = ExtractionStatus.Failed;
            run.ErrorMessage = ex.Message;
            await db.SaveChangesAsync(ct);
            throw;
        }
    }

    private static MemoryStream BuildDocx(List<Section> sections)
    {
        var ms = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteZipEntry(zip, "[Content_Types].xml",
                """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
                """<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">""" +
                """<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>""" +
                """<Default Extension="xml" ContentType="application/xml"/>""" +
                """<Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>""" +
                """<Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>""" +
                """</Types>""");

            WriteZipEntry(zip, "_rels/.rels",
                """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
                """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""" +
                """<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>""" +
                """</Relationships>""");

            WriteZipEntry(zip, "word/_rels/document.xml.rels",
                """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
                """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""" +
                """<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>""" +
                """</Relationships>""");

            WriteZipEntry(zip, "word/styles.xml",
                """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
                """<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">""" +
                """<w:docDefaults><w:rPrDefault><w:rPr>""" +
                """<w:rFonts w:ascii="Calibri" w:hAnsi="Calibri"/>""" +
                """<w:sz w:val="24"/><w:szCs w:val="24"/>""" +
                """</w:rPr></w:rPrDefault></w:docDefaults>""" +
                """<w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/></w:style>""" +
                """<w:style w:type="paragraph" w:styleId="Heading1"><w:name w:val="heading 1"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:pPr><w:outlineLvl w:val="0"/></w:pPr><w:rPr><w:b/><w:sz w:val="32"/></w:rPr></w:style>""" +
                """<w:style w:type="paragraph" w:styleId="Heading2"><w:name w:val="heading 2"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:pPr><w:outlineLvl w:val="1"/></w:pPr><w:rPr><w:b/><w:sz w:val="28"/></w:rPr></w:style>""" +
                """<w:style w:type="paragraph" w:styleId="Heading3"><w:name w:val="heading 3"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:pPr><w:outlineLvl w:val="2"/></w:pPr><w:rPr><w:b/><w:sz w:val="24"/></w:rPr></w:style>""" +
                """</w:styles>""");

            var bodyXml = new System.Text.StringBuilder();
            foreach (var section in sections)
            {
                var headingId = section.HeadingLevel switch { 1 => "Heading1", 2 => "Heading2", _ => "Heading3" };
                var heading = System.Security.SecurityElement.Escape(section.HeadingText);
                bodyXml.Append($"""<w:p><w:pPr><w:pStyle w:val="{headingId}"/></w:pPr><w:r><w:t>{heading}</w:t></w:r></w:p>""");
                if (!string.IsNullOrWhiteSpace(section.Content))
                {
                    var content = System.Security.SecurityElement.Escape(section.Content);
                    bodyXml.Append($"""<w:p><w:r><w:t xml:space="preserve">{content}</w:t></w:r></w:p>""");
                }
            }

            WriteZipEntry(zip, "word/document.xml",
                """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
                """<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">""" +
                $"<w:body>{bodyXml}</w:body>" +
                """</w:document>""");
        }

        ms.Position = 0;
        return ms;
    }

    private static void WriteZipEntry(System.IO.Compression.ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, System.IO.Compression.CompressionLevel.Optimal);
        using var writer = new System.IO.StreamWriter(entry.Open(), System.Text.Encoding.UTF8);
        writer.Write(content);
    }
}

internal record LectureResult(
    [property: JsonPropertyName("sections")] List<SectionDto> Sections,
    [property: JsonPropertyName("docx_filename")] string? DocxFilename = null);

internal record SectionDto(
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("heading")] string Heading,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("pages")] List<int> Pages,
    [property: JsonPropertyName("figures")] List<string> Figures);
