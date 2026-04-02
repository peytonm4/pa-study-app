using Microsoft.EntityFrameworkCore;
using StudyApp.Api.Data;
using StudyApp.Api.Jobs;
using StudyApp.Api.Models;
using StudyApp.Worker.Providers;

namespace StudyApp.Api.Tests.Generation;

public class SectionGenerationJobTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"SectionGenJobTests-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static (Guid sectionId, Guid runId) SeedSectionAndRun(AppDbContext db, string heading, string content)
    {
        var moduleId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var runId = Guid.NewGuid();

        db.Modules.Add(new Module { Id = moduleId, UserId = Guid.NewGuid(), Name = "Test Module" });
        db.Sections.Add(new Section
        {
            Id = sectionId,
            ModuleId = moduleId,
            HeadingLevel = 1,
            HeadingText = heading,
            Content = content,
            SourcePageRefsJson = "[1, 2]",
            SortOrder = 0
        });
        db.GenerationRuns.Add(new GenerationRun
        {
            Id = runId,
            ModuleId = moduleId,
            Status = GenerationStatus.Processing
        });
        db.SaveChanges();

        return (sectionId, runId);
    }

    [Fact]
    public async Task Execute_CreatesStudyGuide_ForEverySection()
    {
        using var db = CreateInMemoryDb();
        var (sectionId, runId) = SeedSectionAndRun(db, "Vital Signs Overview", "Normal values for HR, BP.");
        var job = new SectionGenerationJob(db, new StubGenerationProvider());

        await job.Execute(sectionId, runId);

        Assert.Equal(1, db.StudyGuides.Count(sg => sg.SectionId == sectionId));
    }

    [Fact]
    public async Task Execute_CreatesFlashcards_ForEverySection()
    {
        using var db = CreateInMemoryDb();
        var (sectionId, runId) = SeedSectionAndRun(db, "Vital Signs Overview", "Normal values for HR, BP.");
        var job = new SectionGenerationJob(db, new StubGenerationProvider());

        await job.Execute(sectionId, runId);

        Assert.True(db.Flashcards.Count(f => f.SectionId == sectionId) >= 1);
    }

    [Fact]
    public async Task Execute_CreatesQuizQuestions_ForEverySection()
    {
        using var db = CreateInMemoryDb();
        var (sectionId, runId) = SeedSectionAndRun(db, "Vital Signs Overview", "Normal values for HR, BP.");
        var job = new SectionGenerationJob(db, new StubGenerationProvider());

        await job.Execute(sectionId, runId);

        Assert.True(db.QuizQuestions.Count(q => q.SectionId == sectionId) >= 1);
    }

    [Fact]
    public async Task Execute_AlgorithmicSection_CreatesConceptMap()
    {
        using var db = CreateInMemoryDb();
        var (sectionId, runId) = SeedSectionAndRun(db, "Sepsis Workup Algorithm", "If fever, then blood cultures.");
        var job = new SectionGenerationJob(db, new StubGenerationProvider());

        await job.Execute(sectionId, runId);

        Assert.Equal(1, db.ConceptMaps.Count(cm => cm.SectionId == sectionId));
    }

    [Fact]
    public async Task Execute_NonAlgorithmicSection_NoConceptMap()
    {
        using var db = CreateInMemoryDb();
        var (sectionId, runId) = SeedSectionAndRun(db, "Vital Signs Overview", "Normal values for HR, BP.");
        var job = new SectionGenerationJob(db, new StubGenerationProvider());

        await job.Execute(sectionId, runId);

        Assert.Equal(0, db.ConceptMaps.Count(cm => cm.SectionId == sectionId));
    }

    [Fact]
    public async Task Execute_SourceRefs_PresentOnGeneratedContent()
    {
        using var db = CreateInMemoryDb();
        var (sectionId, runId) = SeedSectionAndRun(db, "Vital Signs Overview", "Normal values for HR, BP.");
        var job = new SectionGenerationJob(db, new StubGenerationProvider());

        await job.Execute(sectionId, runId);

        var studyGuide = db.StudyGuides.First(sg => sg.SectionId == sectionId);
        Assert.False(string.IsNullOrWhiteSpace(studyGuide.SourcesJson));
        Assert.NotEqual("[]", studyGuide.SourcesJson);
    }
}
