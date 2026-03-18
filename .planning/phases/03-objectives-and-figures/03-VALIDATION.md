---
phase: 3
slug: objectives-and-figures
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-18
---

# Phase 3 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 |
| **Config file** | `src/Api.Tests/StudyApp.Api.Tests.csproj` |
| **Quick run command** | `dotnet test src/Api.Tests --filter "Category=Unit" --no-build` |
| **Full suite command** | `dotnet test src/Api.Tests --no-build` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test src/Api.Tests --no-build`
- **After every plan wave:** Run `dotnet test src/Api.Tests --no-build`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 3-W0-01 | W0 | 0 | SKILL-01 | unit | `dotnet test src/Api.Tests --filter "FullyQualifiedName~ProcessSkillRunnerTests" --no-build` | ❌ W0 | ⬜ pending |
| 3-W0-02 | W0 | 0 | SKILL-02, SKILL-03 | unit | `dotnet test src/Api.Tests --filter "FullyQualifiedName~StubSkillRunnerTests" --no-build` | ❌ W0 | ⬜ pending |
| 3-W0-03 | W0 | 0 | SKILL-03 | unit | `dotnet test src/Api.Tests --filter "FullyQualifiedName~SkillProviderConfigTests" --no-build` | ❌ W0 | ⬜ pending |
| 3-W0-04 | W0 | 0 | FIG-01, FIG-03 | unit | `dotnet test src/Api.Tests --filter "FullyQualifiedName~FigureExtractionJobTests" --no-build` | ❌ W0 | ⬜ pending |
| 3-W0-05 | W0 | 0 | FIG-04 | integration | `dotnet test src/Api.Tests --filter "FullyQualifiedName~FigureToggleTests" --no-build` | ❌ W0 | ⬜ pending |
| 3-W0-06 | W0 | 0 | LEXT-04 | unit | `dotnet test src/Api.Tests --filter "FullyQualifiedName~LectureExtractionJobTests" --no-build` | ❌ W0 | ⬜ pending |
| 3-W0-07 | W0 | 0 | LEXT-01 | integration | `dotnet test src/Api.Tests --filter "FullyQualifiedName~LectureExtractionTriggerTests" --no-build` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `src/Api.Tests/Skills/ProcessSkillRunnerTests.cs` — stubs for SKILL-01
- [ ] `src/Api.Tests/Skills/StubSkillRunnerTests.cs` — stubs for SKILL-02, SKILL-03
- [ ] `src/Api.Tests/Skills/SkillProviderConfigTests.cs` — stubs for SKILL-03 (env var selection)
- [ ] `src/Api.Tests/Figures/FigureExtractionJobTests.cs` — stubs for FIG-01, FIG-03
- [ ] `src/Api.Tests/Figures/FigureToggleTests.cs` — stubs for FIG-04
- [ ] `src/Api.Tests/Extraction/LectureExtractionJobTests.cs` — stubs for LEXT-04
- [ ] `src/Api.Tests/Extraction/LectureExtractionTriggerTests.cs` — stubs for LEXT-01

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Figure thumbnails display in UI | FIG-02 | Requires MinIO + browser rendering | Upload doc, navigate to module detail, verify figure thumbnails appear |
| Keep/Ignore toggles update UI immediately | FIG-04 | UI state verification | Toggle a figure, verify visual state changes without page reload |
| Lecture extraction trigger button appears after figure review | LEXT-01 | UI flow dependency | Complete figure review, verify "Extract Lecture" button becomes available |
| Download button appears and works after extraction | LEXT-05 | File download requires browser | Trigger extraction, verify download button enabled, click and verify .docx downloaded |
| Python subprocess runs end-to-end in stub mode | SKILL-03 | Integration smoke test | Set PYTHON_PROVIDER=stub, trigger full pipeline, verify completion without Python installed |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
