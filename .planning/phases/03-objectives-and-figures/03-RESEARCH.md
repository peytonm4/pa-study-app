# Phase 3: Figures and Lecture Extraction - Research

**Researched:** 2026-03-18
**Domain:** Python subprocess integration, EF Core entity modeling, React figure review UI, .docx generation
**Confidence:** HIGH

## Summary

Phase 3 extends the existing ingestion pipeline with two sequential user-facing milestones: figure extraction/curation, then lecture extraction. The codebase already has production-quality patterns for every major concern — Hangfire jobs, vision providers, storage, and polling — so this phase is largely about adding new entities and wiring new jobs that mirror existing ones.

The Python skills (`extract_images.py` and the lecture extractor) do not yet exist in the repo at `src/skills/lecture-extractor-extracted/`. They must be created as new files in Phase 3. The Worker calls them via `System.Diagnostics.Process.Start`, with stub mode returning deterministic content when Python is not installed. The Worker already has `DocumentFormat.OpenXml` 3.4.1 so no new packages are required for .docx creation.

The download link for the generated .docx requires a presigned S3 URL returned from an API endpoint, not a direct S3 URL. The existing `IStorageService` must be extended with a `GetPresignedUrlAsync` method (or the download can go through the API proxy — simpler, more secure).

**Primary recommendation:** Mirror the `IngestionJob`/`VisionExtractionJob` Hangfire pattern exactly. Add `Figure` and `Section` EF entities, two new Hangfire jobs, three new API endpoints (figure list, figure toggle, run extraction), and extend `ModuleDetailPage` with figures review and extraction status sections.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **Objectives removed.** No objectives pasting in Phase 3 or anywhere in the app. OBJ-01 through OBJ-04 are superseded. Content organized by lecture extractor's topic hierarchy (H1/H2/H3), not by objectives.
- **Figure review workflow:**
  - `extract_images.py` runs as a subprocess from the Worker immediately after document ingestion completes
  - Script handles all filtering — no additional filtering logic needed in C#
  - Figure manifest (JSON) stored; user sees extracted figures on module detail page with Keep/Ignore toggles
  - Figures with captions/labels (Figure, Table, Algorithm, Flowchart keywords) are pre-selected as Keep
  - Caption extraction via Gemini runs for all kept figures
  - Figure review happens BEFORE lecture extraction
- **Lecture extraction workflow:**
  - Triggered by user action after figure review completes (not automatic)
  - Python lecture extractor skill called as subprocess from Worker (Hangfire job)
  - Runs with curated figure list; figures embedded at correct locations in output
  - Two outputs: `.docx` stored in S3 (downloadable), and structured sections in DB
- **Docx viewing:** Download only — no inline viewer. Download button on module detail page once job completes.
- **Python skill integration pattern:** Subprocess via `Process.Start` in Phase 3. FastAPI sidecar migration deferred to Phase 4.
- **DB entities (new in Phase 3):**
  - `Section` — module ID, heading level (1/2/3), heading text, content text, source page refs, sort order
  - `Figure` — document ID, S3 key, keep/ignore status, caption, source page, manifest metadata
- **UI additions to module detail page:**
  - Figures review section: thumbnails, source page, Keep/Ignore toggle, pre-selected state
  - "Run Extraction" button — enabled after figure review, triggers lecture extraction Hangfire job
  - Download button — appears when extraction job completes, links to .docx in S3
  - Extraction job status indicator (Queued → Processing → Ready / Failed)

### Claude's Discretion

- Exact subprocess invocation pattern (temp dir management, stdout/stderr handling)
- Section entity schema details beyond required fields
- Figure thumbnail display approach in the review UI
- Hangfire job retry policy for extraction jobs

### Deferred Ideas (OUT OF SCOPE)

