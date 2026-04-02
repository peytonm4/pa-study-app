# Phase 4: Content Generation - Research

**Researched:** 2026-04-02
**Domain:** C# Hangfire job orchestration, LLM provider pattern, EF Core entity modeling, React Query polling
**Confidence:** HIGH

## Summary

Phase 4 is a natural extension of the Phase 3 job architecture. The infrastructure for everything Phase 4 needs already exists in the codebase: `IGenerationProvider` with Claude, Gemini, and Stub implementations is already wired in `ProviderRegistration`; the fan-out Hangfire pattern is proven in `VisionExtractionJob` → `FigureExtractionJob`; and the `ExtractionRun` pattern provides an exact template for `GenerationRun`. The primary work is: (1) five new DB entities with their EF Core configuration and migration, (2) two new Hangfire jobs (`ContentGenerationJob` orchestrator + `SectionGenerationJob` per-section worker), (3) a POST endpoint to trigger generation, (4) expanded frontend polling on `ModuleDetailPage`, and (5) making `StubGenerationProvider` return structured JSON output that all content parsers can consume.

The existing `IGenerationProvider.GenerateAsync(string prompt, IEnumerable<string> sourceChunks)` signature is sufficient for all content types. Each content type (study guide, flashcards, quiz, concept map) requires its own prompt + its own JSON parsing — these are separate calls within `SectionGenerationJob`, not one mega-call.

**Primary recommendation:** Mirror `ExtractionRun` → `GenerationRun`, `LectureExtractionJob` → `ContentGenerationJob`/`SectionGenerationJob`, and `ModuleDetailPage` extraction section → generation section. The patterns are proven; Phase 4 is pattern application, not pattern invention.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Generation trigger**
- Manual "Generate Study Materials" button on module detail page, enabled once extraction status is Ready
- Scope: whole module at once — one button, one status indicator, all sections fan out in parallel
- Re-running lecture extraction resets generation: wipes all existing generated content for the module so stale materials don't persist after sections change
- Generation does NOT auto-start after extraction — user reviews the extracted lecture first, then chooses to generate

**Job architecture**
- `ContentGenerationJob` (orchestrator): enqueues one `SectionGenerationJob` per section in the module
- `SectionGenerationJob` (worker): generates all content types for one section (study guide, flashcards, quiz, concept map if applicable)
- One Hangfire job per section = parallel processing; one section's failure doesn't block others
- Mirrors the vision extraction fan-out pattern from Phase 2

**LLM integration**
- C# calls the LLM directly via `IGenerationProvider` — no Python FastAPI sidecar for Phase 4
- `IGenerationProvider` interface added to `src/Api/Providers/` (mirrors `IVisionProvider` pattern already in place)
- Pluggable providers: Claude, Gemini, Stub — switched via environment variable (same as `VISION_PROVIDER`)
- Concept map is a **separate LLM call** from the study guide — different prompt, different response format (Mermaid), and can be skipped for non-algorithmic sections without affecting other content
- FastAPI sidecar remains deferred — add only if generation logic grows complex enough to warrant it

**Stub mode**
- `StubGenerationProvider` returns deterministic fake content based on section heading text
- Stub study guide, flashcards, quiz, and concept map are all populated so the full UI can be tested without API keys
- Same pattern as Phase 3 stub scripts

**Content storage — DB entities**
- **`StudyGuide`** — one row per section: `DirectAnswer`, `HighYieldDetails`, `KeyTablesJson`, `MustKnowNumbers`, `SourcesJson` as separate columns (structured, not a blob)
- **`Flashcard`** — individual rows per card: `Front`, `Back`, `CardType` (cloze/qa), `SourceRefsJson`, `SortOrder`, FK to Section
- **`QuizQuestion`** — individual rows per question: `QuestionText`, `ChoicesJson`, `CorrectAnswer`, `SourceRef`, `SortOrder`, FK to Section
- **`ConceptMap`** — one row per algorithmic section: `MermaidSyntax`, `SourceNodeRefsJson`, FK to Section
- Individual rows (not JSON blobs) for flashcards and quiz questions so Phase 5 can track per-card and per-question progress

