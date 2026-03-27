using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StudyApp.Api.Data;
using StudyApp.Api.Jobs;
using StudyApp.Api.Models;
using StudyApp.Api.Skills;

namespace StudyApp.Api.Tests.Extraction;

/// <summary>
/// Stub ISkillRunner that returns the canonical sections JSON for lecture_extractor paths.
/// </summary>
public class LectureExtractionStubSkillRunner : ISkillRunner
{
    public List<(string ScriptPath, string InputJson)> Calls { get; } = [];

    public Task<string> RunAsync(string scriptPath, string inputJson, CancellationToken ct = default)
    {
        Calls.Add((scriptPath, inputJson));
        return Task.FromResult("""
            {"sections":[{"level":1,"heading":"Overview","content":"Stub overview content for testing purposes.","pages":[1,2],"figures":[]},{"level":2,"heading":"Key Concepts","content":"Stub key concepts content.","pages":[3],"figures":["stub-fig-1"]}],"docx_filename":"stub_lecture.docx"}
            """);
    }
}

/// <summary>
/// Stub IStorageService that tracks Upload calls without hitting MinIO.
/// </summary>
public class LectureExtractionStubStorage : StudyApp.Api.Services.IStorageService
{
    public List<(string Key, string ContentType)> UploadCalls { get; } = [];

    public Task<string> UploadAsync(Stream stream, string key, string contentType, CancellationToken ct = default)
    {
        UploadCalls.Add((key, contentType));
        return Task.FromResult(key);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default) => Task.CompletedTask;

    public Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
        => Task.FromResult<Stream>(new MemoryStream());
}

public class LectureExtractionJobTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"LectureJobTests-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static IConfiguration CreateConfig()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Skills:BasePath"] = "/skills"
            })
            .Build();

    private static (Guid moduleId, Guid runId) SeedModuleAndRun(AppDbContext db)
    {
        var moduleId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        db.Modules.Add(new Module { Id = moduleId, UserId = Guid.NewGuid(), Name = "Test Module" });
        db.ExtractionRuns.Add(new ExtractionRun { Id = runId, ModuleId = moduleId, Status = ExtractionStatus.Queued });
        db.SaveChanges();
        return (moduleId, runId);
    }

    [Fact]
    public async Task Execute_ParsesSectionsJson_InsertsSectionRows()
    {
        using var db = CreateInMemoryDb();
        var (moduleId, runId) = SeedModuleAndRun(db);

        var job = new LectureExtractionJob(db, new LectureExtractionStubSkillRunner(), new LectureExtractionStubStorage(), CreateConfig());
        await job.Execute(moduleId, runId);

        Assert.Equal(2, db.Sections.Count());

        var sections = db.Sections.OrderBy(s => s.SortOrder).ToList();
        Assert.Equal(1, sections[0].HeadingLevel);
        Assert.Equal("Overview", sections[0].HeadingText);
        Assert.Equal(0, sections[0].SortOrder);
        Assert.Equal(2, sections[1].HeadingLevel);
        Assert.Equal("Key Concepts", sections[1].HeadingText);
        Assert.Equal(1, sections[1].SortOrder);
    }

    [Fact]
    public async Task Execute_BuildsDocx_UploadsToS3()
    {
        using var db = CreateInMemoryDb();
        var (moduleId, runId) = SeedModuleAndRun(db);

        var storage = new LectureExtractionStubStorage();
        var job = new LectureExtractionJob(db, new LectureExtractionStubSkillRunner(), storage, CreateConfig());
        await job.Execute(moduleId, runId);

        var run = await db.ExtractionRuns.FindAsync(runId);
        Assert.NotNull(run);
        Assert.Equal(ExtractionStatus.Ready, run.Status);
        Assert.False(string.IsNullOrEmpty(run.DocxS3Key));

        Assert.Single(storage.UploadCalls);
        Assert.Contains("lecture.docx", storage.UploadCalls[0].Key);
    }

    [Fact]
    public async Task Execute_OnException_SetsFailedStatus()
    {
        using var db = CreateInMemoryDb();
        var (moduleId, runId) = SeedModuleAndRun(db);

        var job = new LectureExtractionJob(db, new FailingSkillRunner(), new LectureExtractionStubStorage(), CreateConfig());
        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute(moduleId, runId));

        var run = await db.ExtractionRuns.FindAsync(runId);
        Assert.NotNull(run);
        Assert.Equal(ExtractionStatus.Failed, run.Status);
        Assert.False(string.IsNullOrEmpty(run.ErrorMessage));
    }
}

public class FailingSkillRunner : ISkillRunner
{
    public Task<string> RunAsync(string scriptPath, string inputJson, CancellationToken ct = default)
        => throw new InvalidOperationException("Skill runner failed");
}