- FastAPI sidecar for Python skills (Phase 4)
- Inline docx viewer
- Student-editable sections (reordering, removing content)
- Objectives as optional layer on top of sections
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| FIG-01 | Extract figures from PPTX and PDFs via `extract_images.py` subprocess | Subprocess pattern; Python script produces JSON manifest; Worker parses and stores Figure entities |
| FIG-02 | Automatically filter logos, watermarks, stock photos, repeated elements | Handled entirely inside `extract_images.py` — C# reads manifest output, no filter logic needed in Worker |
| FIG-03 | Pre-select caption-labeled figures (Figure, Table, Algorithm, Flowchart) as Keep | Manifest JSON from Python includes a `has_caption` / `label_type` flag; Worker sets `Keep=true` on insert |
| FIG-04 | User can toggle Keep/Ignore per figure before lecture extraction | PATCH `/modules/{id}/figures/{figureId}` endpoint + React toggle UI in `ModuleDetailPage` |
| FIG-05 | Extract captions for kept figures via Gemini vision model | Reuse `IVisionProvider.ExtractTextAsync` (already wired); called in `FigureExtractionJob` after manifest parse |
| LEXT-01 | User can trigger lecture extraction after figure review | POST `/modules/{id}/extract` endpoint enqueues `LectureExtractionJob` |
| LEXT-02 | Worker calls lecture extractor Python skill as subprocess with curated figure list | `System.Diagnostics.Process.Start` with JSON args on stdin or temp file |
| LEXT-03 | Skill reorganizes content into H1/H2/H3 topic hierarchy, embedding kept figures | Python skill output: structured JSON of sections + embedded figure references |
| LEXT-04 | Structured sections stored in DB (heading level, content, source page refs, sort order) | New `Section` EF entity; Worker parses JSON output and bulk-inserts |
| LEXT-05 | Generated .docx stored in S3; download link on module detail page | `IStorageService.UploadAsync` for docx; API endpoint returns presigned URL or proxy download |
| LEXT-06 | User can download the reorganized lecture as .docx | Download button on `ModuleDetailPage` — GET `/modules/{id}/docx` returns redirect or presigned URL |
| SKILL-01 | Worker can invoke Python skills as subprocesses | `System.Diagnostics.Process` with `UseShellExecute=false`, `RedirectStandardOutput/Error=true` |
| SKILL-02 | Python skill output (manifest JSON, structured sections) parsed and stored by Worker | `System.Text.Json.JsonSerializer.Deserialize` from process stdout |
| SKILL-03 | App works in stub mode without Python skills installed | `ISkillRunner` interface with `StubSkillRunner` (deterministic fixed output) and `ProcessSkillRunner` (real subprocess) |
</phase_requirements>

---

## Standard Stack

### Core (already in project — no new packages needed)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| DocumentFormat.OpenXml | 3.4.1 | Generate .docx output from lecture extractor result | Already in Worker.csproj; battle-tested for Word documents |
| Hangfire | 1.8.23 | Background job queue for FigureExtractionJob and LectureExtractionJob | Already wired; established job pattern in project |
| System.Diagnostics.Process | (BCL) | Spawn Python subprocesses | Built-in; no extra package needed |
| EF Core / Npgsql | 10.0.5 | Persist Figure and Section entities | Already wired |
| System.Text.Json | (BCL) | Parse Python skill stdout (JSON manifest, sections JSON) | Built-in; already used throughout project |
| React Query (@tanstack/react-query) | (existing) | Poll extraction job status; figure list queries | Already used in ModuleDetailPage polling pattern |
| shadcn/ui Badge + Button | (existing) | Status badges, Keep/Ignore toggle, Run Extraction button | Already imported in ModuleDetailPage |

### No New NuGet Packages Required
All needed libraries are already in the project. The Worker already has `DocumentFormat.OpenXml` so .docx generation does not require adding a dependency.

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| DocumentFormat.OpenXml for .docx | ClosedXML, NPOI | Both add new NuGet deps; OpenXml already present |
| API proxy for docx download | S3 presigned URL | Presigned URL is simpler but exposes S3 directly; proxy is cleaner for auth. Presigned URL is fine for MVP since auth is dev-only |
| Temp file for Python args | stdin pipe | Stdin pipe avoids temp file management but is trickier to debug; temp JSON file in `Path.GetTempPath()` is simpler |

