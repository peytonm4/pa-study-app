# Phase 2: Document Ingestion - Research

**Researched:** 2026-03-17
**Domain:** File upload pipeline, PPTX/PDF extraction, Hangfire job orchestration, LLM provider abstraction
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Module/Upload UI Scope**
- Build real module-creation UI — not just API endpoints
- Module = one exam prep unit (e.g., "Cardio Exam 1") — can contain multiple PPTX/PDF files
- User types the module name manually (simple text input)
- Module list page IS the homepage (`/` redirects to `/modules`) — no sidebar nav item needed yet
- Module detail page (`/modules/{id}`) shows uploaded files, per-file processing status, and the upload button
- Phase 3 will add objectives pasting to the same module detail page

**Module list page appearance**
- Simple list: module name, overall status (Processing / Ready), date created — click to enter module detail
- No card grid, no fancy layout

**Processing Progress UX**
- Frontend polls `GET /documents/{id}/status` (or equivalent module endpoint) using React Query `refetchInterval`
- Document statuses: `Uploading → Queued → Processing → Ready / Failed`
- Status displayed inline on the module detail page — each file row shows its current status + spinner
- On failure: show "Failed" badge + a generic error message — detailed error logged server-side only
- No server-sent events, no toasts for completion

**File Management**
- A module can have multiple files, no hard limit
- Duplicate uploads (same file, same module) are allowed — treated as a new document, reprocessed
- Users can delete a document (`DELETE /documents/{id}`) — removes DB record, chunks, and S3 file
- Upload goes through the API (multipart POST) — no presigned URL flow; DevAuth handles auth

**Vision Extraction Job Shape**
- OCR runs as a separate downstream Hangfire job, not inline with chunking
  - Ingestion job: extract text → detect pages needing OCR → enqueue `VisionExtractionJob` per flagged page → ingestion job completes
  - Vision jobs run independently; each updates the page's chunk when done
- Document status stays `Processing` until ALL vision jobs for that document complete (not "partially ready")
- In stub mode: flagged PDF pages get placeholder text `"[Figure: vision extraction not available in stub mode]"` as their chunk content
- Phase 2 wires real Gemini calls + stub toggle — `IVisionProvider` interface with `GeminiVisionProvider` and `StubVisionProvider`; selected via `VISION_PROVIDER` env var
- Content generation providers (`IGenerationProvider`) are also wired in this phase (Claude, Gemini, Stub) via `GENERATION_PROVIDER` env var — stub mode must work without API keys

### Claude's Discretion
- Exact polling interval for React Query refetch
- Module list styling (within simple list constraint)
- S3 key naming convention for uploaded files
- Hangfire job retry policy
- Exact DB schema for `Documents` and `Chunks` tables beyond the required fields

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| INGEST-01 | User can upload PPTX files as primary study sources | S3 PutObjectRequest + multipart form upload pattern |
| INGEST-02 | User can upload PDF files as supplementary sources | Same upload pipeline; content-type validation |
| INGEST-03 | App extracts text from PPTX slides deterministically (OpenXML SDK) | DocumentFormat.OpenXml 3.4.1 — slide text via `slide.Slide.Descendants<A.Text>()` |
| INGEST-04 | App extracts text layer from PDFs when available | PdfPig 0.1.13 — `page.Letters` collection |
| INGEST-05 | App detects PDF pages with no text layer and routes them for vision extraction | PdfPig — `page.Letters.Count == 0` is the detection signal |
| INGEST-06 | App extracts speaker notes from PPTX slides | `slide.NotesSlidePart` → descend `A.Text` on `notesSlide` |
| INGEST-07 | App creates chunks per slide/page with metadata (file name, slide/page number) | EF Core `Chunks` entity with DocumentId, PageNumber, Content, FileName FK fields |
| LLM-01 | Vision extraction uses Gemini (PDF OCR, figure captions) | Google.GenAI 1.5.0 — inline bytes via `Part` with `InlineData` |
| LLM-02 | Content generation supports pluggable providers (Claude, Gemini, or Stub) | `IGenerationProvider` interface; `Anthropic` 12.9.0 + `Google.GenAI` 1.5.0 + `StubGenerationProvider` |
| LLM-03 | Provider is configured via environment variables | `VISION_PROVIDER` and `GENERATION_PROVIDER` env vars read in `Program.cs`/DI registration |
</phase_requirements>

---

## Summary

