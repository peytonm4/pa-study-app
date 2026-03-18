---
phase: 02-document-ingestion
plan: "02"
subsystem: api
tags: [hangfire, s3, minio, aspnetcore, worker, background-jobs]

# Dependency graph
requires:
  - phase: 02-document-ingestion
    plan: "01"
    provides: "NuGet packages, EF entities (Module, Document, Chunk, DocumentStatus), AppDbContext migrations"
provides:
  - "IStorageService interface and S3StorageService implementation"
  - "ModulesController: GET/POST/DELETE /modules"
  - "DocumentsController: POST /modules/{id}/documents, GET /documents/{id}/status, DELETE /documents/{id}"
  - "Worker wired with Hangfire server, AppDbContext, and IStorageService"
  - "IngestionJob placeholder stub (plan 02-04 fills in real logic)"
affects:
  - 02-document-ingestion
  - 03-study-tools

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "IStorageService abstraction over S3 for testability"
    - "Upload flow: create DB record (Uploading) → stream to S3 → set Queued → Enqueue Hangfire job"
    - "Module status computed from document statuses (any active = Processing)"
    - "IngestionJob stub pattern: placeholder compiles, real impl comes in later plan"

key-files:
  created:
    - src/Api/Services/IStorageService.cs
    - src/Api/Services/S3StorageService.cs
    - src/Api/Controllers/ModulesController.cs
    - src/Api/Controllers/DocumentsController.cs
    - src/Api/Jobs/IngestionJob.cs
    - src/Worker/ProviderConfig.cs
  modified:
    - src/Api/Program.cs
    - src/Worker/Program.cs
    - src/Worker/appsettings.Development.json

key-decisions:
  - "IngestionJob stub placed in src/Api/Jobs/ so both Api and Worker (which references Api) can use it once real implementation is added"
  - "Module DELETE swallows S3 errors to avoid leaving orphaned DB records if S3 key is already missing"
  - "Worker appsettings.Development.json gitignored per project convention — must be configured manually"
  - "ProviderConfig registered as singleton to propagate provider selection (stub/anthropic/google) to job implementations in plan 02-05"

patterns-established:
  - "Ownership check pattern: always filter by both entity id and userId in one query"
  - "PPTX+PDF validation via HashSet<string> of allowed content types"
  - "S3 key format: uploads/{userId}/{moduleId}/{documentId}/{filename}"

requirements-completed:
  - INGEST-01
  - INGEST-02

# Metrics
duration: 15min
completed: 2026-03-17
---

# Phase 2 Plan 02: Upload API and Worker Wiring Summary

**File upload API with S3StorageService, five REST endpoints for modules/documents, and Worker wired for Hangfire job execution with shared Postgres queue**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-03-17T00:00:00Z
- **Completed:** 2026-03-17T00:15:00Z
- **Tasks:** 3
- **Files modified:** 9

## Accomplishments
- IStorageService/S3StorageService abstraction over MinIO/S3 with configurable bucket name
- Five REST endpoints across ModulesController and DocumentsController, all [Authorize]-gated with ownership enforcement
- Worker registers Hangfire server, AppDbContext, and IStorageService — ready to process background jobs

## Task Commits

Each task was committed atomically:

1. **Task 1: IStorageService, S3StorageService, and DI registration** - `ff4bc5d` (feat)
2. **Task 2: ModulesController and DocumentsController** - `1f2f38d` (feat)
3. **Task 3: Wire Worker for Hangfire job execution** - `aaed300` (feat)

## Files Created/Modified
- `src/Api/Services/IStorageService.cs` - Storage abstraction with UploadAsync/DeleteAsync
- `src/Api/Services/S3StorageService.cs` - MinIO/S3 implementation using IAmazonS3
- `src/Api/Controllers/ModulesController.cs` - GET/POST/DELETE /modules with status computation
- `src/Api/Controllers/DocumentsController.cs` - Upload (202), status poll, delete document
- `src/Api/Jobs/IngestionJob.cs` - Placeholder stub for Hangfire job (real in plan 02-04)
- `src/Api/Program.cs` - Added IStorageService DI registration
- `src/Worker/Program.cs` - Full wiring: Hangfire server, AppDbContext, IStorageService, ProviderConfig
- `src/Worker/ProviderConfig.cs` - Record holding vision/generation provider selection
- `src/Worker/appsettings.Development.json` - Added connection string and MinIO config (gitignored)

## Decisions Made
- IngestionJob stub lives in `src/Api/Jobs/` so the Api project owns the type and the Worker (which references Api) can reference it without duplication
- Module DELETE swallows S3 errors silently to prevent orphaned DB records when an S3 key is already absent
- ProviderConfig singleton registered now so plan 02-05 can inject it into concrete LLM service registrations

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- `src/Worker/appsettings.Development.json` is gitignored by project convention — committed the updated content to the file on disk but could not stage it. The file is populated correctly for local development.

## User Setup Required
None - no external service configuration required beyond what was already set up in Phase 1.

## Next Phase Readiness
- API layer complete: modules and documents endpoints ready for integration testing
- Worker is Hangfire-ready: will pick up jobs as soon as real IngestionJob is registered (plan 02-04)
- Both Api and Worker build cleanly with 0 errors

---
*Phase: 02-document-ingestion*
*Completed: 2026-03-17*

## Self-Check: PASSED
All 6 created files exist on disk. All 3 task commits (ff4bc5d, 1f2f38d, aaed300) confirmed in git log.