---

## Architecture Patterns

### Recommended Project Structure (additions only)

```
src/
├── Api/
│   ├── Controllers/
│   │   └── FiguresController.cs       # GET /modules/{id}/figures, PATCH /figures/{id}, POST /modules/{id}/extract, GET /modules/{id}/docx
│   ├── Models/
│   │   ├── Figure.cs                  # New entity
│   │   └── Section.cs                 # New entity
│   ├── Jobs/
│   │   ├── FigureExtractionJob.cs     # Calls extract_images.py subprocess, persists Figure rows
│   │   └── LectureExtractionJob.cs    # Calls lecture extractor subprocess, persists Section rows, uploads .docx
│   └── Migrations/
│       └── XXXXXX_AddFiguresAndSections.cs
├── Worker/
│   └── Skills/
│       ├── ISkillRunner.cs            # Interface: Task<string> RunAsync(string scriptPath, string argsJson)
│       ├── ProcessSkillRunner.cs      # Real subprocess impl
│       └── StubSkillRunner.cs         # Deterministic stub (returns hardcoded manifest/sections JSON)
└── src/skills/
    └── lecture-extractor-extracted/
        ├── extract_images.py          # Figure extraction (to be created)
        ├── lecture_extractor.py       # Lecture reorganizer (to be created)
        └── requirements.txt           # Python deps
src/Frontend/src/
├── api/
│   └── figures.ts                     # API client for figures and extraction
└── pages/
    └── ModuleDetailPage.tsx           # Extended with FigureReview section + ExtractionStatus section
```

### Pattern 1: ISkillRunner Interface (SKILL-01, SKILL-03)
**What:** Thin abstraction over Python subprocess execution so stub mode works without Python installed.
**When to use:** Every time the Worker needs to call a Python skill.
**Example:**
```csharp
// src/Worker/Skills/ISkillRunner.cs
public interface ISkillRunner
{
    // Returns stdout from the Python script as a string.
    // Throws SkillException if process exits non-zero.
    Task<string> RunAsync(string scriptPath, string inputJson, CancellationToken ct = default);
}

// src/Worker/Skills/ProcessSkillRunner.cs
public class ProcessSkillRunner : ISkillRunner
{
    public async Task<string> RunAsync(string scriptPath, string inputJson, CancellationToken ct = default)
    {
        // Write inputJson to temp file to avoid shell escaping issues
        var tmpInput = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        await File.WriteAllTextAsync(tmpInput, inputJson, ct);
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = $"\"{scriptPath}\" \"{tmpInput}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi)!;
            var stdout = await process.StandardOutput.ReadToEndAsync(ct);
            var stderr = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);
            if (process.ExitCode != 0)
                throw new SkillException($"Script exited {process.ExitCode}: {stderr}");
            return stdout;
        }
        finally
        {
            File.Delete(tmpInput);
        }
    }
}

// src/Worker/Skills/StubSkillRunner.cs
public class StubSkillRunner : ISkillRunner
{
    public Task<string> RunAsync(string scriptPath, string inputJson, CancellationToken ct = default)
    {
        // Return different stub JSON depending on which script is being called
        if (scriptPath.Contains("extract_images"))
            return Task.FromResult(StubFigureManifest);
        return Task.FromResult(StubLectureSections);
    }

    private const string StubFigureManifest = """
        {"figures":[{"id":"stub-fig-1","s3_key":"stub/fig1.png","page":1,"has_caption":true,"caption_keywords":["Figure"]}]}
        """;

    private const string StubLectureSections = """
        {"sections":[{"level":1,"heading":"Stub Topic","content":"Stub content for stub mode.","pages":[1],"figures":[]}]}
        """;
}
```

### Pattern 2: FigureExtractionJob — mirrors IngestionJob
**What:** Runs after all documents for a module are Ready; calls `extract_images.py` via `ISkillRunner`; parses manifest JSON; inserts `Figure` rows; triggers caption extraction for kept figures.
**When to use:** Enqueued at the end of `IngestionJob` once document status transitions to Ready.

