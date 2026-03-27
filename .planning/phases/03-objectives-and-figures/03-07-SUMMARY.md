---
phase: 03-objectives-and-figures
plan: 07
status: complete
completed: "2026-03-20"
---

# 03-07 Summary — Human Verification + Live Bug Fixes

## Outcome

Phase 3 human verification completed. Pipeline works end-to-end in stub mode. Multiple live bugs found and fixed during manual testing session.

## Bugs Fixed During Verification

### Test URL prefix mismatch
- **Problem:** 5 integration tests used `/api/modules/...` and `/api/figures/...` routes — controllers have no `/api` prefix so all returned 404
- **Fix:** Corrected URLs in `LectureExtractionTriggerTests.cs` and `FigureToggleTests.cs` to match actual routes
- **Result:** 41/42 tests pass (1 skipped by design)

### Figures shown on module with no documents
- **Problem:** Figures Review and Lecture Extraction sections rendered immediately even on empty modules
- **Fix:** Both sections gated on `mod.documents.length > 0`

### FigureExtractionJob crashing on stub S3 keys
- **Problem:** Job saved figures to DB then tried to download `stub/fig1.png` from MinIO for captioning — file doesn't exist, job threw `NoSuchKeyException`, retried 3x (creating 3 duplicate figures)
- **Fix:** Skip caption download for keys starting with `stub/`; added try-catch for other S3 failures so caption is optional

### Figures query stale on page load
- **Problem:** `useQuery` for figures had no `refetchInterval` — if page loaded before extraction job completed, figures cached as empty forever
- **Fix:** Added `refetchInterval: 5000` until figures appear

### Download 404 in browser
- **Problem:** `window.open('/modules/{id}/docx/download')` opened relative to frontend (`:5173`) not API (`:5159`)
- **Fix:** Prefixed URL with `VITE_API_URL` before calling `window.open`

### Figure image previews removed
- **Problem:** Stub thumbnails are SVG placeholders with no real value; user requested removal
- **Fix:** Removed `<img>` element from `FigureCard`

### IngestionJob throws on stale Hangfire jobs
- **Problem:** Worker startup picked up old queued jobs from previous sessions; document no longer existed → job threw → 3 retries → failed state
- **Fix:** Changed `throw` to early `return` when document not found

### Run Extraction silently fails when status is Ready
- **Problem:** Button not disabled for `Ready` state; clicking returned 409 with no UI feedback
- **Fix:** Added `isExtractionDone` to disabled condition; added `onError` handler with visible error message

## Files Modified

- `src/Api.Tests/Extraction/LectureExtractionTriggerTests.cs` — fixed route URLs
- `src/Api.Tests/Figures/FigureToggleTests.cs` — fixed route URLs
- `src/Api/Jobs/FigureExtractionJob.cs` — skip stub S3 keys + catch on caption download
- `src/Api/Jobs/IngestionJob.cs` — return early instead of throw on missing document
- `src/Frontend/src/pages/ModuleDetailPage.tsx` — figures/extraction section gating, polling, download URL fix, figure previews removed, extraction error display

## Verification Status

- [x] 41 automated tests pass
- [x] Figure cards appear after document upload (stub mode: 1 figure)
- [x] Keep/Ignore toggle works without page reload
- [x] Run Extraction queues job → Queued → Processing → Ready
- [x] Download button appears when Ready
- [x] .docx downloads successfully (contains stub headings)
- [x] Full pipeline works in stub mode

Phase 3 complete.
