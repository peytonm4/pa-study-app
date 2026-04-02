---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
status: planning
stopped_at: Completed 04-content-generation-04-05-PLAN.md
last_updated: "2026-04-02T06:26:04.607Z"
progress:
  total_phases: 5
  completed_phases: 3
  total_plans: 25
  completed_plans: 24
  percent: 60
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-16)

**Core value:** Every piece of generated content must be traceable to uploaded source material — if it isn't in the sources, the app says so rather than hallucinating.
**Current focus:** Phase 4 - Study Material Generation (next)
**GitHub:** https://github.com/peytonm4/pa-study-app

## Current Position

Phase: 3 of 5 (Figures and Lecture Extraction) — **COMPLETE**
Plan: 7 of 7 — all plans executed and human-verified
Status: Ready to plan Phase 4

Progress: [██████░░░░] 60%

## Performance Metrics

*Updated after each plan completion*
| Phase 01-foundation P02 | 7 | 2 tasks | 11 files |
| Phase 01-foundation P01 | 7 | 2 tasks | 3 files |
| Phase 01-foundation P03 | 9min | 2 tasks | 15 files |
| Phase 01-foundation P04 | 25min | 3 tasks | 14 files |
| Phase 01-foundation P05 | 5min | 2 tasks | 0 files |
| Phase 02-document-ingestion P01 | 6min | 3 tasks | 14 files |
| Phase 02-document-ingestion P02 | 15min | 3 tasks | 9 files |
| Phase 02-document-ingestion P03 | 7min | 2 tasks | 13 files |
| Phase 02-document-ingestion P04 | 11min | 2 tasks | 12 files |
| Phase 02-document-ingestion P05 | 21min | 3 tasks | 19 files |
| Phase 02-document-ingestion P06 | 12min | 2 tasks | 9 files |
| Phase 02-document-ingestion P07 | 15min | 2 tasks | 1 files |
| Phase 03-objectives-and-figures P01 | 5min | 2 tasks | 7 files |
| Phase 03-objectives-and-figures P02 | 2min | 2 tasks | 10 files |
| Phase 03-objectives-and-figures P03 | 12min | 2 tasks | 8 files |
| Phase 03-objectives-and-figures P04 | 5min | 2 tasks | 6 files |
| Phase 03-objectives-and-figures P05 | 35min | 2 tasks | 9 files |
| Phase 03-objectives-and-figures P06 | 2min | 2 tasks | 3 files |
| Phase 03-objectives-and-figures P07 | human verify | 8 bugs fixed | 5 files |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- Objectives removed: learning objectives were unreliable proxies for exam content — replaced by lecture extractor topic hierarchy
- Python skills integration: subprocess pattern for Phase 3, FastAPI sidecar planned for Phase 4 when second skill added
- GitHub repo created: https://github.com/peytonm4/pa-study-app (public)
- Grounding-only generation: No model knowledge injected — hallucinations are dangerous in medical education
- Stub mode required: Must run locally without API keys
- Hangfire with Postgres storage: One less infrastructure piece (vs Redis)
- [Phase 01-foundation]: Forced .sln format via --format sln flag (dotnet 10 defaults to .slnx)
- [Phase 01-foundation]: Worker has no Hangfire registration in Phase 1 — deferred to Phase 2
- [Phase 01-foundation]: Use quay.io/minio/minio:latest (Docker Hub minio/minio deprecated Oct 2025)
- [Phase 01-foundation]: appsettings.*.json gitignored broadly; appsettings.Development.example.json deferred to plan 04
- [Phase 01-foundation]: Tailwind CSS v4 with @tailwindcss/vite plugin (CSS-based config, no tailwind.config.js); shadcn/ui New York style with Geist font
- [Phase 01-foundation]: Downgraded Vite 8 to Vite 6 for @tailwindcss/vite peer dep compatibility
- [Phase 01-foundation]: Docker Postgres mapped to host port 5433 (not 5432) due to system PostgreSQL 16 occupying 5432
- [Phase 01-foundation]: AppDbContextFactory added for EF design-time tooling so migrations can be generated before Program.cs wiring
- [Phase 01-foundation]: Microsoft.AspNetCore.Mvc.Testing 10.0.5 used for test infrastructure (legacy 2.x Authentication meta-package avoided)
- [Phase 01-foundation]: Phase 1 foundation verified end-to-end by human: docker compose, API, Worker, and frontend all confirmed working
- [Phase 01-foundation]: All Phase 1 ROADMAP success criteria confirmed true before Phase 2 begins
- [Phase 02-document-ingestion]: Worker references Api via ProjectReference (not duplicating models); EF 10.0.5 pinned in Worker to suppress Hangfire.PostgreSql transitive EF 10.0.4 conflict
- [Phase 02-document-ingestion]: DocumentStatus stored as string column via HasConversion<string>() for DB readability
- [Phase 02-document-ingestion]: IngestionJob stub in src/Api/Jobs/ shared between Api and Worker via ProjectReference
- [Phase 02-document-ingestion]: ProviderConfig singleton registered in Worker for LLM provider selection (stub/anthropic/google)
- [Phase 02-document-ingestion]: Worker ProjectReference added to Api.Tests for extractor type access
- [Phase 02-document-ingestion]: Google.GenAI 1.5.0 uses Client class (not GoogleAI); Blob.Data is byte[] not base64 string
- [Phase 02-document-ingestion]: Google.GenAI SDK uses Client (not GoogleAI) as main entry point in version 1.5.0
- [Phase 02-document-ingestion]: ProviderRegistration extension method replaces ProviderConfig singleton for testable env-var DI switching
- [Phase 02-document-ingestion]: Api.Tests gains Worker ProjectReference to enable unit-testing of provider implementations
- [Phase 02-document-ingestion]: Extractor interfaces (IPptxExtractor, IPdfExtractor, IVisionProvider) moved to Api project so IngestionJob and VisionExtractionJob in Api/Jobs can reference them without circular project dependency
- [Phase 02-document-ingestion]: WebApplicationFactory uses DevAuthHandler as type anchor — both Api and Worker generate ambiguous Program class from top-level statements
- [Phase 02-document-ingestion]: DbContextOptions<AppDbContext> registered as singleton directly in test factory to bypass Npgsql+InMemory dual-provider EF 10 conflict
- [Phase 02-document-ingestion]: Added GET /modules/:id backend endpoint (Rule 2) — no list-documents endpoint existed; ModuleDetailPage requires it to show previously uploaded files on load
- [Phase 02-document-ingestion]: DocumentRow as child component gives each document its own isolated polling useQuery instance with independent lifecycle
- [Phase 02-document-ingestion]: API runs on port 5159 (launchSettings.json default) not 5000; axios fallback updated to match; VITE_API_URL .env was already correct
- [Phase 02-document-ingestion]: All Phase 2 ROADMAP success criteria confirmed true against live stack; Phase 2 complete
- [Phase 03-objectives-and-figures]: Wave 0 stubs use only Xunit; no unimplemented type references — compilation always clean before implementation plans run
- [Phase 03-objectives-and-figures]: Figures FK to Document (not Module directly) — extraction runs per-document; Sections own by Module from DOCX extraction
- [Phase 03-objectives-and-figures]: ExtractionStatus HasConversion<string>() matches DocumentStatus pattern for DB readability
- [Phase 03-objectives-and-figures]: Python stubs use only stdlib — requirements.txt stays empty, scripts run anywhere without pip install
- [Phase 03-objectives-and-figures]: ISkillRunner registered as Scoped in DI; PYTHON_PROVIDER=stub|real env var switch mirrors VISION_PROVIDER pattern
- [Phase 03-objectives-and-figures]: FigureExtractionJob uses IConfiguration key Skills:BasePath for script path resolution — consistent with LectureExtractionJob pattern
- [Phase 03-objectives-and-figures]: FiguresController thumbnail URL is API proxy path /api/figures/{id}/thumbnail — no presigned S3 URLs
- [Phase 03-objectives-and-figures]: ISkillRunner moved from Worker.Skills to Api.Skills — Worker already references Api, so Api.Skills avoids circular project dependency
- [Phase 03-objectives-and-figures]: POST /extract returns 409 if ExtractionStatus not in NotStarted/Failed — prevents duplicate job enqueue
- [Phase 03-objectives-and-figures]: figures.ts API client mirrors documents.ts pattern; ModuleDetailPage extended with FigureCard grid, Keep/Ignore toggles, extraction polling, and download button
- [Phase 03-objectives-and-figures][BUG FIX]: API had AddHangfireServer() — removed; only Worker should execute jobs (API lacked IPptxExtractor DI)
- [Phase 03-objectives-and-figures][BUG FIX]: VisionExtractionJob used FindAsync after ExecuteUpdateAsync — EF tracking cache returned stale PendingVisionJobs; fixed with AsNoTracking query
- [Phase 03-objectives-and-figures][BUG FIX]: VisionExtractionJob never enqueued FigureExtractionJob after last vision job completed — added enqueue call
- [Phase 03-objectives-and-figures][BUG FIX]: FiguresController had [Route("api")] prefix — mismatched frontend /modules/{id}/figures calls; changed to [Route("")]
- [Phase 03-objectives-and-figures][BUG FIX]: ModulesController GET detail never returned ExtractionStatus or DocxS3Key — frontend always saw NotStarted
- [Phase 03-objectives-and-figures][BUG FIX]: setDevUserId() was never called at app startup — all API calls returned 401; added call in main.tsx
- [Phase 03-objectives-and-figures][BUG FIX]: MinIO studyapp bucket did not exist — S3 uploads silently failed before Hangfire enqueue; bucket created
- [Phase 03-objectives-and-figures]: Thumbnail endpoint returns SVG placeholder for stub/ S3 keys and on download failure — avoids broken images in stub mode
- [Phase 03-objectives-and-figures][BUG FIX]: Integration test routes used /api/ prefix — controllers have no prefix; all 5 fixed tests now pass
- [Phase 03-objectives-and-figures][BUG FIX]: Figures/extraction sections shown on empty module — gated on mod.documents.length > 0
- [Phase 03-objectives-and-figures][BUG FIX]: FigureExtractionJob crashed downloading stub/fig1.png — skip stub/ keys in caption loop; catch other S3 failures
- [Phase 03-objectives-and-figures][BUG FIX]: Figures query had no refetchInterval — stale empty cache after page load before jobs ran; added 5s polling until figures appear
- [Phase 03-objectives-and-figures][BUG FIX]: Download URL opened relative to frontend (:5173) not API (:5159) — prefixed with VITE_API_URL
- [Phase 03-objectives-and-figures]: Figure image previews removed from FigureCard — stub SVGs add no value
- [Phase 03-objectives-and-figures][BUG FIX]: IngestionJob threw on missing document (stale Hangfire jobs) — changed to early return
- [Phase 03-objectives-and-figures][BUG FIX]: Run Extraction button not disabled for Ready status; 409 failures were silent — added isExtractionDone guard + onError display
- [Phase 03-objectives-and-figures]: Phase 3 human-verified complete — full pipeline works end-to-end in stub mode
- [Post-phase-3 fixes]: docx download changed from window.open to axios blob download to carry auth header
- [Post-phase-3 fixes]: Module delete button added to ModuleListPage; delete cleans up figure S3 keys and docx
- [Post-phase-3 fixes]: Document delete cancels active ExtractionRuns instead of resetting module fields
- [Post-phase-3 fixes]: ExtractionRun entity introduced — ExtractionStatus/DocxS3Key/ExtractionError removed from Module; migration AddExtractionRunsTable applied
- [Post-phase-3 fixes]: LectureExtractionJob now takes (moduleId, runId); replaces old sections on re-run; re-run allowed after Ready/Failed
- [Post-phase-3 fixes]: Figures Review UI (Keep/Ignore cards) removed — auto-keep via has_caption heuristic; shows figure count only
- [Post-phase-3 fixes]: Objectives confirmed permanently removed from scope — lecture extractor topic hierarchy is the organizational unit
- [Post-phase-3 fixes]: StubFigureSkillRunner keys changed from stub/fig*.png to figures/fig*.png so caption path runs in unit tests (stub/ prefix is skipped in production)
- [Post-phase-3 fixes]: 13 new tests added — ExtractionRun trigger logic (block on Queued/Processing, allow re-run after Ready), document delete (204, 404, cancels active runs), module CRUD (list, detail with run status, NotStarted default, create, delete); 54/55 pass (1 skipped by design)
- [Post-phase-3 fixes]: Test isolation pattern established — each POST/DELETE test that mutates state uses a dedicated seeded resource; shared factories only used for read-only assertions
- [Phase 04-content-generation]: Wave 0 stub pattern for Phase 4: xUnit only, no production type imports, [Fact(Skip='Wave 0 stub')] — mirrors Phase 3 approach
- [Phase 04-content-generation]: GenerationRun adds UpdatedAt (absent from ExtractionRun) for polling freshness in status endpoint
- [Phase 04-content-generation]: Content entity JSON fields stored as plain string columns with [] defaults — consistent with existing SourcePageRefsJson pattern
- [Phase 04-content-generation]: SectionGenerationJob uses StudyApp.Api.Providers.IGenerationProvider (not Worker.Providers) to avoid circular project dependency
- [Phase 04-content-generation]: ContentGenerationJob marks GenerationRun Ready after enqueueing all section jobs — section failures update to Failed independently
- [Phase 04-content-generation]: IGenerationProvider moved to Api.Providers to avoid circular project dependency (Worker references Api)
- [Phase 04-content-generation]: SectionGenerationJob uses keyword-aware StubGenerationProvider — QuizPrompt must include 'quiz' keyword
- [Phase 04-content-generation]: generationStatusVariant placed at module scope alongside extractionStatusVariant; refetchInterval unified to poll both extraction and generation status; Generate Study Materials gated on extractionStatus=Ready

### Pending Todos

None.

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-04-02T06:26:04.602Z
Stopped at: Completed 04-content-generation-04-05-PLAN.md
Resume file: None
