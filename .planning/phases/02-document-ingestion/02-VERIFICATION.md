---
phase: 02-document-ingestion
verified: 2026-03-18T18:00:00Z
status: passed
score: 10/10 must-haves verified
re_verification: false
---

# Phase 2: Document Ingestion Verification Report

**Phase Goal:** User can upload PPTX and PDF files and the app produces indexed, searchable chunks
**Verified:** 2026-03-18T18:00:00Z
**Status:** PASSED
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths

| #  | Truth | Status | Evidence |
|----|-------|--------|----------|
| 1  | User can upload PPTX files and receive 202 | VERIFIED | `DocumentsController.UploadDocument` validates PPTX content type, saves to S3 via `IStorageService`, returns `Accepted(202)`; `DocumentUploadTests.PostDocument_WithPptx_Returns202` passes |
| 2  | User can upload PDF files and receive 202 | VERIFIED | Same controller path validates PDF content type; `DocumentUploadTests.PostDocument_WithPdf_Returns202` passes |
| 3  | App extracts text from PPTX slides including speaker notes | VERIFIED | `PptxExtractor.Extract` reads `A.Text` descendants for body + `NotesSlidePart.NotesSlide` for notes; 4 passing tests including `ExtractSlides_ReturnsSpeakerNotes` and `ExtractSlides_ReturnsSlideBodyText` |
| 4  | App extracts text layer from PDFs | VERIFIED | `PdfExtractor.Extract` uses `page.GetWords()` (not raw Letters) and joins words with space; `ExtractPages_ReturnsTextFromTextLayerPage` passes |
| 5  | App detects PDF pages with no text layer and routes for vision | VERIFIED | `PdfExtractor` sets `NeedsVision = !words.Any()`; `IngestionJob` collects flagged pages and enqueues `VisionExtractionJob` per page; `ExtractPages_FlagsBlankPageForVision` passes |
| 6  | Each slide/page becomes a Chunk with FileName and PageNumber metadata | VERIFIED | `IngestionJob` creates `Chunk` records with `FileName`, `PageNumber`, `Content`, `IsVisionExtracted`; backed by EF `Chunk` entity and `AddModulesDocumentsChunks` migration |
| 7  | Vision extraction (stub mode) runs on flagged pages without manual intervention | VERIFIED | `VisionExtractionJob` calls `IVisionProvider.ExtractTextAsync`, updates `Chunk.Content`, atomically decrements `PendingVisionJobs` via `ExecuteUpdateAsync`; sets `Status=Ready` when count reaches 0 |
| 8  | Content generation providers selectable via environment variable | VERIFIED | `ProviderRegistration.AddProviders()` switches on `VISION_PROVIDER` and `GENERATION_PROVIDER` env vars; 3 passing `ProviderConfigTests` confirm stub selection and missing-var default |
| 9  | User can view modules and documents status in the UI | VERIFIED | `ModuleListPage` and `ModuleDetailPage` fully implemented with `useQuery` + TanStack Query; per-document polling via `refetchInterval` stops on `Ready`/`Failed` |
| 10 | Frontend upload flow triggers backend and reflects status | VERIFIED | `documents.ts` `upload()` posts multipart form to `/modules/{moduleId}/documents`; `App.tsx` routes `/modules` and `/modules/:id` to correct pages; `DocumentRow` polls `getStatus` every 3s until terminal |