**Generation status tracking**
- **`GenerationRun`** entity — mirrors `ExtractionRun` pattern: `ModuleId`, `Status` (Queued → Processing → Ready / Failed), `ErrorMessage`, `CreatedAt`, `UpdatedAt`
- Status stored as string via `HasConversion<string>()` — consistent with `ExtractionStatus` and `DocumentStatus` patterns
- One `GenerationRun` per generation attempt; re-generation creates a new run

**Re-generation behavior**
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

### Deferred Ideas (OUT OF SCOPE)
- FastAPI sidecar for Python skills — still deferred; add only if generation logic becomes complex enough to warrant it
- Per-section regeneration button — Phase 4 generates whole module; granular per-section control is a future enhancement
- Content versioning (keeping previous generations) — out of scope for MVP; silent replace is sufficient
- Student-editable generated content (editing flashcards, adjusting quiz) — future phase
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| GEN-01 | App generates study guide pages per section (Direct Answer, High-Yield Details, Key Tables, Must-Know Numbers, Sources) grounded in reorganized section content | `SectionGenerationJob` calls `IGenerationProvider.GenerateAsync` with study guide prompt + section content as source chunks; parses JSON into `StudyGuide` entity |
| GEN-02 | App generates flashcards (mix of cloze + Q&A format) with source references, organized by section | Separate `IGenerationProvider` call in same `SectionGenerationJob`; returns JSON array parsed into individual `Flashcard` rows; source refs from section `SourcePageRefsJson` |
| GEN-03 | App generates micro-quizzes (3–7 questions) with source citations, organized by section | Same job, third LLM call; returns JSON array parsed into `QuizQuestion` rows with `CorrectAnswer` and `ChoicesJson` |
| GEN-04 | App generates concept maps for sections with algorithmic content (Mermaid flowchart + JSON graph) | Fourth LLM call, only when algorithmic keyword detected in heading/content; returns Mermaid syntax stored in `ConceptMap` entity |
| GEN-05 | App shows "Not found in your sources" when evidence is insufficient | Prompt instructs LLM to return sentinel string instead of hallucinating; `SectionGenerationJob` preserves sentinel value verbatim in DB |
| GEN-06 | All generated content includes source references (slide/page numbers, file names) | Section's `SourcePageRefsJson` is passed as context to each LLM call; prompt asks LLM to cite specific pages; stored in `SourcesJson`/`SourceRefsJson` columns |
| GEN-07 | Concept map generation only triggers when algorithmic content is detected | `SectionGenerationJob` runs keyword check (algorithm, flowchart, workup, stepwise, if/then) on `HeadingText` + `Content` before calling LLM |
| GEN-08 | App works in stub mode (deterministic fallbacks) without LLM API keys | `StubGenerationProvider` returns valid JSON for each content type based on section heading; full pipeline runs with `GENERATION_PROVIDER=stub` |
</phase_requirements>

---

## Standard Stack

### Core (already in codebase — no new dependencies needed)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Hangfire + Hangfire.PostgreSql | Existing | Job orchestration and fan-out | Already powering Phases 2 & 3; `[AutomaticRetry(Attempts = 1)]` pattern established |
| `IGenerationProvider` | Existing (Worker.Providers) | LLM abstraction | Claude, Gemini, Stub already wired in `ProviderRegistration` |
| EF Core 10 + Npgsql | Existing | New entity persistence | All five new tables (GenerationRun, StudyGuide, Flashcard, QuizQuestion, ConceptMap) follow established `AppDbContext` patterns |
| `System.Text.Json` | Existing | LLM response parsing | Already used in `LectureExtractionJob` for skill output; same approach for LLM JSON responses |
| React Query (`@tanstack/react-query`) | Existing | Frontend status polling | `refetchInterval` pattern proven in `ModuleDetailPage` extraction section |

### No New Dependencies Required
Phase 4 introduces no new NuGet packages or npm packages. All needed infrastructure exists. The sole risk area is if Mermaid rendering is needed in the frontend for Phase 4 (study experience display is Phase 5 — so no Mermaid frontend library needed in Phase 4).

**Installation:** None required.

---

## Architecture Patterns

### Recommended File Layout (new files only)