```csharp
// Enqueue at end of IngestionJob when document reaches Ready status:
jobClient.Enqueue<FigureExtractionJob>(j => j.Execute(documentId));

// FigureExtractionJob shape (mirrors VisionExtractionJob):
[AutomaticRetry(Attempts = 2)]
public async Task Execute(Guid documentId)
{
    // 1. Download source file from S3 to temp path (needed by extract_images.py)
    // 2. Call skillRunner.RunAsync(extractScriptPath, inputJson)
    // 3. Parse manifest JSON → List<FigureManifestEntry>
    // 4. For each entry: create Figure entity, set KeepStatus based on has_caption
    // 5. db.Figures.AddRange(...); await db.SaveChangesAsync()
    // 6. For each kept figure: enqueue caption extraction (reuse IVisionProvider)
}
```

### Pattern 3: LectureExtractionJob
**What:** Enqueued by user action (POST /modules/{id}/extract); calls lecture extractor Python skill; parses sections JSON; bulk-inserts Section rows; generates .docx; uploads to S3; updates module extraction status.

```csharp
[AutomaticRetry(Attempts = 1)]  // Do not auto-retry — user should re-trigger
public async Task Execute(Guid moduleId)
{
    // 1. Set module ExtractionStatus = Processing
    // 2. Fetch kept figures for module, serialize to JSON input
    // 3. Fetch all chunks for module, include in input JSON
    // 4. Call skillRunner.RunAsync(lectureScriptPath, inputJson)
    // 5. Parse sections JSON → List<SectionDto>
    // 6. db.Sections.AddRange(...); await db.SaveChangesAsync()
    // 7. Build .docx from sections using DocumentFormat.OpenXml
    // 8. storageService.UploadAsync(docxStream, s3Key, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
    // 9. Store s3Key on module, set ExtractionStatus = Ready
}
```

### Pattern 4: Module ExtractionStatus
**What:** New field on `Module` entity tracking the lecture extraction lifecycle, separate from per-document status.
**Example:**
```csharp
public enum ExtractionStatus { NotStarted, Queued, Processing, Ready, Failed }

// Add to Module.cs:
public ExtractionStatus ExtractionStatus { get; set; } = ExtractionStatus.NotStarted;
public string? DocxS3Key { get; set; }        // set when extraction completes
public string? ExtractionError { get; set; }   // set on failure
```

