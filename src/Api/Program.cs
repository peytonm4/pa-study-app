using Amazon.S3;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using StudyApp.Api.Auth;
using StudyApp.Api.Data;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// CORS — allow Vite dev server in Development only
builder.Services.AddCors(options =>
{
    options.AddPolicy("ViteDev", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Database — EF Core + Npgsql
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Auth — X-Dev-UserId header scheme (Development only in Phase 1)
builder.Services
    .AddAuthentication("DevAuth")
    .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>("DevAuth", null);
builder.Services.AddAuthorization();

// Hangfire — Postgres storage
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Default"))));
builder.Services.AddHangfireServer();

// S3-compatible storage — works with MinIO locally via ForcePathStyle
builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var s3Config = new AmazonS3Config
    {
        ServiceURL = builder.Configuration["Storage:ServiceUrl"],
        ForcePathStyle = true  // REQUIRED for MinIO path-style URLs
    };
    return new AmazonS3Client(
        builder.Configuration["Storage:AccessKey"],
        builder.Configuration["Storage:SecretKey"],
        s3Config
    );
});

var app = builder.Build();

// Apply EF migrations on API startup (Development convenience — not for production)
// Only the API applies migrations; Worker assumes schema exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseCors("ViteDev");
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Hangfire dashboard — local requests only, no login required in dev
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter() }
});

app.Run();
