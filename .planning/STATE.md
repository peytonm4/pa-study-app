---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
status: planning
stopped_at: Phase 3 context gathered, requirements and roadmap updated, GitHub repo created and pushed
last_updated: "2026-03-18T19:21:17.772Z"
last_activity: 2026-03-16 — Roadmap created, ready to plan Phase 1
progress:
  total_phases: 5
  completed_phases: 2
  total_plans: 12
  completed_plans: 12
  percent: 0
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-16)

**Core value:** Every piece of generated content must be traceable to uploaded source material — if it isn't in the sources, the app says so rather than hallucinating.
**Current focus:** Phase 3 - Figures and Lecture Extraction
**GitHub:** https://github.com/peytonm4/pa-study-app

## Current Position

Phase: 1 of 5 (Foundation)
Plan: 0 of TBD in current phase
Status: Ready to plan
Last activity: 2026-03-16 — Roadmap created, ready to plan Phase 1

Progress: [░░░░░░░░░░] 0%

## Performance Metrics

**Velocity:**
- Total plans completed: 0
- Average duration: -
- Total execution time: 0 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| - | - | - | - |

**Recent Trend:**
- Last 5 plans: -
- Trend: -

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

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-03-18T19:21:17.754Z
Stopped at: Phase 3 context gathered, requirements and roadmap updated, GitHub repo created and pushed
Resume file: .planning/phases/03-objectives-and-figures/03-CONTEXT.md
