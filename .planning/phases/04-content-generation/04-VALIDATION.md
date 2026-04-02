---
phase: 4
slug: content-generation
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-02
---

# Phase 4 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (net10.0) |
| **Config file** | `src/Api.Tests/StudyApp.Api.Tests.csproj` |
| **Quick run command** | `dotnet test /Users/Peyton/repos/study-webapp-pa-school/src/Api.Tests/StudyApp.Api.Tests.csproj --filter "Generation" --nologo` |
| **Full suite command** | `dotnet test /Users/Peyton/repos/study-webapp-pa-school/src/Api.Tests/StudyApp.Api.Tests.csproj --nologo` |
| **Estimated runtime** | ~15 seconds |

**Baseline:** 52 passed, 2 failed (pre-existing, unrelated to Phase 4), 1 skipped. Total: 55.

---

## Sampling Rate

- **After every task commit:** Run `dotnet test .../Api.Tests --filter "Generation" --nologo`
- **After every plan wave:** Run `dotnet test .../Api.Tests --nologo`
- **Before `/gsd:verify-work`:** Full suite must be green (minus 2 pre-existing failures)
- **Max feedback latency:** ~15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 4-01-01 | 01 | 0 | GEN-01–08 | unit/integration | `dotnet test --filter "Generation" --nologo` | ❌ Wave 0 | ⬜ pending |
| 4-02-01 | 02 | 1 | GEN-01–07 | unit | `dotnet test --filter "SectionGenerationJobTests" --nologo` | ❌ Wave 0 | ⬜ pending |
| 4-02-02 | 02 | 1 | GEN-04, GEN-07 | unit | `dotnet test --filter "AlgorithmicDetectionTests" --nologo` | ❌ Wave 0 | ⬜ pending |
| 4-02-03 | 02 | 1 | GEN-08 | unit | `dotnet test --filter "StubGenerationProviderTests" --nologo` | ❌ Wave 0 | ⬜ pending |
| 4-03-01 | 03 | 1 | Trigger | integration | `dotnet test --filter "GenerationTriggerTests" --nologo` | ❌ Wave 0 | ⬜ pending |
| 4-04-01 | 04 | 2 | GEN-01–08 | e2e | `dotnet test --filter "Generation" --nologo` | ❌ Wave 0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `src/Api.Tests/Generation/SectionGenerationJobTests.cs` — stubs for GEN-01 through GEN-07
- [ ] `src/Api.Tests/Generation/AlgorithmicDetectionTests.cs` — GEN-07 keyword detection
- [ ] `src/Api.Tests/Generation/StubGenerationProviderTests.cs` — GEN-08 stub correctness
- [ ] `src/Api.Tests/Generation/GenerationTriggerTests.cs` — integration for POST /generate

*Existing xUnit + WebApplicationFactory infrastructure covers all needs — no framework installs required.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Frontend "Generate" button appears and triggers generation | GEN-01 | UI rendering requires browser | Upload PPTX, run extraction, confirm "Generate Study Materials" section appears and button triggers job |
| Study guide page displays all 5 sections | GEN-01 | UI rendering | After generation completes, navigate to a section and confirm: Direct Answer, High-Yield Details, Key Tables, Must-Know Numbers, Sources sections all visible |
| Flashcards and quiz shown per section | GEN-02, GEN-03 | UI rendering | Navigate to flashcard and quiz views; confirm source refs (slide/page) displayed |
| Concept map rendered for algorithmic section | GEN-04 | Mermaid rendering | Upload doc with "algorithm"/"flowchart" keyword; confirm concept map rendered |
| "Not found in your sources" message shown | GEN-05 | UI text | Use stub mode; confirm sentinel text renders correctly in UI |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