```
src/Api/
├── Models/
│   ├── GenerationRun.cs        # mirrors ExtractionRun exactly
│   ├── GenerationStatus.cs     # enum: Queued, Processing, Ready, Failed
│   ├── StudyGuide.cs           # one per section
│   ├── Flashcard.cs            # many per section
│   ├── QuizQuestion.cs         # many per section
│   └── ConceptMap.cs           # zero or one per section
├── Jobs/
│   ├── ContentGenerationJob.cs # orchestrator: enqueues SectionGenerationJob per section
│   └── SectionGenerationJob.cs # worker: all content types for one section
└── Migrations/
    └── [timestamp]_AddGenerationTables.cs

src/Worker/
└── Providers/
    └── (IGenerationProvider, Claude, Gemini, Stub already exist — expand StubGenerationProvider)
```

### Pattern 1: GenerationRun mirrors ExtractionRun

```csharp
// Source: existing src/Api/Models/ExtractionRun.cs
public class GenerationRun
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public Module Module { get; set; } = null!;
    public GenerationStatus Status { get; set; } = GenerationStatus.Queued;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum GenerationStatus { Queued, Processing, Ready, Failed }
```

AppDbContext configuration:
```csharp
modelBuilder.Entity<GenerationRun>(entity =>
{
    entity.HasKey(r => r.Id);
    entity.Property(r => r.Status).HasConversion<string>();
    entity.HasOne(r => r.Module)
          .WithMany(m => m.GenerationRuns)
          .HasForeignKey(r => r.ModuleId)
          .OnDelete(DeleteBehavior.Cascade);
});
```

### Pattern 2: ContentGenerationJob (orchestrator) mirrors LectureExtractionJob

```csharp
// Source: existing src/Api/Jobs/LectureExtractionJob.cs pattern
[AutomaticRetry(Attempts = 1)]
public class ContentGenerationJob(AppDbContext db, IBackgroundJobClient jobClient)
{
    public async Task Execute(Guid moduleId, Guid runId, CancellationToken ct = default)
    {
        var run = await db.GenerationRuns.FindAsync([runId], ct) ?? throw ...;
        try
        {
            run.Status = GenerationStatus.Processing;
            run.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            // Delete stale content for this module (re-generation path)
            await DeleteExistingContent(moduleId, ct);

            var sections = await db.Sections
                .Where(s => s.ModuleId == moduleId)
                .OrderBy(s => s.SortOrder)
                .ToListAsync(ct);

            foreach (var section in sections)
                jobClient.Enqueue<SectionGenerationJob>(j => j.Execute(section.Id, runId, null));

            // Note: run stays Processing — SectionGenerationJob updates it when last section done
            // Simpler: mark Ready here, rely on partial-failure semantics (GenerationRun failed but
            // successful sections persisted). See Pattern 3 for orchestrator completion.
            run.Status = GenerationStatus.Ready;
            run.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            run.Status = GenerationStatus.Failed;
            run.ErrorMessage = ex.Message;
            run.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            throw;
        }
    }
}
```

**Key design decision for orchestrator completion:** Because sections fan out in parallel with no built-in Hangfire continuation, the simplest approach that satisfies requirements is:
- `ContentGenerationJob` marks `GenerationRun` as Ready after enqueuing all sections (DB write is synchronous, enqueue is fast)
- `SectionGenerationJob` failures update `GenerationRun` to Failed independently
- This matches the stated partial-failure behavior: "content for successfully generated sections is kept"

### Pattern 3: SectionGenerationJob (per-section worker)

```csharp
[AutomaticRetry(Attempts = 1)]
public class SectionGenerationJob(AppDbContext db, IGenerationProvider generation)
{
    public async Task Execute(Guid sectionId, Guid runId, CancellationToken ct = default)
    {
        var section = await db.Sections.FindAsync([sectionId], ct) ?? throw ...;
        var sourceChunks = BuildSourceChunks(section);

        try
        {
            // 1. Study guide
            var sgJson = await generation.GenerateAsync(StudyGuidePrompt(section), sourceChunks);
            var sg = ParseStudyGuide(sgJson, sectionId);
            db.StudyGuides.Add(sg);

            // 2. Flashcards
            var fcJson = await generation.GenerateAsync(FlashcardPrompt(section), sourceChunks);
            var cards = ParseFlashcards(fcJson, sectionId);
            db.Flashcards.AddRange(cards);

            // 3. Quiz
            var qJson = await generation.GenerateAsync(QuizPrompt(section), sourceChunks);
            var questions = ParseQuizQuestions(qJson, sectionId);
            db.QuizQuestions.AddRange(questions);

            // 4. Concept map — only for algorithmic sections
            if (IsAlgorithmic(section))
            {
                var cmJson = await generation.GenerateAsync(ConceptMapPrompt(section), sourceChunks);
                var cm = ParseConceptMap(cmJson, sectionId);
                db.ConceptMaps.Add(cm);
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Update GenerationRun to Failed (partial failure preserved)
            var run = await db.GenerationRuns.FindAsync([runId], ct);
            if (run != null)
            {
                run.Status = GenerationStatus.Failed;
                run.ErrorMessage = $"Section {section.HeadingText}: {ex.Message}";
                run.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            throw;
        }
    }
}
```

