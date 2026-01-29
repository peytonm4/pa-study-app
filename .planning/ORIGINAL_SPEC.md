# Original Build Prompt

Source: `build-prompt.rtf`

You are Claude Code. Build an MVP web app for PA (Physician Assistant) didactic-phase graduate students that turns lecture materials into objective-driven study packs. Follow ALL requirements and decisions below. Output: a complete runnable monorepo with backend (.NET 8), worker services, DB migrations, docker compose, and a basic frontend. Prioritize correctness, grounding, and speed-to-MVP over polish.

========================
0) PRODUCT GOAL (MVP)
========================
Build a "slide-native, objective-driven" study web app for PA didactic curriculum.

Core differentiator:
- Students paste learning objectives for a module (manual copy/paste is the required MVP path).
- App indexes uploaded sources and generates study material that answers each objective using ONLY uploaded material.
- No hallucinations: if required info is missing in sources, the app must say "Not found in your sources."

Key user:
- PA didactic students (starting with an ADHD student who struggles with activation + structuring study).

[Full spec continues in build-prompt.rtf - this is a reference copy for planning]

See build-prompt.rtf for complete detailed specification including:
- Hard product rules (grounded-only generation)
- Outputs (objective answers, flashcards, quizzes, concept maps)
- Architecture decisions (.NET 8, React, PostgreSQL, Hangfire, MinIO/S3)
- Data model (EF Core entities)
- Ingestion pipeline (PPTX/PDF extraction, figure handling)
- Evidence linking (deterministic scoring)
- LLM generation requirements (Claude/Gemini/Stub modes)
- API endpoints
- Jobs (Hangfire worker)
- Frontend (React + Vite + TypeScript)
- Infrastructure (docker-compose)
- Quality criteria
