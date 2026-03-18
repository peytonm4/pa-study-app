# Requirements: PA Study App

**Defined:** 2026-03-16
**Core Value:** Every piece of generated content must be traceable to uploaded source material — if it isn't in the sources, the app says so rather than hallucinating.

## v1 Requirements

### Infrastructure

- [x] **INFRA-01**: Developer can run app locally with Docker Compose (Postgres + MinIO) without paid API keys
- [x] **INFRA-02**: .NET 8 ASP.NET Core Web API serves as backend
- [x] **INFRA-03**: .NET 8 Worker Service handles background processing jobs
- [x] **INFRA-04**: React + Vite + TypeScript frontend communicates with API
- [x] **INFRA-05**: Hangfire job queue (Postgres-backed) schedules and tracks processing jobs
- [x] **INFRA-06**: S3-compatible storage (MinIO locally, AWS S3/Cloudflare R2 in prod) stores uploaded files
- [x] **INFRA-07**: PostgreSQL with EF Core migrations manages all relational data

### Auth

- [x] **AUTH-01**: Developer can authenticate via X-Dev-UserId header (Guid) in local dev
- [x] **AUTH-02**: Seeded DevUser exists for local development without login flow

### Document Ingestion

- [x] **INGEST-01**: User can upload PPTX files as primary study sources
- [x] **INGEST-02**: User can upload PDF files as supplementary sources
- [x] **INGEST-03**: App extracts text from PPTX slides deterministically (OpenXML SDK)
- [x] **INGEST-04**: App extracts text layer from PDFs when available
- [x] **INGEST-05**: App detects PDF pages with no text layer and routes them for vision extraction
- [x] **INGEST-06**: App extracts speaker notes from PPTX slides
- [x] **INGEST-07**: App creates chunks per slide/page with metadata (file name, slide/page number)

### Figure Handling

- [x] **FIG-01**: App extracts figures (images) from PPTX slides and PDFs using the lecture extractor Python skill (`extract_images.py`)
- [x] **FIG-02**: App filters out logos, watermarks, template backgrounds, stock photos, and repeated elements automatically (handled by extraction script heuristics)
- [x] **FIG-03**: App pre-selects figures that have associated captions or labels (Figure, Table, Algorithm, Flowchart) as Keep
- [x] **FIG-04**: User can review detected figures and toggle Keep/Ignore for each before lecture extraction runs
- [ ] **FIG-05**: App extracts captions for kept figures via vision model (Gemini)

### Lecture Extraction

- [x] **LEXT-01**: After figure review, user can trigger lecture extraction for a module
- [ ] **LEXT-02**: App calls the lecture extractor Python skill as a subprocess, passing the curated figure list
- [x] **LEXT-03**: Lecture extractor reorganizes scattered lecture content into a coherent topic hierarchy (H1/H2/H3 sections), embedding kept figures at appropriate locations
- [x] **LEXT-04**: App stores the reorganized content as structured sections in the DB (heading level, content, source page refs, sort order) for use by Phase 4 generation
- [ ] **LEXT-05**: App stores the generated .docx in S3 and provides a download link on the module detail page
- [ ] **LEXT-06**: User can download the reorganized lecture as a .docx (their improved lecture notes)

### Content Generation

- [ ] **GEN-01**: App generates study guide pages per section (Direct Answer, High-Yield Details, Key Tables, Must-Know Numbers, Sources) grounded in reorganized section content
- [ ] **GEN-02**: App generates flashcards (mix of cloze + Q&A format) with source references, organized by section
- [ ] **GEN-03**: App generates micro-quizzes (3-7 questions) with source citations, organized by section
- [ ] **GEN-04**: App generates concept maps for sections with algorithmic content (Mermaid flowchart + JSON graph)
- [ ] **GEN-05**: App shows "Not found in your sources" when evidence is insufficient for a claim
- [ ] **GEN-06**: All generated content includes source references (slide/page numbers, file names)
- [ ] **GEN-07**: Concept map generation only triggers when algorithmic content is detected (keywords: algorithm, flowchart, workup, stepwise, if/then)
- [ ] **GEN-08**: App works in stub mode (deterministic fallbacks) without LLM API keys

### Study Experience