Phase 2 is a pipeline phase: files in, structured chunks out. The backend has three distinct sub-systems to build: (1) the file upload + S3 storage layer, (2) the synchronous text-extraction workers (OpenXML for PPTX, PdfPig for PDF), and (3) the asynchronous vision-extraction layer using Gemini for image-only PDF pages. All of this hangs together through Hangfire job chaining, where the ingestion job discovers flagged pages and enqueues one `VisionExtractionJob` per page before marking itself complete.

The frontend is a real UI: a module list page as the app homepage, a module detail page with per-file upload + status polling, and shadcn/ui Badge components for status display. React Query `refetchInterval` drives polling; no websockets or SSE. The LLM provider system is the plumbing for future content generation — both `IVisionProvider` and `IGenerationProvider` interfaces get wired now, but only vision extraction actually runs in Phase 2.

**Primary recommendation:** Use `DocumentFormat.OpenXml` for PPTX (deterministic, official Microsoft library), `PdfPig` for PDF text extraction (pure .NET, no native dependencies), `Google.GenAI` for Gemini vision, and `Anthropic` for Claude. Inject `IBackgroundJobClient` into the ingestion job to enqueue child vision jobs — this is the standard free-tier Hangfire fan-out pattern.

---

## Standard Stack

### Core — New Packages Required

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| DocumentFormat.OpenXml | 3.4.1 | PPTX text + notes extraction | Official Microsoft Open XML SDK; deterministic; no native deps |
| PdfPig | 0.1.13 | PDF text layer extraction + blank-page detection | Pure .NET port of PDFBox; stable; `page.Letters` API is clean |
| Google.GenAI | 1.5.0 | Gemini vision API calls | Official Google SDK; GA since May 2025; supports inline image bytes |
| Anthropic | 12.9.0 | Claude content generation API | Official Anthropic SDK (v10+ is official); .NET Standard 2.0+ |

### Already Installed (verify configured correctly)

| Library | Version | Purpose | Status |
|---------|---------|---------|--------|
| Hangfire.AspNetCore | 1.8.23 | Job scheduling, dashboard | Installed; Worker not yet wired for Hangfire |
| Hangfire.PostgreSql | 1.21.1 | Job storage | Installed |
| AWSSDK.S3 | 4.0.19 | File upload/delete | Installed in API; S3 client registered |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 | DB for new entities | Installed |

### Frontend — New shadcn Components Required

| Component | Install Command | Purpose |
|-----------|----------------|---------|
| Badge | `npx shadcn@latest add badge` | Document status chip (Queued/Processing/Ready/Failed) |
| Progress (optional) | `npx shadcn@latest add progress` | Could show chunk count; skip if not needed |
| Table | `npx shadcn@latest add table` | Module list and file rows |
| Input | `npx shadcn@latest add input` | Module name text input |
| Dialog (optional) | `npx shadcn@latest add dialog` | Create-module form modal; could also be inline |

**Installation (backend):**
```bash
dotnet add src/Api/StudyApp.Api.csproj package DocumentFormat.OpenXml --version 3.4.1
dotnet add src/Api/StudyApp.Api.csproj package PdfPig --version 0.1.13
dotnet add src/Api/StudyApp.Api.csproj package Google.GenAI --version 1.5.0
dotnet add src/Api/StudyApp.Api.csproj package Anthropic --version 12.9.0

# Worker also needs extraction libs since it runs the Hangfire jobs
dotnet add src/Worker/StudyApp.Worker.csproj package DocumentFormat.OpenXml --version 3.4.1
dotnet add src/Worker/StudyApp.Worker.csproj package PdfPig --version 0.1.13
dotnet add src/Worker/StudyApp.Worker.csproj package Google.GenAI --version 1.5.0
dotnet add src/Worker/StudyApp.Worker.csproj package Anthropic --version 12.9.0
```

> **Note on Worker packages:** The Worker runs Hangfire jobs. The ingestion job downloads from S3 and extracts text. Add `AWSSDK.S3` to the Worker csproj as well — currently only in API.

---

## Architecture Patterns

### Recommended Project Structure

