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
[Route("api")]
[Authorize]
public class FiguresController(AppDbContext db, IStorageService storage, IBackgroundJobClient jobClient) : ControllerBase
{
    // GET /api/modules/{moduleId}/figures
    [HttpGet("modules/{moduleId:guid}/figures")]
    public async Task<IActionResult> GetModuleFigures(Guid moduleId)
    {
        var moduleExists = await db.Modules.AnyAsync(m => m.Id == moduleId);
        if (!moduleExists) return NotFound();

        var figures = await db.Figures
            .Include(f => f.Document)
            .Where(f => f.Document.ModuleId == moduleId)
            .Select(f => new FigureDto(
                f.Id,
                $"/api/figures/{f.Id}/thumbnail",
                f.PageNumber,
                f.Keep,
                f.LabelType,
                f.Caption))
            .ToListAsync();

        return Ok(figures);
    }

    // PATCH /api/figures/{id}
    [HttpPatch("figures/{id:guid}")]
    public async Task<IActionResult> ToggleFigure(Guid id, [FromBody] ToggleFigureRequest request)
    {
        var figure = await db.Figures.FindAsync(id);
        if (figure is null) return NotFound();

        figure.Keep = request.Keep;
        await db.SaveChangesAsync();

        return Ok(new FigureDto(
            figure.Id,
            $"/api/figures/{figure.Id}/thumbnail",
            figure.PageNumber,
            figure.Keep,
            figure.LabelType,
            figure.Caption));
    }

    // GET /api/figures/{id}/thumbnail
    [HttpGet("figures/{id:guid}/thumbnail")]
    public async Task<IActionResult> GetFigureThumbnail(Guid id)
    {
        var figure = await db.Figures.FindAsync(id);
        if (figure is null) return NotFound();

        var stream = await storage.DownloadAsync(figure.S3Key);
        return File(stream, "image/png");
    }

    // POST /api/modules/{moduleId}/extract
    [HttpPost("modules/{moduleId:guid}/extract")]
    public async Task<IActionResult> TriggerExtraction(Guid moduleId)
    {
        var module = await db.Modules.FindAsync(moduleId);
        if (module is null) return NotFound();

        // Prevent double-trigger — only allow if NotStarted or Failed
        if (module.ExtractionStatus is not ExtractionStatus.NotStarted and not ExtractionStatus.Failed)
            return Conflict(new ProblemDetails
            {
                Title = "Extraction already in progress or complete.",
                Detail = $"Module extraction status is {module.ExtractionStatus}."
            });

        module.ExtractionStatus = ExtractionStatus.Queued;
        await db.SaveChangesAsync();

        jobClient.Enqueue<LectureExtractionJob>(j => j.Execute(moduleId, default));

        return Accepted();
    }

    // GET /api/modules/{moduleId}/docx
    [HttpGet("modules/{moduleId:guid}/docx")]
    public async Task<IActionResult> GetDocxUrl(Guid moduleId)
    {
        var module = await db.Modules.FindAsync(moduleId);
        if (module is null) return NotFound();

        if (module.ExtractionStatus != ExtractionStatus.Ready)
            return Conflict(new ProblemDetails
            {
                Title = "Docx not ready.",
                Detail = $"Module extraction status is {module.ExtractionStatus}."
            });

        return Ok(new { url = $"/api/modules/{moduleId}/docx/download" });
    }

    // GET /api/modules/{moduleId}/docx/download
    [HttpGet("modules/{moduleId:guid}/docx/download")]
    public async Task<IActionResult> DownloadDocx(Guid moduleId)
    {
        var module = await db.Modules.FindAsync(moduleId);
        if (module is null || string.IsNullOrEmpty(module.DocxS3Key)) return NotFound();

        var stream = await storage.DownloadAsync(module.DocxS3Key);
        return File(stream,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "lecture.docx");
    }
}

public record FigureDto(
    Guid Id,
    string S3ThumbnailUrl,
    int PageNumber,
    bool Keep,
    string? LabelType,
    string? Caption);

public record ToggleFigureRequest(bool Keep);
