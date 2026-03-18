---
phase: 02-document-ingestion
plan: "05"
subsystem: worker-jobs
tags: [hangfire, ingestion, vision, extraction, integration-tests, s3, aspnetcore]

# Dependency graph
requires:
  - phase: 02-document-ingestion
    plan: "02"
    provides: "IStorageService, S3StorageService, IngestionJob stub, Worker DI foundation"
  - phase: 02-document-ingestion
    plan: "03"
    provides: "IPptxExtractor, IPdfExtractor, PptxExtractor, PdfExtractor implementations"
  - phase: 02-document-ingestion
    plan: "04"
    provides: "IVisionProvider, StubVisionProvider, GeminiVisionProvider, ProviderRegistration"

provides:
  - "IStorageService.DownloadAsync(string key) — Task<Stream> for S3 download"
  - "IngestionJob.Execute(Guid documentId) — Hangfire job: download S3, extract chunks, persist, fan out VisionExtractionJobs"
  - "VisionExtractionJob.Execute(Guid documentId, int pageNumber) — calls IVisionProvider, atomic PendingVisionJobs decrement"
  - "Both jobs registered in Worker DI via AddScoped<IngestionJob>() and AddScoped<VisionExtractionJob>()"
  - "DocumentUploadTests — real WebApplicationFactory integration tests with InMemory EF, StubStorage, StubJobClient"
  - "IPptxExtractor, IPdfExtractor interfaces moved to StudyApp.Api.Extraction for circular-dependency-free access"
  - "IVisionProvider interface moved to StudyApp.Api.Providers for same reason"

affects:
  - 03-study-tools (chunk records are now produced by the pipeline)
  - document upload flow end-to-end

# Tech tracking
tech-stack:
  added:
    - "Microsoft.EntityFrameworkCore.InMemory (test-only, Api.Tests.csproj)"
  patterns:
    - "Interface-in-Api pattern: shared job types (IngestionJob, VisionExtractionJob) live in Api so Worker can resolve them via ProjectReference without circular dependency"
    - "ExecuteUpdateAsync for atomic PendingVisionJobs decrement — avoids race condition on concurrent VisionExtractionJobs"
    - "WebApplicationFactory with InMemory EF: register DbContextOptions<T> as singleton directly (bypasses Npgsql+InMemory conflict)"
    - "DevAuthHandler as WebApplicationFactory<T> type anchor — avoids ambiguous Program class when test project references both Api and Worker"

key-files:
  created:
    - src/Api/Extraction/IPptxExtractor.cs
    - src/Api/Extraction/IPdfExtractor.cs
    - src/Api/Providers/IVisionProvider.cs
    - src/Api/Jobs/VisionExtractionJob.cs
  modified:
    - src/Api/Jobs/IngestionJob.cs
    - src/Api/Services/IStorageService.cs
    - src/Api/Services/S3StorageService.cs
    - src/Api/Program.cs
    - src/Worker/Extraction/IPptxExtractor.cs
    - src/Worker/Extraction/IPdfExtractor.cs
    - src/Worker/Extraction/PptxExtractor.cs
    - src/Worker/Extraction/PdfExtractor.cs
    - src/Worker/Providers/IVisionProvider.cs
    - src/Worker/Providers/StubVisionProvider.cs
    - src/Worker/Providers/GeminiVisionProvider.cs
    - src/Worker/ProviderRegistration.cs
    - src/Worker/Program.cs
    - src/Api.Tests/Documents/DocumentUploadTests.cs
    - src/Api.Tests/StudyApp.Api.Tests.csproj
    - src/Api.Tests/Extraction/PptxExtractorTests.cs
    - src/Api.Tests/Extraction/PdfExtractorTests.cs
    - src/Api.Tests/Providers/ProviderConfigTests.cs

