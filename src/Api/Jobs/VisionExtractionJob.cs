using Hangfire;
using Microsoft.EntityFrameworkCore;
using StudyApp.Api.Data;
using StudyApp.Api.Models;
using StudyApp.Api.Providers;
using StudyApp.Api.Services;

namespace StudyApp.Api.Jobs;

public class VisionExtractionJob(
    IVisionProvider visionProvider,
    IStorageService storage,
    AppDbContext db)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task Execute(Guid documentId, int pageNumber)
    {
        // Find the chunk for this page — idempotent if not found
        var chunk = await db.Chunks
            .FirstOrDefaultAsync(c => c.DocumentId == documentId && c.PageNumber == pageNumber);
        if (chunk is null)
        {
            // Already processed or doesn't exist — idempotent
            return;
        }

        var document = await db.Documents.FindAsync(documentId);
        if (document is null) return;

        // In stub mode: pass empty bytes; StubVisionProvider ignores them
        // In real Gemini mode: download full PDF bytes and pass with PDF mime type
        byte[] pdfBytes;
        var downloadStream = await storage.DownloadAsync(document.S3Key);
        using (var ms = new MemoryStream())
        {
            await downloadStream.CopyToAsync(ms);
            pdfBytes = ms.ToArray();
        }

        var extractedText = await visionProvider.ExtractTextAsync(pdfBytes, "application/pdf");

        chunk.Content = extractedText;
        chunk.IsVisionExtracted = true;
        await db.SaveChangesAsync();

        // Atomic decrement of PendingVisionJobs to avoid race condition on concurrent vision jobs
        await db.Documents
            .Where(d => d.Id == documentId)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.PendingVisionJobs, d => d.PendingVisionJobs - 1));

        // Re-fetch to check if this was the last vision job
        var updatedDoc = await db.Documents.FindAsync(documentId);
        if (updatedDoc is not null && updatedDoc.PendingVisionJobs == 0)
        {
            updatedDoc.Status = DocumentStatus.Ready;
            await db.SaveChangesAsync();
        }
    }
}
