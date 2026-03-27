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
[Route("")]
[Authorize]
public class FiguresController(AppDbContext db, IStorageService storage, IBackgroundJobClient jobClient) : ControllerBase
{
    // GET /modules/{moduleId}/figures
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
                $"/figures/{f.Id}/thumbnail",
                f.PageNumber,
                f.Keep,
                f.LabelType,
                f.Caption))
            .ToListAsync();

        return Ok(figures);
    }

    // PATCH /figures/{id}
    [HttpPatch("figures/{id:guid}")]
    public async Task<IActionResult> ToggleFigure(Guid id, [FromBody] ToggleFigureRequest request)
    {
        var figure = await db.Figures.FindAsync(id);
        if (figure is null) return NotFound();

        figure.Keep = request.Keep;
        await db.SaveChangesAsync();

        return Ok(new FigureDto(
            figure.Id,
            $"/figures/{figure.Id}/thumbnail",
            figure.PageNumber,
            figure.Keep,
            figure.LabelType,
            figure.Caption));
    }

    // GET /figures/{id}/thumbnail
    [HttpGet("figures/{id:guid}/thumbnail")]
    public async Task<IActionResult> GetFigureThumbnail(Guid id)
    {
        var figure = await db.Figures.FindAsync(id);
        if (figure is null) return NotFound();

        if (figure.S3Key.StartsWith("stub/"))
        {
            var svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='200' height='150'><rect width='200' height='150' fill='#e2e8f0'/><text x='50%' y='50%' dominant-baseline='middle' text-anchor='middle' font-size='14' fill='#64748b'>Figure {id.ToString()[..4]}</text></svg>";
            return Content(svg, "image/svg+xml");
        }

        try
        {
            var stream = await storage.DownloadAsync(figure.S3Key);
            return File(stream, "image/png");
        }
        catch
        {
            var svg = "<svg xmlns='http://www.w3.org/2000/svg' width='200' height='150'><rect width='200' height='150' fill='#e2e8f0'/><text x='50%' y='50%' dominant-baseline='middle' text-anchor='middle' font-size='14' fill='#64748b'>Preview unavailable</text></svg>";
            return Content(svg, "image/svg+xml");
        }
    }

    // POST /modules/{moduleId}/extract
    [HttpPost("modules/{moduleId:guid}/extract")]
    public async Task<IActionResult> TriggerExtraction(Guid moduleId)
    {
        var module = await db.Modules.FindAsync(moduleId);
        if (module is null) return NotFound();

        // Block if a run is already in progress
        var hasActiveRun = await db.ExtractionRuns.AnyAsync(r =>
            r.ModuleId == moduleId &&
            (r.Status == ExtractionStatus.Queued || r.Status == ExtractionStatus.Processing));

        if (hasActiveRun)
            return Conflict(new ProblemDetails
            {
                Title = "Extraction already in progress.",
                Detail = "Wait for the current run to complete before starting a new one."
            });

        var run = new ExtractionRun { ModuleId = moduleId };
        db.ExtractionRuns.Add(run);
        await db.SaveChangesAsync();

        jobClient.Enqueue<LectureExtractionJob>(j => j.Execute(moduleId, run.Id, default));

        return Accepted();
    }

    // GET /modules/{moduleId}/docx/download
    [HttpGet("modules/{moduleId:guid}/docx/download")]
    public async Task<IActionResult> DownloadDocx(Guid moduleId)
    {
        var latestRun = await db.ExtractionRuns
            .Where(r => r.ModuleId == moduleId && r.Status == ExtractionStatus.Ready)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (latestRun is null || string.IsNullOrEmpty(latestRun.DocxS3Key))
            return NotFound();

        var stream = await storage.DownloadAsync(latestRun.DocxS3Key);
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