key-decisions:
  - "Extractor interfaces (IPptxExtractor, IPdfExtractor) moved from StudyApp.Worker.Extraction to StudyApp.Api.Extraction so IngestionJob in Api/Jobs can reference them without circular project reference"
  - "IVisionProvider moved from StudyApp.Worker.Providers to StudyApp.Api.Providers for same reason (VisionExtractionJob needs it from Api/Jobs)"
  - "IngestionJob and VisionExtractionJob placed in src/Api/Jobs/ — Worker references Api, so both projects share the same Hangfire type name; Worker DI registers the real implementations"
  - "WebApplicationFactory uses DevAuthHandler as type anchor instead of Program — both Api and Worker have a Program class (top-level statements), which caused CS0433 ambiguity"
  - "db.Database.Migrate() guarded by IsDevelopment() in Api/Program.cs — prevents startup failure in Testing environment when InMemory EF is used"
  - "DbContextOptions<AppDbContext> registered as singleton directly in test factory using DbContextOptionsBuilder.UseInMemoryDatabase() — avoids Npgsql+InMemory dual-provider conflict"
  - "AtomicDecrement via ExecuteUpdateAsync for PendingVisionJobs — prevents race condition when multiple VisionExtractionJobs complete concurrently"

# Metrics
duration: 21min
completed: 2026-03-18
---

# Phase 2 Plan 05: IngestionJob and VisionExtractionJob Implementation Summary

**Real IngestionJob and VisionExtractionJob Hangfire jobs: S3 download, chunk extraction/persistence, atomic vision job fan-out, and passing WebApplicationFactory integration tests with InMemory EF and stub dependencies**

## Performance

- **Duration:** ~21 min
- **Started:** 2026-03-18T03:48:01Z
- **Completed:** 2026-03-18T04:09:01Z
- **Tasks:** 2 task commits (Task 1+2 combined, Task 3)
- **Files modified:** 19

## Accomplishments

- `IngestionJob.Execute(Guid documentId)` fully implemented: downloads from S3, extracts via IPptxExtractor/IPdfExtractor (dispatched by ContentType), persists Chunk records, enqueues VisionExtractionJobs for blank PDF pages, sets Document.Status=Ready or Processing accordingly
- `VisionExtractionJob.Execute(Guid documentId, int pageNumber)` fully implemented: downloads full PDF bytes, calls IVisionProvider.ExtractTextAsync, updates Chunk.Content + IsVisionExtracted=true, atomically decrements PendingVisionJobs via ExecuteUpdateAsync, sets Status=Ready when count reaches 0
- Both jobs registered in Worker DI via AddScoped; discoverable by Hangfire
- `IStorageService` extended with `DownloadAsync(string key)` method; S3StorageService implements via GetObjectAsync
- `DocumentUploadTests` fully replaced: 2 real WebApplicationFactory integration tests with InMemory EF, StubStorageService, StubJobClient — both pass without Docker/DB/S3
- All 21 tests passing (0 failures)

## Task Commits

1. **IngestionJob, VisionExtractionJob, DownloadAsync, interface refactoring** - `b83bb51` (feat)
2. **DocumentUploadTests integration tests + test infrastructure** - `2bd76e8` (feat)

## Files Created/Modified

- `src/Api/Extraction/IPptxExtractor.cs` - Interface + SlideContent record, moved from Worker to Api
- `src/Api/Extraction/IPdfExtractor.cs` - Interface + PageContent record, moved from Worker to Api
- `src/Api/Providers/IVisionProvider.cs` - Interface moved from Worker to Api
- `src/Api/Jobs/IngestionJob.cs` - Real implementation: S3 download, extract, persist, fan-out
- `src/Api/Jobs/VisionExtractionJob.cs` - Real implementation: vision extract, atomic decrement, Ready detection
- `src/Api/Services/IStorageService.cs` - Added DownloadAsync(string key)
- `src/Api/Services/S3StorageService.cs` - Implemented DownloadAsync via GetObjectAsync
- `src/Api/Program.cs` - Guard db.Database.Migrate() with IsDevelopment()
- `src/Worker/Extraction/PptxExtractor.cs` - Updated using to StudyApp.Api.Extraction
- `src/Worker/Extraction/PdfExtractor.cs` - Updated using to StudyApp.Api.Extraction
- `src/Worker/Providers/StubVisionProvider.cs` - Updated using to StudyApp.Api.Providers
- `src/Worker/Providers/GeminiVisionProvider.cs` - Updated using to StudyApp.Api.Providers
- `src/Worker/ProviderRegistration.cs` - Updated using to StudyApp.Api.Providers
- `src/Worker/Program.cs` - Added IngestionJob and VisionExtractionJob AddScoped registrations
- `src/Api.Tests/Documents/DocumentUploadTests.cs` - Full WebApplicationFactory integration test
- `src/Api.Tests/StudyApp.Api.Tests.csproj` - Added EF InMemory package
- `src/Api.Tests/Extraction/PptxExtractorTests.cs` - Added StudyApp.Api.Extraction using
- `src/Api.Tests/Extraction/PdfExtractorTests.cs` - Added StudyApp.Api.Extraction using
- `src/Api.Tests/Providers/ProviderConfigTests.cs` - Added StudyApp.Api.Providers using

