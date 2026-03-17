# Phase 1: Foundation - Research

**Researched:** 2026-03-17
**Domain:** .NET 8 ASP.NET Core + Worker Service, React + Vite, Docker Compose (Postgres + MinIO), EF Core, Hangfire, dev auth
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Repo/solution layout**
- `src/` monorepo — `src/Api/`, `src/Worker/`, `src/Frontend/` at the top level
- Single `.sln` file at repo root covering `StudyApp.Api` and `StudyApp.Worker`
- Project names: `StudyApp.Api` (ASP.NET Core Web API) and `StudyApp.Worker` (.NET Worker Service)
- EF Core DbContext and all migrations live inside `StudyApp.Api` — no separate data project
- No shared infrastructure project yet (YAGNI)

**Local dev startup**
- Docker Compose runs infra only: Postgres + MinIO
- .NET API, Worker Service, and React frontend run natively (dotnet run / npm run dev) for hot reload
- Startup: `docker compose up -d`, then `dotnet run` in each .NET project, then `npm run dev` in Frontend
- API and Worker always run as separate processes (matches production behavior)

**Frontend–API communication**
- Axios with hand-written TypeScript interfaces mirroring C# DTOs
- API types live in `src/Frontend/src/api/types.ts` — manually kept in sync with backend DTOs
- API call functions organized in `src/Frontend/src/api/` with one file per domain
- React Query (TanStack Query) for all server state

**Environment configuration**
- .NET backend: `appsettings.Development.json` for local values (gitignored for secrets); `appsettings.json` holds structure and non-secret defaults
- Frontend: `VITE_API_URL` in `.env.local` (gitignored); `.env` committed with default value `http://localhost:5000`
- Both `.env.example` and `appsettings.Development.example.json` committed to repo

**Frontend UI library**
- Tailwind CSS + shadcn/ui (built on Radix UI primitives)
- shadcn/ui components installed as-needed per phase — not all at once

**Frontend app shell**
- React Router v6 for routing
- Phase 1 delivers a minimal shell: root layout with basic nav/sidebar placeholder + one placeholder page at `/`

**Initial EF Core migration**
- Migration name: `InitialCreate` — creates a `Users` table (Id Guid, plus any minimal fields needed)
- One seeded DevUser row with fixed well-known Guid: `00000000-0000-0000-0000-000000000001`
- Seed via EF `HasData` — documented in README

**API response conventions**
- Raw DTOs/records serialized to JSON — no envelope wrapper
- Errors use ASP.NET Core `ProblemDetails` (RFC 7807)
- Frontend axios client handles ProblemDetails errors via an interceptor

### Claude's Discretion
- Exact port assignments for API, Worker health endpoint, and frontend dev server
- README structure and content
- Docker Compose service names and volume names
- Hangfire dashboard configuration (enable/disable in dev)
- Exact Users table schema beyond Id

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| INFRA-01 | Developer can run app locally with Docker Compose (Postgres + MinIO) without paid API keys | Docker Compose service definitions for postgres:16 and minio/minio images; health checks; named volumes |
| INFRA-02 | .NET 8 ASP.NET Core Web API serves as backend | `dotnet new webapi` template, Program.cs minimal hosting model, controller pattern |
| INFRA-03 | .NET 8 Worker Service handles background processing jobs | `dotnet new worker` template, IHostedService, separate process startup |
| INFRA-04 | React + Vite + TypeScript frontend communicates with API | `npm create vite@latest` react-ts template, Axios + TanStack Query wiring |
| INFRA-05 | Hangfire job queue (Postgres-backed) schedules and tracks processing jobs | Hangfire.AspNetCore + Hangfire.PostgreSql 1.21.1; AddHangfire + AddHangfireServer registration |
| INFRA-06 | S3-compatible storage (MinIO locally, AWS S3/Cloudflare R2 in prod) stores uploaded files | AWSSDK.S3 with ForcePathStyle=true + custom ServiceURL pointing at MinIO |
| INFRA-07 | PostgreSQL with EF Core migrations manages all relational data | Npgsql.EntityFrameworkCore.PostgreSQL; dotnet-ef CLI; InitialCreate migration |
| AUTH-01 | Developer can authenticate via X-Dev-UserId header (Guid) in local dev | Custom AuthenticationHandler<TOptions> that reads the header and builds ClaimsPrincipal |
| AUTH-02 | Seeded DevUser exists for local development without login flow | EF HasData with hardcoded Guid `00000000-0000-0000-0000-000000000001` in InitialCreate migration |
</phase_requirements>

