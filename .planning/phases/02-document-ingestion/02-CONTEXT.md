# Phase 2: Document Ingestion - Context

**Gathered:** 2026-03-17
**Status:** Ready for planning

<domain>
## Phase Boundary

PPTX and PDF upload pipeline: user can create a module (named exam unit), upload files to it, and the backend extracts text, creates chunks, and wires LLM providers. Phase ends when a file can be uploaded, processed by Hangfire, and its chunks are stored in the DB — with a real module UI the user can actually interact with. Objectives pasting and figure review are Phase 3.

</domain>

<decisions>
## Implementation Decisions

### Module/Upload UI Scope
- Build real module-creation UI — not just API endpoints
- Module = one exam prep unit (e.g., "Cardio Exam 1") — can contain multiple PPTX/PDF files
- User types the module name manually (simple text input)
- Module list page IS the homepage (`/` redirects to `/modules`) — no sidebar nav item needed yet
- Module detail page (`/modules/{id}`) shows uploaded files, per-file processing status, and the upload button
- Phase 3 will add objectives pasting to the same module detail page

### Module list page appearance
- Simple list: module name, overall status (Processing / Ready), date created — click to enter module detail
- No card grid, no fancy layout — Claude's discretion on exact styling within Tailwind + shadcn/ui

### Processing Progress UX
- Frontend polls `GET /documents/{id}/status` (or equivalent module endpoint) using React Query `refetchInterval`
- Document statuses: `Uploading → Queued → Processing → Ready / Failed`
- Status displayed inline on the module detail page — each file row shows its current status + spinner
- On failure: show "Failed" badge + a generic error message — detailed error logged server-side only
- No server-sent events, no toasts for completion

### File Management
- A module can have multiple files, no hard limit
- Duplicate uploads (same file, same module) are allowed — treated as a new document, reprocessed
- Users can delete a document (`DELETE /documents/{id}`) — removes DB record, chunks, and S3 file
- Upload goes through the API (multipart POST) — no presigned URL flow; DevAuth handles auth

### Vision Extraction Job Shape
- OCR runs as a **separate downstream Hangfire job**, not inline with chunking
  - Ingestion job: extract text → detect pages needing OCR → enqueue `VisionExtractionJob` per flagged page → ingestion job completes
  - Vision jobs run independently; each updates the page's chunk when done
- Document status stays `Processing` until ALL vision jobs for that document complete (not "partially ready")
- In stub mode: flagged PDF pages get placeholder text `"[Figure: vision extraction not available in stub mode]"` as their chunk content
- Phase 2 wires **real Gemini calls + stub toggle** — `IVisionProvider` interface with `GeminiVisionProvider` and `StubVisionProvider`; selected via `VISION_PROVIDER` env var
- Content generation providers (`IGenerationProvider`) are also wired in this phase (Claude, Gemini, Stub) via `GENERATION_PROVIDER` env var — stub mode must work without API keys

### Claude's Discretion
- Exact polling interval for React Query refetch
- Module list styling (within simple list constraint)
- S3 key naming convention for uploaded files
- Hangfire job retry policy
- Exact DB schema for `Documents` and `Chunks` tables beyond the required fields

</decisions>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches within the above decisions.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AppDbContext` (`src/Api/Data/AppDbContext.cs`): Add `Modules`, `Documents`, and `Chunks` DbSets + EF migration
- `DevAuthHandler` (`src/Api/Auth/DevAuthHandler.cs`): Existing auth — all new endpoints use it unchanged
- `src/Frontend/src/api/client.ts`: Axios client with interceptors — new domain files (`modules.ts`, `documents.ts`) follow established pattern
- React Query already configured — use `useQuery` with `refetchInterval` for polling

### Established Patterns
- Raw DTOs serialized to JSON, no envelope wrapper; errors use `ProblemDetails` (RFC 7807)
- Frontend API calls in `src/Frontend/src/api/` one file per domain
- shadcn/ui components added as-needed — install only what Phase 2 requires
- `appsettings.Development.json` for local secrets (gitignored); env vars for provider selection

### Integration Points
- Hangfire is installed (`Hangfire.AspNetCore`, `Hangfire.PostgreSql`) — register ingestion job and vision job in `Program.cs`
- AWSSDK.S3 is installed — add S3 service registration for file upload/delete
- Worker Service (`StudyApp.Worker`) handles background job execution — ingestion and vision jobs run here
- New pages: `/modules` (list) and `/modules/:id` (detail) — add routes to React Router config in `App.tsx`

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 02-document-ingestion*
*Context gathered: 2026-03-17*