**Score:** 10/10 truths verified

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `src/Api/Models/Module.cs` | Module entity (Id, UserId, Name, CreatedAt, ICollection<Document>) | VERIFIED | All fields present; `ICollection<Document> Documents = []` |
| `src/Api/Models/Document.cs` | Document entity with DocumentStatus enum | VERIFIED | All fields present including `S3Key`, `PendingVisionJobs`, `ContentType` |
| `src/Api/Models/DocumentStatus.cs` | Enum: Uploading/Queued/Processing/Ready/Failed | VERIFIED | Exists as separate file per summary |
| `src/Api/Models/Chunk.cs` | Chunk entity (Id, DocumentId, FileName, PageNumber, Content, IsVisionExtracted) | VERIFIED | All required fields present |
| `src/Api/Data/AppDbContext.cs` | DbSets for Modules, Documents, Chunks with cascade deletes | VERIFIED | DbSets present; cascade delete configured for Module→Document and Document→Chunk; `DocumentStatus` stored as string |
| `src/Api/Migrations/20260318032333_AddModulesDocumentsChunks.cs` | EF migration for new entities | VERIFIED | Migration file present in `src/Api/Migrations/` |
| `src/Api/Services/IStorageService.cs` | Storage abstraction with UploadAsync, DeleteAsync, DownloadAsync | VERIFIED | All three methods present; DownloadAsync added in plan 02-05 |
| `src/Api/Services/S3StorageService.cs` | S3/MinIO implementation | VERIFIED | Implements all three IStorageService methods via AWSSDK.S3 |
| `src/Api/Controllers/ModulesController.cs` | GET/POST/DELETE /modules + GET /modules/{id} | VERIFIED | All 4 endpoints present and [Authorize]-gated with user ownership checks |
| `src/Api/Controllers/DocumentsController.cs` | POST /modules/{id}/documents, GET /documents/{id}/status, DELETE /documents/{id} | VERIFIED | All 3 endpoints present; PPTX+PDF validation via HashSet; Hangfire enqueue wired |
| `src/Api/Jobs/IngestionJob.cs` | Hangfire job: Execute(Guid documentId) | VERIFIED | Full implementation: S3 download, PPTX/PDF dispatch, Chunk persistence, vision fan-out, Status updates |
| `src/Api/Jobs/VisionExtractionJob.cs` | Hangfire job: Execute(Guid documentId, int pageNumber) | VERIFIED | Full implementation: IVisionProvider call, Chunk update, atomic PendingVisionJobs decrement via ExecuteUpdateAsync |
| `src/Api/Extraction/IPptxExtractor.cs` | IPptxExtractor interface + SlideContent record | VERIFIED | Defined in Api.Extraction namespace (moved from Worker for layering) |
| `src/Api/Extraction/IPdfExtractor.cs` | IPdfExtractor interface + PageContent record | VERIFIED | Defined in Api.Extraction namespace |
| `src/Api/Providers/IVisionProvider.cs` | IVisionProvider interface | VERIFIED | Defined in Api.Providers namespace (moved from Worker) |
| `src/Worker/Extraction/PptxExtractor.cs` | OpenXml implementation of IPptxExtractor | VERIFIED | Uses `PresentationDocument.Open`, iterates `SlideIdList`, extracts body text and NotesSlidePart; copies stream to MemoryStream first |
| `src/Worker/Extraction/PdfExtractor.cs` | PdfPig implementation of IPdfExtractor using GetWords() | VERIFIED | Reads to byte[], uses `page.GetWords()`, sets `NeedsVision = !words.Any()` |
| `src/Worker/Providers/StubVisionProvider.cs` | Returns exact placeholder string | VERIFIED | Returns `"[Figure: vision extraction not available in stub mode]"` |
| `src/Worker/Providers/StubGenerationProvider.cs` | Returns deterministic stub content | VERIFIED | Returns `$"[Stub] Generated content for: {prompt[..50]}"` |
| `src/Worker/Providers/GeminiVisionProvider.cs` | Google.GenAI implementation | VERIFIED | Compiles; uses `Client` (not `GoogleAI`); model from `Gemini:VisionModel` config |
| `src/Worker/Providers/ClaudeGenerationProvider.cs` | Anthropic SDK implementation | VERIFIED | Compiles; injects `IAnthropicClient`; model from `Claude:Model` config |
| `src/Worker/Providers/GeminiGenerationProvider.cs` | Google.GenAI generation implementation | VERIFIED | Compiles; registered in ProviderRegistration switch |
| `src/Worker/ProviderRegistration.cs` | AddProviders() extension method | VERIFIED | Env-var switch for both vision and generation providers; used from Worker/Program.cs and tests |
| `src/Worker/Program.cs` | Hangfire server, AppDbContext, IStorageService, providers, jobs registered | VERIFIED | All DI registrations present: `AddHangfireServer`, `AddDbContext`, `AddScoped<IStorageService>`, `AddProviders`, `AddScoped<IngestionJob>`, `AddScoped<VisionExtractionJob>` |
| `src/Api.Tests/Documents/DocumentUploadTests.cs` | Real integration tests for INGEST-01, INGEST-02 | VERIFIED | `TestWebApplicationFactory` with InMemory EF, `StubStorageService`, `StubJobClient`; 2 tests pass |
| `src/Api.Tests/Extraction/PptxExtractorTests.cs` | 4 real tests for INGEST-03, INGEST-06, INGEST-07 | VERIFIED | Wave 0 stubs replaced; programmatic PPTX fixtures; 4 tests pass |
| `src/Api.Tests/Extraction/PdfExtractorTests.cs` | 4 real tests for INGEST-04, INGEST-05, INGEST-07 | VERIFIED | Wave 0 stubs replaced; programmatic PDF byte fixtures; 4 tests pass |
| `src/Api.Tests/Providers/VisionProviderTests.cs` | 1 test for LLM-01 | VERIFIED | Asserts exact placeholder string |
| `src/Api.Tests/Providers/GenerationProviderTests.cs` | 2 tests for LLM-02 | VERIFIED | Asserts `[Stub]` prefix and determinism |
| `src/Api.Tests/Providers/ProviderConfigTests.cs` | 3 tests for LLM-03 | VERIFIED | Asserts stub registered on `stub` env var and missing env var default |
| `src/Frontend/src/api/modules.ts` | list, get, create, delete API functions | VERIFIED | All 4 methods implemented; `ModuleDetail` type extends `Module` with `documents` array |
| `src/Frontend/src/api/documents.ts` | upload, getStatus, delete API functions | VERIFIED | All 3 methods implemented; `DocumentStatus` type with all 5 status values |
| `src/Frontend/src/pages/ModuleListPage.tsx` | Module list with create form and status badges | VERIFIED | Full implementation with `useQuery`, `useMutation`, `Badge` variants, inline create form |
| `src/Frontend/src/pages/ModuleDetailPage.tsx` | Module detail with per-document polling and upload | VERIFIED | `DocumentRow` sub-component with independent polling; `refetchInterval` stops on terminal status |
| `src/Frontend/src/App.tsx` | / → /modules redirect, /modules, /modules/:id routes | VERIFIED | All 3 routes wired to correct pages |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `DocumentsController` | `IStorageService` | constructor injection | VERIFIED | `_storage` field injected; `UploadAsync` called on upload |
| `DocumentsController` | `IBackgroundJobClient` (Hangfire) | constructor injection | VERIFIED | `_jobClient.Enqueue<IngestionJob>(j => j.Execute(document.Id))` present |
| `IngestionJob` | `IPptxExtractor` | constructor injection, dispatched by ContentType | VERIFIED | `pptxExtractor.Extract(memStream, document.FileName)` called when `ContentType == PptxContentType` |
| `IngestionJob` | `IPdfExtractor` | constructor injection, dispatched by ContentType | VERIFIED | `pdfExtractor.Extract(memStream, document.FileName)` called when `ContentType == PdfContentType` |
| `IngestionJob` | `VisionExtractionJob` (Hangfire fan-out) | `jobClient.Enqueue<VisionExtractionJob>` | VERIFIED | `jobClient.Enqueue<VisionExtractionJob>(j => j.Execute(documentId, page.PageNumber))` for each `NeedsVision` page |
| `VisionExtractionJob` | `IVisionProvider` | constructor injection, ExtractTextAsync called | VERIFIED | `visionProvider.ExtractTextAsync(pdfBytes, "application/pdf")` called; result assigned to `chunk.Content` |
| `VisionExtractionJob` | `AppDbContext.Documents` (atomic decrement) | `ExecuteUpdateAsync` | VERIFIED | `ExecuteUpdateAsync(s => s.SetProperty(d => d.PendingVisionJobs, d => d.PendingVisionJobs - 1))` present |
| `Worker/Program.cs` | `IVisionProvider` (env var switch) | `AddProviders()` / VISION_PROVIDER | VERIFIED | `ProviderRegistration.AddProviders(builder.Configuration)` registered; switches on `VISION_PROVIDER` |
| `Worker/Program.cs` | `IGenerationProvider` (env var switch) | `AddProviders()` / GENERATION_PROVIDER | VERIFIED | Same `AddProviders()` call switches on `GENERATION_PROVIDER` |
| `AppDbContext` | `Module`, `Document`, `Chunk` entities | `DbSet<T>` properties | VERIFIED | `DbSet<Module>`, `DbSet<Document>`, `DbSet<Chunk>` all present with EF relationship config |
| `Document` | `Module` | navigation property | VERIFIED | `public Module Module { get; set; } = null!` with FK `ModuleId` |
| `Chunk` | `Document` | navigation property | VERIFIED | `public Document Document { get; set; } = null!` with FK `DocumentId` |
| `ModuleDetailPage` | `documents.getStatus` (polling) | `useQuery` refetchInterval | VERIFIED | `DocumentRow` uses `refetchInterval: (query) => (terminal ? false : 3000)` |
| `ModuleDetailPage` | `documents.upload` (mutation) | `useMutation` + hidden file input | VERIFIED | `uploadMutation.mutate(file)` triggered on `handleFileChange` |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| INGEST-01 | 02-01, 02-02, 02-06 | User can upload PPTX files | SATISFIED | `DocumentsController` validates PPTX content type; `DocumentUploadTests.PostDocument_WithPptx_Returns202` passes; frontend `ModuleDetailPage` upload wired |
| INGEST-02 | 02-01, 02-02, 02-06 | User can upload PDF files | SATISFIED | Same controller path for PDF; `PostDocument_WithPdf_Returns202` passes |
| INGEST-03 | 02-01, 02-03 | App extracts text from PPTX slides (OpenXML SDK) | SATISFIED | `PptxExtractor` uses `DocumentFormat.OpenXml`; `ExtractSlides_ReturnsSlideBodyText` passes |
| INGEST-04 | 02-01, 02-03 | App extracts text layer from PDFs when available | SATISFIED | `PdfExtractor` uses `PdfPig.GetWords()`; `ExtractPages_ReturnsTextFromTextLayerPage` passes |
| INGEST-05 | 02-01, 02-03 | App detects PDF pages with no text layer and routes for vision | SATISFIED | `PdfExtractor` sets `NeedsVision = !words.Any()`; `IngestionJob` enqueues `VisionExtractionJob` for flagged pages; `ExtractPages_FlagsBlankPageForVision` passes |
| INGEST-06 | 02-01, 02-03 | App extracts speaker notes from PPTX slides | SATISFIED | `PptxExtractor` reads `NotesSlidePart.NotesSlide`; `ExtractSlides_ReturnsSpeakerNotes` passes; `IngestionJob` appends notes to chunk content |
| INGEST-07 | 02-01, 02-03 | App creates chunks per slide/page with metadata (file name, slide/page number) | SATISFIED | `IngestionJob` creates `Chunk` records with `FileName = slide.FileName`, `PageNumber = slide.SlideNumber`; `ExtractSlides_SetsCorrectSlideNumbers` and `ExtractPages_SetsCorrectPageNumbers` pass |
| LLM-01 | 02-01, 02-04, 02-05 | Vision extraction uses Gemini (PDF OCR, figure captions) | SATISFIED | `GeminiVisionProvider` compiles and registers; `StubVisionProvider` tested; `VisionExtractionJob` calls `IVisionProvider.ExtractTextAsync`; `StubVisionProvider_ReturnsPlaceholderText` passes |
| LLM-02 | 02-01, 02-04 | Content generation supports pluggable providers (Claude, Gemini, Stub) | SATISFIED | `ClaudeGenerationProvider`, `GeminiGenerationProvider`, `StubGenerationProvider` all compile and register; `ProviderRegistration.AddProviders()` switches between them; `StubGenerationProvider_ReturnsDeterministicFallback` passes |
| LLM-03 | 02-01, 02-04 | Provider configured via environment variables | SATISFIED | `AddProviders(config)` reads `VISION_PROVIDER` and `GENERATION_PROVIDER`; 3 `ProviderConfigTests` verify stub selection and missing-var default |

