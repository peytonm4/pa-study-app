---
phase: 5
slug: study-experience
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-04
---

# Phase 5 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 + Microsoft.AspNetCore.Mvc.Testing 10.0.5 |
| **Config file** | `src/Api.Tests/StudyApp.Api.Tests.csproj` |
| **Quick run command** | `dotnet test src/Api.Tests --filter "Category=Study" --no-build` |
| **Full suite command** | `dotnet test src/Api.Tests` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test src/Api.Tests --no-build`
- **After every plan wave:** Run `dotnet test src/Api.Tests`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 5-01-01 | 01 | 0 | STUDY-01 | integration | `dotnet test src/Api.Tests --filter "StudyEndpointTests" --no-build` | ❌ W0 | ⬜ pending |
| 5-01-02 | 01 | 0 | STUDY-01 | integration | `dotnet test src/Api.Tests --filter "StudyEndpointTests" --no-build` | ❌ W0 | ⬜ pending |
| 5-01-03 | 01 | 1 | STUDY-02 | integration | `dotnet test src/Api.Tests --filter "StudyEndpointTests" --no-build` | ❌ W0 | ⬜ pending |
| 5-01-04 | 01 | 1 | STUDY-03 | integration | `dotnet test src/Api.Tests --filter "StudyEndpointTests" --no-build` | ❌ W0 | ⬜ pending |
| 5-01-05 | 01 | 1 | STUDY-05 | integration | `dotnet test src/Api.Tests --filter "StudyProgressTests" --no-build` | ❌ W0 | ⬜ pending |
| 5-02-01 | 02 | 2 | STUDY-01 | manual | n/a — UI smoke | n/a | ⬜ pending |
| 5-02-02 | 02 | 2 | STUDY-02 | manual | n/a — UI smoke | n/a | ⬜ pending |
| 5-02-03 | 02 | 2 | STUDY-03 | manual | n/a — UI smoke | n/a | ⬜ pending |
| 5-03-01 | 03 | 3 | STUDY-04 | manual | n/a — UAT plan | n/a | ⬜ pending |
| 5-03-02 | 03 | 3 | STUDY-05 | integration | `dotnet test src/Api.Tests --filter "StudyProgressTests" --no-build` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `src/Api.Tests/Study/StudyEndpointTests.cs` — stubs for STUDY-01, STUDY-02, STUDY-03
- [ ] `src/Api.Tests/Study/StudyProgressTests.cs` — stubs for STUDY-05 upsert logic
- [ ] `src/Api/Models/SectionProgress.cs` — new entity (required before migration)
- [ ] EF migration: `dotnet ef migrations add AddSectionProgress --project src/Api`
- [ ] `npx shadcn add tabs` (run in `src/Frontend/`) — generates `src/Frontend/src/components/ui/tabs.tsx`
- [ ] `npm install mermaid` (run in `src/Frontend/`) — required for concept map rendering

*All six items must be complete before Wave 1 tasks begin.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Guided study session step machine (preview → flashcards → quiz → done) | STUDY-04 | React state flow — no DOM test coverage for multi-step UI | Navigate to section, click "Start Study Session", advance through all 4 steps, verify section marked done |
| Concept map renders Mermaid SVG correctly | STUDY-03 | SVG rendering requires browser — not covered by API tests | Open a section with a concept map, verify diagram is visible and not a blank box |
| Section reference tabs (Study Guide / Flashcards / Concept Map) render correctly | STUDY-01/02/03 | Tab layout is visual — API tests confirm data, not rendering | Open section detail, click each tab, verify content loads without errors |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
