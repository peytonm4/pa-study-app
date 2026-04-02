---
phase: 04-content-generation
plan: "03"
subsystem: api
tags: [hangfire, json, stub, generation, tdd, csharp]

# Dependency graph
requires:
  - phase: 04-content-generation plan 02
    provides: StudyGuide, Flashcard, QuizQuestion, ConceptMap models and DbSets

provides:
  - SectionGenerationJob: 4 LLM calls per section, algorithmic detection, DB persistence
  - StubGenerationProvider: type-aware JSON for all 4 content types
  - IGenerationProvider: interface moved to Api.Providers (accessible to Api.Jobs)
  - 13 passing tests: SectionGenerationJobTests (6), AlgorithmicDetectionTests (3), StubGenerationProviderTests (4)
  - 4 GenerationTriggerTests (Wave 0 stubs replaced with real tests)

affects: [04-04-generation-trigger, phase-5-study-ui]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - IGenerationProvider moved to StudyApp.Api.Providers (mirrors IVisionProvider pattern — avoids circular project dependency)
    - SectionGenerationJob uses internal DTOs (StudyGuideDto, FlashcardDto, etc.) co-located in same file
    - IsAlgorithmic marked internal static; InternalsVisibleTo("StudyApp.Api.Tests") added to Api.csproj
    - StubGenerationProvider keyword detection: "study guide", "flashcard", "quiz", "concept map"/"mermaid"

key-files:
  created:
    - src/Api/Providers/IGenerationProvider.cs
    - src/Api/Jobs/SectionGenerationJob.cs (replaced stub)
    - src/Api.Tests/Generation/SectionGenerationJobTests.cs (replaced Wave 0 stubs)
    - src/Api.Tests/Generation/AlgorithmicDetectionTests.cs (replaced Wave 0 stubs)
  modified:
    - src/Worker/Providers/StubGenerationProvider.cs
    - src/Worker/Providers/ClaudeGenerationProvider.cs
    - src/Worker/Providers/GeminiGenerationProvider.cs
    - src/Worker/Providers/IGenerationProvider.cs (emptied — interface moved to Api.Providers)
    - src/Api.Tests/Generation/StubGenerationProviderTests.cs (replaced Wave 0 stubs)
    - src/Api.Tests/Generation/GenerationTriggerTests.cs (replaced Wave 0 stubs)
    - src/Api/StudyApp.Api.csproj (added InternalsVisibleTo)

key-decisions:
  - "IGenerationProvider moved to StudyApp.Api.Providers to avoid circular project dependency (Worker references Api, not vice versa)"
  - "IsAlgorithmic detects: algorithm, flowchart, workup, stepwise, if/then, if then — all 6 keywords from requirements"
  - "StubGenerationProvider uses prompt keyword detection so SectionGenerationJob can run stub mode without API keys"
  - "QuizPrompt must contain 'quiz' keyword for StubGenerationProvider to return correct JSON schema"

patterns-established:
  - "IGenerationProvider in Api.Providers: same pattern as IVisionProvider — interface in Api, implementations in Worker"
  - "InternalsVisibleTo Api.Tests pattern for exposing internal helpers to unit tests without making them public"

requirements-completed: [GEN-01, GEN-02, GEN-03, GEN-04, GEN-05, GEN-06, GEN-07, GEN-08]

# Metrics
duration: 35min
completed: 2026-04-02
---

# Phase 4 Plan 03: SectionGenerationJob and StubGenerationProvider Summary

**SectionGenerationJob with 4 LLM calls per section (study guide, flashcards, quiz, concept map) and IsAlgorithmic keyword detection gate for Mermaid concept maps**

## Performance

- **Duration:** 35 min
- **Started:** 2026-04-02T06:15:00Z
- **Completed:** 2026-04-02T06:50:00Z
- **Tasks:** 2
- **Files modified:** 13

## Accomplishments
- SectionGenerationJob generates all 4 content types via IGenerationProvider with error handling that marks GenerationRun.Failed on exception
- StubGenerationProvider returns valid, deserializable JSON for each content type based on prompt keyword detection
- 17 new tests pass (6 SectionGenerationJob, 3 AlgorithmicDetection, 4 StubGenerationProvider, 4 GenerationTrigger)
- Full pipeline runs in stub mode without any API keys

## Task Commits

Each task was committed atomically:

1. **Task 1: Expand StubGenerationProvider** - `51f9981` (feat)
2. **Task 2: SectionGenerationJob + tests** - `9e45ce4` (feat — committed by prior plan)

