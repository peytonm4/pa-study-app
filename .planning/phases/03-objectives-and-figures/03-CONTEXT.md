# Phase 3: Objectives and Figures - Context

**Gathered:** 2026-03-18
**Status:** Ready for planning

<domain>
## Phase Boundary

User completes the input pipeline before generation begins. Phase 3 has two sequential steps:
1. **Figure review** — auto-extracted figures are presented for user curation (Keep/Ignore)
2. **Lecture extraction** — runs after figure review, reorganizes scattered lecture content into a coherent topic hierarchy, produces a downloadable .docx and structured sections in the DB

Phase ends when the module has a downloadable reorganized lecture document and structured section data ready for Phase 4 generation. **Objectives pasting is removed** — the lecture extractor's topic hierarchy replaces objectives as the organizing unit.

</domain>

<decisions>
## Implementation Decisions

### Objectives — removed
- No objectives pasting in Phase 3 or the app
- OBJ-01 through OBJ-04 are superseded by the lecture extractor approach
- Student-pasted learning objectives were found to be an unreliable proxy for exam content — students need comprehensive coverage of all lecture material
- Content is organized by the lecture extractor's topic hierarchy (H1/H2/H3 sections), not by objectives

### Figure review workflow
- `extract_images.py` Python script runs as a subprocess from the Worker immediately after document ingestion completes
- Script already handles all filtering: logos, icons, template backgrounds, stock photos, repeated elements, perceptual dedup — no additional filtering logic needed in C#
- Figure manifest (JSON) stored; user sees extracted figures on module detail page with Keep/Ignore toggles
- Figures that have associated labels/captions (Figure, Table, Algorithm, Flowchart keywords) are pre-selected as Keep
- Caption extraction via vision model (Gemini) runs for all kept figures
- **Figure review happens before lecture extraction** — user curates figures first, then extraction runs with the final figure list

### Lecture extraction workflow
- Triggered after user completes figure review (user action, not automatic)
- Python lecture extractor skill called as a subprocess from the Worker (Hangfire job)
- Runs with the curated figure list so figures are embedded in the correct locations in the output
- Produces two outputs:
  1. `.docx` file — stored in S3, downloadable from module detail page
  2. Structured sections data — stored in DB as hierarchical topic sections (heading level, content, source page refs) for Phase 4 generation

### Docx viewing
- Download only — no inline viewer
- Download button on module detail page, enabled once extraction job completes
- Students open in Word or Google Docs

### Python skill integration pattern
- Phase 3: Python scripts called as subprocesses from the .NET Worker
- Phase 4: Migrate to FastAPI sidecar service when second skill (flashcards/quizzes) is added — avoids managing Python environments across multiple Worker instances at scale
- Skills stay as standalone Python files, called via `Process.Start` in Phase 3

### DB entities (new in Phase 3)
- `Section` — stores reorganized content: module ID, heading level (1/2/3), heading text, content text, source page refs, sort order
- `Figure` — stores curated figures: document ID, S3 key, keep/ignore status, caption, source page, manifest metadata

### UI additions to module detail page
- Figures review section: list of extracted figures with thumbnail, source page, Keep/Ignore toggle, pre-selected state
- "Run Extraction" button — enabled after figure review, triggers lecture extraction job
- Download button — appears when extraction job completes, links to .docx in S3
- Extraction job status indicator (Queued → Processing → Ready / Failed)

### Claude's Discretion
- Exact subprocess invocation pattern (temp dir management, stdout/stderr handling)
- Section entity schema details beyond the required fields
- Figure thumbnail display approach in the review UI
- Hangfire job retry policy for extraction jobs

</decisions>

<specifics>
## Specific Ideas

- Lecture extractor skill already has production-quality filtering logic — do not reimplement in C#, call as-is
- Future skills (flashcards, quizzes, step diagrams) will follow the same subprocess pattern in Phase 3, then migrate to sidecar in Phase 4
- The .docx is a primary deliverable for students, not just an intermediate artifact — it's their reorganized lecture notes

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `IngestionJob` / `VisionExtractionJob` pattern: new `FigureExtractionJob` and `LectureExtractionJob` follow the same Hangfire job shape
- `IVisionProvider` / `GeminiVisionProvider`: figure caption extraction reuses the existing vision provider
- `IStorageService`: .docx output stored in S3 via existing storage service
- `src/Frontend/src/api/documents.ts` + `DocumentRow` polling pattern: figure and extraction job status can reuse the same polling approach

### Established Patterns
- Hangfire jobs enqueued from Controller or upstream job, executed in Worker
- Document status lifecycle (`Uploading → Queued → Processing → Ready / Failed`) — extraction job adds similar lifecycle for module-level extraction
- React Query `refetchInterval` polling for background job status
- shadcn/ui Badge + Button already imported in ModuleDetailPage

### Integration Points
- `ModuleDetailPage` gets two new sections: figures review UI and extraction status/download
- Worker gets two new Hangfire jobs: `FigureExtractionJob` (calls extract_images.py subprocess) and `LectureExtractionJob` (calls lecture extractor skill subprocess)
- New EF Core migration for `Sections` and `Figures` tables
- `src/skills/lecture-extractor-extracted/` contains the Python skill — Worker references this path

</code_context>

<deferred>
## Deferred Ideas

- FastAPI sidecar for Python skills — planned for Phase 4 when second skill is added
- Inline docx viewer in the app — not needed for MVP, students use Word/Google Docs
- Student-editable sections (reordering, removing content from reorganized lecture) — future phase
- Objectives as an optional layer on top of sections — could be revisited post-MVP if useful

</deferred>

---

*Phase: 03-objectives-and-figures*
*Context gathered: 2026-03-18*
