using System.Net;
using System.Net.Http.Json;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudyApp.Api.Auth;
using StudyApp.Api.Data;
using StudyApp.Api.Jobs;
using StudyApp.Api.Models;
using StudyApp.Api.Services;
using StudyApp.Api.Skills;

namespace StudyApp.Api.Tests.Extraction;

/// <summary>
/// Stub IBackgroundJobClient that records enqueued job types.
/// </summary>
public class ExtractionStubJobClient : IBackgroundJobClient
{
    public List<Job> EnqueuedJobs { get; } = [];

    public string Create(Job job, IState state)
    {
        EnqueuedJobs.Add(job);
        return Guid.NewGuid().ToString();
    }

    public bool ChangeState(string jobId, IState state, string? expectedCurrentStateName) => true;
}

/// <summary>
/// Stub ISkillRunner for trigger tests — not called during trigger, only by the job.
/// </summary>
public class TriggerStubSkillRunner : ISkillRunner
{
    public Task<string> RunAsync(string scriptPath, string inputJson, CancellationToken ct = default)
        => Task.FromResult("""{"sections":[]}""");
}

public class ExtractionTriggerTestFactory : WebApplicationFactory<DevAuthHandler>
{
    public ExtractionStubJobClient JobClient { get; } = new();
    public Guid SeededModuleId { get; } = Guid.NewGuid();
    public Guid ReadyModuleId { get; } = Guid.NewGuid();
    public Guid ReRunModuleId { get; } = Guid.NewGuid();
    public Guid QueuedModuleId { get; } = Guid.NewGuid();
    public Guid EnqueueTestModuleId { get; } = Guid.NewGuid();
    public Guid DbCheckModuleId { get; } = Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace Postgres with in-memory EF
            var dbOptionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbOptionsDescriptor != null) services.Remove(dbOptionsDescriptor);

            var dbCtxDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(AppDbContext));
            if (dbCtxDescriptor != null) services.Remove(dbCtxDescriptor);

            var inMemoryOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"TriggerTestDb-{Guid.NewGuid()}")
                .Options;

            services.AddSingleton(inMemoryOptions);
            services.AddScoped<AppDbContext>(sp =>
                new AppDbContext(sp.GetRequiredService<DbContextOptions<AppDbContext>>()));

            // Replace IStorageService with stub
            var storageDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IStorageService));
            if (storageDescriptor != null) services.Remove(storageDescriptor);
            services.AddScoped<IStorageService, ExtractionTriggerStubStorage>();

            // Replace IBackgroundJobClient with our tracking stub (singleton so tests can inspect)
            var jobDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBackgroundJobClient));
            if (jobDescriptor != null) services.Remove(jobDescriptor);
            services.AddSingleton<IBackgroundJobClient>(JobClient);

            // Replace ISkillRunner with stub
            var skillDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ISkillRunner));
            if (skillDescriptor != null) services.Remove(skillDescriptor);
            services.AddScoped<ISkillRunner, TriggerStubSkillRunner>();

            // Seed modules
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Modules.Add(new Module
            {
                Id = SeededModuleId,
                UserId = new Guid("00000000-0000-0000-0000-000000000001"),
                Name = "Not Started Module"
            });

            db.Modules.Add(new Module
            {
                Id = ReadyModuleId,
                UserId = new Guid("00000000-0000-0000-0000-000000000001"),
                Name = "Ready Module"
            });

            db.ExtractionRuns.Add(new ExtractionRun
            {
                Id = Guid.NewGuid(),
                ModuleId = ReadyModuleId,
                Status = ExtractionStatus.Ready,
                DocxS3Key = $"modules/{ReadyModuleId}/runs/{Guid.NewGuid()}/lecture.docx"
            });

            db.Modules.Add(new Module
            {
                Id = ReRunModuleId,
                UserId = new Guid("00000000-0000-0000-0000-000000000001"),
                Name = "Re-Run Module"
            });
            db.ExtractionRuns.Add(new ExtractionRun
            {
                Id = Guid.NewGuid(),
                ModuleId = ReRunModuleId,
                Status = ExtractionStatus.Ready,
                DocxS3Key = $"modules/{ReRunModuleId}/runs/{Guid.NewGuid()}/lecture.docx"
            });

            db.Modules.Add(new Module
            {
                Id = QueuedModuleId,
                UserId = new Guid("00000000-0000-0000-0000-000000000001"),
                Name = "Queued Module"
            });

            db.ExtractionRuns.Add(new ExtractionRun
            {
                Id = Guid.NewGuid(),
                ModuleId = QueuedModuleId,
                Status = ExtractionStatus.Queued
            });

            db.Modules.Add(new Module
            {
                Id = EnqueueTestModuleId,
                UserId = new Guid("00000000-0000-0000-0000-000000000001"),
                Name = "Enqueue Test Module"
            });

            db.Modules.Add(new Module
            {
                Id = DbCheckModuleId,
                UserId = new Guid("00000000-0000-0000-0000-000000000001"),
                Name = "DB Check Module"
            });

            db.SaveChanges();
        });
    }
}