```
src/
├── Api/
│   ├── Controllers/
│   │   ├── ModulesController.cs       # GET /modules, POST /modules, DELETE /modules/{id}
│   │   └── DocumentsController.cs     # POST /documents (upload), GET /documents/{id}/status, DELETE /documents/{id}
│   ├── Data/
│   │   └── AppDbContext.cs            # Add Modules, Documents, Chunks DbSets
│   ├── Models/
│   │   ├── Module.cs
│   │   ├── Document.cs                # Status enum: Uploading/Queued/Processing/Ready/Failed
│   │   └── Chunk.cs
│   ├── Services/
│   │   ├── IStorageService.cs         # Upload(stream, key) → Task; Delete(key) → Task
│   │   └── S3StorageService.cs
│   └── Migrations/
│       └── [timestamp]_AddModulesDocumentsChunks.cs
│
├── Worker/
│   ├── Jobs/
│   │   ├── IngestionJob.cs            # Downloads file, extracts text, enqueues VisionJobs
│   │   └── VisionExtractionJob.cs     # Fetches page from S3, calls Gemini, updates chunk
│   ├── Extraction/
│   │   ├── IPptxExtractor.cs
│   │   ├── PptxExtractor.cs           # Uses DocumentFormat.OpenXml
│   │   ├── IPdfExtractor.cs
│   │   └── PdfExtractor.cs            # Uses PdfPig
│   └── Providers/
│       ├── IVisionProvider.cs
│       ├── GeminiVisionProvider.cs
│       ├── StubVisionProvider.cs
│       ├── IGenerationProvider.cs
│       ├── ClaudeGenerationProvider.cs
│       ├── GeminiGenerationProvider.cs
│       └── StubGenerationProvider.cs
│
└── Frontend/src/
    ├── api/
    │   ├── modules.ts                 # getModules(), createModule(), deleteModule()
    │   └── documents.ts               # uploadDocument(), getDocumentStatus(), deleteDocument()
    ├── pages/
    │   ├── ModuleListPage.tsx          # Route: /modules
    │   └── ModuleDetailPage.tsx        # Route: /modules/:id
    └── components/
        └── ui/
            └── badge.tsx              # Status badge (shadcn)
```

### Pattern 1: PPTX Slide + Notes Extraction

**What:** Open PPTX as stream, iterate slides in order, collect slide body text and notes text separately.
**When to use:** `IngestionJob` when document content-type is `application/vnd.openxmlformats-officedocument.presentationml.presentation`.

```csharp
// Source: https://learn.microsoft.com/en-us/office/open-xml/presentation/how-to-get-all-the-text-in-all-slides-in-a-presentation
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;

public record SlideContent(int SlideNumber, string BodyText, string NotesText);

public IEnumerable<SlideContent> ExtractSlides(Stream pptxStream)
{
    using var ppt = PresentationDocument.Open(pptxStream, isEditable: false);
    var presentationPart = ppt.PresentationPart!;
    var slideIds = presentationPart.Presentation.SlideIdList!.ChildElements;

    for (int i = 0; i < slideIds.Count; i++)
    {
        var relId = ((SlideId)slideIds[i]).RelationshipId!;
        var slidePart = (SlidePart)presentationPart.GetPartById(relId);

        // Body text: all A.Text descendants of the slide itself
        var bodyText = string.Concat(
            slidePart.Slide.Descendants<A.Text>().Select(t => t.Text));

        // Speaker notes: A.Text descendants of the notes slide part
        var notesText = "";
        if (slidePart.NotesSlidePart is { } notesPart)
        {
            notesText = string.Concat(
                notesPart.NotesSlide.Descendants<A.Text>().Select(t => t.Text));
        }

        yield return new SlideContent(i + 1, bodyText, notesText);
    }
}
```

### Pattern 2: PDF Text Extraction + Blank-Page Detection

**What:** Open PDF, iterate pages, collect letter text; flag pages with no letters for vision extraction.
**When to use:** `IngestionJob` when document content-type is `application/pdf`.

```csharp
// Source: https://github.com/UglyToad/PdfPig/wiki/Letters
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

public record PageContent(int PageNumber, string Text, bool NeedsVision);

public IEnumerable<PageContent> ExtractPages(Stream pdfStream)
{
    using var document = PdfDocument.Open(pdfStream);
    foreach (var page in document.GetPages())
    {
        var letters = page.Letters;
        bool hasText = letters.Count > 0;

        var text = hasText
            ? string.Concat(letters.Select(l => l.Value))
            : string.Empty;

        yield return new PageContent(page.Number, text, !hasText);
    }
}
```

**Important:** PdfPig's `page.Letters` gives individual glyphs. Use word extraction (`page.GetWords()`) for more readable output if raw letter concatenation produces garbled text.

### Pattern 3: Hangfire Fan-Out (Free Tier — Inject IBackgroundJobClient)

**What:** The ingestion job enqueues one `VisionExtractionJob` per flagged PDF page by injecting `IBackgroundJobClient` via constructor.
**When to use:** When batch continuations (Pro) are unavailable. This is the standard free-tier fan-out.

