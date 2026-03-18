using Amazon.S3;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using StudyApp.Api.Data;
using StudyApp.Api.Extraction;
using StudyApp.Api.Jobs;
using StudyApp.Api.Services;
using StudyApp.Worker;
using StudyApp.Worker.Extraction;

var builder = Host.CreateApplicationBuilder(args);

// Database — same connection string as API
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// S3-compatible storage
builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var s3Config = new AmazonS3Config
    {
        ServiceURL = builder.Configuration["Storage:ServiceUrl"],
        ForcePathStyle = true
    };
    return new AmazonS3Client(
        builder.Configuration["Storage:AccessKey"],
        builder.Configuration["Storage:SecretKey"],
        s3Config
    );
});
builder.Services.AddScoped<IStorageService, S3StorageService>();

// Hangfire — process jobs from shared Postgres queue
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Default"))));
builder.Services.AddHangfireServer();

// Extraction services — interfaces in Api.Extraction, implementations in Worker.Extraction
builder.Services.AddScoped<IPptxExtractor, PptxExtractor>();
builder.Services.AddScoped<IPdfExtractor, PdfExtractor>();

// Provider selection via env vars (LLM-03)
builder.Services.AddProviders(builder.Configuration);

// Hangfire jobs — registered so Hangfire can resolve via DI
builder.Services.AddScoped<IngestionJob>();
builder.Services.AddScoped<VisionExtractionJob>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