- [ ] **STUDY-01**: User can browse reorganized lecture sections and their study guide pages in reference mode
- [ ] **STUDY-02**: User can view flashcards for any section in reference mode
- [ ] **STUDY-03**: User can view concept maps for algorithmic sections in reference mode
- [ ] **STUDY-04**: User can launch a guided study session for a section (preview → flashcards → quiz → done)
- [ ] **STUDY-05**: App tracks basic progress per section (cards reviewed, quizzes completed)

### LLM Integration

- [x] **LLM-01**: Vision extraction uses Gemini (PDF OCR, figure captions)
- [x] **LLM-02**: Content generation supports pluggable providers (Claude, Gemini, or Stub)
- [x] **LLM-03**: Provider is configured via environment variables

### Python Skills Integration

- [x] **SKILL-01**: Worker can invoke Python skills as subprocesses (Phase 3 pattern)
- [x] **SKILL-02**: Python skill output (manifest JSON, structured sections) is parsed and stored by Worker
- [x] **SKILL-03**: App works in stub mode without Python skills installed (deterministic fallback content)

## v2 Requirements

### Auth

- **AUTH-V2-01**: Real authentication system with user accounts and sessions
- **AUTH-V2-02**: OAuth login (Google)

### Document Ingestion

- **INGEST-V2-01**: Support text file uploads and pasted notes as sources

### Study Experience

- **STUDY-V2-01**: Spaced repetition scheduling for flashcard review
- **STUDY-V2-02**: Module-level progress dashboard with completion percentages

### Skills Infrastructure

- **SKILL-V2-01**: Python skills run as a FastAPI sidecar service (replaces subprocess pattern when 2+ skills exist)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Learning objectives pasting | Objectives are unreliable proxies for exam content — lecture extractor's topic hierarchy replaces them |
| External medical knowledge in generation | Violates grounding requirement — hallucinations are dangerous in medical education |
| Web browsing during generation | Same as above — only uploaded sources allowed |
| Mobile app | Web-first; mobile is a future milestone |
| Real-time collaboration | Not needed for solo study tool |
| OCR for all slides | Only for pages with no text layer; most PPTX is text-rich |
| Advanced spaced repetition | Basic progress tracking sufficient for MVP |
| Inline docx viewer | Download to Word/Google Docs is sufficient for MVP |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| INFRA-01 | Phase 1 | Complete |
| INFRA-02 | Phase 1 | Complete |
| INFRA-03 | Phase 1 | Complete |
| INFRA-04 | Phase 1 | Complete |
| INFRA-05 | Phase 1 | Complete |
| INFRA-06 | Phase 1 | Complete |
| INFRA-07 | Phase 1 | Complete |
| AUTH-01 | Phase 1 | Complete |
| AUTH-02 | Phase 1 | Complete |
| INGEST-01 | Phase 2 | Complete |
| INGEST-02 | Phase 2 | Complete |
| INGEST-03 | Phase 2 | Complete |
| INGEST-04 | Phase 2 | Complete |
| INGEST-05 | Phase 2 | Complete |
| INGEST-06 | Phase 2 | Complete |
| INGEST-07 | Phase 2 | Complete |
| LLM-01 | Phase 2 | Complete |
| LLM-02 | Phase 2 | Complete |
| LLM-03 | Phase 2 | Complete |
| FIG-01 | Phase 3 | Complete |
| FIG-02 | Phase 3 | Complete |
| FIG-03 | Phase 3 | Complete |
| FIG-04 | Phase 3 | Complete |
| FIG-05 | Phase 3 | Pending |
| LEXT-01 | Phase 3 | Complete |
| LEXT-02 | Phase 3 | Pending |
| LEXT-03 | Phase 3 | Complete |
| LEXT-04 | Phase 3 | Complete |
| LEXT-05 | Phase 3 | Pending |
| LEXT-06 | Phase 3 | Pending |
| SKILL-01 | Phase 3 | Complete |
| SKILL-02 | Phase 3 | Complete |
| SKILL-03 | Phase 3 | Complete |
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
- v1 requirements: 47 total
- Mapped to phases: 47/47 ✓
- Unmapped: 0

---
*Requirements defined: 2026-03-16*
*Last updated: 2026-03-18 — removed objectives (OBJ-01–04), added LEXT and SKILL requirement groups, updated GEN/STUDY to reference sections instead of objectives*
