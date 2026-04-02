using Hangfire;
using StudyApp.Api.Data;

namespace StudyApp.Api.Jobs;

/// <summary>
/// Generates study content (StudyGuide, Flashcards, QuizQuestions, ConceptMap) for a single section.
/// Implementation in plan 04-03. This stub allows ContentGenerationJob to compile in plan 04-04.
/// </summary>
[AutomaticRetry(Attempts = 2)]
public class SectionGenerationJob(AppDbContext db)
{
    public async Task Execute(Guid sectionId, Guid runId, CancellationToken ct = default)
    {
        // Implementation in plan 04-03 (SectionGenerationJob)
        await Task.CompletedTask;
    }
}