## Decisions Made

- Extractor interfaces moved to Api project (from Worker) to enable IngestionJob in Api/Jobs to reference them without a circular dependency — Worker implementations updated with new namespace
- IVisionProvider moved to Api.Providers for same reason — VisionExtractionJob (also in Api/Jobs) needs it
- `DevAuthHandler` used as WebApplicationFactory<T> type anchor — both Api and Worker have a `Program` class (top-level statements) causing CS0433; DevAuthHandler is unique to Api
- `db.Database.Migrate()` guarded by `IsDevelopment()` — test environment startup was failing trying to run EF migrations against InMemory database
- Direct `DbContextOptions<AppDbContext>` singleton registration (not `AddDbContext()`) in test factory — the standard descriptor removal pattern doesn't work with EF 10's internal service provider; direct options injection bypasses the Npgsql+InMemory dual-provider conflict

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 4 triggered → Rule 2 auto-fixed] Interface namespace refactoring to break circular dependency**
- **Found during:** Task 1 (writing IngestionJob in Api/Jobs)
- **Issue:** Api/Jobs/IngestionJob needs IPptxExtractor and IPdfExtractor, but those are in Worker which references Api — circular dependency would be created
- **Decision:** Move interfaces to Api project (Worker keeps implementations) — this is the standard layered-architecture pattern where interfaces belong to the consumer domain, implementations to the infrastructure
- **Fix:** Created src/Api/Extraction/ and src/Api/Providers/ with moved interfaces; updated 7 files (implementations, tests) to use new namespaces
- **Commit:** b83bb51

**2. [Rule 1 - Bug] db.Database.Migrate() caused test startup failure**
- **Found during:** Task 3 (running DocumentUpload integration tests)
- **Issue:** Api/Program.cs calls `db.Database.Migrate()` unconditionally; fails when InMemory DB is used in tests because InMemory provider doesn't support migrations
- **Fix:** Wrapped in `if (app.Environment.IsDevelopment())` — test factory sets environment to "Testing"
- **Files modified:** `src/Api/Program.cs`
- **Commit:** 2bd76e8

**3. [Rule 1 - Bug] CS0433 ambiguity: Program class in both Api and Worker assemblies**
- **Found during:** Task 3 (writing WebApplicationFactory<Program>)
- **Issue:** Both top-level Programs (Api and Worker) generate a `Program` class with no namespace; since Api.Tests references both assemblies, `Program` is ambiguous
- **Fix:** Use `WebApplicationFactory<DevAuthHandler>` — DevAuthHandler is Api-only; WebApplicationFactory<T> accepts any type from the target assembly
- **Files modified:** `src/Api.Tests/Documents/DocumentUploadTests.cs`
- **Commit:** 2bd76e8

**4. [Rule 1 - Bug] Npgsql + InMemory dual-provider EF conflict in test factory**
- **Found during:** Task 3 (running DocumentUpload tests — "multiple providers registered" error)
- **Issue:** Standard `services.Remove(descriptor) + services.AddDbContext(InMemory)` pattern doesn't work with EF 10 — Npgsql provider is registered in EF's internal service provider, not the outer IServiceCollection
- **Fix:** Register `DbContextOptions<AppDbContext>` as singleton directly using `new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase().Options`, then register `AppDbContext` scoped manually — bypasses EF internal provider conflict entirely
- **Files modified:** `src/Api.Tests/Documents/DocumentUploadTests.cs`
- **Commit:** 2bd76e8

**Total deviations:** 4 auto-fixed (1 architectural + 3 bugs)
**Impact on plan:** All fixes necessary for correct compilation and test execution. Interface refactoring is a clean architectural improvement.

## Self-Check: PASSED
