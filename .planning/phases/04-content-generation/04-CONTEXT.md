# Phase 4: Content Generation - Context

**Gathered:** 2026-03-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Generate grounded study materials for every section produced by the lecture extractor. For each section the app produces: a study guide page, flashcards, a micro-quiz, and (for algorithmic sections only) a concept map. All content is traceable to uploaded source chunks — if evidence is insufficient, the app shows "Not found in your sources" rather than hallucinating. Phase ends when the full generation pipeline runs end-to-end in stub mode and all content is stored in the DB.

Study experience (browsing and guided sessions) is Phase 5.

</domain>

<decisions>
## Implementation Decisions

### Generation trigger
- Manual "Generate Study Materials" button on module detail page, enabled once extraction status is Ready
- Scope: whole module at once — one button, one status indicator, all sections fan out in parallel
- Re-running lecture extraction resets generation: wipes all existing generated content for the module so stale materials don't persist after sections change
- Generation does NOT auto-start after extraction — user reviews the extracted lecture first, then chooses to generate

### Job architecture
- `ContentGenerationJob` (orchestrator): enqueues one `SectionGenerationJob` per section in the module
- `SectionGenerationJob` (worker): generates all content types for one section (study guide, flashcards, quiz, concept map if applicable)
- One Hangfire job per section = parallel processing; one section's failure doesn't block others
- Mirrors the vision extraction fan-out pattern from Phase 2

### LLM integration
- C# calls the LLM directly via `IGenerationProvider` — no Python FastAPI sidecar for Phase 4
- `IGenerationProvider` interface added to `src/Api/Providers/` (mirrors `IVisionProvider` pattern already in place)
- Pluggable providers: Claude, Gemini, Stub — switched via environment variable (same as `VISION_PROVIDER`)
- Concept map is a **separate LLM call** from the study guide — different prompt, different response format (Mermaid), and can be skipped for non-algorithmic sections without affecting other content
- FastAPI sidecar remains deferred — add only if generation logic grows complex enough to warrant it

### Stub mode
- `StubGenerationProvider` returns deterministic fake content based on section heading text
- Stub study guide, flashcards, quiz, and concept map are all populated so the full UI can be tested without API keys
- Same pattern as Phase 3 stub scripts

### Content storage — DB entities
- **`StudyGuide`** — one row per section: `DirectAnswer`, `HighYieldDetails`, `KeyTablesJson`, `MustKnowNumbers`, `SourcesJson` as separate columns (structured, not a blob)
- **`Flashcard`** — individual rows per card: `Front`, `Back`, `CardType` (cloze/qa), `SourceRefsJson`, `SortOrder`, FK to Section
- **`QuizQuestion`** — individual rows per question: `QuestionText`, `ChoicesJson`, `CorrectAnswer`, `SourceRef`, `SortOrder`, FK to Section
- **`ConceptMap`** — one row per algorithmic section: `MermaidSyntax`, `SourceNodeRefsJson`, FK to Section
- Individual rows (not JSON blobs) for flashcards and quiz questions so Phase 5 can track per-card and per-question progress

### Generation status tracking
- **`GenerationRun`** entity — mirrors `ExtractionRun` pattern: `ModuleId`, `Status` (Queued → Processing → Ready / Failed), `ErrorMessage`, `CreatedAt`, `UpdatedAt`
- Status stored as string via `HasConversion<string>()` — consistent with `ExtractionStatus` and `DocumentStatus` patterns
- One `GenerationRun` per generation attempt; re-generation creates a new run

### Re-generation behavior
- Re-generation is allowed: same "Generate Study Materials" button is available after generation completes
- Re-running deletes all existing `StudyGuide`, `Flashcard`, `QuizQuestion`, and `ConceptMap` rows for the module, then starts fresh — no versioning
- Partial failure: `GenerationRun` status is set to Failed with error info, but content for successfully generated sections is kept — user can re-run to fill in gaps

### Claude's Discretion
- Exact prompt templates for study guide, flashcards, quiz, and concept map
- Algorithmic content detection heuristics (keywords: algorithm, flowchart, workup, stepwise, if/then) — threshold for triggering concept map generation
- JSON response schema for LLM output parsing
- Flashcard count per section (target range from requirements: 5–10 cards)
- Quiz question count per section (target range: 3–7 questions)
- Hangfire job retry policy for generation jobs

</decisions>

<specifics>
## Specific Ideas

- "Not found in your sources" must appear when evidence is insufficient — this is a hard requirement, not optional polish
- Concept maps should only appear for sections where algorithmic content is genuinely detected — don't generate for every section
- The GenerationRun pattern should feel identical to ExtractionRun from the user's perspective (same status lifecycle, same polling behavior)

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ExtractionRun` entity + `ExtractionStatus` enum: `GenerationRun` follows the same pattern exactly
- `IVisionProvider` / `GeminiVisionProvider`: `IGenerationProvider` mirrors this interface shape
- `LectureExtractionJob`: `ContentGenerationJob` (orchestrator) follows the same Hangfire job structure
- `VisionExtractionJob` fan-out: `ContentGenerationJob` enqueuing per-section `SectionGenerationJob` mirrors how `VisionExtractionJob` fans out per page
- `ModuleDetailPage` trigger button + polling pattern: already has "Run Extraction" button with status polling — "Generate Study Materials" is the same pattern
- `ProviderRegistration` extension method: new `IGenerationProvider` registration follows the same env-var DI switch

### Established Patterns
- Hangfire jobs: enqueued from Controller (or orchestrator job), executed in Worker
- Status lifecycle via `HasConversion<string>()` for DB readability
- React Query `refetchInterval` polling for background job status
- `[AutomaticRetry(Attempts = 1)]` on jobs
- Test isolation: each mutating test uses a dedicated seeded resource (established in Phase 3 hardening)

### Integration Points
- `ModulesController`: add POST `/modules/{id}/generate` endpoint to trigger generation run
- `ModuleDetailPage`: new "Generate Study Materials" section with status indicator and polling
- Worker: register `ContentGenerationJob` and `SectionGenerationJob` in DI
- New EF Core migration for `StudyGuide`, `Flashcard`, `QuizQuestion`, `ConceptMap`, `GenerationRun` tables
- `Section` entity (already exists) is the FK parent for all generated content entities

</code_context>

<deferred>
## Deferred Ideas

- FastAPI sidecar for Python skills — still deferred; add only if generation logic becomes complex enough to warrant it
- Per-section regeneration button — Phase 4 generates whole module; granular per-section control is a future enhancement
- Content versioning (keeping previous generations) — out of scope for MVP; silent replace is sufficient
- Student-editable generated content (editing flashcards, adjusting quiz) — future phase

</deferred>

---

*Phase: 04-content-generation*
*Context gathered: 2026-03-24*
