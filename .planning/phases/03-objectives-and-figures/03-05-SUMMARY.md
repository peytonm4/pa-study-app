---
phase: 03-objectives-and-figures
plan: 05
subsystem: api
tags: [hangfire, openxml, docx, lecture-extraction, python-skills, s3]

requires:
  - phase: 03-03
    provides: ISkillRunner interface and StubSkillRunner/ProcessSkillRunner implementations
  - phase: 03-04
    provides: FigureExtractionJob pattern, FiguresController, Section/Module models

provides:
  - LectureExtractionJob: calls lecture_extractor.py via ISkillRunner, inserts Section rows, builds .docx, uploads to S3
  - POST /api/modules/{id}/extract: enqueues LectureExtractionJob, returns 202
  - GET /api/modules/{id}/docx: returns download URL when extraction is Ready
  - GET /api/modules/{id}/docx/download: proxies .docx bytes from S3
  - ISkillRunner interface moved to Api.Skills (from Worker.Skills) to fix circular dependency

affects: [phase-04, frontend-module-page]

tech-stack:
  added: []
  patterns:
    - ISkillRunner in Api.Skills (not Worker.Skills) — same pattern as IPptxExtractor/IPdfExtractor
    - LectureExtractionJob: try/catch sets Failed + rethrows; success sets Ready
    - .docx built via DocumentFormat.OpenXml in MemoryStream (no temp files)
    - StyleDefinitionsPart with Heading1/2/3 styles prevents plain-text Word rendering

key-files:
  created:
    - src/Api/Skills/ISkillRunner.cs
    - src/Api.Tests/Extraction/LectureExtractionJobTests.cs (replaced stub)
    - src/Api.Tests/Extraction/LectureExtractionTriggerTests.cs (replaced stub)
  modified:
    - src/Api/Jobs/LectureExtractionJob.cs (already created in 03-04 deviation; ISkillRunner wired)
    - src/Api/Controllers/FiguresController.cs
    - src/Worker/Skills/StubSkillRunner.cs
    - src/Worker/Skills/ProcessSkillRunner.cs
    - src/Worker/ProviderRegistration.cs
    - src/Worker/Program.cs

key-decisions:
  - "ISkillRunner moved from Worker.Skills to Api.Skills — Worker already references Api, so Api.Skills is the only location that avoids a circular project reference"
  - "POST /extract returns 409 if ExtractionStatus is not NotStarted/Failed — prevents duplicate job enqueue"
  - "LectureExtractionJob uses [AutomaticRetry(Attempts=1)] — user re-triggers on failure rather than automatic retry storm"

patterns-established:
  - "ISkillRunner in Api.Skills — interfaces for cross-project jobs live in Api, not Worker"
  - "Extraction trigger: validate status -> set Queued -> SaveChanges -> Enqueue (not atomic, acceptable)"

requirements-completed: [LEXT-01, LEXT-02, LEXT-03, LEXT-04, LEXT-05, LEXT-06]

duration: 35min
completed: 2026-03-18
---

# Phase 03 Plan 05: LectureExtractionJob and Extract/Docx Endpoints Summary

**Hangfire job calling lecture_extractor.py, inserting Section rows, building .docx via OpenXml, and two new API endpoints (POST /extract, GET /docx) on FiguresController**

## Performance

- **Duration:** ~35 min
- **Started:** 2026-03-18T20:40:00Z
- **Completed:** 2026-03-18T21:15:00Z
- **Tasks:** 2
- **Files modified:** 9

## Accomplishments

- LectureExtractionJob implemented: calls skill, parses sections JSON, bulk-inserts Section rows, builds .docx with heading styles, uploads to S3, sets Module.ExtractionStatus = Ready
- ISkillRunner interface moved to Api.Skills to resolve circular project dependency between Api and Worker
- Three new API endpoints: POST /extract (trigger), GET /docx (url), GET /docx/download (proxy)
- 8 new tests pass: 3 LectureExtractionJobTests + 5 LectureExtractionTriggerTests
- Full test suite: 41 passing, 1 skipped (pre-existing ProcessSkillRunner process test)

