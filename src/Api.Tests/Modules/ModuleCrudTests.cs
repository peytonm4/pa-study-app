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
using StudyApp.Api.Models;
using StudyApp.Api.Services;
using StudyApp.Api.Tests.Documents;

namespace StudyApp.Api.Tests.Modules;

public class ModuleCrudTestFactory : WebApplicationFactory<DevAuthHandler>
{
    public Guid ModuleWithRunId { get; } = Guid.NewGuid();
    public Guid ModuleNoRunId { get; } = Guid.NewGuid();
    public Guid ModuleForDeleteId { get; } = Guid.NewGuid();

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
                .UseInMemoryDatabase($"ModuleCrudDb-{Guid.NewGuid()}")
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
                Id = ModuleWithRunId,
                UserId = new Guid("00000000-0000-0000-0000-000000000001"),
                Name = "Module With Run"
            });
            db.ExtractionRuns.Add(new ExtractionRun
            {
                ModuleId = ModuleWithRunId,
                Status = ExtractionStatus.Ready,
                DocxS3Key = "modules/run/lecture.docx"
            });

            db.Modules.Add(new Module
            {
                Id = ModuleNoRunId,
                UserId = new Guid("00000000-0000-0000-0000-000000000001"),
                Name = "Module No Run"
            });

            db.Modules.Add(new Module
            {
                Id = ModuleForDeleteId,
                UserId = new Guid("00000000-0000-0000-0000-000000000001"),
                Name = "Module For Delete"
            });

            db.SaveChanges();
        });
    }
}

public class ModuleCrudTests(ModuleCrudTestFactory factory) : IClassFixture<ModuleCrudTestFactory>
{
    private HttpClient CreateAuthenticatedClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-UserId", "00000000-0000-0000-0000-000000000001");
        return client;
    }

    [Fact]
    public async Task GetModules_ReturnsList()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/modules");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(factory.ModuleWithRunId.ToString(), body);
    }

    [Fact]
    public async Task GetModule_WithReadyRun_ReturnsReadyExtractionStatus()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/modules/{factory.ModuleWithRunId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Ready", body);
    }

    [Fact]
    public async Task GetModule_WithNoRun_ReturnsNotStartedExtractionStatus()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/modules/{factory.ModuleNoRunId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("NotStarted", body);
    }

    [Fact]
    public async Task GetModule_UnknownId_Returns404()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/modules/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateModule_Returns201WithId()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/modules", new { name = "New Module" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("New Module", body);
    }

    [Fact]
    public async Task DeleteModule_Returns204()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/modules/{factory.ModuleForDeleteId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteModule_UnknownId_Returns404()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/modules/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