```csharp
// Source: Hangfire docs — Dependency Injection pattern
public class IngestionJob(
    IBackgroundJobClient jobClient,
    IPdfExtractor pdfExtractor,
    AppDbContext db,
    IStorageService storage)
{
    public async Task Execute(Guid documentId)
    {
        var document = await db.Documents.FindAsync(documentId);
        // ... download from S3, extract pages ...

        var flaggedPages = pages.Where(p => p.NeedsVision).ToList();

        foreach (var page in flaggedPages)
        {
            // Enqueue child jobs directly from within the parent job
            jobClient.Enqueue<VisionExtractionJob>(
                j => j.Execute(documentId, page.PageNumber));
        }

        // Track vision job count for completion detection
        document.PendingVisionJobs = flaggedPages.Count;
        document.Status = flaggedPages.Count > 0
            ? DocumentStatus.Processing
            : DocumentStatus.Ready;

        await db.SaveChangesAsync();
    }
}
```

**Completion detection pattern:** `VisionExtractionJob` decrements `PendingVisionJobs` atomically (use a SQL UPDATE with a WHERE clause or EF row version). When it reaches 0, set `Status = Ready`.

### Pattern 4: S3 Upload from IFormFile

**What:** Receive multipart file upload in API controller, stream directly to S3, return document ID.

```csharp
// Source: AWS SDK for .NET V3 docs — PutObjectRequest
public async Task<string> UploadAsync(IFormFile file, string key, CancellationToken ct)
{
    using var stream = file.OpenReadStream();
    var request = new PutObjectRequest
    {
        BucketName = _bucketName,
        Key = key,
        InputStream = stream,
        ContentType = file.ContentType,
        AutoCloseStream = false
    };
    await _s3.PutObjectAsync(request, ct);
    return key;
}
```