### Pattern 4: Algorithmic content detection

```csharp
private static readonly string[] AlgorithmicKeywords =
    ["algorithm", "flowchart", "workup", "stepwise", "if/then", "if then"];

private static bool IsAlgorithmic(Section section)
{
    var text = $"{section.HeadingText} {section.Content}".ToLowerInvariant();
    return AlgorithmicKeywords.Any(kw => text.Contains(kw));
}
```

**Confidence:** HIGH — keyword list is in CONTEXT.md as a locked/discretion item; exact threshold is Claude's discretion.

### Pattern 5: StubGenerationProvider expansion

The current stub returns a generic string. Phase 4 requires JSON output for each content type. The stub must detect content type from the prompt and return type-appropriate JSON:

```csharp
public class StubGenerationProvider : IGenerationProvider
{
    public Task<string> GenerateAsync(string prompt, IEnumerable<string> sourceChunks)
    {
        var p = prompt.ToLowerInvariant();
        if (p.Contains("study guide"))
            return Task.FromResult(StubStudyGuideJson(prompt));
        if (p.Contains("flashcard"))
            return Task.FromResult(StubFlashcardsJson());
        if (p.Contains("quiz"))
            return Task.FromResult(StubQuizJson());
        if (p.Contains("concept map") || p.Contains("mermaid"))
            return Task.FromResult(StubConceptMapJson());
        return Task.FromResult("[Stub] " + prompt[..Math.Min(50, prompt.Length)]);
    }
    // Each helper returns valid JSON matching the real LLM response schema
}
```

### Pattern 6: Frontend polling — generation section on ModuleDetailPage

```typescript
// mirrors existing extraction polling exactly
const { data: mod } = useQuery({
  queryKey: ['module', id],
  queryFn: () => modules.get(id!),
  refetchInterval: (query) => {
    const es = query.state.data?.extractionStatus;
    const gs = query.state.data?.generationStatus;
    const running = (s: string) => s === 'Queued' || s === 'Processing';
    return running(es) || running(gs) ? 3000 : false;
  },
});
```

The GET /modules/:id response must include `generationStatus` (from latest `GenerationRun`).

### Pattern 7: API trigger endpoint

```
POST /modules/{id}/generate
```

