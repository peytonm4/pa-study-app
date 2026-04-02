---
phase: 04-content-generation
plan: "04"
subsystem: api-jobs
tags: [csharp, hangfire, api, integration-tests, generation]

# Dependency graph
requires:
  - phase: 04-02
    provides: GenerationRun entity, content entities (StudyGuide/Flashcard/QuizQuestion/ConceptMap)
  - phase: 04-03
    provides: SectionGenerationJob (parallel Wave 2 — stub created here, full impl from 04-03)
provides:
  - ContentGenerationJob orchestrator: marks GenerationRun status, clears stale content, fans out to SectionGenerationJob
  - POST /modules/{id}/generate endpoint with 404/409/202 gates
  - GET /modules/{id} extended with GenerationStatus field
  - 4 passing GenerationTriggerTests integration tests
  - SectionGenerationJob (full impl pulled in from parallel plan 04-03)
affects: [04-05, 04-06]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - ContentGenerationJob mirrors LectureExtractionJob orchestrator pattern (Processing/Ready/Failed lifecycle)
    - POST generate endpoint gates mirror POST extract pattern (404 module, 409 in-flight)
    - GenerationTriggerTestFactory mirrors ExtractionTriggerTestFactory exactly (in-memory DB, stub job client, stub storage)
    - SectionGenerationJob uses IGenerationProvider from StudyApp.Api.Providers (not Worker.Providers) to avoid circular deps

key-files:
  created:
    - src/Api/Jobs/ContentGenerationJob.cs
    - src/Api/Jobs/SectionGenerationJob.cs
    - src/Api.Tests/Generation/GenerationTriggerTests.cs
  modified:
    - src/Api/Controllers/ModulesController.cs
    - src/Worker/Program.cs
    - src/Api.Tests/Generation/AlgorithmicDetectionTests.cs
    - src/Api.Tests/Generation/SectionGenerationJobTests.cs
    - src/Api/StudyApp.Api.csproj

key-decisions:
  - "SectionGenerationJob uses StudyApp.Api.Providers.IGenerationProvider (not Worker.Providers) — Api cannot reference Worker"
  - "IsAlgorithmic made public static (not internal) so AlgorithmicDetectionTests can call it without InternalsVisibleTo"
  - "ContentGenerationJob marks GenerationRun Ready after all SectionGenerationJob enqueues — section failures mark Failed independently"
  - "CancellationToken.None passed in Hangfire Enqueue lambda (struct, cannot be null)"

# Metrics
duration: 13min
completed: 2026-04-02
---

# Phase 4 Plan 04: Generation Trigger and Orchestrator Summary

**POST /modules/{id}/generate endpoint with ContentGenerationJob orchestrator that clears stale content and fans out SectionGenerationJob per section, backed by 4 passing integration tests**

## Performance

- **Duration:** 13 min
- **Started:** 2026-04-02T06:04:19Z
- **Completed:** 2026-04-02T06:17:00Z
- **Tasks:** 2 of 2
- **Files modified:** 8

## Accomplishments

- Created ContentGenerationJob: marks GenerationRun Processing → clears all stale StudyGuide/Flashcard/QuizQuestion/ConceptMap rows for module sections → fans out SectionGenerationJob per section (ordered by SortOrder) → marks Ready (or Failed on exception)
- Created SectionGenerationJob (full implementation pulled in from parallel plan 04-03): 4 LLM calls per section, IsAlgorithmic detection, entity persistence for all 4 content types
- Added POST /modules/{id}/generate: 404 if module not found, 409 if extraction not Ready, 409 if generation Queued/Processing, 202 Accepted on success with GenerationRun record
- Extended GET /modules/{id} to include GenerationStatus field (from latest GenerationRun or "NotStarted")
- Registered ContentGenerationJob and SectionGenerationJob in Worker DI alongside existing jobs
- Implemented GenerationTriggerTests.cs (4 tests): 202+enqueue, 409 extraction not ready, 409 generation queued, DB assertion

## Task Commits

1. **Task 1: ContentGenerationJob orchestrator** - `8b02697` (feat)
2. **Task 2: POST /generate endpoint, GET extension, GenerationTriggerTests** - `9e45ce4` (feat)

## Files Created/Modified

