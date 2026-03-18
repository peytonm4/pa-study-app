---
phase: 03-objectives-and-figures
plan: "06"
subsystem: frontend
tags: [figures, extraction, react, typescript, tanstack-query]
dependency_graph:
  requires: [03-04, 03-05]
  provides: [figure-review-ui, extraction-trigger-ui, docx-download-ui]
  affects: [ModuleDetailPage]
tech_stack:
  added: []
  patterns: [useQuery-refetchInterval, useMutation-invalidate, responsive-grid]
key_files:
  created:
    - src/Frontend/src/api/figures.ts
  modified:
    - src/Frontend/src/pages/ModuleDetailPage.tsx
    - src/Frontend/src/api/modules.ts
decisions:
  - "Run Extraction button requires at least one figure to exist (hasFigures) — empty state guard"
  - "Module query polls every 3s until extractionStatus is Ready or Failed — same pattern as DocumentRow"
  - "hasReviewedFigure check uses figuresList?.some() — all figures have keep defined so gate is effectively hasFigures"
  - "Download opens in new tab via window.open(_blank) per locked decision (no inline viewer)"
metrics:
  duration: "2min"
  completed_date: "2026-03-18"
  tasks: 2
  files_changed: 3
---

# Phase 03 Plan 06: Figures Review UI and Lecture Extraction Summary

Frontend figures review grid, Keep/Ignore toggles, extraction status polling, and .docx download — backed by a new figures.ts API client.

## What Was Built

### Task 1: figures.ts API client (ca08ac4)

Created `src/Frontend/src/api/figures.ts` following the `documents.ts` pattern (axios client, typed responses):
- `FigureDto` interface (id, s3ThumbnailUrl, pageNumber, keep, labelType, caption)
- `figures.list(moduleId)` — GET /api/modules/{id}/figures
- `figures.toggle(figureId, keep)` — PATCH /api/figures/{id}
- `figures.runExtraction(moduleId)` — POST /api/modules/{id}/extract
- `figures.getDocxDownloadUrl(moduleId)` — GET /api/modules/{id}/docx

Updated `ModuleDetail` in `modules.ts` to add `extractionStatus` and `docxS3Key` fields.

### Task 2: Extended ModuleDetailPage (86274e2)

Added two new sections below the documents list:

**Figures Review section:**
- `FigureCard` component: thumbnail image, page badge, optional label badge, caption text, Keep/Ignore toggle button
- Responsive 3-column grid (lg:3, md:2, sm:1)
- `toggleMutation` with query invalidation on success
- Empty/loading states handled

**Lecture Extraction section:**
- `extractionStatus` badge with color coding (secondary/default/destructive) and pulse indicator for in-progress states
- Module query uses `refetchInterval` — polls every 3s until Ready or Failed
- Run Extraction button: disabled when extraction is running, no figures exist, or none reviewed
- Download button: only rendered when `extractionStatus === 'Ready'`, opens presigned URL in new tab

## Deviations from Plan

None — plan executed exactly as written.

## Verification

- `npx tsc --noEmit`: PASS (0 errors)
- `npm run build`: PASS (240 modules, built in ~4s)

## Self-Check

Files exist:
- src/Frontend/src/api/figures.ts: FOUND
- src/Frontend/src/pages/ModuleDetailPage.tsx: FOUND (modified)
- src/Frontend/src/api/modules.ts: FOUND (modified)

Commits:
- ca08ac4: feat(03-06): create figures.ts API client and extend ModuleDetail type
- 86274e2: feat(03-06): extend ModuleDetailPage with figure review and extraction sections
