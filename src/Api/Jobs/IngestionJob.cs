using Hangfire;
using Microsoft.EntityFrameworkCore;
using StudyApp.Api.Data;
using StudyApp.Api.Extraction;
using StudyApp.Api.Models;
using StudyApp.Api.Services;

namespace StudyApp.Api.Jobs;

public class IngestionJob(
    IBackgroundJobClient jobClient,
    IPptxExtractor pptxExtractor,
    IPdfExtractor pdfExtractor,
    IStorageService storage,
    AppDbContext db)
{
    private const string PptxContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
    private const string PdfContentType = "application/pdf";

    [AutomaticRetry(Attempts = 3)]
    public async Task Execute(Guid documentId)
    {
        var document = await db.Documents.FindAsync(documentId)
            ?? throw new InvalidOperationException($"Document {documentId} not found");

        document.Status = DocumentStatus.Processing;
        await db.SaveChangesAsync();

        // Download file from S3
        var downloadStream = await storage.DownloadAsync(document.S3Key);

        // Copy to MemoryStream to avoid disposal issues with OpenXml/PdfPig
        var memStream = new MemoryStream();
        await downloadStream.CopyToAsync(memStream);
        memStream.Position = 0;

        var chunks = new List<Chunk>();
        var flaggedPages = new List<Chunk>();

        if (document.ContentType == PptxContentType)
        {
            var slides = pptxExtractor.Extract(memStream, document.FileName);
            foreach (var slide in slides)
            {
                var chunk = new Chunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,
                    FileName = slide.FileName,
                    PageNumber = slide.SlideNumber,
                    Content = slide.BodyText + (string.IsNullOrEmpty(slide.NotesText) ? "" : "\n" + slide.NotesText),
                    IsVisionExtracted = false
                };
                chunks.Add(chunk);
            }
            // No vision jobs for PPTX — all chunks have text content
        }
        else if (document.ContentType == PdfContentType)
        {
            var pages = pdfExtractor.Extract(memStream, document.FileName);
            foreach (var page in pages)
            {
                var chunk = new Chunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,
                    FileName = page.FileName,
                    PageNumber = page.PageNumber,
                    Content = page.Text,
                    IsVisionExtracted = !page.NeedsVision
                };
                chunks.Add(chunk);

                if (page.NeedsVision)
                {
                    flaggedPages.Add(chunk);
                }
            }
        }
        else
        {
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = "Unsupported file type";
            await db.SaveChangesAsync();
            return;
        }

        db.Chunks.AddRange(chunks);
        await db.SaveChangesAsync();

        document.PendingVisionJobs = flaggedPages.Count;
        document.Status = flaggedPages.Count > 0 ? DocumentStatus.Processing : DocumentStatus.Ready;
        await db.SaveChangesAsync();

        foreach (var page in flaggedPages)
        {
            jobClient.Enqueue<VisionExtractionJob>(j => j.Execute(documentId, page.PageNumber));
        }

        // Enqueue figure extraction when document is already Ready (no vision jobs needed)
        if (flaggedPages.Count == 0)
        {
            jobClient.Enqueue<FigureExtractionJob>(j => j.Execute(documentId, CancellationToken.None));
        }
    }
}
