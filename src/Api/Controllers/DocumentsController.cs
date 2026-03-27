using System.Security.Claims;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyApp.Api.Data;
using StudyApp.Api.Jobs;
using StudyApp.Api.Models;
using StudyApp.Api.Services;

namespace StudyApp.Api.Controllers;

[ApiController]
[Authorize]
public class DocumentsController : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation"
    ];

    private readonly AppDbContext _db;
    private readonly IStorageService _storage;
    private readonly IBackgroundJobClient _jobClient;

    public DocumentsController(AppDbContext db, IStorageService storage, IBackgroundJobClient jobClient)
    {
        _db = db;
        _storage = storage;
        _jobClient = jobClient;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // POST /modules/{moduleId}/documents
    [HttpPost("modules/{moduleId:guid}/documents")]
    public async Task<IActionResult> UploadDocument(Guid moduleId, IFormFile file)
    {
        var userId = CurrentUserId;

        var module = await _db.Modules
            .FirstOrDefaultAsync(m => m.Id == moduleId && m.UserId == userId);
        if (module is null) return NotFound();

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Unsupported file type.",
                Detail = "Only PDF and PPTX files are accepted."
            });
        }

        var documentId = Guid.NewGuid();
        var s3Key = $"uploads/{userId}/{moduleId}/{documentId}/{file.FileName}";

        var document = new Document
        {
            Id = documentId,
            ModuleId = moduleId,
            FileName = file.FileName,
            S3Key = s3Key,
            ContentType = file.ContentType,
            Status = DocumentStatus.Uploading
        };

        _db.Documents.Add(document);
        await _db.SaveChangesAsync();

        using var stream = file.OpenReadStream();
        await _storage.UploadAsync(stream, s3Key, file.ContentType);

        document.Status = DocumentStatus.Queued;
        await _db.SaveChangesAsync();

        _jobClient.Enqueue<IngestionJob>(j => j.Execute(document.Id));

        return Accepted($"/documents/{document.Id}/status", new
        {
            id = document.Id,
            status = "Queued"
        });
    }

    // GET /documents/{id}/status
    [HttpGet("documents/{id:guid}/status")]
    public async Task<IActionResult> GetDocumentStatus(Guid id)
    {
        var userId = CurrentUserId;

        var document = await _db.Documents
            .Include(d => d.Module)
            .FirstOrDefaultAsync(d => d.Id == id && d.Module.UserId == userId);

        if (document is null) return NotFound();

        return Ok(new
        {
            document.Id,
            document.FileName,
            Status = document.Status.ToString(),
            document.CreatedAt
        });
    }

    // DELETE /documents/{id}
    [HttpDelete("documents/{id:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var userId = CurrentUserId;

        var document = await _db.Documents
            .Include(d => d.Module)
            .Include(d => d.Figures)
            .FirstOrDefaultAsync(d => d.Id == id && d.Module.UserId == userId);

        if (document is null) return NotFound();

        try { await _storage.DeleteAsync(document.S3Key); }
        catch { /* log but proceed with DB deletion */ }

        foreach (var figure in document.Figures)
        {
            try { await _storage.DeleteAsync(figure.S3Key); }
            catch { }
        }

        // Cancel any active extraction runs for this module so user can re-run cleanly
        var activeRuns = await _db.ExtractionRuns
            .Where(r => r.ModuleId == document.Module.Id &&
                        (r.Status == ExtractionStatus.Queued || r.Status == ExtractionStatus.Processing))
            .ToListAsync();
        foreach (var run in activeRuns)
            run.Status = ExtractionStatus.Failed;

        _db.Documents.Remove(document);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
