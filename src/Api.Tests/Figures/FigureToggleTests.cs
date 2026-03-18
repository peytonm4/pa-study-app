using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudyApp.Api.Auth;
using StudyApp.Api.Data;
using StudyApp.Api.Models;
using StudyApp.Api.Services;

namespace StudyApp.Api.Tests.Figures;

public class FiguresTestWebApplicationFactory : WebApplicationFactory<DevAuthHandler>
{
    public Guid SeededModuleId { get; } = Guid.NewGuid();
    public Guid SeededDocumentId { get; } = Guid.NewGuid();
    public Guid SeededFigureId { get; } = Guid.NewGuid();

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
                .UseInMemoryDatabase($"FigureToggleDb-{Guid.NewGuid()}")
                .Options;

            services.AddSingleton(inMemoryOptions);
            services.AddScoped<AppDbContext>(sp =>
                new AppDbContext(sp.GetRequiredService<DbContextOptions<AppDbContext>>()));

            // Replace IStorageService with stub
            var storageDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IStorageService));
            if (storageDescriptor != null) services.Remove(storageDescriptor);
            services.AddScoped<IStorageService, FigureStubStorageService>();

            // Seed the database
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var module = new Module
            {
                Id = SeededModuleId,
                Name = "Test Module",
                UserId = new Guid("00000000-0000-0000-0000-000000000001")
            };
            var document = new Document
            {
                Id = SeededDocumentId,
                ModuleId = SeededModuleId,
                FileName = "test.pptx",
                S3Key = "uploads/test.pptx",
                ContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                Status = DocumentStatus.Ready
            };
            var figure = new Figure
            {
                Id = SeededFigureId,
                DocumentId = SeededDocumentId,
                S3Key = "figures/test-fig.png",
                Keep = false,
                PageNumber = 1,
                LabelType = "Figure"
            };

            db.Modules.Add(module);
            db.Documents.Add(document);
            db.Figures.Add(figure);
            db.SaveChanges();
        });
    }
}

public class FigureStubStorageService : IStorageService
{
    public Task<string> UploadAsync(Stream stream, string key, string contentType, CancellationToken ct = default)
        => Task.FromResult(key);

    public Task DeleteAsync(string key, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
        => Task.FromResult<Stream>(new MemoryStream(new byte[] { 1, 2, 3 }));
}

public class FigureToggleTests(FiguresTestWebApplicationFactory factory) : IClassFixture<FiguresTestWebApplicationFactory>
{
    private HttpClient CreateAuthenticatedClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-UserId", "00000000-0000-0000-0000-000000000001");
        return client;
    }

    [Fact]
    public async Task PatchFigure_TogglesKeepField_Returns200()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/figures/{factory.SeededFigureId}",
            new { keep = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetModuleFigures_ReturnsListWithFigure_Returns200()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/modules/{factory.SeededModuleId}/figures");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(factory.SeededFigureId.ToString(), body);
    }

    [Fact]
    public async Task GetModuleFigures_UnknownModule_Returns404()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/modules/{Guid.NewGuid()}/figures");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