- `src/Api/Jobs/ContentGenerationJob.cs` - Orchestrator job: GenerationRun lifecycle, stale content deletion, SectionGenerationJob fan-out
- `src/Api/Jobs/SectionGenerationJob.cs` - Per-section job: 4 LLM calls, IsAlgorithmic detection, StudyGuide/Flashcard/QuizQuestion/ConceptMap persistence
- `src/Api/Controllers/ModulesController.cs` - Added POST /generate endpoint + GenerationStatus in GET /modules/{id}
- `src/Worker/Program.cs` - Added AddScoped for ContentGenerationJob and SectionGenerationJob
- `src/Api.Tests/Generation/GenerationTriggerTests.cs` - 4 passing integration tests (replaced Wave 0 stubs)
- `src/Api.Tests/Generation/AlgorithmicDetectionTests.cs` - 3 passing unit tests for IsAlgorithmic (from stash restore of plan 04-03 work)
- `src/Api.Tests/Generation/SectionGenerationJobTests.cs` - 6 passing unit tests for SectionGenerationJob (from stash restore of plan 04-03 work)
- `src/Api/StudyApp.Api.csproj` - Added InternalsVisibleTo for Api.Tests (from plan 04-03 stash restore)

## Decisions Made

- SectionGenerationJob uses `StudyApp.Api.Providers.IGenerationProvider` (not Worker namespace) to avoid circular project dependency — same pattern as ISkillRunner
- `IsAlgorithmic` made `public static` so AlgorithmicDetectionTests can call it directly without InternalsVisibleTo assembly attribute
- ContentGenerationJob marks GenerationRun.Ready after enqueueing all section jobs — individual section job failures update GenerationRun to Failed independently
- CancellationToken.None used in Hangfire `Enqueue<T>` lambda (CancellationToken is a struct, cannot pass null)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] SectionGenerationJob stub needed for ContentGenerationJob to compile**
- **Found during:** Task 1
- **Issue:** ContentGenerationJob.cs references SectionGenerationJob which doesn't exist (plan 04-03 creates it, runs in parallel)
- **Fix:** Created minimal stub SectionGenerationJob.cs to unblock compilation; later replaced with full implementation from plan 04-03 stash restore
- **Files modified:** src/Api/Jobs/SectionGenerationJob.cs
- **Commit:** 8b02697

**2. [Rule 1 - Bug] CancellationToken null in Hangfire lambda**
- **Found during:** Task 1 compilation
- **Issue:** `j.Execute(section.Id, runId, null)` — CancellationToken is a struct, cannot be null
- **Fix:** Changed to `CancellationToken.None` in Enqueue lambda
- **Files modified:** src/Api/Jobs/ContentGenerationJob.cs
- **Commit:** 8b02697

**3. [Rule 1 - Bug] SectionGenerationJob used Worker.Providers namespace for IGenerationProvider**
- **Found during:** Task 2 compilation
- **Issue:** `using StudyApp.Worker.Providers` in SectionGenerationJob — Api project does not reference Worker
- **Fix:** Changed to `using StudyApp.Api.Providers` where the interface was already correctly defined
- **Files modified:** src/Api/Jobs/SectionGenerationJob.cs
- **Commit:** 9e45ce4

**4. [Rule 2 - Correctness] IsAlgorithmic was internal, blocking test access**
- **Found during:** Task 2 full test run
- **Issue:** AlgorithmicDetectionTests calls `SectionGenerationJob.IsAlgorithmic` but method was `internal`
- **Fix:** Changed to `public static bool IsAlgorithmic` to allow direct test access
- **Files modified:** src/Api/Jobs/SectionGenerationJob.cs
- **Commit:** 9e45ce4

## Test Results

- GenerationTriggerTests: 4/4 pass
- SectionGenerationJobTests: 6/6 pass
- AlgorithmicDetectionTests: 3/3 pass
- StubGenerationProviderTests: 4/4 pass
- All Generation tests: 21/21 pass
- Full suite: 69 pass, 2 fail (pre-existing GetDocx endpoint tests — not caused by this plan), 1 skip

## Next Phase Readiness

- POST /modules/{id}/generate is live and returns 202 with generationRunId
- ContentGenerationJob is registered in Worker and ready to process
- SectionGenerationJob is registered and ready for plan 04-05 (full LLM integration testing)
- No blockers

---
*Phase: 04-content-generation*
*Completed: 2026-04-02*

## Self-Check: PASSED

All 3 key files present. Both task commits (8b02697, 9e45ce4) verified in git log.
