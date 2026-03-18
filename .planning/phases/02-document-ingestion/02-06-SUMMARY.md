---
phase: 02-document-ingestion
plan: "06"
subsystem: ui
tags: [react, react-router, tanstack-query, shadcn, tailwind, typescript, vite]

requires:
  - phase: 02-02
    provides: ModulesController, DocumentsController, GET /modules, POST /modules, POST /modules/:id/documents, GET /documents/:id/status, DELETE /documents/:id

provides:
  - ModuleListPage at /modules with create form and status badges
  - ModuleDetailPage at /modules/:id with per-document polling, upload, delete
  - modules.ts API client with list/get/create/delete
  - documents.ts API client with upload/getStatus/delete
  - shadcn Badge, Table, Input components
  - / → /modules redirect

affects:
  - phase-03 (quiz/study features will build on module detail page)
  - future-pages (any page needing module context uses modules.get())

tech-stack:
  added: [shadcn/ui Badge, shadcn/ui Table, shadcn/ui Input]
  patterns:
    - useQuery with refetchInterval for conditional polling (stops on Ready/Failed)
    - Inline file upload via hidden input ref triggered by button
    - Per-row polling queries keyed by document ID

key-files:
  created:
    - src/Frontend/src/api/modules.ts
    - src/Frontend/src/api/documents.ts
    - src/Frontend/src/pages/ModuleListPage.tsx
    - src/Frontend/src/pages/ModuleDetailPage.tsx
    - src/Frontend/src/components/ui/badge.tsx
    - src/Frontend/src/components/ui/table.tsx
    - src/Frontend/src/components/ui/input.tsx
  modified:
    - src/Frontend/src/App.tsx
    - src/Api/Controllers/ModulesController.cs

key-decisions:
  - "Added GET /modules/:id endpoint to backend (deviation Rule 2) — no list-documents endpoint existed; ModuleDetailPage cannot render without it"
  - "DocumentRow as child component so each document gets its own useQuery polling instance"
  - "initialData from parent query passed to polling query to avoid flash of empty state"
  - "modules.get() returns ModuleDetail with embedded documents array — single fetch on page load"

patterns-established:
  - "Polling pattern: refetchInterval: (query) => (terminal ? false : 3000)"
  - "File upload: hidden input ref, onChange triggers mutation, value reset after selection"

requirements-completed: [INGEST-01, INGEST-02]

duration: 12min
completed: 2026-03-17
---

# Phase 2 Plan 06: Frontend Module UI Summary

**React module list and detail pages with live document status polling via TanStack Query refetchInterval, backed by shadcn/ui Badge/Table/Input components**

## Performance

- **Duration:** ~12 min
- **Started:** 2026-03-17T20:13:32Z
- **Completed:** 2026-03-17T20:25:00Z
- **Tasks:** 2
- **Files modified:** 9

## Accomplishments
- ModuleListPage at /modules: simple list with Processing/Ready badges, inline create-module form, empty state
- ModuleDetailPage at /modules/:id: per-document status polling (stops on Ready/Failed), file upload via hidden input, delete buttons
- App.tsx updated: / redirects to /modules, /modules and /modules/:id routes wired to RootLayout
- modules.ts and documents.ts API domain files matching established client.ts axios pattern
- shadcn Badge, Table, Input installed; button was already present

## Task Commits

Each task was committed atomically:

1. **Task 1: API domain files and shadcn component installation** - `c5c4489` (feat)
2. **Task 2: ModuleListPage, ModuleDetailPage, and routing** - `e872e13` (feat)

**Plan metadata:** _(docs commit follows)_

## Files Created/Modified
- `src/Frontend/src/api/modules.ts` - list(), get(id), create(name), delete(id) with Module and ModuleDetail types
- `src/Frontend/src/api/documents.ts` - upload(moduleId, file), getStatus(id), delete(id) with DocumentStatus type
- `src/Frontend/src/pages/ModuleListPage.tsx` - Module list with status badges and inline create form
- `src/Frontend/src/pages/ModuleDetailPage.tsx` - File list with per-document polling, upload button, delete
- `src/Frontend/src/App.tsx` - Routes updated: / → /modules redirect, /modules, /modules/:id
- `src/Frontend/src/components/ui/badge.tsx` - shadcn Badge component
- `src/Frontend/src/components/ui/table.tsx` - shadcn Table component
- `src/Frontend/src/components/ui/input.tsx` - shadcn Input component
- `src/Api/Controllers/ModulesController.cs` - Added GET /modules/{id} endpoint with embedded documents

## Decisions Made
- Added `GET /modules/:id` to the backend (Rule 2 auto-fix) — ModuleDetailPage has no way to load its document list without it; the initial plan only specified frontend files but the required backend endpoint was missing
- Used `DocumentRow` as a child component so each document gets its own isolated `useQuery` instance with independent polling lifecycle
- Passed `initialData: doc` to polling query to prevent flash of empty state on first render
- `modules.get()` returns `ModuleDetail` (Module + embedded documents array) — one network round-trip on page load instead of two

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Added GET /modules/{id} backend endpoint**
- **Found during:** Task 2 (ModuleDetailPage implementation)
- **Issue:** No endpoint existed to fetch a module's documents list; without it ModuleDetailPage cannot render previously uploaded files on page load — only files uploaded in the current session would be visible
- **Fix:** Added `GetModule(Guid id)` action to ModulesController returning module with embedded Documents array; added `modules.get(id)` to frontend API and `ModuleDetail` type extending `Module`
- **Files modified:** src/Api/Controllers/ModulesController.cs, src/Frontend/src/api/modules.ts
- **Verification:** `dotnet build` exits 0; `npx tsc --noEmit` exits 0; `npm run build` exits 0
- **Committed in:** e872e13 (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 missing critical)
**Impact on plan:** Essential for page correctness — without GET /modules/:id the detail page cannot show previously uploaded files. No scope creep.

## Issues Encountered
None beyond the missing backend endpoint documented above.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Complete frontend UI for Phase 2 ingestion pipeline is live
- Module list is now the app homepage (/modules)
- Document upload → processing → status polling flow is fully wired
- Phase 3 (quiz/study features) can build on top of ModuleDetailPage or add new routes
- No blockers

---
*Phase: 02-document-ingestion*
*Completed: 2026-03-17*

## Self-Check: PASSED

- modules.ts: FOUND
- documents.ts: FOUND
- ModuleListPage.tsx: FOUND
- ModuleDetailPage.tsx: FOUND
- 02-06-SUMMARY.md: FOUND
- Commit c5c4489: FOUND
- Commit e872e13: FOUND
- TypeScript build: PASSED
