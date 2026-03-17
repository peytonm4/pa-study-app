# Phase 1: Foundation - Context

**Gathered:** 2026-03-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Scaffold the complete runnable local stack: .NET 8 API + Worker Service, React frontend, Docker Compose infra (Postgres + MinIO), dev auth via X-Dev-UserId header, and EF Core migrations. Phase ends when a developer can clone the repo, run docker compose up + two terminal commands, make authenticated API requests, and the frontend loads in a browser.

</domain>

<decisions>
## Implementation Decisions

### Repo/solution layout
- `src/` monorepo — `src/Api/`, `src/Worker/`, `src/Frontend/` at the top level
- Single `.sln` file at repo root covering `StudyApp.Api` and `StudyApp.Worker`
- Project names: `StudyApp.Api` (ASP.NET Core Web API) and `StudyApp.Worker` (.NET Worker Service)
- EF Core DbContext and all migrations live inside `StudyApp.Api` — no separate data project
- No shared infrastructure project yet (YAGNI — add if shared code actually accumulates)

### Local dev startup
- Docker Compose runs infra only: Postgres + MinIO
- .NET API, Worker Service, and React frontend run natively (dotnet run / npm run dev) for hot reload
- Startup: `docker compose up -d`, then `dotnet run` in each .NET project, then `npm run dev` in Frontend
- API and Worker always run as separate processes (matches production behavior)

### Frontend–API communication
- Axios with hand-written TypeScript interfaces mirroring C# DTOs
- API types live in `src/Frontend/src/api/types.ts` — manually kept in sync with backend DTOs
- API call functions organized in `src/Frontend/src/api/` with one file per domain (e.g., `modules.ts`, `documents.ts`)
- React Query (TanStack Query) for all server state — handles caching, loading/error states, refetching

### Environment configuration
- .NET backend: `appsettings.Development.json` for local values (gitignored for secrets); `appsettings.json` holds structure and non-secret defaults
- Frontend: `VITE_API_URL` in `.env.local` (gitignored); `.env` committed with default value `http://localhost:5000`
- Both `.env.example` and `appsettings.Development.example.json` committed to repo — new dev copies and runs

### Claude's Discretion
- Exact port assignments for API, Worker health endpoint, and frontend dev server
- README structure and content
- Docker Compose service names and volume names
- Hangfire dashboard configuration (enable/disable in dev)

</decisions>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches within the above decisions.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- None — greenfield project, empty directory

### Established Patterns
- None yet — this phase establishes the patterns

### Integration Points
- This phase creates all integration points that subsequent phases will build on

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 01-foundation*
*Context gathered: 2026-03-17*