**S3 key convention (Claude's discretion):** `uploads/{userId}/{moduleId}/{documentId}/{filename}` — this scopes files to user and module for future multi-user support.

### Pattern 5: Gemini Vision Call (Google.GenAI SDK)

**What:** Download image bytes from S3, send to Gemini with an OCR prompt, return extracted text.

```csharp
// Source: Google.GenAI 1.5.0 — InlineData pattern (verified against Python SDK docs)
using Google.GenAI;
using Google.GenAI.Types;

public class GeminiVisionProvider(GoogleAI client) : IVisionProvider
{
    public async Task<string> ExtractTextAsync(byte[] imageBytes, string mimeType)
    {
        var response = await client.Models.GenerateContentAsync(
            model: "gemini-2.0-flash",   // confirmed model ID for vision tasks
            contents: new Content
            {
                Parts =
                [
                    new Part { InlineData = new() { Data = Convert.ToBase64String(imageBytes), MimeType = mimeType } },
                    new Part { Text = "Extract all text from this image. Return only the extracted text, no commentary." }
                ]
            });

        return response.Text ?? string.Empty;
    }
}
```

> **Model name note:** `gemini-2.0-flash` is the current stable vision model in the Gemini Developer API. `gemini-3-flash-preview` was announced but is not yet GA. Use `gemini-2.0-flash` for production until 3.x is stable. [MEDIUM confidence — verify model string against Google AI Studio at implementation time]

### Pattern 6: Provider Selection via Environment Variable

**What:** Read `VISION_PROVIDER` and `GENERATION_PROVIDER` env vars in `Program.cs`, register the correct implementation.

```csharp
// In Worker/Program.cs
var visionProvider = builder.Configuration["VISION_PROVIDER"] ?? "stub";
if (visionProvider == "gemini")
    builder.Services.AddSingleton<IVisionProvider, GeminiVisionProvider>();
else
    builder.Services.AddSingleton<IVisionProvider, StubVisionProvider>();

var generationProvider = builder.Configuration["GENERATION_PROVIDER"] ?? "stub";
switch (generationProvider)
{
    case "claude":
        builder.Services.AddSingleton<IGenerationProvider, ClaudeGenerationProvider>();
        break;
    case "gemini":
        builder.Services.AddSingleton<IGenerationProvider, GeminiGenerationProvider>();
        break;
    default:
        builder.Services.AddSingleton<IGenerationProvider, StubGenerationProvider>();
        break;
}
```

### Pattern 7: React Query Polling

**What:** Poll document status while `status` is not terminal (`Ready` or `Failed`).

```typescript
// Source: TanStack Query v5 docs — refetchInterval
const { data } = useQuery({
  queryKey: ['document-status', documentId],
  queryFn: () => documents.getStatus(documentId),
  refetchInterval: (query) => {
    const status = query.state.data?.status;
    // Stop polling once terminal state reached
    if (status === 'Ready' || status === 'Failed') return false;
    return 3000; // poll every 3 seconds
  },
});
```

**Recommended interval (Claude's discretion):** 3 seconds. Fast enough to feel responsive; not so aggressive as to hammer the API. Stop polling automatically when terminal status reached.

### Anti-Patterns to Avoid

- **Extracting text inline in the API controller:** All extraction must go through Hangfire — the API should only enqueue the job and return 202 Accepted immediately.
- **Storing file bytes in the DB:** Files always go to S3; DB stores the S3 key only.
- **Using `BackgroundJob.ContinueJobWith` for fan-out:** ContinueJobWith creates a single continuation. For multiple child jobs from one parent, inject `IBackgroundJobClient` into the parent job and call `Enqueue` in a loop.
- **Using `Guid.NewGuid()` in `HasData` migrations:** Hardcode GUIDs for seed data as already established in Phase 1.
- **Checking `page.Letters.Count > 0` on scanned PDFs with invisible text layers:** Some scanned PDFs embed empty text layers. Consider checking minimum letter count (e.g., > 5) or average word confidence if needed. For MVP, `Count == 0` is sufficient.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| PPTX parsing | Custom XML reader | DocumentFormat.OpenXml | OOXML is deeply nested XML; relationships between slide parts are non-trivial |
| PDF text extraction | iTextSharp / custom PDF parser | PdfPig | PDF spec edge cases (encoding, CMap, Type3 fonts) are enormous; PdfPig handles them |
| Image OCR | Direct Tesseract integration | Gemini vision API | Gemini handles rotated text, tables, medical diagrams; Tesseract requires native binaries |
| Job fan-out tracking | Custom "pending jobs" polling table | `PendingVisionJobs` counter on Document + atomic decrement | Simpler than a separate tracking table; adequate for sequential document processing |
| File streaming | Buffering IFormFile to disk first | `file.OpenReadStream()` → S3 | Memory-efficient; avoids temp file cleanup issues |

**Key insight:** The PDF spec has ~700 pages. PdfPig handles encoding normalization, CMap lookup tables, Type0/Type1/TrueType/CIDFont font handling, and content stream parsing. Text extraction from "simple" PDFs is deceptively complex.

---

## Common Pitfalls

### Pitfall 1: Worker Has No Hangfire Registration Yet

**What goes wrong:** The Worker `Program.cs` currently has no Hangfire setup — only a basic hosted service. Jobs enqueued by the API will sit in the queue forever.
**Why it happens:** Phase 1 explicitly deferred Hangfire worker registration.
**How to avoid:** Add Hangfire server registration and `AppDbContext` to the Worker's `Program.cs` before testing any job execution. The Worker needs its own connection string, Hangfire server config, and DI registrations for all job dependencies.
**Warning signs:** Jobs show `Enqueued` in Hangfire dashboard but never move to `Processing`.

### Pitfall 2: PdfPig Letter Ordering

**What goes wrong:** Concatenating `page.Letters` directly produces garbled text (letters may not be in reading order).
**Why it happens:** PDF spec doesn't require content stream order to match visual reading order.
**How to avoid:** Use `page.GetWords()` instead of raw Letters concatenation. This applies word-grouping heuristics. For even better results, use the `DefaultPageSegmenter` or `RecursiveXYCut` word extractor.
**Warning signs:** Extracted text looks correct for simple slides but garbled for complex layouts.

### Pitfall 3: Document Status Race Condition

**What goes wrong:** Multiple `VisionExtractionJob` instances complete near-simultaneously and the "decrement and check" for `PendingVisionJobs` has a race condition.
**Why it happens:** Non-atomic read-modify-write in EF.
**How to avoid:** Use a raw SQL UPDATE with a check:
```sql
UPDATE "Documents"
SET "PendingVisionJobs" = "PendingVisionJobs" - 1,
    "Status" = CASE WHEN "PendingVisionJobs" - 1 = 0 THEN 'Ready' ELSE "Status" END
WHERE "Id" = @documentId
RETURNING "PendingVisionJobs";
```
Or use EF's `ExecuteUpdateAsync` with atomic decrement. Avoid fetching, decrementing in memory, and saving.

### Pitfall 4: S3 Stream Already Consumed

**What goes wrong:** `IFormFile.OpenReadStream()` returns a forward-only stream. If any middleware reads it first, the stream is empty when the controller reads it.
**Why it happens:** ASP.NET body buffering settings, or reading the form twice.
**How to avoid:** Enable `EnableBuffering()` in middleware if needed, or ensure the controller is the only reader. Alternatively, copy stream to `MemoryStream` before passing to S3 (small files only).

### Pitfall 5: Hangfire Job Arguments Must Be Serializable

**What goes wrong:** Hangfire serializes job arguments to JSON. Complex objects (like `Stream`, `IFormFile`, EF entities) cannot be job arguments.
**Why it happens:** Hangfire uses Newtonsoft.Json or System.Text.Json for argument serialization.
**How to avoid:** Job arguments should only be primitive types or simple records (Guid, int, string). The `IngestionJob` receives `documentId: Guid` only — it fetches everything else from DB and S3 at execution time.

### Pitfall 6: OpenXml SDK Stream Ownership

**What goes wrong:** `PresentationDocument.Open(stream, false)` takes a `Stream` — if the stream is disposed before the document is used, you get `ObjectDisposedException`.
**Why it happens:** Async patterns + `using` blocks on outer stream.
**How to avoid:** Keep the stream open for the lifetime of the `PresentationDocument` usage, or copy to `MemoryStream` first if the S3 download stream may close.

### Pitfall 7: Gemini Model ID Volatility

**What goes wrong:** Gemini model ID strings change with new releases and old aliases are deprecated.
**Why it happens:** Google rotates model IDs and aliases frequently.
**How to avoid:** Put the model ID in `appsettings.json` / environment variable, not hardcoded. Default: `"gemini-2.0-flash"`.

---

## Code Examples

### DB Entities (EF Core)

```csharp
// Models/Module.cs
public class Module
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Document> Documents { get; set; } = [];
}

// Models/Document.cs
public enum DocumentStatus { Uploading, Queued, Processing, Ready, Failed }

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ModuleId { get; set; }
    public Module Module { get; set; } = null!;
    public string FileName { get; set; } = "";
    public string S3Key { get; set; } = "";
    public string ContentType { get; set; } = "";
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploading;
    public int PendingVisionJobs { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Chunk> Chunks { get; set; } = [];
}

// Models/Chunk.cs
public class Chunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public string FileName { get; set; } = "";  // denormalized for query convenience
    public int PageNumber { get; set; }
    public string Content { get; set; } = "";
    public bool IsVisionExtracted { get; set; } = false;
}
```

### Frontend: API Domain Files

```typescript
// src/Frontend/src/api/modules.ts
import client from './client';

export interface Module {
  id: string;
  name: string;
  status: 'Processing' | 'Ready';
  createdAt: string;
}

export const modules = {
  list: () => client.get<Module[]>('/modules').then(r => r.data),
  create: (name: string) => client.post<Module>('/modules', { name }).then(r => r.data),
  delete: (id: string) => client.delete(`/modules/${id}`),
};

// src/Frontend/src/api/documents.ts
import client from './client';

export interface DocumentStatus {
  id: string;
  fileName: string;
  status: 'Uploading' | 'Queued' | 'Processing' | 'Ready' | 'Failed';
  createdAt: string;
}

export const documents = {
  upload: (moduleId: string, file: File) => {
    const form = new FormData();
    form.append('file', file);
    return client.post<DocumentStatus>(`/modules/${moduleId}/documents`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then(r => r.data);
  },
  getStatus: (id: string) =>
    client.get<DocumentStatus>(`/documents/${id}/status`).then(r => r.data),
  delete: (id: string) => client.delete(`/documents/${id}`),
};
```

### Frontend: Module Detail Page (polling skeleton)

```tsx
// Polling pattern with auto-stop on terminal state
const { data: doc } = useQuery({
  queryKey: ['doc-status', documentId],
  queryFn: () => documents.getStatus(documentId),
  refetchInterval: (query) => {
    const s = query.state.data?.status;
    return s === 'Ready' || s === 'Failed' ? false : 3000;
  },
});
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| iTextSharp (AGPL) for PDF | PdfPig (Apache 2.0) | ~2019 onward | No license cost or compliance risk |
| DocumentFormat.OpenXml 2.x (verbose API) | 3.x (using-statement focus, no Close()) | v3.0 (2023) | `Close()` removed; always use `using` |
| Hangfire static `BackgroundJob.Enqueue(...)` | Inject `IBackgroundJobClient` | Hangfire 1.6+ | Testable; DI-compatible |
| Gemini REST API calls (raw HttpClient) | `Google.GenAI` SDK | GA May 2025 | Official, type-safe, maintained |
| Anthropic.SDK (community) | `Anthropic` (official) | v10+, ~2025 | Endorsed by Anthropic |

**Deprecated/outdated:**
- `DocumentFormat.OpenXml.Close()`: Removed in v3.0 — use `using` instead
- `Mscc.GenerativeAI` / `Google_GenerativeAI` community SDKs: Superseded by official `Google.GenAI`
- `Anthropic.SDK` (tghamm): Still functional but `Anthropic` (official) is now preferred

---

## Open Questions

1. **PDF rendering for vision extraction — what format does Gemini expect?**
   - What we know: PdfPig can detect blank pages; Gemini takes `image/png` or `image/jpeg` inline bytes
   - What's unclear: PdfPig does NOT render PDFs to images (it's a text extraction library). A separate PDF-to-image step is needed for vision extraction. Options: `PDFtoImage` (Skia-based), `Aspose.PDF` (paid), or save the original PDF page bytes for Gemini's native PDF understanding.
   - **Recommendation:** Use `Google.GenAI` with the raw PDF page bytes if Gemini's PDF understanding mode is available, OR use `PDFtoImage` NuGet package (Apache 2.0, uses libSkiaSharp) to rasterize the page to PNG. This is a WAVE 0 decision — pick one approach before implementing `VisionExtractionJob`.
   - Confidence: LOW — verify Gemini's ability to accept PDF bytes directly vs. requiring rasterized image

2. **Worker needs `AppDbContext` — shared library or duplication?**
   - What we know: Worker currently has no EF Core context; API has AppDbContext
   - What's unclear: The project uses separate csproj files without a shared library project
   - **Recommendation:** Add a `ProjectReference` from Worker to Api for now (simple, avoids creating a third project). Worker accesses AppDbContext and Models through the reference. If this becomes coupling concern, extract a `StudyApp.Core` library in a future phase.
   - Confidence: MEDIUM

3. **Gemini model ID for production vision tasks**
   - What we know: `gemini-2.0-flash` is stable; `gemini-3-flash` is GA as of Dec 2025 but documentation is still settling
   - What's unclear: Whether `gemini-3-flash` is accessible via the Gemini Developer API (vs Vertex AI only) and its exact model ID string in `Google.GenAI` 1.5.0
   - **Recommendation:** Default to `gemini-2.0-flash` in config; make model ID an env-var override so it can be changed without code changes.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 with `Microsoft.AspNetCore.Mvc.Testing` 10.0.5 |
| Config file | `src/Api.Tests/StudyApp.Api.Tests.csproj` |
| Quick run command | `dotnet test src/Api.Tests/ --no-build -x` |
| Full suite command | `dotnet test src/ --no-build` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| INGEST-01 | POST /modules/{id}/documents accepts PPTX, returns 202 | Integration | `dotnet test src/Api.Tests/ -x --filter "FullyQualifiedName~DocumentUpload"` | ❌ Wave 0 |
| INGEST-02 | POST /modules/{id}/documents accepts PDF, returns 202 | Integration | `dotnet test src/Api.Tests/ -x --filter "FullyQualifiedName~DocumentUpload"` | ❌ Wave 0 |
| INGEST-03 | PPTX extractor returns correct slide text | Unit | `dotnet test src/Api.Tests/ -x --filter "FullyQualifiedName~PptxExtractor"` | ❌ Wave 0 |
| INGEST-04 | PDF extractor returns text from text-layer page | Unit | `dotnet test src/Api.Tests/ -x --filter "FullyQualifiedName~PdfExtractor"` | ❌ Wave 0 |
| INGEST-05 | PDF extractor flags blank pages (no letters) | Unit | `dotnet test src/Api.Tests/ -x --filter "FullyQualifiedName~PdfExtractor"` | ❌ Wave 0 |
| INGEST-06 | PPTX extractor returns speaker notes text | Unit | `dotnet test src/Api.Tests/ -x --filter "FullyQualifiedName~PptxExtractor"` | ❌ Wave 0 |
| INGEST-07 | Chunks have correct page number + file name metadata | Unit | `dotnet test src/Api.Tests/ -x --filter "FullyQualifiedName~Chunk"` | ❌ Wave 0 |
| LLM-01 | StubVisionProvider returns placeholder text | Unit | `dotnet test src/Api.Tests/ -x --filter "FullyQualifiedName~VisionProvider"` | ❌ Wave 0 |
| LLM-02 | StubGenerationProvider returns deterministic fallback | Unit | `dotnet test src/Api.Tests/ -x --filter "FullyQualifiedName~GenerationProvider"` | ❌ Wave 0 |
| LLM-03 | VISION_PROVIDER=stub wires StubVisionProvider | Integration | `dotnet test src/Api.Tests/ -x --filter "FullyQualifiedName~ProviderConfig"` | ❌ Wave 0 |

### Sampling Rate

- **Per task commit:** `dotnet test src/Api.Tests/ --no-build -x`
- **Per wave merge:** `dotnet test src/ --no-build`
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps

- [ ] `src/Api.Tests/Documents/DocumentUploadTests.cs` — covers INGEST-01, INGEST-02
- [ ] `src/Api.Tests/Extraction/PptxExtractorTests.cs` — covers INGEST-03, INGEST-06; needs a minimal `.pptx` fixture file
- [ ] `src/Api.Tests/Extraction/PdfExtractorTests.cs` — covers INGEST-04, INGEST-05; needs a minimal `.pdf` fixture file (one text page + one blank page)
- [ ] `src/Api.Tests/Providers/VisionProviderTests.cs` — covers LLM-01 (stub path only)
- [ ] `src/Api.Tests/Providers/GenerationProviderTests.cs` — covers LLM-02 (stub path only)
- [ ] `src/Api.Tests/Providers/ProviderConfigTests.cs` — covers LLM-03 (env var DI wiring)
- [ ] Test fixture files: `src/Api.Tests/TestFixtures/sample.pptx` and `src/Api.Tests/TestFixtures/sample.pdf` (minimal; can be created programmatically with DocumentFormat.OpenXml in test setup)

---

## Sources

### Primary (HIGH confidence)
- Microsoft Learn — [Get all text in all slides (OpenXML SDK)](https://learn.microsoft.com/en-us/office/open-xml/presentation/how-to-get-all-the-text-in-all-slides-in-a-presentation) — slide text extraction pattern, NotesSlidePart access
- PdfPig Wiki — [Letters page](https://github.com/UglyToad/PdfPig/wiki/Letters) — text extraction and blank-page detection via `page.Letters.Count`
- NuGet Gallery — [DocumentFormat.OpenXml 3.4.1](https://www.nuget.org/packages/DocumentFormat.OpenXml) — current stable version confirmed
- NuGet Gallery — [PdfPig 0.1.13](https://www.nuget.org/packages/PdfPig/) — current stable version confirmed
- NuGet Gallery — [Google.GenAI 1.5.0](https://www.nuget.org/packages/Google.GenAI/) — official Google SDK, GA
- NuGet Gallery — [Anthropic 12.9.0](https://www.nuget.org/packages/Anthropic/) — official Claude SDK
- Hangfire Docs — [IBackgroundJobClient](https://api.hangfire.io/html/T_Hangfire_IBackgroundJobClient.htm) — injection pattern for job fan-out
- Google AI Docs — [Image understanding](https://ai.google.dev/gemini-api/docs/image-understanding) — inline image bytes pattern
- TanStack Query Docs — [refetchInterval](https://tanstack.com/query/v4/docs/framework/react/reference/useQuery) — polling interval with conditional stop

### Secondary (MEDIUM confidence)
- AWS SDK for .NET V3 Docs — [PutObjectRequest](https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/S3/TTransferUtility.html) — S3 upload from stream
- Google Cloud Blog — [Introducing Google Gen AI .NET SDK](https://cloud.google.com/blog/topics/developers-practitioners/introducing-google-gen-ai-net-sdk) — GA announcement

### Tertiary (LOW confidence)
- Google.GenAI SDK — InlineData Part structure for C#: inferred from Python SDK docs + SDK type structure; verify at implementation time against `googleapis.github.io/dotnet-genai/api/`

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all package versions verified on NuGet; official SDKs confirmed
- Architecture: HIGH — patterns match existing Phase 1 conventions exactly
- PPTX extraction: HIGH — verified against official Microsoft Learn docs with working C# code
- PDF extraction: HIGH — PdfPig API verified against wiki; `page.Letters` is stable API
- Hangfire fan-out: HIGH — IBackgroundJobClient injection pattern is documented and standard
- Gemini .NET SDK InlineData: MEDIUM — structure inferred from Python SDK + SDK types; .NET-specific example not found in official docs
- PDF-to-image for vision: LOW — open question; verify whether Gemini accepts raw PDF bytes directly

**Research date:** 2026-03-17
**Valid until:** 2026-04-17 (30 days; Google.GenAI moves fast — re-check model IDs if > 2 weeks)
