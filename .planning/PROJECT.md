# PA Study App

**STATUS: QUESTIONING IN PROGRESS** - Resume with `/gsd:new-project`

## What This Is

A web app for PA (Physician Assistant) didactic-phase students that transforms lecture materials into objective-driven study packs. Students upload PowerPoint slides and PDFs, paste their learning objectives, and get study materials (answers, flashcards, quizzes, concept maps) that are grounded exclusively in their uploaded sources - no hallucinations, no external medical knowledge.

The core differentiator: objectives-based organization. PA curriculum is structured around learning objectives, and existing tools (NotebookLLM, Quizlet) don't organize study materials to match that structure.

## Core Value

Every piece of generated content must be traceable to uploaded source material. If information isn't in the sources, the app says "Not found in your sources" rather than filling gaps with model knowledge. Trust and accuracy over comprehensiveness.

## Requirements

### Validated

(None yet — ship to validate)

### Active

**Document Ingestion:**
- [ ] Support PPTX uploads (primary format, ~75% text / 25% diagrams)
- [ ] Support PDF uploads (optional supplementary sources)
- [ ] Support text file/pasted notes (optional)
- [ ] Extract text from slides deterministically (OpenXML SDK)
- [ ] Extract PDF text layer when available
- [ ] Detect when PDF pages need vision extraction (no text layer)
- [ ] Handle speaker notes from PPTX
- [ ] Create chunks per slide/page with metadata

**Objectives Management:**
- [ ] Students paste objectives manually (MVP path - copy/paste from slides)
- [ ] Parse objectives from common formats (bullets, numbered, lines)
- [ ] Store objectives with sort order
- [ ] Link objectives to relevant source chunks (deterministic scoring)

**Figure Handling:**
- [ ] Detect figures in slides and PDFs
- [ ] Filter out logos/watermarks via heuristics (repetition, size, corner placement)
- [ ] Keep figures with captions/labels (Figure, Table, Algorithm, Flowchart)
- [ ] Provide "Figures Review" UI (Keep/Ignore toggles)
- [ ] Extract figure captions via vision when kept

**Grounded Content Generation:**
- [ ] Generate objective answer pages (sections: Direct Answer, High-Yield Details, Key Tables, Must-Know Numbers, Sources)
- [ ] Generate flashcards (mix of cloze + Q/A) with source references
- [ ] Generate micro-quizzes (3-7 questions) with source citations
- [ ] Generate concept maps for algorithmic content (diagnostic steps, treatment pathways)
- [ ] Show "Not found in your sources" when evidence is missing
- [ ] Store source references (slide/page numbers, file names) for all generated content

**Concept Maps (NEW requirement from users):**
- [ ] Detect algorithmic content via keywords/patterns (algorithm, flowchart, workup, stepwise, if/then)
- [ ] Generate Mermaid flowchart + JSON graph structure
- [ ] Only include steps/branches found in evidence (no invented logic)
- [ ] Map concept map nodes to source chunks
- [ ] Skip concept map if insufficient algorithmic content

**Study Flow:**
- [ ] "Start Study" mode: 30-60s preview → 5-10 cards → 3-question quiz → mark done
- [ ] Optional: show concept map in preview step
- [ ] Track basic progress (cards reviewed, quizzes completed)

**Architecture:**
- [ ] .NET 8 backend (ASP.NET Core Web API)
- [ ] .NET 8 Worker Service for background jobs
- [ ] PostgreSQL database with EF Core migrations
- [ ] S3-compatible storage (MinIO for local dev, AWS S3/Cloudflare R2 for prod)
- [ ] Hangfire for job queue (using Postgres storage)
- [ ] React frontend (Vite + TypeScript)
- [ ] Docker Compose for local infrastructure (Postgres + MinIO)

**LLM Integration:**
- [ ] Separate vision extraction (Gemini for PDF OCR, figure captions)
- [ ] Pluggable generation providers (Claude, Gemini, or Stub mode)
- [ ] Stub mode works without API keys (deterministic fallbacks)
- [ ] Config via environment variables

**Auth (MVP):**
- [ ] Dev auth via X-Dev-UserId header (Guid)
- [ ] Seeded DevUser for local development
- [ ] Document clearly in README

### Out of Scope

- Auto-detect objectives from PPTX (manual paste is MVP; can add later)
- Web browsing or external medical knowledge (violates grounding requirement)
- Real authentication system (MVP uses dev auth only)
- Advanced spaced repetition algorithms (basic progress tracking is sufficient)
- Mobile app (web-first)
- Real-time collaboration
- OCR for all slides (only when text layer missing)

## Context

**User Story:**
Built for PA didactic students who struggle with existing study tools. The builder's girlfriend and her classmates were frustrated with:
- NotebookLLM: Creates generic summaries, doesn't organize by objectives, doesn't provide evidence chronologically
- Quizlet: Good for definitions, too shallow for complex interconnected PA concepts

PA curriculum structure:
- Modules organized by topics (e.g., "Cardio Exam 1")
- Each module has learning objectives listed at start of slides
- Lecture slides answer those objectives throughout the deck
- Students need to map "which slides answer which objective" to study effectively

**Key insight from users:**
Concept maps (from NotebookLLM) are popular for studying systems/algorithms. Students want these tied to specific objectives.

**Workflow (NEEDS CLARIFICATION - questioning paused here):**
- User creates module → uploads PPTX/PDF → pastes objectives → hits "Generate"
- App indexes sources, maps chunks to objectives, generates study materials
- Student studies via... (TBD: objective answer pages? flashcards? study flow mode? need to clarify actual study session workflow)

## Constraints

- **Tech Stack**: .NET 8 backend, React frontend, PostgreSQL, S3-compatible storage (per detailed spec)
- **Grounding**: All generated content MUST be traceable to uploaded sources - no hallucinations allowed
- **MVP Speed**: Prioritize correctness and speed-to-MVP over polish
- **Format Reality**: PowerPoints are primary format (~75% text / 25% diagrams), PDFs are supplementary
- **Development**: Must run locally without paid API keys (stub mode required)

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Objectives are manual-paste only (MVP) | Auto-detection blocks MVP; user paste is reliable | — Pending |
| Grounding-only generation (no model knowledge) | Trust is critical for medical education; hallucinations are dangerous | — Pending |
| .NET 8 backend | Builder's preference from detailed spec | — Pending |
| Hangfire with Postgres storage | One less infrastructure piece (vs separate Redis) | — Pending |
| Stub mode required | Must work locally without API keys for development | — Pending |
| Concept maps from user request | PA students already use these from NotebookLLM; proven valuable | — Pending |

## Questioning Progress

**What we've covered:**
- ✓ Motivation: Built for girlfriend + classmates struggling with existing tools
- ✓ Problem with existing tools: Don't organize by objectives, don't provide evidence
- ✓ Concept maps: Users requested (from NotebookLLM), valuable for systems/algorithms
- ✓ Grounding requirement: Critical for medical education trust

**What we still need to clarify:**
- Actual study session workflow (how does a student use this the night before an exam?)
- Is this "generate once, reference during study" or "active study session tool" or both?
- When do students use objective answers vs flashcards vs quizzes vs concept maps?
- Any other must-have features not in the detailed spec?

---
*Last updated: 2026-01-29 during initial questioning (IN PROGRESS)*