**All 10 Phase 2 requirements SATISFIED.**

---

### Anti-Patterns Found

No stubs, `NotImplementedException` throws, placeholder returns, or `TODO`/`FIXME` comments found in any production source files. All Wave 0 test stubs were replaced with real assertions.

Build warnings (informational only):
- `PptxExtractor.cs` lines 19, 24, 27, 33 — nullable dereference warnings (CS8602/CS8604) on OpenXML `SlideIdList` and `GetPartById`. These are low-severity null-safety warnings in a null-safe codebase; the code handles null `NotesSlidePart` correctly with a null check on line 30. Not blocking.

---

### Human Verification Required

The following items require a running stack to confirm. All were previously verified live in plan 02-07 per the summary (confirmed by the user against the live stack), so they are documented here for completeness rather than as blockers.

**1. Full ingestion pipeline end-to-end**

**Test:** Upload a multi-slide PPTX with speaker notes. Navigate to the module detail page.
**Expected:** Document status transitions Uploading → Queued → Processing → Ready; chunks stored in Postgres (verify with `SELECT * FROM "Chunks"` or via a future API endpoint).
**Why human:** Requires Docker (Postgres + MinIO), running API, and running Worker. Cannot verify DB row insertion programmatically without a live DB connection in this context.

**2. Blank PDF page vision extraction (stub mode)**

