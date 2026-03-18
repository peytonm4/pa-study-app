---
phase: 2
slug: document-ingestion
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-17
---

# Phase 2 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 with `Microsoft.AspNetCore.Mvc.Testing` 10.0.5 |
| **Config file** | `src/Api.Tests/StudyApp.Api.Tests.csproj` |
| **Quick run command** | `dotnet test src/Api.Tests/ --no-build -x` |
| **Full suite command** | `dotnet test src/ --no-build` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test src/Api.Tests/ --no-build -x`
- **After every plan wave:** Run `dotnet test src/ --no-build`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 2-W0-01 | W0 | 0 | INGEST-01, INGEST-02 | Integration | `dotnet test src/Api.Tests/ -x --filter "FullyQualifiedName~DocumentUpload"` | ❌ Wave 0 | ⬜ pending |
| 2-W0-02 | W0 | 0 | INGEST-03, INGEST-06 | Unit | `dotnet test src/Api.Tests/ -x --filter "FullyQualifiedName~PptxExtractor"` | ❌ Wave 0 | ⬜ pending |
| 2-W0-03 | W0 | 0 | INGEST-04, INGEST-05 | Unit | `dotnet test src/Api.Tests/ -x --filter "FullyQualifiedName~PdfExtractor"` | ❌ Wave 0 | ⬜ pending |
| 2-W0-04 | W0 | 0 | LLM-01 | Unit | `dotnet test src/Api.Tests/ -x --filter "FullyQualifiedName~VisionProvider"` | ❌ Wave 0 | ⬜ pending |
| 2-W0-05 | W0 | 0 | LLM-02 | Unit | `dotnet test src/Api.Tests/ -x --filter "FullyQualifiedName~GenerationProvider"` | ❌ Wave 0 | ⬜ pending |
| 2-W0-06 | W0 | 0 | LLM-03 | Integration | `dotnet test src/Api.Tests/ -x --filter "FullyQualifiedName~ProviderConfig"` | ❌ Wave 0 | ⬜ pending |
| 2-xx-07 | TBD | TBD | INGEST-07 | Unit | `dotnet test src/Api.Tests/ -x --filter "FullyQualifiedName~Chunk"` | ❌ Wave 0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `src/Api.Tests/Documents/DocumentUploadTests.cs` — stubs for INGEST-01, INGEST-02
- [ ] `src/Api.Tests/Extraction/PptxExtractorTests.cs` — stubs for INGEST-03, INGEST-06; needs minimal `.pptx` fixture file
- [ ] `src/Api.Tests/Extraction/PdfExtractorTests.cs` — stubs for INGEST-04, INGEST-05; needs minimal `.pdf` fixture file (one text page + one blank page)
- [ ] `src/Api.Tests/Providers/VisionProviderTests.cs` — stubs for LLM-01 (stub path only)
- [ ] `src/Api.Tests/Providers/GenerationProviderTests.cs` — stubs for LLM-02 (stub path only)
- [ ] `src/Api.Tests/Providers/ProviderConfigTests.cs` — stubs for LLM-03 (env var DI wiring)
- [ ] Test fixture files: `src/Api.Tests/TestFixtures/sample.pptx` and `src/Api.Tests/TestFixtures/sample.pdf` (minimal; generated programmatically in test setup)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Module UI renders, upload button triggers file picker | INGEST-01 | Browser interaction required | Open `/modules`, create a module, click upload, verify file picker opens |
| Per-file status updates from Queued → Processing → Ready | INGEST-01, INGEST-02 | Polling timing and visual state | Upload a file, watch status badges update in real time |
| Vision extraction runs on flagged PDF pages | INGEST-05, LLM-01 | Requires Gemini API or stub end-to-end | Upload a scanned PDF, confirm chunk for blank page has placeholder or Gemini result |
| Delete document removes DB record + S3 object | INGEST-02 | S3 state verification | Upload, delete, confirm 404 on re-fetch and S3 key gone |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
