# Requirements: PA Study App

**Defined:** 2026-03-16
**Core Value:** Every piece of generated content must be traceable to uploaded source material — if it isn't in the sources, the app says so rather than hallucinating.

## v1 Requirements

### Infrastructure

- [ ] **INFRA-01**: Developer can run app locally with Docker Compose (Postgres + MinIO) without paid API keys
- [x] **INFRA-02**: .NET 8 ASP.NET Core Web API serves as backend
- [x] **INFRA-03**: .NET 8 Worker Service handles background processing jobs
- [ ] **INFRA-04**: React + Vite + TypeScript frontend communicates with API
- [ ] **INFRA-05**: Hangfire job queue (Postgres-backed) schedules and tracks processing jobs
- [ ] **INFRA-06**: S3-compatible storage (MinIO locally, AWS S3/Cloudflare R2 in prod) stores uploaded files
- [ ] **INFRA-07**: PostgreSQL with EF Core migrations manages all relational data

### Auth

- [ ] **AUTH-01**: Developer can authenticate via X-Dev-UserId header (Guid) in local dev
- [ ] **AUTH-02**: Seeded DevUser exists for local development without login flow

### Document Ingestion

- [ ] **INGEST-01**: User can upload PPTX files as primary study sources
- [ ] **INGEST-02**: User can upload PDF files as supplementary sources
- [ ] **INGEST-03**: App extracts text from PPTX slides deterministically (OpenXML SDK)
- [ ] **INGEST-04**: App extracts text layer from PDFs when available
- [ ] **INGEST-05**: App detects PDF pages with no text layer and routes them for vision extraction
- [ ] **INGEST-06**: App extracts speaker notes from PPTX slides
- [ ] **INGEST-07**: App creates chunks per slide/page with metadata (file name, slide/page number)

### Objectives

- [ ] **OBJ-01**: User can paste learning objectives manually (copy from slides)
- [ ] **OBJ-02**: App parses objectives from common formats (bullets, numbered lists, line-separated)
- [ ] **OBJ-03**: Objectives are stored with sort order
- [ ] **OBJ-04**: App links each objective to relevant source chunks using deterministic scoring

### Figure Handling

- [ ] **FIG-01**: App detects figures (images) in PPTX slides and PDFs
- [ ] **FIG-02**: App filters out logos/watermarks using heuristics (repetition, small size, corner placement)
- [ ] **FIG-03**: App retains figures that have associated captions or labels (Figure, Table, Algorithm, Flowchart)
- [ ] **FIG-04**: User can review detected figures and toggle Keep/Ignore for each
- [ ] **FIG-05**: App extracts captions for kept figures via vision model

### Content Generation

- [ ] **GEN-01**: App generates objective answer pages (sections: Direct Answer, High-Yield Details, Key Tables, Must-Know Numbers, Sources)
- [ ] **GEN-02**: App generates flashcards (mix of cloze + Q&A format) with source references
- [ ] **GEN-03**: App generates micro-quizzes (3-7 questions) with source citations
- [ ] **GEN-04**: App generates concept maps for objectives with algorithmic content (Mermaid flowchart + JSON graph)
- [ ] **GEN-05**: App shows "Not found in your sources" when evidence is insufficient for a claim
- [ ] **GEN-06**: All generated content includes source references (slide/page numbers, file names)
- [ ] **GEN-07**: Concept map generation only triggers when algorithmic content is detected (keywords: algorithm, flowchart, workup, stepwise, if/then)
- [ ] **GEN-08**: App works in stub mode (deterministic fallbacks) without LLM API keys

### Study Experience

- [ ] **STUDY-01**: User can browse objective answer pages in reference mode (read freely like study notes)
- [ ] **STUDY-02**: User can view flashcards for any objective in reference mode
- [ ] **STUDY-03**: User can view concept maps for algorithmic objectives in reference mode
- [ ] **STUDY-04**: User can launch a guided study session for an objective (preview → flashcards → quiz → done)
- [ ] **STUDY-05**: App tracks basic progress per objective (cards reviewed, quizzes completed)

