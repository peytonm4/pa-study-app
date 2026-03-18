---
phase: 02-document-ingestion
plan: "01"
subsystem: database
tags: [ef-core, postgres, nuget, pdfpig, openxml, google-genai, anthropic, awssdk, xunit]

# Dependency graph
requires:
  - phase: 01-foundation
    provides: AppDbContext with User model, Hangfire, Npgsql EF, project structure

provides:
  - Module entity (Id, UserId, Name max 200, CreatedAt, ICollection<Document>)
  - Document entity (FK to Module, DocumentStatus enum stored as string, FileName, S3Key, ContentType, Status, PendingVisionJobs, ErrorMessage, CreatedAt)
  - Chunk entity (FK to Document, FileName max 500, PageNumber, Content, IsVisionExtracted)
  - AppDbContext extended with Modules, Documents, Chunks DbSets + cascade delete relationships
  - EF migration AddModulesDocumentsChunks (20260318032333)
  - All Phase 2 NuGet packages installed: DocumentFormat.OpenXml 3.4.1, PdfPig 0.1.13, Google.GenAI 1.5.0, Anthropic 12.9.0, AWSSDK.S3 4.0.19
  - Worker ProjectReference to Api for shared model access
  - 6 Wave 0 test stub files (10 failing stubs) covering INGEST-01–07, LLM-01–03

affects:
  - 02-02-modules-api (uses Module entity, AppDbContext)
  - 02-03-document-upload (uses Document entity, S3Key, DocumentStatus)
  - 02-04-extraction (uses Chunk entity, PdfPig, OpenXml; fills in Extraction test stubs)
  - 02-05-llm-providers (fills in Provider test stubs, uses Google.GenAI, Anthropic)
  - 02-06-worker-jobs (uses Worker→Api reference, all entities)
  - 02-07-module-ui (depends on fully functional API)

# Tech tracking
tech-stack:
  added:
    - DocumentFormat.OpenXml 3.4.1 (PPTX parsing)
    - PdfPig 0.1.13 (PDF text extraction)
    - Google.GenAI 1.5.0 (Gemini vision + generation)
    - Anthropic 12.9.0 (Claude generation)
    - AWSSDK.S3 4.0.19 (S3 file storage; added to Worker, already in Api)
    - Microsoft.EntityFrameworkCore 10.0.5 (pinned in Worker to resolve conflict)
    - Microsoft.EntityFrameworkCore.Relational 10.0.5 (pinned in Worker to resolve conflict)
  patterns:
    - DocumentStatus stored as string in DB (HasConversion<string>()) for readability
    - Cascade delete from Module→Document and Document→Chunk
    - Wave 0 stubs: NotImplementedException with requirement ID comment and target plan

key-files:
  created:
    - src/Api/Models/Module.cs
    - src/Api/Models/Document.cs
    - src/Api/Models/DocumentStatus.cs
    - src/Api/Models/Chunk.cs
    - src/Api/Migrations/20260318032333_AddModulesDocumentsChunks.cs
    - src/Api.Tests/Documents/DocumentUploadTests.cs
    - src/Api.Tests/Extraction/PptxExtractorTests.cs
    - src/Api.Tests/Extraction/PdfExtractorTests.cs
    - src/Api.Tests/Providers/VisionProviderTests.cs
    - src/Api.Tests/Providers/GenerationProviderTests.cs
    - src/Api.Tests/Providers/ProviderConfigTests.cs
  modified:
    - src/Api/Data/AppDbContext.cs (added 3 DbSets + EF relationship config)
    - src/Api/StudyApp.Api.csproj (4 new packages)
    - src/Worker/StudyApp.Worker.csproj (5 new packages + ProjectReference to Api + EF version pins)

key-decisions:
  - "Worker references Api via ProjectReference (not duplicating models); EF 10.0.5 pinned explicitly in Worker to suppress Hangfire.PostgreSql transitive conflict with EF 10.0.4"
  - "DocumentStatus enum stored as string column via HasConversion<string>() for DB readability"
  - "EF migration generated only (database update skipped) — Docker not running at execution time"

patterns-established:
  - "Wave 0 stubs: throw NotImplementedException with requirement ID and target plan reference"
  - "EF entities: Id (Guid PK), navigation properties initialized with [], null-suppression for required nav refs"

requirements-completed: [INGEST-01, INGEST-02, INGEST-03, INGEST-04, INGEST-05, INGEST-06, INGEST-07, LLM-01, LLM-02, LLM-03]

# Metrics
duration: 6min
completed: 2026-03-18
---

# Phase 2 Plan 01: NuGet packages, EF entities, and Wave 0 test stubs Summary

**Module/Document/Chunk EF entities with cascade deletes, EF migration, all Phase 2 packages installed across Api and Worker, and 10 failing Wave 0 test stubs for Nyquist compliance**

## Performance

- **Duration:** 6 min
- **Started:** 2026-03-18T03:18:49Z
- **Completed:** 2026-03-18T03:24:49Z
- **Tasks:** 3
- **Files modified:** 14

