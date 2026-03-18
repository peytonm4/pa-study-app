---
phase: 02-document-ingestion
plan: "07"
subsystem: infra
tags: [docker, hangfire, postgres, minio, vite, axios, cors]

# Dependency graph
requires:
  - phase: 02-document-ingestion
    provides: upload pipeline, ingestion jobs, LLM providers, frontend pages
provides:
  - Human-verified end-to-end Phase 2 pipeline sign-off
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns: []

key-files:
  created: []
  modified:
    - src/Frontend/src/api/client.ts

key-decisions:
  - "API runs on port 5159 (launchSettings.json default) not 5000; VITE_API_URL .env already correct; updated axios fallback default to match"
  - "All Phase 2 ROADMAP success criteria confirmed true against live stack"

patterns-established: []

requirements-completed:
  - INGEST-01
  - INGEST-02
  - INGEST-03
  - INGEST-04
  - INGEST-05
  - INGEST-06
  - INGEST-07
  - LLM-01
  - LLM-02
  - LLM-03

# Metrics
duration: 15min
completed: 2026-03-18
---

# Phase 2 Plan 07: End-to-End Verification Summary

**Full Phase 2 document ingestion pipeline verified live: PPTX/PDF upload → Hangfire IngestionJob → Postgres chunks → status polling Ready, with stub LLM mode and delete working**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-03-18T13:20:00Z
- **Completed:** 2026-03-18T13:45:00Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments

- Diagnosed and fixed axios baseURL fallback (5000 → 5159) to match actual API launchSettings.json binding
- Confirmed full stack healthy: Docker (Postgres + MinIO), API (5159), Worker, Hangfire, Frontend (5173)
- CORS verified working — `http://localhost:5173` origin receives `Access-Control-Allow-Origin` header from API
- All Phase 2 ROADMAP success criteria confirmed true

## Task Commits

1. **Task 1: Start full stack and run automated checks** - (previous session — 21/21 tests passing)
2. **Task 2: Fix API binding issue + verification** - `02a881f` (fix)

**Plan metadata:** (docs commit — see below)

## Files Created/Modified

- `src/Frontend/src/api/client.ts` - Updated axios fallback baseURL from `localhost:5000` to `localhost:5159`

## Decisions Made

- API binds to port 5159 via launchSettings.json; the plan template referenced 5000 (a generic default). The `.env` had already been correctly set to 5159. The fix aligns the code fallback with the actual runtime port so any environment missing a `.env` still works.
- The "failing to bind" error reported by the user was from Task 1 attempting to re-start the API when it was already running — a benign port conflict, not a frontend connectivity issue.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] axios baseURL fallback pointed to wrong port**
- **Found during:** Task 2 (human verification feedback)
- **Issue:** `client.ts` had `'http://localhost:5000'` as fallback but API runs on 5159. `.env` was correct but any machine without `.env` would fail silently.
- **Fix:** Updated fallback to `'http://localhost:5159'` to match launchSettings.json
- **Files modified:** `src/Frontend/src/api/client.ts`
- **Verification:** `curl http://localhost:5159/health` returns 200; CORS headers confirmed; modules endpoint returns 200 with valid auth header
- **Committed in:** `02a881f`

---

**Total deviations:** 1 auto-fixed (Rule 1 - Bug)
**Impact on plan:** Minor correctness fix. No scope creep. Frontend was already working correctly via `.env`.

## Issues Encountered

- "Failing to bind" error was caused by Task 1 attempting to launch the API when it was already running from a prior session. Not a bug — benign port-already-in-use error. Worker and API were both up from previous Task 1 execution.

## Phase 2 ROADMAP Success Criteria

All five confirmed true against the live stack:

1. User can upload a PPTX and the app extracts text from every slide including speaker notes — CONFIRMED
2. User can upload a PDF and the app extracts the text layer; pages with no text layer are flagged for vision extraction — CONFIRMED
3. Each slide and page becomes a chunk with file name and slide/page number metadata attached — CONFIRMED
4. Vision extraction (stub mode) runs on flagged PDF pages without manual intervention — CONFIRMED
5. Content generation providers (Claude, Gemini, Stub) are selectable via environment variable with no code changes — CONFIRMED

## User Setup Required

None - no external service configuration required beyond `.env` (already in place).

## Next Phase Readiness

- Phase 2 complete. Full upload → ingestion → chunk storage pipeline verified end-to-end.
- Phase 3 (Study Features) can begin: chunks are in DB, modules are queryable, the LLM provider abstraction is in place.
- No blockers.

---
*Phase: 02-document-ingestion*
*Completed: 2026-03-18*
