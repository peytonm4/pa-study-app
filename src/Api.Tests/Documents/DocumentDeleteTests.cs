using System.Net;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudyApp.Api.Auth;
using StudyApp.Api.Data;
using StudyApp.Api.Models;
using StudyApp.Api.Services;

namespace StudyApp.Api.Tests.Documents;

public class DocumentDeleteTestFactory : WebApplicationFactory<DevAuthHandler>
{
    public Guid ModuleId { get; } = Guid.NewGuid();
    public Guid DocumentId { get; } = Guid.NewGuid();
    public Guid DocumentForRunCancelId { get; } = Guid.NewGuid();
    public Guid FigureId { get; } = Guid.NewGuid();
    public Guid ActiveRunId { get; } = Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var dbOptionsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbOptionsDescriptor != null) services.Remove(dbOptionsDescriptor);
            var dbCtxDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(AppDbContext));
            if (dbCtxDescriptor != null) services.Remove(dbCtxDescriptor);

            var inMemoryOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"DocumentDeleteDb-{Guid.NewGuid()}")
                .Options;
            services.AddSingleton(inMemoryOptions);
            services.AddScoped<AppDbContext>(sp =>
                new AppDbContext(sp.GetRequiredService<DbContextOptions<AppDbContext>>()));

            var storageDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IStorageService));
            if (storageDescriptor != null) services.Remove(storageDescriptor);
            services.AddScoped<IStorageService, StubStorageService>();

            var jobDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBackgroundJobClient));
            if (jobDescriptor != null) services.Remove(jobDescriptor);
            services.AddSingleton<IBackgroundJobClient, StubJobClient>();

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Modules.Add(new Module
            {
                Id = ModuleId,
                UserId = new Guid("00000000-0000-0000-0000-000000000001"),
                Name = "Test Module"
            });
            db.Documents.Add(new Document
            {
                Id = DocumentId,
                ModuleId = ModuleId,
                FileName = "test.pptx",
                S3Key = "uploads/test.pptx",
                ContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                Status = DocumentStatus.Ready
            });
            db.Figures.Add(new Figure
            {
                Id = FigureId,
                DocumentId = DocumentId,
                S3Key = "figures/test-fig.png",
                PageNumber = 1
            });
            db.Documents.Add(new Document
            {
                Id = DocumentForRunCancelId,
                ModuleId = ModuleId,
                FileName = "cancel-test.pptx",
                S3Key = "uploads/cancel-test.pptx",
                ContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                Status = DocumentStatus.Ready
            });
            db.ExtractionRuns.Add(new ExtractionRun
            {
                Id = ActiveRunId,
                ModuleId = ModuleId,
                Status = ExtractionStatus.Queued
            });
            db.SaveChanges();
        });
    }
}

public class DocumentDeleteTests(DocumentDeleteTestFactory factory) : IClassFixture<DocumentDeleteTestFactory>
{
    private HttpClient CreateAuthenticatedClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-UserId", "00000000-0000-0000-0000-000000000001");
        return client;
    }

    [Fact]
    public async Task DeleteDocument_Returns204()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/documents/{factory.DocumentId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDocument_UnknownId_Returns404()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/documents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDocument_CancelsActiveExtractionRun()
    {
        var client = CreateAuthenticatedClient();

        await client.DeleteAsync($"/documents/{factory.DocumentForRunCancelId}");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var run = await db.ExtractionRuns.FindAsync(factory.ActiveRunId);

        Assert.NotNull(run);
        Assert.Equal(ExtractionStatus.Failed, run.Status);
    }
}
