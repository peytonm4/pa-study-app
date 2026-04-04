# Roadmap: PA Study App

## Overview

Five phases that build the app from runnable skeleton to a complete study tool. Phase 1 creates the foundation students never see but everything depends on. Phase 2 gets source documents indexed and searchable. Phase 3 completes the input pipeline (objectives + figures). Phase 4 produces the grounded study materials. Phase 5 delivers the study experience that makes it all worth building.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Foundation** - Runnable local stack with dev auth and all infrastructure wired up (completed 2026-03-17)
- [x] **Phase 2: Document Ingestion** - POSTA and PDF sources uploaded, processed, and chunked with LLM providers wired (completed 2026-03-18)
- [ ] **Phase 3: Figures and Lecture Extraction** - Figures curated, lecture reorganized into structured .docx + sections in DB
- [ ] **Phase 4: Content Generation** - Grounded study materials generated for every objective
- [ ] **Phase 5: Study Experience** - Students can browse and do guided study sessions

## Phase Details

### Phase 1: Foundation
**Goal**: Developer can run the complete local stack and make authenticated API requests
**Depends on**: Nothing (first phase)
**Requirements**: INFRA-01, INFRA-02, INFRA-03, INFRA-04, INFRA-05, INFRA-06, INFRA-07, AUTH-01, AUTH-02
**Success Criteria** (what must be TRUE):
  1. Running `docker compose up` starts Postgres and MinIO with no errors
  2. The .NET API and Worker Service start and respond to health checks
  3. The React frontend loads in a browser and can reach the API
  4. A developer can send requests with X-Dev-UserId header and the API treats them as authenticated
  5. EF Core migrations run successfully against the local Postgres instance
**Plans**: 5 plans

Plans:
- [ ] 01-01-PLAN.md — Docker Compose infra (Postgres + MinIO) and repo foundation
- [ ] 01-02-PLAN.md — .NET solution scaffold: StudyApp.Api + StudyApp.Worker projects with NuGet packages
- [ ] 01-03-PLAN.md — React + Vite frontend scaffold with Axios, React Query, shadcn/ui, app shell
- [ ] 01-04-PLAN.md — API core wiring: DevAuthHandler (TDD), AppDbContext, EF migration, Program.cs
- [ ] 01-05-PLAN.md — Full-stack smoke test and human verification checkpoint

### Phase 2: Document Ingestion
**Goal**: User can upload PPTX and PDF files and the app produces indexed, searchable chunks
**Depends on**: Phase 1
**Requirements**: INGEST-01, INGEST-02, INGEST-03, INGEST-04, INGEST-05, INGEST-06, INGEST-07, LLM-01, LLM-02, LLM-03
**Success Criteria** (what must be TRUE):
  1. User can upload a PPTX and the app extracts text from every slide including speaker notes
  2. User can upload a PDF and the app extracts the text layer; pages with no text layer are flagged for vision extraction
  3. Each slide and page becomes a chunk with file name and slide/page number metadata attached
  4. Vision extraction (Gemini) runs on flagged PDF pages without manual intervention
  5. Content generation providers (Claude, Gemini, Stub) are selectable via environment variable with no code changes
**Plans**: 7 plans

Plans:
- [ ] 02-01-PLAN.md — NuGet packages, EF entities (Module/Document/Chunk), migration, Wave 0 test stubs
- [ ] 02-02-PLAN.md — IStorageService, ModulesController, DocumentsController, Worker Hangfire wiring
- [ ] 02-03-PLAN.md — TDD: PptxExtractor (OpenXML) and PdfExtractor (PdfPig)
- [ ] 02-04-PLAN.md — TDD: IVisionProvider/IGenerationProvider interfaces, stub and real provider implementations
- [ ] 02-05-PLAN.md — IngestionJob and VisionExtractionJob Hangfire jobs; upload integration test
- [ ] 02-06-PLAN.md — React frontend: ModuleListPage, ModuleDetailPage, routing, status polling
- [ ] 02-07-PLAN.md — Human verification: end-to-end pipeline confirmation

### Phase 3: Figures and Lecture Extraction
**Goal**: User curates extracted figures, then the lecture extractor skill reorganizes scattered lecture content into a structured document — producing both a downloadable .docx and structured sections in the DB ready for generation
**Depends on**: Phase 2
**Requirements**: FIG-01, FIG-02, FIG-03, FIG-04, FIG-05, LEXT-01, LEXT-02, LEXT-03, LEXT-04, LEXT-05, LEXT-06, SKILL-01, SKILL-02, SKILL-03
**Success Criteria** (what must be TRUE):
  1. After document ingestion, figures are automatically extracted and filtered (logos, watermarks, template elements removed) with caption-labeled figures pre-selected as Keep
  2. User can review extracted figures on the module detail page and toggle Keep/Ignore for each
  3. After figure review, user can trigger lecture extraction — the Python skill runs as a subprocess and reorganizes content into a coherent topic hierarchy
  4. Reorganized content is stored in the DB as structured sections (heading level, content, source page refs) for Phase 4 generation
  5. User can download the reorganized lecture as a .docx from the module detail page
  6. The full figure extraction and lecture extraction pipeline works in stub mode without API keys or Python installed