---

## Summary

Phase 1 is a pure scaffolding phase on a greenfield .NET 8 + React codebase. Every technical component has a clear, well-documented setup path: .NET 8 uses the minimal hosting model (no Startup.cs), EF Core 8 targets Postgres via Npgsql, Hangfire uses its Postgres storage provider, and React uses Vite with TanStack Query + shadcn/ui.

The trickiest parts are (1) the X-Dev-UserId dev auth — it must be wired as a real ASP.NET Core authentication scheme (not just middleware) so `[Authorize]` works correctly from day one; (2) AWSSDK.S3 requires `ForcePathStyle=true` and a custom `ServiceURL` to work with MinIO; and (3) shadcn/ui requires `@/*` path aliases configured in BOTH `tsconfig.json` and `vite.config.ts` before running its init command.

**Primary recommendation:** Scaffold all five pieces (Docker Compose, API, Worker, Frontend, migrations) as separate tasks in sequence, wiring health checks as proof-of-life for each layer before moving to the next.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET SDK | 8.0 (LTS) | Runtime for API + Worker | Locked in requirements; LTS until Nov 2026 |
| ASP.NET Core Web API | 8.0 | HTTP server, routing, DI | Project requirement |
| .NET Worker Service | 8.0 | Background job host | Project requirement |
| EF Core + Npgsql provider | 8.x | ORM + Postgres driver | Standard EF pattern for .NET + Postgres |
| Hangfire.AspNetCore | 1.8.x | Job queue server/client API | Project decision |
| Hangfire.PostgreSql | 1.21.1 | Postgres storage for Hangfire | Latest stable; .NET Standard 2.0 compatible |
| AWSSDK.S3 | 3.7.x | S3-compatible file storage | Works with MinIO via path-style config |
| Vite | 6.x | Frontend build tool / dev server | Project requirement |
| React | 18.x | UI library | Project requirement |
| TypeScript | 5.x | Type safety | Project requirement |
| TanStack Query (React Query) | 5.x | Server state management | Locked decision |
| Axios | 1.x | HTTP client for API calls | Locked decision |
| React Router DOM | 6.x | Client-side routing | Locked decision |
| Tailwind CSS | 3.x | Utility CSS | Locked decision |
| shadcn/ui | latest | Component library (Radix-based) | Locked decision |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| @tanstack/react-query-devtools | 5.x | Dev panel for query state | Dev only, strip from prod build |
| dotnet-ef (global tool) | 8.x | CLI for add-migration / update-database | Developer machine only |
| postgres:16 (Docker image) | 16 | Local Postgres | Docker Compose infra |
| minio/minio (Docker image) | latest stable | Local S3 | Docker Compose infra |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.x | EF provider for Postgres | Required by both EF and Hangfire |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| AWSSDK.S3 | Minio .NET SDK | AWSSDK.S3 works in both local and prod (AWS/R2); Minio SDK is local-only |
| Hangfire.PostgreSql | Hangfire with Redis | Postgres avoids an extra service; locked decision |
| shadcn/ui | MUI / Ant Design | shadcn/ui is locked; Radix primitives give accessible headless components |

### Installation

.NET backend (from repo root):
```bash
dotnet new sln -n StudyApp
dotnet new webapi -n StudyApp.Api -o src/Api
dotnet new worker -n StudyApp.Worker -o src/Worker
dotnet sln add src/Api/StudyApp.Api.csproj src/Worker/StudyApp.Worker.csproj

# NuGet packages for API project
dotnet add src/Api/StudyApp.Api.csproj package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/Api/StudyApp.Api.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add src/Api/StudyApp.Api.csproj package Hangfire.AspNetCore
dotnet add src/Api/StudyApp.Api.csproj package Hangfire.PostgreSql
dotnet add src/Api/StudyApp.Api.csproj package AWSSDK.S3

# EF global tool
dotnet tool install --global dotnet-ef
```