**Test:** Upload a PDF containing at least one image-only page. Confirm the Worker processes it.
**Expected:** Page chunk content updates to `"[Figure: vision extraction not available in stub mode]"`; document status eventually reaches Ready.
**Why human:** Requires the Worker to process a Hangfire job end-to-end; requires a real PDF with image-only pages.

**3. Real Gemini vision provider**

**Test:** Set `VISION_PROVIDER=gemini` with a valid Gemini API key; upload a PDF with scanned pages.
**Expected:** `GeminiVisionProvider.ExtractTextAsync` calls Gemini API; returns actual OCR text for each image page.
**Why human:** Requires a paid Gemini API key. Not testable in stub mode.

---

### Test Run Summary

```
Passed! - Failed: 0, Passed: 21, Skipped: 0, Total: 21, Duration: 3s
```

21 tests covering:
- 2 integration tests (DocumentUploadTests — WebApplicationFactory + InMemory EF)
- 4 unit tests (PptxExtractorTests)
- 4 unit tests (PdfExtractorTests)
- 1 unit test (VisionProviderTests)
- 2 unit tests (GenerationProviderTests)
- 3 unit tests (ProviderConfigTests)
- 5 pre-existing tests from Phase 1

Build: 0 errors, 4 nullable warnings in PptxExtractor (non-blocking).

---

### Architecture Note

The interfaces `IPptxExtractor`, `IPdfExtractor`, and `IVisionProvider` were intentionally moved from `StudyApp.Worker.*` to `StudyApp.Api.*` namespaces during plan 02-05 to break a circular project dependency (IngestionJob and VisionExtractionJob live in Api/Jobs, which would create a cycle if they imported from Worker). The Worker `Extraction/IPptxExtractor.cs` and `Providers/IVisionProvider.cs` files are intentionally empty comment stubs directing to the authoritative locations. This is the correct layered-architecture pattern.

---

*Verified: 2026-03-18T18:00:00Z*
*Verifier: Claude (gsd-verifier)*