**Plans**: 7 plans

Plans:
- [ ] 03-01-PLAN.md — Wave 0 test stubs for Skills, Figures, Extraction test classes
- [ ] 03-02-PLAN.md — Figure and Section EF entities, ExtractionStatus, EF migration, Python stub scripts
- [ ] 03-03-PLAN.md — ISkillRunner interface, ProcessSkillRunner, StubSkillRunner, Worker DI wiring (TDD)
- [ ] 03-04-PLAN.md — FigureExtractionJob, FiguresController (GET/PATCH endpoints), IngestionJob wiring (TDD)
- [ ] 03-05-PLAN.md — LectureExtractionJob (.docx generation), extract trigger and docx download endpoints (TDD)
- [ ] 03-06-PLAN.md — Frontend: figures review UI, extraction status polling, download button
- [ ] 03-07-PLAN.md — Human verification: end-to-end pipeline in stub mode

### Phase 4: Content Generation
**Goal**: The app generates grounded study materials for every section, traceable to uploaded sources
**Depends on**: Phase 3
**Requirements**: GEN-01, GEN-02, GEN-03, GEN-04, GEN-05, GEN-06, GEN-07, GEN-08
**Success Criteria** (what must be TRUE):
  1. Every section has a generated study guide page with Direct Answer, High-Yield Details, Key Tables, Must-Know Numbers, and Sources sections
  2. Every section has flashcards (cloze and Q&A mix) and a micro-quiz, each with slide/page source references
  3. Sections with algorithmic content (algorithm, flowchart, workup, stepwise, if/then keywords) get a Mermaid concept map; sections without do not
  4. When evidence is insufficient the app shows "Not found in your sources" rather than generating unsupported content
  5. The full generation pipeline runs end-to-end in stub mode without any LLM API keys configured
**Plans**: 6 plans

Plans:
- [ ] 04-01-PLAN.md — Wave 0 test stubs: Generation/ test directory with 13 skipped [Fact] stubs
- [ ] 04-02-PLAN.md — EF entities (GenerationRun, StudyGuide, Flashcard, QuizQuestion, ConceptMap) + migration
- [ ] 04-03-PLAN.md — StubGenerationProvider expansion + SectionGenerationJob (TDD)
- [ ] 04-04-PLAN.md — ContentGenerationJob orchestrator + POST /modules/{id}/generate endpoint (TDD)
- [ ] 04-05-PLAN.md — Frontend: Generate Study Materials section on ModuleDetailPage with polling
- [ ] 04-06-PLAN.md — Human verification: end-to-end pipeline in stub mode

### Phase 5: Study Experience
**Goal**: Students can browse generated materials freely or complete a guided study session per section
**Depends on**: Phase 4
**Requirements**: STUDY-01, STUDY-02, STUDY-03, STUDY-04, STUDY-05
**Success Criteria** (what must be TRUE):
  1. Student can open any section and read its study guide page, flashcards, and concept map in reference mode without being forced into a flow
  2. Student can launch a guided study session for any section that walks through preview, flashcards, and quiz in sequence then marks the section done
  3. After completing cards and a quiz, the section shows progress (cards reviewed count, quiz completed status)
**Plans**: 5 plans

Plans:
- [ ] 05-01-PLAN.md — Wave 0: SectionProgress entity + migration + test stubs + frontend deps (mermaid, shadcn tabs)
- [ ] 05-02-PLAN.md — Backend: StudyController with 6 endpoints + integration tests (TDD)
- [ ] 05-03-PLAN.md — Frontend reference mode: study.ts API client + StudyPage + SectionDetailPage + MermaidDiagram
- [ ] 05-04-PLAN.md — Frontend guided session: SessionPage step machine + ModuleDetailPage Study link
- [ ] 05-05-PLAN.md — Human verification: end-to-end study experience confirmation

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation | 5/5 | Complete    | 2026-03-17 |
| 2. Document Ingestion | 6/7 | Complete    | 2026-03-18 |
| 3. Figures and Lecture Extraction | 5/7 | In Progress|  |
| 4. Content Generation | 5/6 | In Progress|  |
| 5. Study Experience | 0/5 | Not started | - |
