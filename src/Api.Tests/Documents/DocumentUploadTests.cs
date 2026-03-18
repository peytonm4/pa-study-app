using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Amazon.S3;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudyApp.Api.Auth;
using StudyApp.Api.Data;
using StudyApp.Api.Services;
using A = DocumentFormat.OpenXml.Drawing;

namespace StudyApp.Api.Tests.Documents;

/// <summary>
/// Integration test factory that replaces infrastructure services with in-memory/stub equivalents.
/// Allows upload tests to run without Postgres, MinIO, or Hangfire.
/// </summary>
// Use DevAuthHandler (StudyApp.Api.Auth) as type anchor — unambiguous across Api and Worker assemblies.
// WebApplicationFactory<T> only requires T to be from the target assembly; it does not have to be Program.
public class TestWebApplicationFactory : WebApplicationFactory<DevAuthHandler>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace Postgres with in-memory EF.
            // Build fresh DbContextOptions<AppDbContext> with ONLY InMemory configured,
            // then register it as singleton + register AppDbContext scoped manually.
            // This bypasses the "multiple providers" error from EF's internal service provider.
            var dbOptionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbOptionsDescriptor != null) services.Remove(dbOptionsDescriptor);

            var dbCtxDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(AppDbContext));
            if (dbCtxDescriptor != null) services.Remove(dbCtxDescriptor);

            var inMemoryOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"TestDb-{Guid.NewGuid()}")
                .Options;

            services.AddSingleton(inMemoryOptions);
            services.AddScoped<AppDbContext>(sp =>
                new AppDbContext(sp.GetRequiredService<DbContextOptions<AppDbContext>>()));

            // Replace IAmazonS3 with null (not used via stub storage)
            var s3Descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAmazonS3));
            if (s3Descriptor != null) services.Remove(s3Descriptor);
            services.AddSingleton<IAmazonS3>(_ => null!);

            // Replace IStorageService with stub that accepts uploads without MinIO
            var storageDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IStorageService));
            if (storageDescriptor != null) services.Remove(storageDescriptor);
            services.AddScoped<IStorageService, StubStorageService>();

            // Replace IBackgroundJobClient with stub that records enqueued jobs
            var jobDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBackgroundJobClient));
            if (jobDescriptor != null) services.Remove(jobDescriptor);
            services.AddSingleton<IBackgroundJobClient, StubJobClient>();
        });
    }
}

/// <summary>Stub storage that accepts uploads and returns the key; no S3 calls.</summary>
public class StubStorageService : IStorageService
{
    public Task<string> UploadAsync(Stream stream, string key, string contentType, CancellationToken ct = default)
        => Task.FromResult(key);

    public Task DeleteAsync(string key, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
        => Task.FromResult<Stream>(new MemoryStream());
}

/// <summary>Stub Hangfire client that accepts enqueue calls without Postgres.</summary>
public class StubJobClient : IBackgroundJobClient
{
    public string Create(Job job, IState state) => Guid.NewGuid().ToString();
    public bool ChangeState(string jobId, IState state, string? expectedCurrentStateName) => true;
}

public class DocumentUploadTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private HttpClient CreateAuthenticatedClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-UserId", "00000000-0000-0000-0000-000000000001");
        return client;
    }

    private async Task<string> CreateModuleAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/modules", new { name = "Test Module" });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ModuleDto>();
        return result!.id.ToString();
    }

    [Fact]
    public async Task PostDocument_WithPptx_Returns202()
    {
        var client = CreateAuthenticatedClient();
        var moduleId = await CreateModuleAsync(client);

        using var pptxContent = new ByteArrayContent(CreateMinimalPptxBytes());
        pptxContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.presentationml.presentation");

        using var form = new MultipartFormDataContent();
        form.Add(pptxContent, "file", "test.pptx");

        var response = await client.PostAsync($"/modules/{moduleId}/documents", form);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task PostDocument_WithPdf_Returns202()
    {
        var client = CreateAuthenticatedClient();
        var moduleId = await CreateModuleAsync(client);

        using var pdfContent = new ByteArrayContent(CreateMinimalPdfBytes());
        pdfContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        using var form = new MultipartFormDataContent();
        form.Add(pdfContent, "file", "test.pdf");

        var response = await client.PostAsync($"/modules/{moduleId}/documents", form);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    private static byte[] CreateMinimalPptxBytes()
    {
        using var ms = new MemoryStream();
        using (var ppt = PresentationDocument.Create(ms, PresentationDocumentType.Presentation, autoSave: true))
        {
            var pp = ppt.AddPresentationPart();
            pp.Presentation = new Presentation
            {
                SlideIdList = new SlideIdList(),
                SlideSize = new SlideSize { Cx = 9144000, Cy = 6858000 },
                NotesSize = new NotesSize { Cx = 6858000, Cy = 9144000 },
            };
            pp.Presentation.Save();
        }
        return ms.ToArray();
    }

    private static byte[] CreateMinimalPdfBytes()
    {
        // Minimal valid 1-page blank PDF
        const string raw = """
            %PDF-1.4
            1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj
            2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj
            3 0 obj<</Type/Page/MediaBox[0 0 612 792]/Parent 2 0 R>>endobj
            xref
            0 4
            0000000000 65535 f
            0000000009 00000 n
            0000000058 00000 n
            0000000115 00000 n
            trailer<</Size 4/Root 1 0 R>>
            startxref
            190
            %%EOF
            """;
        return System.Text.Encoding.Latin1.GetBytes(raw);
    }

    // Helper DTO for deserializing module response
    private record ModuleDto(Guid id, string name);
}
