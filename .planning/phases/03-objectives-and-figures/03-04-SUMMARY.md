---
phase: 03-objectives-and-figures
plan: "04"
subsystem: api
tags: [hangfire, efcore, python-skill, figure-extraction, image-proxy]

requires:
  - phase: 03-objectives-and-figures
    provides: ISkillRunner interface and StubSkillRunner/ProcessSkillRunner implementations
  - phase: 03-objectives-and-figures
    provides: Figure entity model and AppDbContext.Figures DbSet

provides:
  - FigureExtractionJob — Hangfire job that calls extract_images.py, inserts Figure rows with Keep/Caption
  - FiguresController — GET figures list, PATCH toggle Keep, GET thumbnail proxy endpoints
  - IngestionJob enqueues FigureExtractionJob when document reaches Ready status

affects: [03-05-lecture-extraction, frontend-figures-ui]

tech-stack:
  added: []
  patterns:
    - FigureExtractionJob follows VisionExtractionJob pattern — ISkillRunner + temp file + EF inserts
    - FiguresController follows DocumentsController route pattern — [ApiController][Route("api")]
    - Thumbnail endpoint proxies S3 bytes directly (no presigned URLs) — consistent with storage abstraction

key-files:
  created:
    - src/Api/Jobs/FigureExtractionJob.cs
    - src/Api/Controllers/FiguresController.cs
    - src/Api.Tests/Figures/FigureExtractionJobTests.cs (fully implemented — was Wave 0 stub)
    - src/Api.Tests/Figures/FigureToggleTests.cs (fully implemented — was Wave 0 stub)
  modified:
    - src/Api/Jobs/IngestionJob.cs — enqueues FigureExtractionJob at Ready transition
    - src/Worker/Program.cs — registers FigureExtractionJob in DI

key-decisions:
  - "FigureExtractionJob uses IConfiguration key Skills:BasePath (default: src/skills) for script resolution — consistent with LectureExtractionJob pattern"
  - "FiguresController uses proxy thumbnail URL /api/figures/{id}/thumbnail — no presigned S3 URLs; matches plan spec"
  - "GET /api/modules/{id}/figures returns 404 when module not found — explicit check before figure query"

patterns-established:
  - "Skill jobs: ISkillRunner.RunAsync + temp file write + JSON deserialization + EF AddRange + SaveChanges"
  - "Caption population: download image bytes via IStorageService, call IVisionProvider.ExtractTextAsync for Keep=true figures only"

requirements-completed: [FIG-01, FIG-02, FIG-03, FIG-04, FIG-05]

duration: 5min
completed: 2026-03-18
---

# Phase 03 Plan 04: FigureExtractionJob and FiguresController Summary

**Hangfire job extracting figures from documents via Python skill with Keep/Caption persistence, plus REST endpoints for figure list, toggle, and thumbnail proxy.**

## Performance

- **Duration:** 5 min
- **Started:** 2026-03-18T20:39:11Z
- **Completed:** 2026-03-18T20:44:27Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments

- FigureExtractionJob calls extract_images.py, deserializes FigureManifest, inserts Figure rows (Keep=true when has_caption=true), populates Caption via IVisionProvider for kept figures
- FiguresController provides GET /api/modules/{id}/figures, PATCH /api/figures/{id}, GET /api/figures/{id}/thumbnail
- IngestionJob now enqueues FigureExtractionJob when a document reaches Ready status (no vision jobs path)

## Task Commits

Each task was committed atomically:

1. **test(03-04): add failing tests for FigureExtractionJob** - `13df2da` (test - RED)
2. **feat(03-04): implement FigureExtractionJob and wire into IngestionJob** - `78f2abc` (feat - GREEN)
3. **test(03-04): add failing tests for FiguresController endpoints** - `efbf1aa` (test - RED)
4. **feat(03-04): implement FiguresController** - `d4be50f` (feat - GREEN)

_Note: TDD tasks have separate test (RED) and feat (GREEN) commits._

## Files Created/Modified

- `src/Api/Jobs/FigureExtractionJob.cs` — Hangfire job: calls extract_images.py via ISkillRunner, inserts Figure rows, populates captions
- `src/Api/Controllers/FiguresController.cs` — GET figures list, PATCH toggle, GET thumbnail proxy endpoints
- `src/Api/Jobs/IngestionJob.cs` — enqueues FigureExtractionJob when document reaches Ready
- `src/Worker/Program.cs` — registers FigureExtractionJob as Scoped service
- `src/Api.Tests/Figures/FigureExtractionJobTests.cs` — 3 unit tests for job behavior
- `src/Api.Tests/Figures/FigureToggleTests.cs` — 3 integration tests via WebApplicationFactory

## Decisions Made

- `ISkillRunner` lives in `StudyApp.Api.Skills` namespace (moved in plan 03-03) — FigureExtractionJob references it from there without circular dependency
- Thumbnail URL in FigureDto is the API proxy path `/api/figures/{id}/thumbnail` (not a direct S3 URL)
- `FigureExtractionJob` accepts `IConfiguration` for `Skills:BasePath` — same pattern as `LectureExtractionJob`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Fixed pre-existing ambiguous Document reference in LectureExtractionJob**
- **Found during:** Task 1 build (FigureExtractionJob implementation)
- **Issue:** `LectureExtractionJob.cs` line 101 had `new Document(new Body())` — ambiguous between `StudyApp.Api.Models.Document` and `DocumentFormat.OpenXml.Wordprocessing.Document`; blocked solution build
- **Fix:** Qualified as `new DocumentFormat.OpenXml.Wordprocessing.Document(new Body())`
- **Files modified:** `src/Api/Jobs/LectureExtractionJob.cs`
- **Verification:** Solution build succeeds with 0 errors after fix
- **Committed in:** `78f2abc` (Task 1 feat commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Required to unblock Task 1 build. No scope creep.

## Issues Encountered

None beyond the auto-fixed blocking issue.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- FIG-01 through FIG-05 complete: figures are extracted, stored, and queryable via API
- Ready for plan 03-05: LectureExtractionJob and LectureController implementation
- Frontend can now display figure thumbnails and toggle Keep state

---
*Phase: 03-objectives-and-figures*
*Completed: 2026-03-18*
