# Roadmap: PA Study App

## Overview

Five phases that build the app from runnable skeleton to a complete study tool. Phase 1 creates the foundation students never see but everything depends on. Phase 2 gets source documents indexed and searchable. Phase 3 completes the input pipeline (objectives + figures). Phase 4 produces the grounded study materials. Phase 5 delivers the study experience that makes it all worth building.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Foundation** - Runnable local stack with dev auth and all infrastructure wired up
- [ ] **Phase 2: Document Ingestion** - PPTX and PDF sources uploaded, processed, and chunked with LLM providers wired
- [ ] **Phase 3: Objectives and Figures** - User input pipeline complete (objectives pasted + figures reviewed)
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
**Plans**: TBD

### Phase 3: Objectives and Figures
**Goal**: User completes the input pipeline — objectives pasted and figures reviewed — before generation begins
**Depends on**: Phase 2
**Requirements**: OBJ-01, OBJ-02, OBJ-03, OBJ-04, FIG-01, FIG-02, FIG-03, FIG-04, FIG-05
**Success Criteria** (what must be TRUE):
  1. User can paste a block of learning objectives (bullets, numbered, or line-separated) and the app parses them into individual objectives with sort order preserved
  2. Each objective is linked to the source chunks most relevant to it using deterministic scoring (no LLM required for this step)
  3. User sees a figures review UI listing all detected figures with Keep/Ignore toggles, with logos and watermarks already filtered out
  4. Figures that have labels or captions (Figure, Table, Algorithm, Flowchart) are pre-selected as Keep
  5. Captions are extracted via vision model for all kept figures
**Plans**: TBD

### Phase 4: Content Generation
**Goal**: The app generates grounded study materials for every objective, traceable to uploaded sources
**Depends on**: Phase 3
**Requirements**: GEN-01, GEN-02, GEN-03, GEN-04, GEN-05, GEN-06, GEN-07, GEN-08
**Success Criteria** (what must be TRUE):
  1. Every objective has a generated answer page with Direct Answer, High-Yield Details, Key Tables, Must-Know Numbers, and Sources sections
  2. Every objective has flashcards (cloze and Q&A mix) and a micro-quiz, each with slide/page source references
  3. Objectives with algorithmic content (algorithm, flowchart, workup, stepwise, if/then keywords) get a Mermaid concept map; objectives without do not
  4. When evidence is insufficient the app shows "Not found in your sources" rather than generating unsupported content
  5. The full generation pipeline runs end-to-end in stub mode without any LLM API keys configured
**Plans**: TBD

### Phase 5: Study Experience
**Goal**: Students can browse generated materials freely or complete a guided study session per objective
**Depends on**: Phase 4
**Requirements**: STUDY-01, STUDY-02, STUDY-03, STUDY-04, STUDY-05
**Success Criteria** (what must be TRUE):
  1. Student can open any objective and read its answer page, flashcards, and concept map in reference mode without being forced into a flow
  2. Student can launch a guided study session for any objective that walks through preview, flashcards, and quiz in sequence then marks the objective done
  3. After completing cards and a quiz, the objective shows progress (cards reviewed count, quiz completed status)
**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation | 1/5 | In Progress|  |
| 2. Document Ingestion | 0/TBD | Not started | - |
| 3. Objectives and Figures | 0/TBD | Not started | - |
| 4. Content Generation | 0/TBD | Not started | - |
| 5. Study Experience | 0/TBD | Not started | - |
