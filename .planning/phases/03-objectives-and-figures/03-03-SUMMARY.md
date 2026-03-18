---
phase: 03-objectives-and-figures
plan: 03
subsystem: worker
tags: [csharp, skills, subprocess, dependency-injection, python, tdd]

# Dependency graph
requires:
  - phase: 03-01
    provides: Wave 0 test stubs for Skills (ProcessSkillRunnerTests, StubSkillRunnerTests, SkillProviderConfigTests)
provides:
  - ISkillRunner interface for all Python subprocess calls from Worker
  - StubSkillRunner returning deterministic JSON per script type
  - ProcessSkillRunner with deadlock-safe concurrent stdout/stderr reads
  - SkillException for non-zero process exit codes
  - PYTHON_PROVIDER env var DI switch in ProviderRegistration.AddProviders
affects: [03-04, 03-05, FigureExtractionJob, LectureExtractionJob]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - ISkillRunner abstraction mirrors IVisionProvider/IGenerationProvider patterns
    - PYTHON_PROVIDER env var switch follows existing VISION_PROVIDER/GENERATION_PROVIDER pattern
    - Concurrent stdout/stderr ReadToEndAsync with Task.WhenAll before WaitForExitAsync (deadlock prevention)
    - Temp file for Python args via Path.GetTempPath() + Guid.NewGuid() + finally delete

key-files:
  created:
    - src/Worker/Skills/ISkillRunner.cs
    - src/Worker/Skills/SkillException.cs
    - src/Worker/Skills/ProcessSkillRunner.cs
    - src/Worker/Skills/StubSkillRunner.cs
  modified:
    - src/Worker/ProviderRegistration.cs
    - src/Api.Tests/Skills/StubSkillRunnerTests.cs
    - src/Api.Tests/Skills/SkillProviderConfigTests.cs
    - src/Api.Tests/Skills/ProcessSkillRunnerTests.cs

key-decisions:
  - "ISkillRunner registered as Scoped (not Singleton) in DI — matches existing Scoped pattern for job-related services"
  - "ProcessSkillRunnerTests.RunAsync_InvokesProcess_ReturnsStdout skipped with [Fact(Skip)] for python3 CI safety; SkillException throw test runs in all environments"
  - "StubSkillRunner returns 'figures' key for extract_images paths and 'sections' key for all other paths"

patterns-established:
  - "Python skill runner pattern: ISkillRunner.RunAsync(scriptPath, inputJson) → stdout string or SkillException"
  - "Env var provider switch: PYTHON_PROVIDER=stub|real, defaults to stub"

requirements-completed: [SKILL-01, SKILL-02, SKILL-03]

# Metrics
duration: 12min
completed: 2026-03-18
---

# Phase 3 Plan 3: ISkillRunner and DI Wiring Summary

**ISkillRunner abstraction with ProcessSkillRunner (deadlock-safe subprocess) and StubSkillRunner (deterministic JSON), wired into Worker DI via PYTHON_PROVIDER env var switch**

## Performance

- **Duration:** 12 min
- **Started:** 2026-03-18T20:35:00Z
- **Completed:** 2026-03-18T20:47:00Z
- **Tasks:** 2
- **Files modified:** 8

## Accomplishments
- ISkillRunner interface, SkillException, ProcessSkillRunner, and StubSkillRunner created in src/Worker/Skills/
- StubSkillRunner returns distinct JSON shapes: `figures` key for extract_images paths, `sections` key for all others
- ProcessSkillRunner reads stdout and stderr concurrently (Task.WhenAll before WaitForExitAsync) — prevents deadlock when process writes to both streams
- PYTHON_PROVIDER env var switch added to ProviderRegistration.AddProviders (mirrors VISION_PROVIDER pattern)
- All Skills tests pass: 6 pass, 1 skipped (python3 CI guard), 0 failed

## Task Commits

1. **Task 1: ISkillRunner interface and both implementations** - `7e38704` (feat)
2. **Task 2: Wire ISkillRunner into Worker DI and fill Skills tests** - `b551c2c` (feat)

## Files Created/Modified
- `src/Worker/Skills/ISkillRunner.cs` — Interface: Task<string> RunAsync(scriptPath, inputJson, ct)
- `src/Worker/Skills/SkillException.cs` — Custom exception for non-zero process exit
- `src/Worker/Skills/ProcessSkillRunner.cs` — Real subprocess impl with deadlock-safe concurrent reads
- `src/Worker/Skills/StubSkillRunner.cs` — Deterministic stub: figures vs sections JSON per scriptPath
- `src/Worker/ProviderRegistration.cs` — Added PYTHON_PROVIDER switch registering ISkillRunner
- `src/Api.Tests/Skills/StubSkillRunnerTests.cs` — 2 passing tests (figures key, sections key)
- `src/Api.Tests/Skills/SkillProviderConfigTests.cs` — 3 passing tests (stub/unset/real DI resolution)
- `src/Api.Tests/Skills/ProcessSkillRunnerTests.cs` — SkillException throw test + skipped python3 test

## Decisions Made
- ISkillRunner registered as `AddScoped` to match existing Scoped pattern for job services
- ProcessSkillRunnerTests.RunAsync_InvokesProcess_ReturnsStdout marked `[Fact(Skip)]` — requires python3 on PATH which is not guaranteed in CI; SkillException throw test runs everywhere by creating a real temp .py file
- StubSkillRunner uses `Contains("extract_images")` path check to route between figure manifest and lecture sections JSON

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- ISkillRunner is ready for FigureExtractionJob (plan 03-04) and LectureExtractionJob (plan 03-05) to inject and call
- Set `PYTHON_PROVIDER=real` when python3 + skill scripts are available; default stub mode works without Python

---
*Phase: 03-objectives-and-figures*
*Completed: 2026-03-18*

## Self-Check: PASSED
- ISkillRunner.cs: FOUND
- ProcessSkillRunner.cs: FOUND
- StubSkillRunner.cs: FOUND
- SkillException.cs: FOUND
- SUMMARY.md: FOUND
- Commit 7e38704: FOUND
- Commit b551c2c: FOUND