Frontend (from src/Frontend):
```bash
npm create vite@latest . -- --template react-ts
npm install axios @tanstack/react-query @tanstack/react-query-devtools react-router-dom
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p
# shadcn/ui init (AFTER tsconfig path alias is configured)
npx shadcn@latest init -t vite
```

---

## Architecture Patterns

### Recommended Project Structure
```
/                               # repo root
├── StudyApp.sln
├── docker-compose.yml
├── .env.example                # template for docker compose env vars
├── src/
│   ├── Api/                    # StudyApp.Api
│   │   ├── StudyApp.Api.csproj
│   │   ├── Program.cs
│   │   ├── Controllers/
│   │   │   └── HealthController.cs
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── Migrations/
│   │   ├── Models/
│   │   │   └── User.cs
│   │   ├── Auth/
│   │   │   └── DevAuthHandler.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json    # gitignored
│   │   └── appsettings.Development.example.json
│   ├── Worker/                 # StudyApp.Worker
│   │   ├── StudyApp.Worker.csproj
│   │   ├── Program.cs
│   │   └── Worker.cs
│   └── Frontend/               # React app
│       ├── src/
│       │   ├── api/
│       │   │   ├── types.ts    # TypeScript mirrors of C# DTOs
│       │   │   ├── client.ts   # Axios instance + interceptors
│       │   │   └── health.ts   # Health check API calls
│       │   ├── components/
│       │   │   └── ui/         # shadcn/ui generated components
│       │   ├── layouts/
│       │   │   └── RootLayout.tsx
│       │   ├── pages/
│       │   │   └── HomePage.tsx
│       │   ├── App.tsx
│       │   └── main.tsx
│       ├── .env                # VITE_API_URL=http://localhost:5000 (committed)
│       ├── .env.local          # gitignored — developer overrides
│       ├── tsconfig.json
│       └── vite.config.ts
```

### Pattern 1: .NET 8 Minimal Hosting Model (Program.cs)
**What:** Single `Program.cs` replaces Startup.cs; services registered via `builder.Services`, pipeline via `app.Use*`.
**When to use:** All .NET 8 projects — Startup.cs is gone.
**Example:**
```csharp
// src/Api/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services
    .AddAuthentication("DevAuth")
    .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>("DevAuth", null);
builder.Services.AddAuthorization();

builder.Services.AddHangfire(cfg =>
    cfg.UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Default"))));
builder.Services.AddHangfireServer();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseHangfireDashboard("/hangfire");
app.Run();
```

### Pattern 2: Dev Auth Handler
**What:** Custom `AuthenticationHandler<TOptions>` that reads `X-Dev-UserId` header and constructs a `ClaimsPrincipal`. Only registered in Development environment.
**When to use:** Auth-01 requirement — allows `[Authorize]` on controllers without a real auth system.
**Example:**
```csharp
// src/Api/Auth/DevAuthHandler.cs
public class DevAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DevAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Dev-UserId", out var userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!Guid.TryParse(userId, out _))
            return Task.FromResult(AuthenticateResult.Fail("Invalid Guid"));

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

### Pattern 3: EF Core DbContext with HasData Seed
**What:** AppDbContext configured with Npgsql, Users entity, and `HasData` seeding a fixed-Guid DevUser.
**When to use:** Auth-02 + INFRA-07.
**Example:**
```csharp
// src/Api/Data/AppDbContext.cs
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = new Guid("00000000-0000-0000-0000-000000000001"),
            Name = "Dev User",
            Email = "dev@local"
        });
    }
}
```

Seed `HasData` requires hardcoded Guid values — never use `Guid.NewGuid()` here, as it generates a new migration every run.

### Pattern 4: AWSSDK.S3 with MinIO (ForcePathStyle)
**What:** `AmazonS3Client` configured with custom endpoint pointing at MinIO.
**When to use:** INFRA-06 — same code works against MinIO locally and AWS/R2 in prod by swapping config.
**Example:**
```csharp
// Registration in Program.cs (or extension method)
builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var s3Config = new AmazonS3Config
    {
        ServiceURL = builder.Configuration["Storage:ServiceUrl"], // http://localhost:9000
        ForcePathStyle = true   // REQUIRED for MinIO
    };
    return new AmazonS3Client(
        builder.Configuration["Storage:AccessKey"],
        builder.Configuration["Storage:SecretKey"],
        s3Config
    );
});
```

### Pattern 5: Axios Client with ProblemDetails Interceptor
**What:** Axios instance configured with base URL from env, response interceptor that normalises ProblemDetails errors.
**When to use:** INFRA-04 — all frontend API calls route through this client.
**Example:**
```typescript
// src/Frontend/src/api/client.ts
import axios from 'axios';

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5000',
  headers: { 'Content-Type': 'application/json' },
});

