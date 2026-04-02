---
phase: 04-content-generation
plan: "02"
subsystem: database
tags: [efcore, postgres, migrations, csharp, models]

# Dependency graph
requires:
  - phase: 03-objectives-and-figures
    provides: Section entity and ExtractionRun pattern that GenerationRun mirrors
  - phase: 04-01
    provides: GenerationJob stubs that reference these entities
provides:
  - GenerationRun entity with Queued/Processing/Ready/Failed status + UpdatedAt for polling
  - StudyGuide entity with DirectAnswer, HighYieldDetailsJson, KeyTablesJson, MustKnowNumbersJson, SourcesJson
  - Flashcard entity with Front, Back, CardType, SourceRefsJson, SortOrder
  - QuizQuestion entity with QuestionText, ChoicesJson, CorrectAnswer, SourceRef, SortOrder
  - ConceptMap entity with MermaidSyntax, SourceNodeRefsJson
  - AppDbContext with 5 new DbSet properties and OnModelCreating FK/cascade config
  - EF migration AddGenerationTables (20260402055947) creating 5 new tables
affects: [04-03, 04-04, 04-05, 04-06]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - GenerationRun mirrors ExtractionRun with HasConversion<string>() for status column
    - Content entities (StudyGuide, Flashcard, QuizQuestion, ConceptMap) FK to Section with cascade delete
    - JSON stored as string columns (*Json suffix) — no JSON column type, plain text

key-files:
  created:
    - src/Api/Models/GenerationStatus.cs
    - src/Api/Models/GenerationRun.cs
    - src/Api/Models/StudyGuide.cs
    - src/Api/Models/Flashcard.cs
    - src/Api/Models/QuizQuestion.cs
    - src/Api/Models/ConceptMap.cs
    - src/Api/Migrations/20260402055947_AddGenerationTables.cs
    - src/Api/Migrations/20260402055947_AddGenerationTables.Designer.cs
  modified:
    - src/Api/Models/Module.cs
    - src/Api/Data/AppDbContext.cs
    - src/Api/Migrations/AppDbContextModelSnapshot.cs

key-decisions:
  - "GenerationRun includes UpdatedAt (not in ExtractionRun) for polling freshness in status endpoint"
  - "Content entity JSON fields stored as plain string columns with [] defaults — no Postgres JSON type"
  - "GenerationRun.Status uses HasConversion<string>() matching ExtractionStatus/DocumentStatus pattern"
  - "Postgres not running at migration time — AddGenerationTables migration file committed, DB update deferred to stack startup"

patterns-established:
  - "Content entities (StudyGuide, Flashcard, QuizQuestion, ConceptMap) all FK to Section (not Module directly)"
  - "WithMany() without navigation on Section — Section has no back-reference collection to these"

requirements-completed: [GEN-01, GEN-02, GEN-03, GEN-04, GEN-06, GEN-08]

# Metrics
duration: 4min
completed: 2026-04-02
---

# Phase 4 Plan 02: Generation Data Layer Summary

**Five EF Core content entities (GenerationRun, StudyGuide, Flashcard, QuizQuestion, ConceptMap) with AppDbContext config and AddGenerationTables migration creating the table foundation for AI content generation jobs**

## Performance

- **Duration:** 4 min
- **Started:** 2026-04-02T05:58:06Z
- **Completed:** 2026-04-02T06:01:38Z
- **Tasks:** 2 of 2
- **Files modified:** 10

## Accomplishments

- Created GenerationStatus enum and GenerationRun entity (mirrors ExtractionRun, adds UpdatedAt)
- Created 4 content entities (StudyGuide, Flashcard, QuizQuestion, ConceptMap) with Section FK + cascade delete
- Updated AppDbContext with 5 new DbSet properties and full OnModelCreating EF configuration
- Generated EF migration 20260402055947_AddGenerationTables
- Test baseline preserved: 52 pass, 2 fail (pre-existing)

## Task Commits

1. **Task 1: GenerationStatus enum, GenerationRun entity, Module.GenerationRuns nav** - `d844f0a` (feat)
2. **Task 2: Content entities, AppDbContext config, AddGenerationTables migration** - `8f35f99` (feat)

## Files Created/Modified

- `src/Api/Models/GenerationStatus.cs` - Queued/Processing/Ready/Failed enum
- `src/Api/Models/GenerationRun.cs` - Run tracking with ModuleId FK, Status, ErrorMessage, CreatedAt, UpdatedAt
- `src/Api/Models/StudyGuide.cs` - Study guide content with JSON fields for high-yield details/tables/numbers
- `src/Api/Models/Flashcard.cs` - Flashcard with Front/Back/CardType (qa|cloze)/SourceRefsJson/SortOrder
- `src/Api/Models/QuizQuestion.cs` - MCQ with ChoicesJson/CorrectAnswer/SourceRef/SortOrder
- `src/Api/Models/ConceptMap.cs` - Mermaid diagram entity with SourceNodeRefsJson
- `src/Api/Models/Module.cs` - Added GenerationRuns navigation property
- `src/Api/Data/AppDbContext.cs` - 5 new DbSet properties + OnModelCreating config for all 5 entities
- `src/Api/Migrations/20260402055947_AddGenerationTables.cs` - Creates generation_runs, study_guides, flashcards, quiz_questions, concept_maps tables

## Decisions Made

- GenerationRun adds UpdatedAt (absent from ExtractionRun) since generation jobs poll status and need freshness signal
- All JSON payload fields stored as plain string columns with `[]` defaults — consistent with existing SourcePageRefsJson pattern on Section
- GenerationRun.Status uses HasConversion<string>() to match DocumentStatus/ExtractionStatus DB-readability pattern

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- Postgres not running at execution time; `dotnet ef database update` failed with connection refused. Per plan note, migration file was committed and DB update deferred to when stack is running. No action required.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- All 5 entity tables are defined in migration; apply with `dotnet ef database update` when docker compose is up
- Plans 03 and 04 (generation jobs) can now reference GenerationRun and content entity types directly
- No blockers

---
*Phase: 04-content-generation*
*Completed: 2026-04-02*

## Self-Check: PASSED

All 8 key files present. Both task commits (d844f0a, 8f35f99) verified in git log.
