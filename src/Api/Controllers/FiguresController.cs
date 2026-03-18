using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyApp.Api.Data;
using StudyApp.Api.Services;

namespace StudyApp.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class FiguresController(AppDbContext db, IStorageService storage) : ControllerBase
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
}

public record FigureDto(
    Guid Id,
    string S3ThumbnailUrl,
    int PageNumber,
    bool Keep,
    string? LabelType,
    string? Caption);

public record ToggleFigureRequest(bool Keep);