client.interceptors.response.use(
  (res) => res,
  (error) => {
    // RFC 7807 ProblemDetails shape
    const problem = error.response?.data;
    const message = problem?.detail ?? problem?.title ?? 'Unexpected error';
    return Promise.reject(new Error(message));
  }
);

// Dev auth header — set once after reading userId from env/config
export function setDevUserId(id: string) {
  client.defaults.headers.common['X-Dev-UserId'] = id;
}

export default client;
```

### Pattern 6: React Router v6 App Shell with Nested Layout
**What:** `createBrowserRouter` with a root layout route using `<Outlet>` for nested pages.
**When to use:** INFRA-04 — gives later phases a real place to add routes.
**Example:**
```tsx
// src/Frontend/src/App.tsx
import { createBrowserRouter, RouterProvider, Outlet } from 'react-router-dom';

function RootLayout() {
  return (
    <div className="flex min-h-screen">
      <nav className="w-56 bg-muted p-4">Sidebar placeholder</nav>
      <main className="flex-1 p-6"><Outlet /></main>
    </div>
  );
}

const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    children: [
      { index: true, element: <div>Home placeholder</div> },
    ],
  },
]);

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  );
}
```

### Anti-Patterns to Avoid
- **Startup.cs in .NET 8:** Does not exist — everything goes in Program.cs.
- **`Guid.NewGuid()` in HasData:** Generates a new migration on every `add-migration` run. Always hardcode Guid values.
- **Running EF migrations at app startup with `Database.Migrate()`:** Fine for demos, but creates a race condition when API and Worker both start simultaneously. Use a startup extension that runs only in the API, or run `dotnet ef database update` as an explicit step.
- **MinIO without `ForcePathStyle=true`:** AWSSDK.S3 defaults to virtual-hosted-style URLs (`bucket.host`) which MinIO does not support; requests will 404.
- **shadcn/ui init before tsconfig alias:** Running `npx shadcn@latest init` before adding `@/*` paths to tsconfig.json and vite.config.ts will error or misconfigure component paths.
- **Registering Hangfire in the Worker:** The Worker Service does NOT host the Hangfire dashboard or the HTTP server — it runs `IBackgroundJobServer`. Register `AddHangfireServer()` in the Worker; register the dashboard and `AddHangfire()` client in the API.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Job queue persistence | Custom job table + polling loop | Hangfire.PostgreSql | Retry logic, failure handling, dashboard, concurrency control |
| S3/object storage client | Manual HTTP calls to MinIO | AWSSDK.S3 | Multipart upload, presigned URLs, retry, all handled |
| Authentication scheme wiring | Custom middleware setting HttpContext.User | `AuthenticationHandler<TOptions>` | Required for `[Authorize]` and policy-based auth to work correctly |
| RFC 7807 error responses | Custom error object | `Results.Problem()` / `ProblemDetails` | Built into ASP.NET Core; consistent across all error paths |
| EF Core migration runner | Custom SQL scripts | `dotnet ef database update` / `context.Database.Migrate()` | Handles schema versioning, ordering, and rollback tracking |

**Key insight:** Every infrastructure concern in this stack has a first-party or widely-adopted library. Custom solutions add maintenance burden with no benefit at MVP scale.

---

## Common Pitfalls

### Pitfall 1: CORS not configured for local dev
**What goes wrong:** Browser blocks API calls from `localhost:5173` (Vite dev server) to `localhost:5000` (API). Requests fail with CORS errors in the console.
**Why it happens:** ASP.NET Core's default CORS policy denies cross-origin requests.
**How to avoid:** Add `app.UseCors()` in Program.cs with a named policy that allows `http://localhost:5173`. Only enable the permissive policy in Development.
**Warning signs:** Network tab shows preflight `OPTIONS` returning 405 or missing `Access-Control-Allow-Origin` header.

### Pitfall 2: `appsettings.Development.json` not gitignored
**What goes wrong:** Connection string with MinIO credentials or other local secrets committed to source control.
**Why it happens:** Developers copy the example file and forget the gitignore.
**How to avoid:** Add `src/Api/appsettings.Development.json` and `src/Frontend/.env.local` to `.gitignore` before the first commit. Commit the `*.example` variants instead.
**Warning signs:** `git status` shows `appsettings.Development.json` as untracked/staged.

### Pitfall 3: Hangfire schema not created before first job enqueue
**What goes wrong:** API throws `NpgsqlException: relation "hangfire.job" does not exist` on first request.
**Why it happens:** `UsePostgreSqlStorage` creates the `hangfire` schema on startup, but if the DB isn't reachable yet (race condition with Docker Compose), schema creation is skipped and Hangfire enters degraded state silently.
**How to avoid:** Add a health check that verifies Postgres is up before the API accepts traffic. Use `docker compose`'s `depends_on` with `condition: service_healthy` for the Postgres service.
**Warning signs:** `hangfire.job` table missing after first startup; Hangfire dashboard shows no server registered.

### Pitfall 4: EF migration applied twice (API + Worker both call `Migrate()`)
**What goes wrong:** Race condition or constraint violation when two processes try to apply the same migration simultaneously.
**Why it happens:** If both API and Worker call `context.Database.Migrate()` at startup.
**How to avoid:** Only the API applies migrations (`context.Database.Migrate()` in API's startup). Worker assumes schema exists; if it needs DB access, it should wait via retry/health check.
**Warning signs:** EF throws `duplicate key` or migration history table conflict on parallel startup.

### Pitfall 5: `tsconfig.json` path aliases not set before shadcn init
**What goes wrong:** `npx shadcn@latest init` fails with "No import alias found in your tsconfig.json file" or generates components with incorrect import paths.
**Why it happens:** shadcn CLI reads tsconfig paths to determine where to place component files.
**How to avoid:** Add `"paths": { "@/*": ["./src/*"] }` to both `tsconfig.json` AND `tsconfig.app.json` (Vite generates two), and add the corresponding `resolve.alias` in `vite.config.ts`, BEFORE running shadcn init.
**Warning signs:** Component imports use relative paths instead of `@/components/ui/...`.

### Pitfall 6: MinIO image deprecation on Docker Hub
**What goes wrong:** `docker compose pull` may pull a stale or deprecated MinIO image.
**Why it happens:** MinIO stopped updating Docker Hub and Quay images in October 2025. Official images now come from `quay.io/minio/minio`.
**How to avoid:** Use `quay.io/minio/minio:latest` as the image in docker-compose.yml, not `minio/minio` from Docker Hub. Verify in docker-compose that MinIO exposes port 9000 (API) and 9001 (console).
**Warning signs:** MinIO container starts but returns outdated version or console is inaccessible.

---

## Code Examples

Verified patterns from official sources:

### Docker Compose — Postgres + MinIO
```yaml
# docker-compose.yml
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: studyapp
      POSTGRES_USER: studyapp
      POSTGRES_PASSWORD: studyapp
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U studyapp"]
      interval: 5s
      timeout: 5s
      retries: 5

  minio:
    image: quay.io/minio/minio:latest
    command: server /data --console-address ":9001"
    environment:
      MINIO_ROOT_USER: minioadmin
      MINIO_ROOT_PASSWORD: minioadmin
    ports:
      - "9000:9000"   # S3 API
      - "9001:9001"   # Web console
    volumes:
      - minio_data:/data

volumes:
  postgres_data:
  minio_data:
```

### EF Core — Migrations Commands
```bash
# From repo root
dotnet ef migrations add InitialCreate --project src/Api
dotnet ef database update --project src/Api
```

### Hangfire — Program.cs Registration (API)
```csharp
// In StudyApp.Api Program.cs
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Default"))));
builder.Services.AddHangfireServer(); // Only in API for job processing

// After app.Build():
app.UseHangfireDashboard("/hangfire"); // localhost:5000/hangfire in dev
```

### Hangfire — Program.cs Registration (Worker)
```csharp
// In StudyApp.Worker Program.cs
// Worker only needs to know how to enqueue/process — no dashboard
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Default"))));
builder.Services.AddHangfireServer();
```

### Frontend — TanStack Query Provider Setup
```tsx
// src/Frontend/src/main.tsx
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';

const queryClient = new QueryClient({
  defaultOptions: { queries: { staleTime: 5 * 60 * 1000, retry: 1 } },
});

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <App />
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  </React.StrictMode>
);
```

### shadcn/ui — tsconfig.json Required Alias (before init)
```json
// tsconfig.json AND tsconfig.app.json
{
  "compilerOptions": {
    "baseUrl": ".",
    "paths": { "@/*": ["./src/*"] }
  }
}
```
```typescript
// vite.config.ts
import path from 'path';
export default defineConfig({
  resolve: { alias: { '@': path.resolve(__dirname, './src') } },
  // ...
});
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Startup.cs + ConfigureServices | Minimal hosting model — Program.cs only | .NET 6 (2021) | No Startup.cs to create in .NET 8 |
| `UseHangfireServer()` | `AddHangfireServer()` in DI | Hangfire 1.7+ | Server registered via DI, not middleware |
| `minio/minio` on Docker Hub | `quay.io/minio/minio` | Oct 2025 | Docker Hub images may be stale |
| Hangfire Npgsql v5 | Npgsql v6+ required | Hangfire.PostgreSql 1.9+ | `Hangfire.PostgreSql` 1.21.1 requires Npgsql ≥ 6.0 |

**Deprecated/outdated:**
- `minio/minio` Docker Hub image: No longer updated as of Oct 2025. Use `quay.io/minio/minio`.
- `app.UseHangfireServer()` middleware pattern: Replaced by `AddHangfireServer()` DI registration.

---

## Open Questions

1. **Hangfire: API only or API + Worker both run the server?**
   - What we know: Hangfire server (job processor) can run in any process that has the DI registration.
   - What's unclear: Whether Worker Service should also run `AddHangfireServer()` or just enqueue jobs. For Phase 1 there are no jobs yet — only the queue infrastructure is scaffolded.
   - Recommendation: Register `AddHangfireServer()` in the API for Phase 1 (simpler). Move to Worker in Phase 2 when background jobs are actually implemented.

2. **EF migrations: `context.Database.Migrate()` on startup vs explicit CLI step**
   - What we know: Auto-migrate on startup is convenient for dev but risky with two processes.
   - What's unclear: Whether the user wants fully automated `Migrate()` or explicit CLI steps in the README.
   - Recommendation: Call `context.Database.Migrate()` in API's startup for Phase 1 dev experience (one less manual step). Document it. Revisit for production approach in a later phase.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None detected — greenfield project |
| Config file | None — Wave 0 must create |
| Quick run command | `dotnet test src/Api.Tests/ --no-build -v q` (once scaffolded) |
| Full suite command | `dotnet test --no-build -v q` |

> Note: This is a greenfield project. No tests exist yet. Phase 1 is infrastructure scaffolding; the primary proof-of-life is manual health check verification. Automated tests for Phase 1 behaviors would be integration tests requiring a live Postgres instance — not suitable for fast unit test runs. The planner should treat validation for this phase as smoke-test commands rather than a full test suite.

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| INFRA-01 | `docker compose up` starts Postgres + MinIO | smoke (manual) | `docker compose ps` shows healthy | ❌ manual |
| INFRA-02 | API health endpoint responds | smoke | `curl http://localhost:5000/health` | ❌ Wave 0 |
| INFRA-03 | Worker Service starts without error | smoke (manual) | Process exit code 0 after 5s | ❌ manual |
| INFRA-04 | Frontend loads and reaches API | smoke (manual) | Browser loads `http://localhost:5173` | ❌ manual |
| INFRA-05 | Hangfire tables exist in Postgres | smoke | `docker exec postgres psql -U studyapp -c "\dt hangfire.*"` | ❌ manual |
| INFRA-06 | MinIO bucket accessible via S3 client | smoke (manual) | API startup log shows S3 connection | ❌ manual |
| INFRA-07 | EF migrations apply cleanly | smoke | `dotnet ef database update` exits 0 | ❌ manual CLI |
| AUTH-01 | X-Dev-UserId header authenticates request | unit | `dotnet test --filter "DevAuth"` | ❌ Wave 0 |
| AUTH-02 | DevUser row exists after migration | smoke | `docker exec postgres psql -U studyapp -c "SELECT id FROM \"Users\""` | ❌ manual |

### Sampling Rate
- **Per task commit:** `dotnet build src/Api` and `dotnet build src/Worker` (compile check)
- **Per wave merge:** `docker compose up -d && curl http://localhost:5000/health`
- **Phase gate:** All smoke tests passing before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `src/Api.Tests/Auth/DevAuthHandlerTests.cs` — covers AUTH-01 (unit test, no DB needed)
- [ ] `src/Api.Tests/StudyApp.Api.Tests.csproj` — test project scaffold
- [ ] No additional framework install needed if `dotnet new xunit` is used

*(All other Phase 1 requirements are infrastructure smoke tests best verified with CLI commands, not automated unit tests.)*

---

## Sources

### Primary (HIGH confidence)
- Microsoft Learn — [Managing EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing) — migration patterns, `HasData` behavior
- Microsoft Learn — [Applying Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying) — `Database.Migrate()` pattern
- Microsoft Learn — [ASP.NET Core Authentication Overview](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/) — `AuthenticationHandler<TOptions>` pattern
- shadcn/ui official docs — [Vite Installation](https://ui.shadcn.com/docs/installation/vite) — init command, tsconfig alias requirement
- NuGet Gallery — [Hangfire.PostgreSql 1.21.1](https://www.nuget.org/packages/Hangfire.PostgreSql/) — current version, .NET Standard 2.0 support

### Secondary (MEDIUM confidence)
- [Build your first Hangfire job .NET8 with PostgreSQL](https://dev.to/pradeepradyumna/your-first-hangfire-job-fornet8-with-postgresql-30nd) — Program.cs setup code (cross-referenced with Hangfire docs)
- [MinIO Docker setup — DataCamp](https://www.datacamp.com/tutorial/minio-docker) — port mapping (9000/9001), volume configuration
- [MinIO S3 API compatibility](https://www.min.io/product/aistor/s3-api) — `ForcePathStyle` requirement confirmed
- [shadcn/ui tsconfig path alias discussion](https://github.com/shadcn-ui/ui/discussions/4702) — `tsconfig.app.json` must also have paths

### Tertiary (LOW confidence)
- MinIO Docker Hub deprecation (Oct 2025) — mentioned in [DataCamp MinIO Docker guide](https://www.datacamp.com/tutorial/minio-docker); use `quay.io/minio/minio` — **flag for validation when starting Docker Compose setup**

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all libraries are well-documented and version-pinnable
- Architecture: HIGH — .NET 8 minimal hosting model and React+Vite patterns are stable
- Pitfalls: MEDIUM — CORS, secret management, and EF migration pitfalls verified via official docs; MinIO Docker Hub deprecation is LOW (single source)

**Research date:** 2026-03-17
**Valid until:** 2026-04-17 (stable ecosystem; MinIO image source is the only fast-moving item)
