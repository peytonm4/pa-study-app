using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StudyApp.Api.Data;
using StudyApp.Api.Jobs;
using StudyApp.Api.Models;
using StudyApp.Api.Providers;
using StudyApp.Api.Services;
using StudyApp.Api.Skills;

namespace StudyApp.Api.Tests.Figures;

// Stub ISkillRunner that returns a deterministic figure manifest with 2 entries:
// one with has_caption=true (stub-fig-1) and one with has_caption=false (stub-fig-2)
public class StubFigureSkillRunner : ISkillRunner
{
    public Task<string> RunAsync(string scriptPath, string inputJson, CancellationToken ct = default)
        => Task.FromResult("""
            {"figures":[
              {"id":"stub-fig-1","s3_key":"stub/fig1.png","page":1,"has_caption":true,"label_type":"Figure"},
              {"id":"stub-fig-2","s3_key":"stub/fig2.png","page":3,"has_caption":false,"label_type":null}
            ]}
            """);
}

// Stub IVisionProvider that returns a fixed caption
public class StubCaptionVisionProvider : IVisionProvider
{
    public Task<string> ExtractTextAsync(byte[] imageBytes, string mimeType)
        => Task.FromResult("Stub caption");
}

public class FigureExtractionJobTests
{
    private AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"FigureTestDb-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(opts);
    }

    private IConfiguration CreateConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Skills:BasePath"] = "src/skills"
            })
            .Build();
    }

    private (AppDbContext db, Module module, Document document) SeedDb(AppDbContext db)
    {
        var module = new Module { Id = Guid.NewGuid(), Name = "Test Module", UserId = Guid.NewGuid() };
        var document = new Document
        {
            Id = Guid.NewGuid(),
            ModuleId = module.Id,
            Module = module,
            FileName = "test.pptx",
            S3Key = "uploads/test/test.pptx",
            ContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            Status = DocumentStatus.Ready
        };
        db.Modules.Add(module);
        db.Documents.Add(document);
        db.SaveChanges();
        return (db, module, document);
    }

    [Fact]
    public async Task Execute_ParsesManifestJson_InsertsFigureRows()
    {
        using var db = CreateDb();
        var (_, _, document) = SeedDb(db);

        var job = new FigureExtractionJob(
            db,
            new StubFigureSkillRunner(),
            new StubStorageServiceFigure(),
            new StubCaptionVisionProvider(),
            CreateConfig());

        await job.Execute(document.Id);

        Assert.Equal(2, db.Figures.Count());
    }

    [Fact]
    public async Task Execute_HasCaptionTrue_SetsFigureKeepTrue()
    {
        using var db = CreateDb();
        var (_, _, document) = SeedDb(db);

        var job = new FigureExtractionJob(
            db,
            new StubFigureSkillRunner(),
            new StubStorageServiceFigure(),
            new StubCaptionVisionProvider(),
            CreateConfig());

        await job.Execute(document.Id);

        var figures = db.Figures.ToList();
        var figWithCaption = figures.First(f => f.S3Key == "stub/fig1.png");
        var figWithoutCaption = figures.First(f => f.S3Key == "stub/fig2.png");

        Assert.True(figWithCaption.Keep);
        Assert.False(figWithoutCaption.Keep);
    }

    [Fact]
    public async Task Execute_KeepTrue_PopulatesCaption()
    {
        using var db = CreateDb();
        var (_, _, document) = SeedDb(db);

        var job = new FigureExtractionJob(
            db,
            new StubFigureSkillRunner(),
            new StubStorageServiceFigure(),
            new StubCaptionVisionProvider(),
            CreateConfig());

        await job.Execute(document.Id);

        var keptFigure = db.Figures.First(f => f.Keep);
        Assert.NotNull(keptFigure.Caption);
        Assert.NotEmpty(keptFigure.Caption);
    }
}

// Stub storage that returns empty stream for downloads
public class StubStorageServiceFigure : IStorageService
{
    public Task<string> UploadAsync(Stream stream, string key, string contentType, CancellationToken ct = default)
        => Task.FromResult(key);

    public Task DeleteAsync(string key, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
        => Task.FromResult<Stream>(new MemoryStream(new byte[] { 1, 2, 3 }));
}
