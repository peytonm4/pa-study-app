using System.Security.Claims;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyApp.Api.Data;
using StudyApp.Api.Models;
using StudyApp.Api.Services;

namespace StudyApp.Api.Controllers;

[ApiController]
[Route("modules")]
[Authorize]
public class ModulesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IStorageService _storage;

    public ModulesController(AppDbContext db, IStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static string ComputeStatus(Module module)
    {
        var active = new[] { DocumentStatus.Uploading, DocumentStatus.Queued, DocumentStatus.Processing };
        return module.Documents.Any(d => active.Contains(d.Status)) ? "Processing" : "Ready";
    }

    // GET /modules
    [HttpGet]
    public async Task<IActionResult> GetModules()
    {
        var userId = CurrentUserId;
        var modules = await _db.Modules
            .Include(m => m.Documents)
            .Where(m => m.UserId == userId)
            .ToListAsync();

        return Ok(modules.Select(m => new
        {
            m.Id,
            m.Name,
            Status = ComputeStatus(m),
            m.CreatedAt
        }));
    }

    // POST /modules
    [HttpPost]
    public async Task<IActionResult> CreateModule([FromBody] CreateModuleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ProblemDetails { Title = "Name is required." });

        var module = new Module
        {
            UserId = CurrentUserId,
            Name = request.Name.Trim()
        };

        _db.Modules.Add(module);
        await _db.SaveChangesAsync();

        return Created($"/modules/{module.Id}", new
        {
            module.Id,
            module.Name,
            module.CreatedAt
        });
    }

    // GET /modules/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetModule(Guid id)
    {
        var userId = CurrentUserId;
        var module = await _db.Modules
            .Include(m => m.Documents)
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

        if (module is null) return NotFound();

        var latestRun = await _db.ExtractionRuns
            .Where(r => r.ModuleId == id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            module.Id,
            module.Name,
            Status = ComputeStatus(module),
            module.CreatedAt,
            ExtractionStatus = latestRun?.Status.ToString() ?? "NotStarted",
            Documents = module.Documents.Select(d => new
            {
                d.Id,
                d.FileName,
                Status = d.Status.ToString(),
                d.CreatedAt
            })
        });
    }

    // DELETE /modules/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteModule(Guid id)
    {
        var userId = CurrentUserId;
        var module = await _db.Modules
            .Include(m => m.Documents)
                .ThenInclude(d => d.Figures)
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

        if (module is null) return NotFound();

        foreach (var doc in module.Documents)
        {
            try { await _storage.DeleteAsync(doc.S3Key); }
            catch { }
            foreach (var figure in doc.Figures)
            {
                try { await _storage.DeleteAsync(figure.S3Key); }
                catch { }
            }
        }

        var runs = await _db.ExtractionRuns.Where(r => r.ModuleId == id).ToListAsync();
        foreach (var run in runs)
        {
            if (!string.IsNullOrEmpty(run.DocxS3Key))
            {
                try { await _storage.DeleteAsync(run.DocxS3Key); }
                catch { }
            }
        }

        _db.Modules.Remove(module);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateModuleRequest(string Name);