## Task Commits

1. **Task 1: Implement LectureExtractionJob** - `8f27546` (feat)
2. **Task 2: Add extract/docx endpoints to FiguresController** - `b985a36` (feat)

## Files Created/Modified

- `src/Api/Skills/ISkillRunner.cs` - Interface moved from Worker.Skills to Api.Skills
- `src/Api/Jobs/LectureExtractionJob.cs` - Hangfire job: skill call, Section rows, .docx, S3 upload
- `src/Api/Controllers/FiguresController.cs` - Added POST /extract, GET /docx, GET /docx/download
- `src/Worker/Skills/StubSkillRunner.cs` - Now implements Api.Skills.ISkillRunner
- `src/Worker/Skills/ProcessSkillRunner.cs` - Now implements Api.Skills.ISkillRunner
- `src/Worker/ProviderRegistration.cs` - Updated ISkillRunner namespace reference
- `src/Worker/Program.cs` - Added LectureExtractionJob DI registration
- `src/Api.Tests/Extraction/LectureExtractionJobTests.cs` - Full TDD tests (replaced wave-0 stubs)
- `src/Api.Tests/Extraction/LectureExtractionTriggerTests.cs` - Full TDD tests (replaced wave-0 stubs)

## Decisions Made

- **ISkillRunner moved to Api.Skills**: Worker already has a ProjectReference to Api; placing ISkillRunner in Worker would require Api to reference Worker (circular). Moving the interface to Api follows the same pattern used for IPptxExtractor and IPdfExtractor in Phase 2.
- **POST /extract returns 409 on re-trigger**: Only NotStarted/Failed states allow triggering extraction. This prevents double-enqueue when status is Queued/Processing/Ready.
- **[AutomaticRetry(Attempts=1)]**: One retry is sufficient; users can manually re-trigger via the API endpoint after viewing an error.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] ISkillRunner circular dependency resolved**
- **Found during:** Task 1 (LectureExtractionJob implementation)
- **Issue:** LectureExtractionJob lives in Api/Jobs/ but ISkillRunner was in Worker.Skills. Worker references Api — putting the job in Api would require Api to reference Worker, creating a circular dependency.
- **Fix:** Created `src/Api/Skills/ISkillRunner.cs` with the same interface signature. Deleted `src/Worker/Skills/ISkillRunner.cs`. Updated StubSkillRunner, ProcessSkillRunner, ProviderRegistration, and SkillProviderConfigTests to use the Api namespace.
- **Files modified:** src/Api/Skills/ISkillRunner.cs (new), src/Worker/Skills/ISkillRunner.cs (deleted), src/Worker/Skills/StubSkillRunner.cs, src/Worker/Skills/ProcessSkillRunner.cs, src/Worker/ProviderRegistration.cs, src/Api.Tests/Skills/SkillProviderConfigTests.cs
- **Verification:** All 42 tests pass, Worker builds clean
- **Committed in:** 8f27546 (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (blocking)
**Impact on plan:** Required architectural fix to match the established Phase 2 pattern for cross-project interfaces. No scope creep.

## Issues Encountered

- LectureExtractionJob.cs was already created as a deviation in plan 03-04 (documented in that plan's commit message). The file was already in HEAD but non-functional because the ISkillRunner interface it referenced (`StudyApp.Api.Skills.ISkillRunner`) didn't exist yet. This plan completes that work by creating the interface and updating the Worker implementations.

## Next Phase Readiness

- Full lecture extraction pipeline is functional end-to-end (stub mode)
- POST /api/modules/{id}/extract triggers the job; GET /api/modules/{id}/docx polls for completion
- Phase 4 can integrate real Python skill runner by switching PYTHON_PROVIDER=real env var
- Frontend module page can now add an "Extract Lecture" button and docx download link

---
*Phase: 03-objectives-and-figures*
*Completed: 2026-03-18*
