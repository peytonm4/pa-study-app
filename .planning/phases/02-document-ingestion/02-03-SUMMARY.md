---
phase: 02-document-ingestion
plan: "03"
subsystem: extraction
tags: [tdd, pptx, pdf, openxml, pdfpig, xunit, worker]

# Dependency graph
requires:
  - phase: 02-document-ingestion
    plan: "01"
    provides: NuGet packages (DocumentFormat.OpenXml 3.4.1, PdfPig 0.1.13) installed; Wave 0 test stubs in place
  - phase: 02-document-ingestion
    plan: "02"
    provides: Worker DI foundation; StorageService; Hangfire wiring

provides:
  - IPptxExtractor interface with Extract(Stream, string) returning IEnumerable<SlideContent>
  - PptxExtractor: OpenXml-based implementation extracting body text + speaker notes per slide
  - IPdfExtractor interface with Extract(Stream, string) returning IEnumerable<PageContent>
  - PdfExtractor: PdfPig-based implementation using GetWords() with NeedsVision blank-page flag
  - SlideContent record (SlideNumber, FileName, BodyText, NotesText)
  - PageContent record (PageNumber, FileName, Text, NeedsVision)
  - IVisionProvider/StubVisionProvider stub wired (plan 02-05 adds real Gemini impl)
  - IGenerationProvider/StubGenerationProvider stub wired (plan 02-05 adds real impls)
  - Both extractors registered as AddScoped in Worker DI
  - Worker ProjectReference added to Api.Tests for extractor type access

affects:
  - 02-04 (IngestionJob injects IPptxExtractor and IPdfExtractor)
  - 02-05 (IVisionProvider/IGenerationProvider interfaces already in place)
  - 02-06 (Worker jobs use extraction results to create Chunks)

# Tech tracking
tech-stack:
  added:
    - Google.GenAI.Client (corrected from GoogleAI; 1.5.0 API uses Client not GoogleAI)
  patterns:
    - Copy incoming Stream to MemoryStream before passing to PresentationDocument.Open (avoids ObjectDisposedException)
    - PdfPig: open from byte[] not Stream; use page.GetWords() not raw page.Letters for reading order
    - NeedsVision = !words.Any() (no words = image-only page)
    - TDD cycle: RED commit → GREEN commit per feature pair
    - Worker ProjectReference added to Api.Tests to test Worker-owned types

key-files:
  created:
    - src/Worker/Extraction/IPptxExtractor.cs
    - src/Worker/Extraction/PptxExtractor.cs
    - src/Worker/Extraction/IPdfExtractor.cs
    - src/Worker/Extraction/PdfExtractor.cs
    - src/Worker/Providers/IVisionProvider.cs
    - src/Worker/Providers/StubVisionProvider.cs
    - src/Worker/Providers/IGenerationProvider.cs
    - src/Worker/Providers/StubGenerationProvider.cs
    - src/Worker/Providers/GeminiVisionProvider.cs
  modified:
    - src/Api.Tests/Extraction/PptxExtractorTests.cs (Wave 0 stubs replaced with real assertions)
    - src/Api.Tests/Extraction/PdfExtractorTests.cs (Wave 0 stubs replaced with real assertions)
    - src/Api.Tests/StudyApp.Api.Tests.csproj (added Worker ProjectReference)
    - src/Worker/Program.cs (IPptxExtractor/IPdfExtractor AddScoped registrations)

key-decisions:
  - "Worker ProjectReference added to Api.Tests so extractor types can be used directly in tests without duplication"
  - "PdfPig accepts byte[] not Stream in PdfDocument.Open — copy to byte[] first"
  - "Google.GenAI 1.5.0 uses Client class (not GoogleAI); Blob.Data is byte[] not base64 string"

patterns-established:
  - "Stream safety: copy to MemoryStream/byte[] before passing to third-party document parsers"
  - "GetWords() over raw Letters: word grouping heuristics give correct reading order"
  - "TDD fixture: programmatic in-memory PPTX via DocumentFormat.OpenXml; minimal PDF via byte literals"

requirements-completed: [INGEST-03, INGEST-04, INGEST-05, INGEST-06, INGEST-07]

# Metrics
duration: 7min
completed: 2026-03-18
---

# Phase 2 Plan 03: PPTX and PDF Extraction (TDD) Summary

**PPTX body+notes extraction via DocumentFormat.OpenXml and PDF text/vision-flag extraction via PdfPig.GetWords(), both TDD-driven with programmatic in-memory fixtures and registered in Worker DI**

## Performance

- **Duration:** 7 min
- **Started:** 2026-03-18T03:32:59Z
- **Completed:** 2026-03-18T03:39:49Z
- **Tasks:** 2 (RED + GREEN)
- **Files modified:** 13

## Accomplishments

- Replaced all 8 Wave 0 extraction test stubs with real assertions using programmatic PPTX/PDF fixtures
- PptxExtractor reads slide body text and speaker notes from NotesSlidePart per research pattern
- PdfExtractor uses GetWords() (not raw Letters) for correct reading-order text; flags image-only pages with NeedsVision=true
- Both extractors registered in Worker DI (AddScoped) ready for IngestionJob injection in plan 02-04
- StubVisionProvider and StubGenerationProvider stubs created so provider Wave 0 tests compile

## Task Commits

Each task was committed atomically:

1. **RED: Failing tests for PPTX and PDF extraction** - `7a02f0a` (test)
2. **GREEN: Implement PptxExtractor and PdfExtractor** - `2055b4b` (feat)

## Files Created/Modified

- `src/Worker/Extraction/IPptxExtractor.cs` - Interface: Extract(Stream, string) → IEnumerable<SlideContent>
- `src/Worker/Extraction/PptxExtractor.cs` - OpenXml implementation; copies to MemoryStream; iterates SlideIdList
- `src/Worker/Extraction/IPdfExtractor.cs` - Interface: Extract(Stream, string) → IEnumerable<PageContent>
- `src/Worker/Extraction/PdfExtractor.cs` - PdfPig implementation; reads to byte[]; GetWords() for text; NeedsVision flag
- `src/Worker/Providers/IVisionProvider.cs` - Interface for plan 02-05
- `src/Worker/Providers/StubVisionProvider.cs` - Returns placeholder text (Wave 0 stub impl)
- `src/Worker/Providers/IGenerationProvider.cs` - Interface for plan 02-05
- `src/Worker/Providers/StubGenerationProvider.cs` - Returns deterministic [Stub] fallback
- `src/Worker/Providers/GeminiVisionProvider.cs` - Fixed: GoogleAI→Client, base64→raw bytes
- `src/Api.Tests/Extraction/PptxExtractorTests.cs` - 4 real tests replacing Wave 0 stubs
- `src/Api.Tests/Extraction/PdfExtractorTests.cs` - 4 real tests replacing Wave 0 stubs
- `src/Api.Tests/StudyApp.Api.Tests.csproj` - Worker ProjectReference added
- `src/Worker/Program.cs` - IPptxExtractor/IPdfExtractor AddScoped registrations

## Decisions Made

- Worker ProjectReference added to Api.Tests so tests can reference extractor types directly without duplication
- PdfDocument.Open requires byte[] (not Stream) — copy pdfStream to MemoryStream then .ToArray() before opening
- Google.GenAI 1.5.0: the main client class is `Client` (not `GoogleAI`); `Blob.Data` is `byte[]` not base64 string

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added Worker ProjectReference to Api.Tests**
- **Found during:** RED task (writing tests that reference Worker types)
- **Issue:** Api.Tests only referenced Api; extractor interfaces live in Worker; tests wouldn't compile
- **Fix:** Added `<ProjectReference Include="..\Worker\StudyApp.Worker.csproj" />` to Api.Tests.csproj
- **Files modified:** `src/Api.Tests/StudyApp.Api.Tests.csproj`
- **Verification:** Build succeeds
- **Committed in:** `7a02f0a` (RED commit)

**2. [Rule 3 - Blocking] Created provider stubs (IVisionProvider, StubVisionProvider, IGenerationProvider, StubGenerationProvider)**
- **Found during:** RED task build
- **Issue:** Adding Worker ProjectReference exposed existing Wave 0 provider tests that reference `StudyApp.Worker.Providers` namespace (types not yet created); build failed with CS0234
- **Fix:** Created IVisionProvider, StubVisionProvider, IGenerationProvider, StubGenerationProvider interfaces and implementations so those tests compile
- **Files modified:** 4 new files in `src/Worker/Providers/`
- **Verification:** Build succeeds; provider tests still throw NotImplementedException correctly (Wave 0)
- **Committed in:** `7a02f0a` (RED commit)

**3. [Rule 1 - Bug] Fixed GeminiVisionProvider.cs compile errors**
- **Found during:** RED task build (pre-existing file with wrong Google.GenAI 1.5.0 API usage)
- **Issue:** (a) Class used `GoogleAI` which doesn't exist in Google.GenAI 1.5.0 — correct class is `Client`; (b) `Blob.Data` is `byte[]` but code passed `Convert.ToBase64String(bytes)` which returns `string`
- **Fix:** Changed constructor parameter from `GoogleAI client` to `Client client`; changed `Data` assignment from `Convert.ToBase64String(imageBytes)` to `imageBytes`
- **Files modified:** `src/Worker/Providers/GeminiVisionProvider.cs`
- **Verification:** Build succeeds; no CS0246 or CS0029 errors
- **Committed in:** `7a02f0a` (RED commit)

---

**Total deviations:** 3 auto-fixed (2 blocking, 1 bug)
**Impact on plan:** All fixes necessary for compilation. No scope creep — provider stubs were already required by Wave 0 tests created in plan 02-01.

## Issues Encountered

- PdfPig's `PdfDocument.Open(Stream)` requires a seekable stream. Wrapping in byte[] via MemoryStream.ToArray() is the safest approach. Documented in PdfExtractor as pattern.
- Pre-existing Wave 0 stubs in DocumentUploadTests (INGEST-01/02) and ProviderConfigTests (LLM-03) still throw NotImplementedException — these are for plans 02-02 and 02-05 respectively and are expected failures in this plan's scope.

## User Setup Required

None — tests run without Docker or external services.

## Next Phase Readiness

- IPptxExtractor and IPdfExtractor ready for injection into IngestionJob (plan 02-04)
- SlideContent and PageContent records defined with all required fields
- IVisionProvider and IGenerationProvider interfaces in place for plan 02-05
- All 8 extraction tests passing; 4 provider stub tests still failing (Wave 0, will be filled in 02-05)

---
*Phase: 02-document-ingestion*
*Completed: 2026-03-18*