- 404 if module not found / wrong user
- 409 if GenerationRun status is Queued or Processing
- 409 if ExtractionRun status is not Ready (can't generate without extracted sections)
- 202 Accepted with `{ generationRunId }` on success

Creates `GenerationRun`, enqueues `ContentGenerationJob`.

### Anti-Patterns to Avoid

- **One mega-LLM call for all content types:** Do not combine study guide + flashcards + quiz + concept map into a single prompt. Each content type has a different structure, and a combined call makes parsing fragile and increases hallucination risk.
- **Storing generated content as a single JSON blob on Section:** The decisions explicitly require individual `StudyGuide`, `Flashcard`, `QuizQuestion`, and `ConceptMap` rows for Phase 5 progress tracking.
- **Generating concept maps for all sections:** Keyword gate is required (GEN-07). A section about "vital signs overview" should not produce a Mermaid flowchart.
- **Hardcoding LLM model in SectionGenerationJob:** Model selection belongs in the provider implementation, not the job. Both `ClaudeGenerationProvider` and `GeminiGenerationProvider` already read model from `IConfiguration`.
- **Not passing source chunks to provider:** `IGenerationProvider.GenerateAsync` accepts `IEnumerable<string> sourceChunks`. Always pass the section's content/source refs — this is the grounding that prevents hallucination (GEN-05, GEN-06).

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Parallel section processing | Custom task coordination | Hangfire enqueue per section | Fan-out pattern already proven; Hangfire handles retry, failure isolation, worker scaling |
| Job status polling | WebSocket or SignalR push | React Query `refetchInterval` | Pattern already works for extraction; zero new complexity |
| LLM provider switching | Another DI pattern | Existing `ProviderRegistration.AddProviders()` — add `GENERATION_PROVIDER` branch | Already wired for Claude/Gemini/Stub |
| JSON response parsing | Regex or custom parser | `System.Text.Json.JsonSerializer.Deserialize<T>` with record types | Same as `LectureResult`/`SectionDto` in `LectureExtractionJob` |

---

## Common Pitfalls

### Pitfall 1: IGenerationProvider in wrong project

**What goes wrong:** Placing `IGenerationProvider` in `Worker.Providers` while jobs in `Api.Jobs` need to reference it creates a circular dependency (`Api` → `Worker` → `Api`).

**Why it happens:** The existing `IGenerationProvider` is in `src/Worker/Providers/` — but jobs like `SectionGenerationJob` are in `src/Api/Jobs/` which is compiled into the Worker via ProjectReference. The job can reference Worker types directly.

**How to avoid:** `SectionGenerationJob` lives in `src/Api/Jobs/` (so it can reference `AppDbContext`). It receives `IGenerationProvider` injected from the Worker's DI container. This is exactly how `LectureExtractionJob` uses `ISkillRunner` — the interface is in `Api.Skills` but the implementation is in `Worker.Skills`.

**Warning signs:** Compilation error "could not find type" for `IGenerationProvider` in job file — check which project the interface lives in.

### Pitfall 2: StubGenerationProvider returning plain text instead of JSON

**What goes wrong:** `SectionGenerationJob` tries to `JsonSerializer.Deserialize` the stub response and throws `JsonException`, causing all section jobs to fail in stub mode.

**How to avoid:** Expand `StubGenerationProvider` to detect content type from prompt keyword and return valid JSON matching each schema. Write a unit test that verifies stub output is deserializable for each content type.

### Pitfall 3: GenerationRun completion race condition with parallel section jobs

**What goes wrong:** If the orchestrator marks `GenerationRun.Status = Ready` only after all sections complete (e.g., using a counter), race conditions arise with no transaction. If it marks Ready immediately after enqueuing, a failing section job may try to update the same run to Failed concurrently.

**How to avoid:** The locked decision says partial failure is acceptable — keep successful section content, mark run Failed. The simplest safe approach: orchestrator marks Ready after enqueue; section jobs set Failed on exception. EF `SaveChanges` on each job independently; no shared counter needed. PostgreSQL row-level locking handles concurrent updates to the same `GenerationRun` row without deadlock.

### Pitfall 4: Missing `UpdatedAt` on GenerationRun

**What goes wrong:** `ExtractionRun` does not have `UpdatedAt`, but the locked decision for `GenerationRun` explicitly requires it for status polling freshness.

**How to avoid:** Add `UpdatedAt` to `GenerationRun` model and update it on every status change. Include it in the migration.

### Pitfall 5: Extraction status gate missing

**What goes wrong:** User triggers generation before extraction is complete, resulting in zero sections fetched and an empty study guide generated with no errors — a confusing silent failure.

**How to avoid:** The POST /modules/{id}/generate endpoint must check that the latest `ExtractionRun.Status == Ready` before creating a `GenerationRun`. Return 409 otherwise.

### Pitfall 6: Re-generation content deletion timing

**What goes wrong:** Old `StudyGuide`/`Flashcard`/`QuizQuestion`/`ConceptMap` rows are deleted at the start of `ContentGenerationJob`, but if the deletion fails mid-way, some sections have old content and some have no content.

**How to avoid:** Delete all old content rows in a single `RemoveRange` + `SaveChangesAsync` before enqueuing section jobs (same as `LectureExtractionJob` does with existing sections). If deletion throws, the run fails before any new content is written.

---

## Code Examples

### LLM response schema — study guide

```csharp
// These record types are defined inside SectionGenerationJob.cs (like LectureResult in LectureExtractionJob)
internal record StudyGuideDto(
    [property: JsonPropertyName("direct_answer")] string DirectAnswer,
    [property: JsonPropertyName("high_yield_details")] List<string> HighYieldDetails,
    [property: JsonPropertyName("key_tables")] List<KeyTableDto> KeyTables,
    [property: JsonPropertyName("must_know_numbers")] List<string> MustKnowNumbers,
    [property: JsonPropertyName("sources")] List<string> Sources);

internal record KeyTableDto(
    [property: JsonPropertyName("header")] List<string> Header,
    [property: JsonPropertyName("rows")] List<List<string>> Rows);
```

### LLM response schema — flashcards

```csharp
internal record FlashcardDto(
    [property: JsonPropertyName("front")] string Front,
    [property: JsonPropertyName("back")] string Back,
    [property: JsonPropertyName("type")] string Type,       // "cloze" or "qa"
    [property: JsonPropertyName("source_refs")] List<string> SourceRefs);

// Response is: { "cards": [ ...FlashcardDto... ] }
internal record FlashcardsResponse(
    [property: JsonPropertyName("cards")] List<FlashcardDto> Cards);
```

### LLM response schema — quiz

```csharp
internal record QuizQuestionDto(
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("choices")] List<string> Choices,
    [property: JsonPropertyName("correct_answer")] string CorrectAnswer,
    [property: JsonPropertyName("source_ref")] string SourceRef);

// Response is: { "questions": [ ...QuizQuestionDto... ] }
internal record QuizResponse(
    [property: JsonPropertyName("questions")] List<QuizQuestionDto> Questions);
```

### LLM response schema — concept map

```csharp
internal record ConceptMapDto(
    [property: JsonPropertyName("mermaid")] string Mermaid,
    [property: JsonPropertyName("source_node_refs")] List<string> SourceNodeRefs);
```

### Study guide prompt template (discretionary — planner refines)

```
You are a medical education assistant. Given the source material below, generate a study guide
for the section "{HeadingText}".

IMPORTANT: Use ONLY information present in the source material. If there is insufficient evidence
for a field, write "Not found in your sources" for that field.

Respond with valid JSON in this exact schema:
{
  "direct_answer": "one-sentence summary of the core concept",
  "high_yield_details": ["bullet 1", "bullet 2", ...],
  "key_tables": [{"header": ["col1", "col2"], "rows": [["val", "val"], ...]}],
  "must_know_numbers": ["specific number/value with context", ...],
  "sources": ["Slide 3 - Hemodynamics.pptx", ...]
}

Source material (pages/slides referenced):
{SourcePageRefsJson}

Section content:
{Content}
```

### Concept map prompt template

```
Given the source material below for section "{HeadingText}", generate a Mermaid flowchart
representing the algorithm or decision process described.

Use ONLY information present in the source material. Keep node labels concise.

Respond with valid JSON:
{
  "mermaid": "flowchart TD\n  A[Start] --> B{Decision}\n  ...",
  "source_node_refs": ["Slide 5", ...]
}

Source material:
{Content}
```

### Integration test pattern — trigger (mirrors LectureExtractionTriggerTests)

```csharp
[Fact]
public async Task PostGenerate_EnqueuesContentGenerationJob_Returns202()
{
    // Module must have extraction status Ready
    var client = CreateAuthenticatedClient();
    factory.JobClient.EnqueuedJobs.Clear();

    var response = await client.PostAsync(
        $"/modules/{factory.ReadyExtractionModuleId}/generate", null);

    Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    Assert.Single(factory.JobClient.EnqueuedJobs);
    Assert.Equal(typeof(ContentGenerationJob),
        factory.JobClient.EnqueuedJobs[0].Type);
}

[Fact]
public async Task PostGenerate_WhenExtractionNotReady_Returns409()
{
    var client = CreateAuthenticatedClient();
    var response = await client.PostAsync(
        $"/modules/{factory.NotStartedExtractionModuleId}/generate", null);
    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
}
```

### Unit test pattern — SectionGenerationJob (mirrors LectureExtractionJobTests)

```csharp
[Fact]
public async Task Execute_CreatesStudyGuide_FlashcardsAndQuiz()
{
    using var db = CreateInMemoryDb();
    var (moduleId, sectionId, runId) = SeedSectionAndRun(db);

    var provider = new StubGenerationProvider();
    var job = new SectionGenerationJob(db, provider);
    await job.Execute(sectionId, runId);

    Assert.Equal(1, db.StudyGuides.Count());
    Assert.True(db.Flashcards.Count() >= 1);
    Assert.True(db.QuizQuestions.Count() >= 1);
}

[Fact]
public async Task Execute_NonAlgorithmicSection_NoConceptMap()
{
    // Section with no algorithmic keywords
    using var db = CreateInMemoryDb();
    var (_, sectionId, runId) = SeedSectionAndRun(db,
        heading: "Vital Signs Overview", content: "Normal values for HR, BP, RR.");
    var job = new SectionGenerationJob(db, new StubGenerationProvider());
    await job.Execute(sectionId, runId);
    Assert.Equal(0, db.ConceptMaps.Count());
}

[Fact]
public async Task Execute_AlgorithmicSection_CreatesConceptMap()
{
    using var db = CreateInMemoryDb();
    var (_, sectionId, runId) = SeedSectionAndRun(db,
        heading: "Sepsis Workup Algorithm", content: "If fever, then...");
    var job = new SectionGenerationJob(db, new StubGenerationProvider());
    await job.Execute(sectionId, runId);
    Assert.Equal(1, db.ConceptMaps.Count());
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `IGenerationProvider` on Module object | Not yet used for generation | Phase 4 adds usage | Interface already exists; no new wiring needed |
| `ExtractionRun` without `UpdatedAt` | `GenerationRun` adds `UpdatedAt` | Phase 4 | Fresher polling signal |
| Generic stub response (plain string) | Stub returns type-specific JSON | Phase 4 | Stub mode is testable end-to-end |

---

## Open Questions

1. **SectionGenerationJob DI scope for IGenerationProvider**
   - What we know: `IGenerationProvider` is registered as Singleton in `ProviderRegistration`. `SectionGenerationJob` will be `AddScoped` in Worker. Singleton injected into Scoped is fine in .NET DI.
   - What's unclear: Whether Singleton is correct given potential concurrent section jobs sharing the same provider instance and HTTP client.
   - Recommendation: `ClaudeGenerationProvider` and `GeminiGenerationProvider` are stateless (they receive `IAnthropicClient`/`Client` which are themselves singletons); Singleton is safe. No change needed.

2. **GenerationRun status when all sections fail vs partial failure**
   - What we know: Locked decision says `GenerationRun` is set to Failed with error info, but successful sections are kept.
   - What's unclear: If the orchestrator marks Ready after enqueue, and then all sections fail, the `GenerationRun` ends up Failed (set by section jobs) even though the user sees a stale Ready state briefly.
   - Recommendation: This is an acceptable UX tradeoff for Phase 4. The next polling cycle will show Failed. Document this behavior in the trigger endpoint response.

3. **Frontend API client for generation status**
   - What we know: `modules.get(id)` already returns `ExtractionStatus`. Must now also return `GenerationStatus`.
   - What's unclear: Whether `generationStatus` should be a new field on the existing `ModuleDto` or a separate endpoint.
   - Recommendation: Add `generationStatus` field to the existing GET /modules/{id} response (same approach as `extractionStatus`). No new endpoint needed.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (net10.0) |
| Config file | `src/Api.Tests/StudyApp.Api.Tests.csproj` |
| Quick run command | `dotnet test /Users/Peyton/repos/study-webapp-pa-school/src/Api.Tests/StudyApp.Api.Tests.csproj --filter "Generation" --nologo` |
| Full suite command | `dotnet test /Users/Peyton/repos/study-webapp-pa-school/src/Api.Tests/StudyApp.Api.Tests.csproj --nologo` |

**Current baseline:** 52 passed, 2 failed (pre-existing failures unrelated to Phase 4), 1 skipped. Total: 55.

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| GEN-01 | `SectionGenerationJob` creates `StudyGuide` row | unit | `dotnet test --filter "SectionGenerationJobTests"` | ❌ Wave 0 |
| GEN-02 | `SectionGenerationJob` creates `Flashcard` rows | unit | `dotnet test --filter "SectionGenerationJobTests"` | ❌ Wave 0 |
| GEN-03 | `SectionGenerationJob` creates `QuizQuestion` rows | unit | `dotnet test --filter "SectionGenerationJobTests"` | ❌ Wave 0 |
| GEN-04 | Algorithmic section gets `ConceptMap` row | unit | `dotnet test --filter "SectionGenerationJobTests"` | ❌ Wave 0 |
| GEN-05 | Stub provider returns "Not found in your sources" sentinel passthrough | unit | `dotnet test --filter "SectionGenerationJobTests"` | ❌ Wave 0 |
| GEN-06 | Source refs present on generated content rows | unit | `dotnet test --filter "SectionGenerationJobTests"` | ❌ Wave 0 |
| GEN-07 | Non-algorithmic section produces no `ConceptMap` | unit | `dotnet test --filter "SectionGenerationJobTests"` | ❌ Wave 0 |
| GEN-07 | `IsAlgorithmic` detects all locked keywords | unit | `dotnet test --filter "AlgorithmicDetectionTests"` | ❌ Wave 0 |
| GEN-08 | Full pipeline with `StubGenerationProvider` — no exceptions | unit | `dotnet test --filter "StubGenerationProviderTests"` | ❌ Wave 0 |
| GEN-08 | `StubGenerationProvider` returns valid JSON for each content type | unit | `dotnet test --filter "StubGenerationProviderTests"` | ❌ Wave 0 |
| Trigger | POST /modules/{id}/generate returns 202 + enqueues job | integration | `dotnet test --filter "GenerationTriggerTests"` | ❌ Wave 0 |
| Trigger | POST /generate blocked when extraction not Ready → 409 | integration | `dotnet test --filter "GenerationTriggerTests"` | ❌ Wave 0 |
| Trigger | POST /generate blocked when generation Queued/Processing → 409 | integration | `dotnet test --filter "GenerationTriggerTests"` | ❌ Wave 0 |
| Trigger | POST /generate creates `GenerationRun` with Queued status in DB | integration | `dotnet test --filter "GenerationTriggerTests"` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test /Users/Peyton/repos/study-webapp-pa-school/src/Api.Tests/StudyApp.Api.Tests.csproj --filter "Generation" --nologo`
- **Per wave merge:** `dotnet test /Users/Peyton/repos/study-webapp-pa-school/src/Api.Tests/StudyApp.Api.Tests.csproj --nologo`
- **Phase gate:** Full suite green (minus the 2 pre-existing failures) before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `src/Api.Tests/Generation/SectionGenerationJobTests.cs` — covers GEN-01 through GEN-07
- [ ] `src/Api.Tests/Generation/AlgorithmicDetectionTests.cs` — covers GEN-07 keyword logic
- [ ] `src/Api.Tests/Generation/StubGenerationProviderTests.cs` — covers GEN-08
- [ ] `src/Api.Tests/Generation/GenerationTriggerTests.cs` — integration tests for POST /generate endpoint
- [ ] No new framework installs needed — xUnit + WebApplicationFactory already in use

---

## Sources

### Primary (HIGH confidence)
- Direct codebase inspection: `src/Api/Jobs/LectureExtractionJob.cs` — orchestrator pattern
- Direct codebase inspection: `src/Api/Models/ExtractionRun.cs` — `GenerationRun` template
- Direct codebase inspection: `src/Worker/Providers/IGenerationProvider.cs`, `ClaudeGenerationProvider.cs`, `GeminiGenerationProvider.cs`, `StubGenerationProvider.cs` — provider layer complete
- Direct codebase inspection: `src/Worker/ProviderRegistration.cs` — DI wiring for `GENERATION_PROVIDER` env var already in place
- Direct codebase inspection: `src/Api/Data/AppDbContext.cs` — EF Core model configuration patterns
- Direct codebase inspection: `src/Api.Tests/Extraction/LectureExtractionJobTests.cs` and `LectureExtractionTriggerTests.cs` — test patterns to replicate
- Direct codebase inspection: `src/Frontend/src/pages/ModuleDetailPage.tsx` — polling pattern for generation UI
- `.planning/phases/04-content-generation/04-CONTEXT.md` — locked decisions

### Secondary (MEDIUM confidence)
- `src/Api.Tests/` test run: 52/55 passing — baseline for Phase 4 to preserve

### Tertiary (LOW confidence)
- None — all research grounded in direct codebase analysis and locked decisions.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all libraries already in codebase, verified by inspection
- Architecture: HIGH — patterns are copies of proven Phase 2/3 implementations with verified test examples
- Pitfalls: HIGH — derived from Phase 3 bug fixes logged in STATE.md and direct code review
- Test map: HIGH — based on existing test file structure and patterns

**Research date:** 2026-04-02
**Valid until:** 2026-05-02 (stable stack; no fast-moving dependencies introduced)
