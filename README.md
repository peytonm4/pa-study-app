# PA Study App

A study tool for PA (Physician Assistant) didactic-phase students that transforms lecture slide decks into reorganized, study-ready materials — grounded exclusively in uploaded source content.

## What It Does

Students upload PPTX and PDF lecture files. The app:

1. **Extracts and reorganizes** scattered lecture content into a coherent topic hierarchy using an AI-powered lecture extractor skill
2. **Produces a downloadable .docx** — improved lecture notes the student can open in Word or Google Docs
3. **Extracts and curates figures** — diagrams, charts, and clinical images are detected, filtered (logos/watermarks removed automatically), and presented for Keep/Ignore review
4. **Generates grounded study materials** per topic section: study guide pages, flashcards, micro-quizzes, and concept maps for algorithmic content
5. **Traces every claim** to its source slide/page — if something isn't in the uploaded material, the app says so rather than hallucinating

## Why

Existing tools (NotebookLM, Quizlet) don't organize study materials to match how PA curriculum is structured. PA lectures jump around and are dense — students need comprehensive coverage of all material, not just what learning objectives happen to mention.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend API | .NET 8 ASP.NET Core Web API |
| Background jobs | .NET 8 Worker Service + Hangfire (Postgres-backed) |
| Database | PostgreSQL + EF Core |
| File storage | S3-compatible (MinIO locally, AWS S3 / Cloudflare R2 in prod) |
| Frontend | React + Vite + TypeScript + Tailwind CSS + shadcn/ui |
| LLM providers | Claude, Gemini, or Stub (no API keys required for local dev) |
| Extraction skills | Python (pymupdf, python-pptx, Pillow, docx) |

## Local Development

### Prerequisites

- Docker Desktop
- .NET 8 SDK
- Node.js 18+
- Python 3.10+ (for extraction skills)

### Start the stack

```bash
# 1. Start infrastructure (Postgres + MinIO)
docker compose up -d

# 2. Run database migrations
cd src/Api
dotnet ef database update

# 3. Start the API
dotnet run --project src/Api

# 4. Start the Worker
dotnet run --project src/Worker

# 5. Start the frontend
cd src/Frontend
npm install
npm run dev
```

### Environment configuration

Copy the example config and fill in your values:

```bash
cp src/Api/appsettings.Development.example.json src/Api/appsettings.Development.json
cp src/Frontend/.env.example src/Frontend/.env.local
```

The app runs in **stub mode** by default — no LLM API keys required. Set `GENERATION_PROVIDER=anthropic` or `GENERATION_PROVIDER=google` in your environment to use real providers.

### Dev authentication

All API requests use a dev auth header — no login flow needed locally:

```
X-Dev-UserId: 00000000-0000-0000-0000-000000000001
```

## Project Structure

```
src/
├── Api/              # ASP.NET Core Web API (controllers, EF models, jobs)
├── Worker/           # .NET Worker Service (Hangfire job execution)
├── Frontend/         # React + Vite frontend
└── skills/           # Python extraction skills
    └── lecture-extractor-v2/
        └── scripts/extract_images.py
```

## Build Status

| Phase | Description | Status |
|-------|-------------|--------|
| 1 | Foundation — local stack, dev auth, infra | Complete |
| 2 | Document Ingestion — PPTX/PDF upload, chunking, LLM providers | Complete |
| 3 | Figures and Lecture Extraction — figure review, reorganized .docx, structured sections | In progress |
| 4 | Content Generation — study guides, flashcards, quizzes, concept maps | Pending |
| 5 | Study Experience — reference mode + guided study sessions | Pending |