public class ExtractionTriggerStubStorage : IStorageService
{
    public Task<string> UploadAsync(Stream stream, string key, string contentType, CancellationToken ct = default)
        => Task.FromResult(key);

    public Task DeleteAsync(string key, CancellationToken ct = default) => Task.CompletedTask;

    public Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
        => Task.FromResult<Stream>(new MemoryStream(new byte[] { 1, 2, 3 }));
}

public class LectureExtractionTriggerTests(ExtractionTriggerTestFactory factory)
    : IClassFixture<ExtractionTriggerTestFactory>
{
    private HttpClient CreateAuthenticatedClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-UserId", "00000000-0000-0000-0000-000000000001");
        return client;
    }

    [Fact]
    public async Task PostExtract_EnqueuesLectureExtractionJob_Returns202()
    {
        var client = CreateAuthenticatedClient();
        factory.JobClient.EnqueuedJobs.Clear();

        var response = await client.PostAsync(
            $"/modules/{factory.EnqueueTestModuleId}/extract",
            null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        // Verify a job was enqueued
        Assert.Single(factory.JobClient.EnqueuedJobs);
        var enqueuedJob = factory.JobClient.EnqueuedJobs[0];
        Assert.Equal(typeof(LectureExtractionJob), enqueuedJob.Type);
    }

    [Fact]
    public async Task PostExtract_UnknownModule_Returns404()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.PostAsync(
            $"/modules/{Guid.NewGuid()}/extract",
            null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDocx_WhenReady_ReturnsDownloadUrl()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync(
            $"/modules/{factory.ReadyModuleId}/docx");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DocxUrlDto>();
        Assert.NotNull(body);
        Assert.Contains(factory.ReadyModuleId.ToString(), body.Url);
    }

    [Fact]
    public async Task GetDocx_UnknownModule_Returns404()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync(
            $"/modules/{Guid.NewGuid()}/docx");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDocx_NotReady_Returns409()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync(
            $"/modules/{factory.SeededModuleId}/docx");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostExtract_WhenRunQueued_Returns409()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.PostAsync(
            $"/modules/{factory.QueuedModuleId}/extract",
            null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostExtract_WhenPreviousRunReady_Returns202()
    {
        var client = CreateAuthenticatedClient();

        // Ready modules allow re-extraction (dedicated module to avoid polluting ReadyModuleId)
        var response = await client.PostAsync(
            $"/modules/{factory.ReRunModuleId}/extract",
            null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task PostExtract_CreatesQueuedRunInDb()
    {
        var client = CreateAuthenticatedClient();

        await client.PostAsync($"/modules/{factory.DbCheckModuleId}/extract", null);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var run = db.ExtractionRuns.SingleOrDefault(r => r.ModuleId == factory.DbCheckModuleId);

        Assert.NotNull(run);
        Assert.Equal(ExtractionStatus.Queued, run.Status);
    }
}

// DTO for deserializing docx URL response
public record DocxUrlDto(string Url);