**Plan metadata:** (docs commit below)

## Files Created/Modified
- `src/Api/Providers/IGenerationProvider.cs` - Interface moved from Worker.Providers to Api.Providers to avoid circular dependency
- `src/Api/Jobs/SectionGenerationJob.cs` - Full implementation replacing Wave 0 stub
- `src/Worker/Providers/StubGenerationProvider.cs` - Keyword-aware JSON responses
- `src/Worker/Providers/IGenerationProvider.cs` - Emptied (interface moved to Api.Providers)
- `src/Worker/Providers/ClaudeGenerationProvider.cs` - Updated to implement Api.Providers.IGenerationProvider
- `src/Worker/Providers/GeminiGenerationProvider.cs` - Updated to implement Api.Providers.IGenerationProvider
- `src/Api.Tests/Generation/SectionGenerationJobTests.cs` - 6 tests, Wave 0 stubs replaced
- `src/Api.Tests/Generation/AlgorithmicDetectionTests.cs` - 3 tests, Wave 0 stubs replaced
- `src/Api.Tests/Generation/StubGenerationProviderTests.cs` - 4 tests, Wave 0 stubs replaced
- `src/Api.Tests/Generation/GenerationTriggerTests.cs` - 4 tests, Wave 0 stubs replaced
- `src/Api/StudyApp.Api.csproj` - Added InternalsVisibleTo("StudyApp.Api.Tests")

## Decisions Made
- Moved IGenerationProvider to Api.Providers (not Api.Generation) to match the IVisionProvider pattern already established in the codebase
- InternalsVisibleTo added via csproj AssemblyAttribute (not a source file) to expose IsAlgorithmic for unit tests
- QuizPrompt explicitly includes "quiz" keyword in prompt text so StubGenerationProvider returns correct JSON schema

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Moved IGenerationProvider to Api.Providers**
- **Found during:** Task 1 (StubGenerationProvider expansion)
- **Issue:** Plan assumed SectionGenerationJob could reference IGenerationProvider from StudyApp.Worker.Providers, but Api does not reference Worker — only Worker references Api. Circular dependency would result.
- **Fix:** Created IGenerationProvider in src/Api/Providers/ (StudyApp.Api.Providers namespace), updated all Worker providers to implement the Api interface. Emptied the Worker.Providers.IGenerationProvider file.
- **Files modified:** src/Api/Providers/IGenerationProvider.cs (created), src/Worker/Providers/IGenerationProvider.cs (emptied), ClaudeGenerationProvider.cs, GeminiGenerationProvider.cs, StubGenerationProvider.cs
- **Verification:** dotnet build succeeds on all projects; all Generation tests pass
- **Committed in:** 51f9981

**2. [Rule 1 - Bug] Fixed ContentGenerationJob CancellationToken null compile error**
- **Found during:** Task 1 (first build attempt)
- **Issue:** ContentGenerationJob.cs line 50 passed null for CancellationToken parameter — CS1503 compile error
- **Fix:** Changed null to CancellationToken.None
- **Files modified:** src/Api/Jobs/ContentGenerationJob.cs
- **Committed in:** 51f9981

**3. [Rule 1 - Bug] Fixed QuizPrompt missing "quiz" keyword**
- **Found during:** Task 2 (GREEN phase — test failures)
- **Issue:** QuizPrompt text said "multiple-choice questions" without "quiz" keyword, so StubGenerationProvider fell to fallback and returned non-JSON string; deserialization as QuizResponse failed
- **Fix:** Added "quiz" to prompt text: "Generate 3-7 quiz questions (multiple-choice)..."
- **Files modified:** src/Api/Jobs/SectionGenerationJob.cs
- **Committed in:** 9e45ce4

---

**Total deviations:** 3 auto-fixed (2 blocking, 1 bug)
**Impact on plan:** All fixes essential for correctness. No scope creep.

## Issues Encountered
- Raw string interpolation (`$"""`) in C# requires `$$"""` when prompt templates contain literal `{` and `}` JSON examples — linter auto-converted to string concatenation style which resolves the issue cleanly

## Next Phase Readiness
- SectionGenerationJob ready for ContentGenerationJob orchestration (Plan 04-04 already implemented)
- Stub pipeline complete: ContentGenerationJob -> SectionGenerationJob -> StubGenerationProvider -> DB
- Plan 04-05 (study content API endpoints) can be implemented

---
*Phase: 04-content-generation*
*Completed: 2026-04-02*