### Pattern 5: Figure Entity
```csharp
public class Figure
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public string S3Key { get; set; } = string.Empty;   // thumbnail stored in S3
    public bool Keep { get; set; } = false;
    public int PageNumber { get; set; }
    public string? Caption { get; set; }                 // populated by vision model
    public string? LabelType { get; set; }               // "Figure", "Table", etc.
    public string? ManifestMetadataJson { get; set; }    // raw manifest entry for reference
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### Pattern 6: Section Entity
```csharp
public class Section
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public Module Module { get; set; } = null!;
    public int HeadingLevel { get; set; }           // 1, 2, or 3
    public string HeadingText { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? SourcePageRefsJson { get; set; } // JSON array of page numbers
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### Pattern 7: .docx Generation via DocumentFormat.OpenXml
**What:** Build a Word document from Section entities. Already have OpenXml 3.4.1 in Worker.
**Example (minimal):**
```csharp
// Source: DocumentFormat.OpenXml SDK — WordprocessingDocument pattern
using var ms = new MemoryStream();
using (var wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
{
    var mainPart = wordDoc.AddMainDocumentPart();
    mainPart.Document = new Document(new Body());

    foreach (var section in sections.OrderBy(s => s.SortOrder))
    {
        var para = new Paragraph();
        var props = new ParagraphProperties(
            new ParagraphStyleId { Val = $"Heading{section.HeadingLevel}" });
        para.PrependChild(props);
        para.AppendChild(new Run(new Text(section.HeadingText)));
        mainPart.Document.Body!.AppendChild(para);

        if (!string.IsNullOrEmpty(section.Content))
        {
            var contentPara = new Paragraph(new Run(new Text(section.Content)));
            mainPart.Document.Body!.AppendChild(contentPara);
        }
    }
    mainPart.Document.Save();
}
ms.Position = 0;
await storageService.UploadAsync(ms, docxS3Key, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
```

### Pattern 8: Frontend Figures Review Section
**What:** Extend `ModuleDetailPage` with a figure list (using existing `useQuery`/`useMutation` patterns) and an extraction status + download section.
**Example:**
```typescript
// src/Frontend/src/api/figures.ts
export interface FigureDto {
  id: string;
  s3ThumbnailUrl: string;
  pageNumber: number;
  keep: boolean;
  labelType: string | null;
  caption: string | null;
}

export const figures = {
  list: (moduleId: string) =>
    client.get<FigureDto[]>(`/modules/${moduleId}/figures`).then(r => r.data),
  toggle: (figureId: string, keep: boolean) =>
    client.patch(`/figures/${figureId}`, { keep }).then(r => r.data),
  runExtraction: (moduleId: string) =>
    client.post(`/modules/${moduleId}/extract`).then(r => r.data),
  getDocxDownloadUrl: (moduleId: string) =>
    client.get<{ url: string }>(`/modules/${moduleId}/docx`).then(r => r.data),
};
```

### Anti-Patterns to Avoid
- **Filtering figures in C#:** The Python script already filters — do not reimplment in the Worker. Just read the manifest.
- **Storing figure image bytes in the DB:** Store the S3 key, serve the thumbnail via an API proxy or presigned URL.
- **Auto-retry on LectureExtractionJob:** Set `Attempts = 1`. The user should re-trigger extraction manually if it fails rather than the job silently retrying with stale state.
- **Running LectureExtractionJob before figure review completes:** The "Run Extraction" button should only be enabled on the frontend when figure review is complete (all figures in the module have been reviewed). Backend should also guard against this.
- **Creating .docx in LectureExtractionJob using a file on disk:** Use MemoryStream — avoids temp file cleanup issues and works in Docker.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Figure filtering (logos, watermarks, dedup) | Custom C# image heuristics | `extract_images.py` skill output | Python script has production-quality perceptual hashing, aspect ratio checks, repeat detection — reimplementing in C# is months of work |
| Lecture topic reorganization | LLM prompt in C# | Lecture extractor Python skill | Skill encapsulates document structure analysis, heading inference, figure placement — call it as-is |
| .docx creation from scratch | Custom XML writer | `DocumentFormat.OpenXml` (already present) | OpenXml handles all Word XML namespaces, relationships, and part packaging |
| Python availability detection | Shell `which python3` | Env var `PYTHON_PROVIDER=stub\|real` mirroring existing `VISION_PROVIDER` | Consistent with existing provider selection pattern; explicit config beats runtime detection |

**Key insight:** Both Python skills are black boxes from the Worker's perspective. The Worker's only job is to pass JSON in and parse JSON out. All intelligence stays in Python.

---

## Common Pitfalls

### Pitfall 1: Triggering FigureExtractionJob Too Early
**What goes wrong:** `FigureExtractionJob` is enqueued per-document but it should run once per module after ALL documents for that module reach `Ready`. If enqueued per-document, the script runs multiple times with partial data.
**Why it happens:** The existing `IngestionJob` pattern enqueues follow-up jobs per document — easy to copy naively.
**How to avoid:** At the end of `IngestionJob`, after setting `DocumentStatus.Ready`, check if all other documents for the module are also Ready. If yes, enqueue one `FigureExtractionJob` for the module (not per-document). If `extract_images.py` is designed to run per-document, enqueue per-document — but make clear in the job that figures are scoped to a document.
**Warning signs:** Figure rows in DB with duplicate entries for the same image.

### Pitfall 2: Process.Start with python3 Not Found
**What goes wrong:** Worker throws `Win32Exception` (or `FileNotFoundException`) because `python3` is not on PATH in the Docker container.
**Why it happens:** Docker image may not have Python installed; developer machines vary.
**How to avoid:** Check `PYTHON_PROVIDER` env var before calling `ProcessSkillRunner`. When `PYTHON_PROVIDER=stub` (the default), skip subprocess entirely. In Docker Compose, only add Python to the Worker image when needed.
**Warning signs:** Job fails immediately with exit code -1 or exception mentioning process start.

### Pitfall 3: stdout/stderr Deadlock in Process.Start
**What goes wrong:** Process hangs because both stdout and stderr buffers fill, each waiting for the other to be read.
**Why it happens:** Calling `ReadToEndAsync` sequentially on stdout then stderr can deadlock if the process writes to both streams simultaneously.
**How to avoid:** Read stdout and stderr concurrently using `Task.WhenAll`:
```csharp
var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
var stderrTask = process.StandardError.ReadToEndAsync(ct);
await process.WaitForExitAsync(ct);
var stdout = await stdoutTask;
var stderr = await stderrTask;
```
**Warning signs:** Job hangs indefinitely with no timeout.

### Pitfall 4: OpenXml .docx Missing Document Styles
**What goes wrong:** The downloaded .docx opens in Word/Google Docs but headings look like plain text — no H1/H2/H3 styling.
**Why it happens:** `WordprocessingDocument.Create` creates a document with no default styles part. `ParagraphStyleId { Val = "Heading1" }` references a style that doesn't exist.
**How to avoid:** Add a `StyleDefinitionsPart` with at minimum Heading1/Heading2/Heading3 styles, or use `Normal` style and apply manual `RunProperties` (bold, font size). The simpler alternative: use `Normal` text with manually formatted runs rather than semantic heading styles.
**Warning signs:** Headings render as plain text in Word.

### Pitfall 5: Figure Thumbnail Access in UI
**What goes wrong:** Frontend can't display figure thumbnails because MinIO is not publicly accessible from the browser.
**Why it happens:** MinIO runs in Docker at `http://localhost:9000` but is not configured for anonymous read.
**How to avoid:** The API must proxy the thumbnail or return a short-lived presigned URL. Add a `GET /figures/{id}/thumbnail` endpoint that calls `IStorageService.DownloadAsync` and streams bytes back, or add `GetPresignedUrlAsync` to `IStorageService`. The presigned URL approach requires the S3 client to know the public URL — cleaner to proxy through the API for MVP.
**Warning signs:** Broken images in the figure review UI.

### Pitfall 6: Module `ExtractionStatus` Migration Conflict
**What goes wrong:** EF migration fails or generates incorrect SQL because `ExtractionStatus` enum stored as int clashes with `DocumentStatus` stored as string.
**Why it happens:** The project uses `HasConversion<string>()` for `DocumentStatus` — must do the same for `ExtractionStatus` (or store as int consistently).
**How to avoid:** Follow the existing `HasConversion<string>()` pattern for the new enum in `OnModelCreating`.

---

## Code Examples

### Concurrent stdout/stderr Read (SKILL-01 safety)
```csharp
// Source: .NET System.Diagnostics.Process docs — deadlock prevention
var process = Process.Start(psi)!;
var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
var stderrTask = process.StandardError.ReadToEndAsync(ct);
await process.WaitForExitAsync(ct);
var stdout = await stdoutTask;
var stderr = await stderrTask;
```

### Bulk Insert Sections (LEXT-04)
```csharp
// Source: EF Core AddRange — same pattern as Chunks in IngestionJob
var sections = sectionDtos.Select((dto, i) => new Section
{
    Id = Guid.NewGuid(),
    ModuleId = moduleId,
    HeadingLevel = dto.Level,
    HeadingText = dto.Heading,
    Content = dto.Content,
    SourcePageRefsJson = JsonSerializer.Serialize(dto.Pages),
    SortOrder = i
}).ToList();

db.Sections.AddRange(sections);
await db.SaveChangesAsync();
```

### FiguresController Endpoints
```csharp
// GET /modules/{moduleId}/figures
[HttpGet("modules/{moduleId:guid}/figures")]
public async Task<IActionResult> GetFigures(Guid moduleId) { ... }

// PATCH /figures/{id}
[HttpPatch("figures/{id:guid}")]
public async Task<IActionResult> ToggleFigure(Guid id, [FromBody] ToggleFigureRequest request) { ... }

// POST /modules/{moduleId}/extract
[HttpPost("modules/{moduleId:guid}/extract")]
public async Task<IActionResult> TriggerExtraction(Guid moduleId) { ... }

// GET /modules/{moduleId}/docx — returns presigned URL or proxy download
[HttpGet("modules/{moduleId:guid}/docx")]
public async Task<IActionResult> DownloadDocx(Guid moduleId) { ... }
```

### React Query Figure Toggle (FIG-04)
```typescript
// Mirrors deleteMutation pattern in existing DocumentRow component
const toggleMutation = useMutation({
  mutationFn: (keep: boolean) => figures.toggle(fig.id, keep),
  onSuccess: () => queryClient.invalidateQueries({ queryKey: ['figures', moduleId] }),
});
```

### ExtractionStatus Polling (mirrors DocumentRow refetchInterval)
```typescript
const { data: moduleStatus } = useQuery({
  queryKey: ['module-extraction', moduleId],
  queryFn: () => modules.get(moduleId),
  refetchInterval: (query) => {
    const s = query.state.data?.extractionStatus;
    return s === 'Ready' || s === 'Failed' ? false : 3000;
  },
});
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Objectives-based organization | Lecture extractor topic hierarchy (H1/H2/H3) | Phase 3 context session 2026-03-18 | Eliminates OBJ-01–04; sections replace objectives throughout |
| FastAPI sidecar for Python (planned) | Subprocess per-job (Phase 3) | Phase 3 context session 2026-03-18 | Simpler for Phase 3; sidecar migration planned for Phase 4 |

---

## Open Questions

1. **Does `extract_images.py` run per-document or per-module?**
   - What we know: CONTEXT.md says it runs "immediately after document ingestion completes" (per-document phrasing) but figures are reviewed at the module level
   - What's unclear: Whether the script operates on a single file or all files in a module
   - Recommendation: Design `FigureExtractionJob` to run per-document (one job per uploaded file), scoping `Figure` rows to `DocumentId`. The module detail UI aggregates all document figures.

2. **Python skill files: create stubs or expect real implementations?**
   - What we know: No Python files exist in the repo yet at `src/skills/lecture-extractor-extracted/`
   - What's unclear: Whether the user intends to provide real Python implementations or wants stub Python scripts created
   - Recommendation: Create the stub Python scripts in Phase 3 (they receive JSON, print deterministic JSON to stdout) so the subprocess integration path is testable. Real ML logic can be dropped in as a replacement without changing the C# caller.

3. **Figure thumbnail storage: separate S3 key or extract from source file?**
   - What we know: `IStorageService` supports upload; figures need a displayable thumbnail in the UI
   - What's unclear: Whether `extract_images.py` produces image files or just a manifest with coordinates
   - Recommendation: Have the Python script produce image files (PNG crops) and upload them to S3 alongside the manifest. The Worker stores the S3 key per figure. API proxy endpoint serves thumbnails.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 |
| Config file | `src/Api.Tests/StudyApp.Api.Tests.csproj` |
| Quick run command | `dotnet test src/Api.Tests --filter "Category=Unit" --no-build` |
| Full suite command | `dotnet test src/Api.Tests --no-build` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SKILL-01 | ProcessSkillRunner invokes process and returns stdout | unit | `dotnet test src/Api.Tests --filter "FullyQualifiedName~ProcessSkillRunnerTests" --no-build` | Wave 0 |
| SKILL-02 | StubSkillRunner returns deterministic JSON per script type | unit | `dotnet test src/Api.Tests --filter "FullyQualifiedName~StubSkillRunnerTests" --no-build` | Wave 0 |
| SKILL-03 | When PYTHON_PROVIDER=stub, LectureExtractionJob uses StubSkillRunner | unit | `dotnet test src/Api.Tests --filter "FullyQualifiedName~SkillProviderConfigTests" --no-build` | Wave 0 |
| FIG-01 | FigureExtractionJob parses manifest JSON and inserts Figure rows | unit | `dotnet test src/Api.Tests --filter "FullyQualifiedName~FigureExtractionJobTests" --no-build` | Wave 0 |
| FIG-03 | Figures with has_caption=true are inserted with Keep=true | unit | `dotnet test src/Api.Tests --filter "FullyQualifiedName~FigureExtractionJobTests" --no-build` | Wave 0 |
| FIG-04 | PATCH /figures/{id} toggles Keep field in DB | integration | `dotnet test src/Api.Tests --filter "FullyQualifiedName~FigureToggleTests" --no-build` | Wave 0 |
| LEXT-01 | POST /modules/{id}/extract enqueues LectureExtractionJob | integration | `dotnet test src/Api.Tests --filter "FullyQualifiedName~LectureExtractionTriggerTests" --no-build` | Wave 0 |
| LEXT-04 | LectureExtractionJob parses sections JSON and inserts Section rows | unit | `dotnet test src/Api.Tests --filter "FullyQualifiedName~LectureExtractionJobTests" --no-build` | Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test src/Api.Tests --no-build`
- **Per wave merge:** `dotnet test src/Api.Tests --no-build`
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `src/Api.Tests/Skills/ProcessSkillRunnerTests.cs` — covers SKILL-01
- [ ] `src/Api.Tests/Skills/StubSkillRunnerTests.cs` — covers SKILL-02, SKILL-03
- [ ] `src/Api.Tests/Skills/SkillProviderConfigTests.cs` — covers SKILL-03 (env var selection)
- [ ] `src/Api.Tests/Figures/FigureExtractionJobTests.cs` — covers FIG-01, FIG-03
- [ ] `src/Api.Tests/Figures/FigureToggleTests.cs` — covers FIG-04 (integration via TestWebApplicationFactory)
- [ ] `src/Api.Tests/Extraction/LectureExtractionJobTests.cs` — covers LEXT-04
- [ ] `src/Api.Tests/Extraction/LectureExtractionTriggerTests.cs` — covers LEXT-01 (integration)

---

## Sources

### Primary (HIGH confidence)
- Codebase read: `src/Api/Jobs/IngestionJob.cs`, `VisionExtractionJob.cs` — established Hangfire job pattern
- Codebase read: `src/Worker/Program.cs`, `ProviderRegistration.cs` — DI and provider selection pattern
- Codebase read: `src/Api/Data/AppDbContext.cs`, `Models/` — EF entity conventions (`HasConversion<string>()` for enums)
- Codebase read: `src/Frontend/src/pages/ModuleDetailPage.tsx` — polling pattern, mutation pattern
- Codebase read: `src/Api.Tests/Documents/DocumentUploadTests.cs` — test factory and stub pattern
- Codebase read: `src/Worker/StudyApp.Worker.csproj` — confirms DocumentFormat.OpenXml 3.4.1 already present

### Secondary (MEDIUM confidence)
- .NET BCL `System.Diagnostics.Process` — stdout/stderr concurrent read pattern is well-documented; deadlock risk confirmed by multiple sources
- DocumentFormat.OpenXml 3.4.1 heading styles behavior — based on SDK usage patterns; heading style names ("Heading1", "Heading2") are standard

### Tertiary (LOW confidence)
- Python skill file structure (`extract_images.py`, `lecture_extractor.py`) — neither file exists in the repo; structure inferred from CONTEXT.md descriptions. Actual script interfaces TBD.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all libraries already in project; confirmed from .csproj files
- Architecture: HIGH — all patterns mirror existing code; EF/Hangfire/React Query conventions are established
- Python skill structure: LOW — no Python files exist yet; interface design is inferred from CONTEXT.md
- Pitfalls: HIGH — Process.Start deadlock and OpenXml styles issues are well-known .NET patterns

**Research date:** 2026-03-18
**Valid until:** 2026-04-18 (stable stack — all dependencies already pinned in project)
