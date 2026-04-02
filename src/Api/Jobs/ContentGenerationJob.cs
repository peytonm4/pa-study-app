using Hangfire;
using Microsoft.EntityFrameworkCore;
using StudyApp.Api.Data;
using StudyApp.Api.Models;

namespace StudyApp.Api.Jobs;

[AutomaticRetry(Attempts = 1)]
public class ContentGenerationJob(AppDbContext db, IBackgroundJobClient jobClient)
{
    public async Task Execute(Guid moduleId, Guid runId, CancellationToken ct = default)
    {
        var run = await db.GenerationRuns.FindAsync([runId], ct)
            ?? throw new InvalidOperationException($"GenerationRun {runId} not found");

        try
        {
            run.Status = GenerationStatus.Processing;
            run.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            // Delete all existing generated content for this module (re-generation path)
            var sectionIds = await db.Sections
                .Where(s => s.ModuleId == moduleId)
                .Select(s => s.Id)
                .ToListAsync(ct);

            var oldGuides = await db.StudyGuides
                .Where(sg => sectionIds.Contains(sg.SectionId)).ToListAsync(ct);
            var oldCards = await db.Flashcards
                .Where(fc => sectionIds.Contains(fc.SectionId)).ToListAsync(ct);
            var oldQuestions = await db.QuizQuestions
                .Where(qq => sectionIds.Contains(qq.SectionId)).ToListAsync(ct);
            var oldMaps = await db.ConceptMaps
                .Where(cm => sectionIds.Contains(cm.SectionId)).ToListAsync(ct);

            db.StudyGuides.RemoveRange(oldGuides);
            db.Flashcards.RemoveRange(oldCards);
            db.QuizQuestions.RemoveRange(oldQuestions);
            db.ConceptMaps.RemoveRange(oldMaps);
            await db.SaveChangesAsync(ct);

            // Fan out: one SectionGenerationJob per section
            var sections = await db.Sections
                .Where(s => s.ModuleId == moduleId)
                .OrderBy(s => s.SortOrder)
                .ToListAsync(ct);

            foreach (var section in sections)
                jobClient.Enqueue<SectionGenerationJob>(j => j.Execute(section.Id, runId, CancellationToken.None));

            // Mark Ready after enqueueing — section job failures will set Failed independently
            run.Status = GenerationStatus.Ready;
            run.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            run.Status = GenerationStatus.Failed;
            run.ErrorMessage = ex.Message;
            run.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            throw;
        }
    }
}
