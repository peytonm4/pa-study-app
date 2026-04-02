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

namespace StudyApp.Api.Tests.Generation;

/// <summary>
/// Stub IBackgroundJobClient for generation trigger tests.
/// </summary>
public class GenerationStubJobClient : IBackgroundJobClient
{
    public List<Job> EnqueuedJobs { get; } = [];

    public string Create(Job job, IState state)
    {
        EnqueuedJobs.Add(job);
        return Guid.NewGuid().ToString();
    }

    public bool ChangeState(string jobId, IState state, string? expectedCurrentStateName) => true;
}

public class GenerationStubStorage : IStorageService
{
    public Task<string> UploadAsync(Stream stream, string key, string contentType, CancellationToken ct = default)
        => Task.FromResult(key);
    public Task DeleteAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    public Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
        => Task.FromResult<Stream>(new MemoryStream(new byte[] { 1, 2, 3 }));
}

public class GenerationStubSkillRunner : ISkillRunner
{
    public Task<string> RunAsync(string scriptPath, string inputJson, CancellationToken ct = default)
        => Task.FromResult("""{"sections":[]}""");
}

public class GenerationTriggerTestFactory : WebApplicationFactory<DevAuthHandler>
{
    public GenerationStubJobClient JobClient { get; } = new();

    // Module with ExtractionRun.Status=Ready — valid for triggering generation
    public Guid ReadyExtractionModuleId { get; } = Guid.NewGuid();

    // Module with no ExtractionRun — extraction not started, cannot generate
    public Guid NoExtractionModuleId { get; } = Guid.NewGuid();

    // Module with GenerationRun.Status=Queued — generation already in-flight
    public Guid QueuedGenerationModuleId { get; } = Guid.NewGuid();

    // Module with ExtractionRun.Ready for DB assertion test
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
                .UseInMemoryDatabase($"GenTriggerTestDb-{Guid.NewGuid()}")
                .Options;

            services.AddSingleton(inMemoryOptions);
            services.AddScoped<AppDbContext>(sp =>
                new AppDbContext(sp.GetRequiredService<DbContextOptions<AppDbContext>>()));

            // Replace IStorageService with stub
            var storageDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IStorageService));
            if (storageDescriptor != null) services.Remove(storageDescriptor);
            services.AddScoped<IStorageService, GenerationStubStorage>();

            // Replace IBackgroundJobClient with tracking stub
            var jobDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBackgroundJobClient));
            if (jobDescriptor != null) services.Remove(jobDescriptor);
            services.AddSingleton<IBackgroundJobClient>(JobClient);

            // Replace ISkillRunner with stub
            var skillDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ISkillRunner));
            if (skillDescriptor != null) services.Remove(skillDescriptor);
            services.AddScoped<ISkillRunner, GenerationStubSkillRunner>();

            // Seed test data
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var devUserId = new Guid("00000000-0000-0000-0000-000000000001");

            // Module with ExtractionRun.Ready — can trigger generation
            db.Modules.Add(new Module
            {
                Id = ReadyExtractionModuleId,
                UserId = devUserId,
                Name = "Ready Extraction Module"
            });
            db.ExtractionRuns.Add(new ExtractionRun
            {
                Id = Guid.NewGuid(),
                ModuleId = ReadyExtractionModuleId,
                Status = ExtractionStatus.Ready,
                DocxS3Key = $"modules/{ReadyExtractionModuleId}/runs/{Guid.NewGuid()}/lecture.docx"
            });

            // Module with no ExtractionRun — cannot trigger generation
            db.Modules.Add(new Module
            {
                Id = NoExtractionModuleId,
                UserId = devUserId,
                Name = "No Extraction Module"
            });

            // Module with ExtractionRun.Ready AND GenerationRun.Queued — cannot re-trigger
            db.Modules.Add(new Module
            {
                Id = QueuedGenerationModuleId,
                UserId = devUserId,
                Name = "Queued Generation Module"
            });
            db.ExtractionRuns.Add(new ExtractionRun
            {
                Id = Guid.NewGuid(),
                ModuleId = QueuedGenerationModuleId,
                Status = ExtractionStatus.Ready,
                DocxS3Key = $"modules/{QueuedGenerationModuleId}/runs/{Guid.NewGuid()}/lecture.docx"
            });
            db.GenerationRuns.Add(new GenerationRun
            {
                Id = Guid.NewGuid(),
                ModuleId = QueuedGenerationModuleId,
                Status = GenerationStatus.Queued
            });

            // Module for DB assertion — has ExtractionRun.Ready
            db.Modules.Add(new Module
            {
                Id = DbCheckModuleId,
                UserId = devUserId,
                Name = "DB Check Module"
            });
            db.ExtractionRuns.Add(new ExtractionRun
            {
                Id = Guid.NewGuid(),
                ModuleId = DbCheckModuleId,
                Status = ExtractionStatus.Ready,
                DocxS3Key = $"modules/{DbCheckModuleId}/runs/{Guid.NewGuid()}/lecture.docx"
            });

            db.SaveChanges();
        });
    }
}

public class GenerationTriggerTests(GenerationTriggerTestFactory factory)
    : IClassFixture<GenerationTriggerTestFactory>
{
    private HttpClient CreateAuthenticatedClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-UserId", "00000000-0000-0000-0000-000000000001");
        return client;
    }

    [Fact]
    public async Task PostGenerate_WhenExtractionReady_Returns202AndEnqueuesJob()
    {
        var client = CreateAuthenticatedClient();
        factory.JobClient.EnqueuedJobs.Clear();

        var response = await client.PostAsync(
            $"/modules/{factory.ReadyExtractionModuleId}/generate",
            null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Single(factory.JobClient.EnqueuedJobs);
        var enqueuedJob = factory.JobClient.EnqueuedJobs[0];
        Assert.Equal(typeof(ContentGenerationJob), enqueuedJob.Type);
    }

    [Fact]
    public async Task PostGenerate_WhenExtractionNotReady_Returns409()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.PostAsync(
            $"/modules/{factory.NoExtractionModuleId}/generate",
            null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostGenerate_WhenGenerationQueued_Returns409()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.PostAsync(
            $"/modules/{factory.QueuedGenerationModuleId}/generate",
            null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostGenerate_CreatesGenerationRunInDb()
    {
        var client = CreateAuthenticatedClient();

        await client.PostAsync($"/modules/{factory.DbCheckModuleId}/generate", null);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var run = db.GenerationRuns.SingleOrDefault(r => r.ModuleId == factory.DbCheckModuleId);

        Assert.NotNull(run);
        Assert.Equal(GenerationStatus.Queued, run.Status);
    }
}