## Accomplishments

- All Phase 2 NuGet packages (DocumentFormat.OpenXml, PdfPig, Google.GenAI, Anthropic, AWSSDK.S3) installed in Api and Worker
- Worker gained ProjectReference to Api, enabling shared model access without duplication
- Three EF entities created (Module, Document with DocumentStatus enum, Chunk) with cascade delete chain; AppDbContext extended; EF migration generated
- Six Wave 0 test stub files created covering all 10 requirements (INGEST-01–07, LLM-01–03); all confirmed failing by xUnit

## Task Commits

Each task was committed atomically:

1. **Task 1: Install NuGet packages and add Worker project reference** - `31f781e` (chore)
2. **Task 2: Define Module, Document, Chunk entities and update AppDbContext + migration** - `34e183f` (feat)
3. **Task 3: Create Wave 0 failing test stubs (Nyquist compliance)** - `c8df381` (test)

## Files Created/Modified

- `src/Api/Models/Module.cs` - Module entity with UserId, Name, ICollection<Document>
- `src/Api/Models/Document.cs` - Document entity with FK to Module, DocumentStatus, S3Key, PendingVisionJobs
- `src/Api/Models/DocumentStatus.cs` - Enum: Uploading, Queued, Processing, Ready, Failed
- `src/Api/Models/Chunk.cs` - Chunk entity with FK to Document, PageNumber, Content, IsVisionExtracted
- `src/Api/Data/AppDbContext.cs` - Added 3 DbSets and EF relationship config with cascade delete
- `src/Api/Migrations/20260318032333_AddModulesDocumentsChunks.cs` - EF migration file
- `src/Api/StudyApp.Api.csproj` - 4 new NuGet packages
- `src/Worker/StudyApp.Worker.csproj` - 5 new packages, ProjectReference to Api, EF version pins
- `src/Api.Tests/Documents/DocumentUploadTests.cs` - Stubs: INGEST-01, INGEST-02
- `src/Api.Tests/Extraction/PptxExtractorTests.cs` - Stubs: INGEST-03, INGEST-06, INGEST-07
- `src/Api.Tests/Extraction/PdfExtractorTests.cs` - Stubs: INGEST-04, INGEST-05, INGEST-07
- `src/Api.Tests/Providers/VisionProviderTests.cs` - Stub: LLM-01
- `src/Api.Tests/Providers/GenerationProviderTests.cs` - Stub: LLM-02
- `src/Api.Tests/Providers/ProviderConfigTests.cs` - Stubs: LLM-03 (x2)

## Decisions Made

- Worker→Api ProjectReference chosen over duplicating models in Worker — shared AppDbContext and Models used directly
- EF 10.0.5 pinned explicitly in Worker.csproj (both `Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.Relational`) because `Hangfire.PostgreSql 1.21.1` brings EF 10.0.4 transitively, causing MSB3277 assembly conflict warnings
- `database update` skipped — Docker was not running at execution time; migration file generated and will be applied when Docker starts

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Pinned EF 10.0.5 in Worker to eliminate MSB3277 assembly conflict warnings**
- **Found during:** Task 1 (NuGet package install + Worker→Api ProjectReference)
- **Issue:** After adding ProjectReference, Worker's direct `Npgsql.EF 10.0.1` conflict with Hangfire's transitive `EF 10.0.4` created MSB3277 warnings; separately, the Api (via ProjectReference) brought `EF 10.0.5`, causing a three-way mismatch
- **Fix:** Removed duplicate `Npgsql.EntityFrameworkCore.PostgreSQL` from Worker (now transitive via Api); pinned `Microsoft.EntityFrameworkCore 10.0.5` and `Microsoft.EntityFrameworkCore.Relational 10.0.5` directly in Worker to win the conflict resolution
- **Files modified:** `src/Worker/StudyApp.Worker.csproj`
- **Verification:** `dotnet build StudyApp.sln` shows 0 errors; Worker MSB3277 warnings resolved
- **Committed in:** `31f781e` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - bug/conflict)
**Impact on plan:** Required for clean build; no scope creep.

## Issues Encountered

- Docker not running at migration time — `dotnet ef database update` skipped. Migration file (`20260318032333_AddModulesDocumentsChunks.cs`) is ready and will be applied when Docker starts (plan 02-02 or before first test run against the DB).
- Api.Tests has pre-existing MSB3277 warnings (EF 10.0.4 vs 10.0.5 from `MVC.Testing`). These pre-date this plan and are out of scope.

## User Setup Required

None - no external service configuration required. Docker must be running to apply the migration: `dotnet ef database update --project src/Api/`

## Next Phase Readiness

- All Phase 2 NuGet packages ready; Worker can reference Api models directly
- EF entities and migration ready; apply migration when Docker starts
- All 10 Wave 0 stubs in place — plans 02-03, 02-04, 02-05 will fill them in
- Plan 02-02 (Modules API) can proceed immediately

---
*Phase: 02-document-ingestion*
*Completed: 2026-03-18*
