---
phase: 03-objectives-and-figures
plan: 01
subsystem: testing
tags: [xunit, tdd, stubs, wave0, skills, figures, extraction]

# Dependency graph
requires:
  - phase: 02-document-ingestion
    provides: Api.Tests project with WebApplicationFactory, stub patterns, Extraction/ folder
provides:
  - 7 Wave 0 xUnit test stub files covering Skills, Figures, and Extraction subsystems
  - Test class names matching VALIDATION.md filter patterns for plans 03-02 through 03-07
affects:
  - 03-02-PLAN.md (Skills implementation — references ProcessSkillRunnerTests, StubSkillRunnerTests, SkillProviderConfigTests)
  - 03-03-PLAN.md (FigureExtractionJobTests)
  - 03-04-PLAN.md (FigureToggleTests)
  - 03-05-PLAN.md (LectureExtractionJobTests)
  - 03-06-PLAN.md (LectureExtractionTriggerTests)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Wave 0 stub pattern: [Fact(Skip='Wave 0 stub — implement in plan 03')] with void body — compiles clean, discovered by runner, skipped until implementation plan"

key-files:
  created:
    - src/Api.Tests/Skills/ProcessSkillRunnerTests.cs
    - src/Api.Tests/Skills/StubSkillRunnerTests.cs
    - src/Api.Tests/Skills/SkillProviderConfigTests.cs
    - src/Api.Tests/Figures/FigureExtractionJobTests.cs
    - src/Api.Tests/Figures/FigureToggleTests.cs
    - src/Api.Tests/Extraction/LectureExtractionJobTests.cs
    - src/Api.Tests/Extraction/LectureExtractionTriggerTests.cs
  modified: []

key-decisions:
  - "Stub files use only `using Xunit;` — no references to unimplemented types, so compilation never breaks before implementation plans run"
  - "Skills/ and Figures/ directories created fresh; Extraction/ already existed from Phase 2"

patterns-established:
  - "Wave 0 stub: minimal file with Xunit only, Skip-attributed facts, void bodies — safe to build and run at any time"

requirements-completed: [SKILL-01, SKILL-02, SKILL-03, FIG-01, FIG-03, FIG-04, LEXT-01, LEXT-04]

# Metrics
duration: 5min
completed: 2026-03-18
---

# Phase 3 Plan 01: Wave 0 Test Stubs Summary

**7 xUnit stub files created across Skills, Figures, and Extraction — all discovered and skipped by dotnet test, zero compilation errors, Nyquist rule satisfied for plans 03-02 through 03-07**

## Performance

- **Duration:** 5 min
- **Started:** 2026-03-18T20:09:50Z
- **Completed:** 2026-03-18T20:14:42Z
- **Tasks:** 2
- **Files modified:** 7 created, 0 modified

## Accomplishments

- Created `src/Api.Tests/Skills/` directory with 3 stub test classes (6 skipped facts) covering SKILL-01/02/03
- Created `src/Api.Tests/Figures/` directory with 2 stub test classes (3 skipped facts) covering FIG-01/03/04
- Added 2 stub test classes to existing `src/Api.Tests/Extraction/` covering LEXT-01/04
- Full test suite: 21 passed, 12 skipped, 0 failed — exit 0

## Task Commits

Each task was committed atomically:

1. **Task 1: Create Skills test stub files** - `1c23533` (test)
2. **Task 2: Create Figures and Extraction test stub files** - `9cf816c` (test)

## Files Created/Modified

- `src/Api.Tests/Skills/ProcessSkillRunnerTests.cs` - Stubs for SKILL-01 (process invocation, non-zero exit)
- `src/Api.Tests/Skills/StubSkillRunnerTests.cs` - Stubs for SKILL-02/03 (deterministic manifest, deterministic sections)
- `src/Api.Tests/Skills/SkillProviderConfigTests.cs` - Stubs for SKILL-03 env var selection (stub vs real runner)
- `src/Api.Tests/Figures/FigureExtractionJobTests.cs` - Stubs for FIG-01/03 (manifest parsing, caption-to-keep)
- `src/Api.Tests/Figures/FigureToggleTests.cs` - Stubs for FIG-04 (PATCH keep field toggle)
- `src/Api.Tests/Extraction/LectureExtractionJobTests.cs` - Stubs for LEXT-04 (sections insert, docx upload)
- `src/Api.Tests/Extraction/LectureExtractionTriggerTests.cs` - Stubs for LEXT-01 (POST /extract enqueue 202)

## Decisions Made

- Stub files use only `using Xunit;` with no references to unimplemented types, ensuring compilation is always clean before implementation plans run.
- `Skills/` and `Figures/` directories were created new; `Extraction/` already existed from Phase 2 (PdfExtractorTests.cs, PptxExtractorTests.cs).

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- All 7 Wave 0 stub files exist with class names matching VALIDATION.md filter patterns
- Every subsequent plan's `<automated>` verify step has a real test class to reference
- Ready for 03-02 (Skills infrastructure implementation)

---
*Phase: 03-objectives-and-figures*
*Completed: 2026-03-18*

## Self-Check: PASSED

- All 7 stub files: FOUND
- SUMMARY.md: FOUND
- Commit 1c23533 (Task 1): FOUND
- Commit 9cf816c (Task 2): FOUND