### LLM Integration

- [ ] **LLM-01**: Vision extraction uses Gemini (PDF OCR, figure captions)
- [ ] **LLM-02**: Content generation supports pluggable providers (Claude, Gemini, or Stub)
- [ ] **LLM-03**: Provider is configured via environment variables

## v2 Requirements

### Auth

- **AUTH-V2-01**: Real authentication system with user accounts and sessions
- **AUTH-V2-02**: OAuth login (Google)

### Document Ingestion

- **INGEST-V2-01**: Auto-detect and extract objectives from PPTX (slide titles, bullet patterns)
- **INGEST-V2-02**: Support text file uploads and pasted notes as sources

### Study Experience

- **STUDY-V2-01**: Spaced repetition scheduling for flashcard review
- **STUDY-V2-02**: Module-level progress dashboard with completion percentages

## Out of Scope

| Feature | Reason |
|---------|--------|
| External medical knowledge in generation | Violates grounding requirement — hallucinations are dangerous in medical education |
| Web browsing during generation | Same as above — only uploaded sources allowed |
| Mobile app | Web-first; mobile is a future milestone |
| Real-time collaboration | Not needed for solo study tool |
| OCR for all slides | Only for pages with no text layer; most PPTX is text-rich |
| Advanced spaced repetition | Basic progress tracking sufficient for MVP |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| INFRA-01 | Phase 1 | Pending |
| INFRA-02 | Phase 1 | Complete |
| INFRA-03 | Phase 1 | Complete |
| INFRA-04 | Phase 1 | Pending |
| INFRA-05 | Phase 1 | Pending |
| INFRA-06 | Phase 1 | Pending |
| INFRA-07 | Phase 1 | Pending |
| AUTH-01 | Phase 1 | Pending |
| AUTH-02 | Phase 1 | Pending |
| INGEST-01 | Phase 2 | Pending |
| INGEST-02 | Phase 2 | Pending |
| INGEST-03 | Phase 2 | Pending |
| INGEST-04 | Phase 2 | Pending |
| INGEST-05 | Phase 2 | Pending |
| INGEST-06 | Phase 2 | Pending |
| INGEST-07 | Phase 2 | Pending |
| LLM-01 | Phase 2 | Pending |
| LLM-02 | Phase 2 | Pending |
| LLM-03 | Phase 2 | Pending |
| OBJ-01 | Phase 3 | Pending |
| OBJ-02 | Phase 3 | Pending |
| OBJ-03 | Phase 3 | Pending |
| OBJ-04 | Phase 3 | Pending |
| FIG-01 | Phase 3 | Pending |
| FIG-02 | Phase 3 | Pending |
| FIG-03 | Phase 3 | Pending |
| FIG-04 | Phase 3 | Pending |
| FIG-05 | Phase 3 | Pending |
| GEN-01 | Phase 4 | Pending |
| GEN-02 | Phase 4 | Pending |
| GEN-03 | Phase 4 | Pending |
| GEN-04 | Phase 4 | Pending |
| GEN-05 | Phase 4 | Pending |
| GEN-06 | Phase 4 | Pending |
| GEN-07 | Phase 4 | Pending |
| GEN-08 | Phase 4 | Pending |
| STUDY-01 | Phase 5 | Pending |
| STUDY-02 | Phase 5 | Pending |
| STUDY-03 | Phase 5 | Pending |
| STUDY-04 | Phase 5 | Pending |
| STUDY-05 | Phase 5 | Pending |

**Coverage:**
- v1 requirements: 41 total
- Mapped to phases: 41/41 ✓
- Unmapped: 0

---
*Requirements defined: 2026-03-16*
*Last updated: 2026-03-16 after roadmap created*
