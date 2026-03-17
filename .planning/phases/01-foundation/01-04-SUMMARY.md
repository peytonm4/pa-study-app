---
phase: 01-foundation
plan: 04
subsystem: api
tags: [dotnet, aspnetcore, ef-core, postgres, hangfire, aws-s3, minio, auth, tdd, xunit]

# Dependency graph
requires:
  - phase: 01-02
    provides: "StudyApp.sln with Api + Worker projects and all Phase 1 NuGet packages"
  - phase: 01-03
    provides: "Docker Compose with Postgres and MinIO services"
provides:
  - "DevAuthHandler: X-Dev-UserId header auth scheme for ASP.NET Core"
  - "AppDbContext with Users DbSet and DevUser seed (Guid 00000000-0000-0000-0000-000000000001)"
  - "EF InitialCreate migration creating Users table"
  - "Program.cs wiring: EF, DevAuth, Hangfire, IAmazonS3, CORS, middleware pipeline"
  - "GET /health endpoint returning {status:'healthy'}"
  - "StudyApp.Api.Tests xunit project with 4 DevAuthHandler unit tests"
affects: [01-05, 02-01, 02-02]

# Tech tracking
tech-stack:
  added:
    - "Microsoft.AspNetCore.Mvc.Testing 10.0.5 (test infrastructure)"
    - "xunit 2.9.3 (unit tests)"
    - "dotnet-ef 10.0.5 (global EF tooling, upgraded from 7.0.0)"
  patterns:
    - "DevAuthHandler: custom ASP.NET Core auth scheme, reads X-Dev-UserId header, returns NoResult/Fail/Success"
    - "Design-time DbContext factory (AppDbContextFactory) for EF migrations without full Program.cs"
    - "db.Database.Migrate() on startup for Development convenience (not production pattern)"
    - "ForcePathStyle=true on AmazonS3Client required for MinIO path-style URLs"
    - "TDD cycle: RED (compile fail) → GREEN (all 4 pass) committed separately"

key-files:
  created:
    - "src/Api/Auth/DevAuthHandler.cs"
    - "src/Api/Models/User.cs"
    - "src/Api/Data/AppDbContext.cs"
    - "src/Api/Data/AppDbContextFactory.cs"
    - "src/Api/Controllers/HealthController.cs"
    - "src/Api/Migrations/20260317165831_InitialCreate.cs"
    - "src/Api/appsettings.Development.example.json"
    - "src/Api.Tests/StudyApp.Api.Tests.csproj"
    - "src/Api.Tests/Auth/DevAuthHandlerTests.cs"
  modified:
    - "src/Api/Program.cs"
    - "src/Api/appsettings.json"
    - "docker-compose.yml"
    - ".gitignore"
    - "StudyApp.sln"

key-decisions:
  - "Docker Postgres mapped to host port 5433 (not 5432) due to system PostgreSQL 16 occupying 5432"
  - "AppDbContextFactory added for EF design-time tooling (allows migrations without full DI wiring)"
  - "Microsoft.AspNetCore.Mvc.Testing used instead of Microsoft.AspNetCore.Authentication (2.x legacy package)"
  - ".gitignore exception added for appsettings.Development.example.json so template is committable"

patterns-established:
  - "DevAuth scheme: missing header=NoResult, invalid Guid=Fail, valid Guid=Success+NameIdentifier claim"
  - "EF HasData with literal DateTime values (not DateTime.UtcNow) to avoid regenerating migrations"

requirements-completed: [INFRA-02, INFRA-05, INFRA-06, INFRA-07, AUTH-01, AUTH-02]

# Metrics
duration: 25min
completed: 2026-03-17
---

# Phase 1 Plan 04: API Wiring Summary

**ASP.NET Core API fully wired: DevAuthHandler (X-Dev-UserId TDD auth), AppDbContext + EF migration + DevUser seed, Program.cs with Hangfire/S3/CORS, and GET /health — runnable at http://localhost:5100**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-03-17T16:55:00Z
- **Completed:** 2026-03-17T17:20:00Z
- **Tasks:** 3
- **Files modified:** 14

## Accomplishments
- DevAuthHandler passes 4 TDD unit tests: NoResult (missing header), Fail (invalid Guid), Success+NameIdentifier claim, DevAuth scheme name
- EF InitialCreate migration creates Users table and seeds DevUser (00000000-0000-0000-0000-000000000001)
- API starts cleanly: EF migrations applied, Hangfire tables created, IAmazonS3 registered, GET /health returns {"status":"healthy"}

## Task Commits

Each task was committed atomically:

1. **Task 1: DevAuthHandler with TDD unit tests** - `2fbb67f` (feat)
2. **Task 2: User model, AppDbContext, and EF migration** - `bcde193` (feat)
3. **Task 3: Program.cs full wiring + HealthController + config files** - `151f945` (feat)

**Plan metadata:** (docs commit — this summary)

## Files Created/Modified
- `src/Api/Auth/DevAuthHandler.cs` - Custom auth handler reading X-Dev-UserId header
- `src/Api/Models/User.cs` - User entity with Id, Name, Email, CreatedAt
- `src/Api/Data/AppDbContext.cs` - EF DbContext with Users DbSet and DevUser seed data
- `src/Api/Data/AppDbContextFactory.cs` - Design-time factory for EF migrations tooling
- `src/Api/Controllers/HealthController.cs` - GET /health returning {status:"healthy"}
- `src/Api/Migrations/20260317165831_InitialCreate.cs` - Creates Users table + seeds DevUser
- `src/Api/Program.cs` - Full wiring: EF, DevAuth, Hangfire, S3, CORS, middleware pipeline
- `src/Api/appsettings.json` - Added Postgres (port 5433) and MinIO config
- `src/Api/appsettings.Development.example.json` - Committed template for local dev setup
- `src/Api.Tests/StudyApp.Api.Tests.csproj` - xunit test project with Mvc.Testing reference
- `src/Api.Tests/Auth/DevAuthHandlerTests.cs` - 4 unit tests for DevAuthHandler
- `docker-compose.yml` - Changed Postgres host port from 5432 to 5433
- `.gitignore` - Added exception for appsettings.Development.example.json
- `StudyApp.sln` - Added Api.Tests project

## Decisions Made
- **Postgres port 5433:** System PostgreSQL 16 (EnterpriseDB) was occupying port 5432. Docker Compose postgres mapped to 5433:5432 instead. All connection strings updated accordingly.
- **AppDbContextFactory:** EF `dotnet ef migrations add` requires a resolvable DbContext. Since Program.cs wiring was deferred to Task 3, added `IDesignTimeDbContextFactory<AppDbContext>` to allow migration generation in Task 2.
- **Mvc.Testing over legacy Authentication package:** `Microsoft.AspNetCore.Authentication` resolves to version 2.3.9 (ASP.NET Core 2.x meta-package). Replaced with `Microsoft.AspNetCore.Mvc.Testing` 10.0.5 which provides all ASP.NET Core test infrastructure.
- **Gitignore exception for example file:** `.gitignore` had `appsettings.*.json` covering all environment-specific files. Added `!appsettings.Development.example.json` negation to allow committing the template.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added IDesignTimeDbContextFactory for EF migrations**
- **Found during:** Task 2 (User model and AppDbContext)
- **Issue:** `dotnet ef migrations add` failed — "Unable to create a 'DbContext' of type 'AppDbContext'" because EF tooling needed DI registration which is in Program.cs (Task 3)
- **Fix:** Created `AppDbContextFactory.cs` implementing `IDesignTimeDbContextFactory<AppDbContext>` with hardcoded connection string for tooling use only
- **Files modified:** `src/Api/Data/AppDbContextFactory.cs`
- **Verification:** Migration generated successfully; `InitialCreate.cs` contains correct Users table and DevUser seed
- **Committed in:** `bcde193` (Task 2 commit)

**2. [Rule 3 - Blocking] Changed Docker Postgres host port from 5432 to 5433**
- **Found during:** Task 3 (Program.cs wiring — API runtime test)
- **Issue:** Port 5432 occupied by system EnterpriseDB PostgreSQL 16 installation. Docker container failed with "address already in use".
- **Fix:** Changed `docker-compose.yml` port mapping to `5433:5432`. Updated all connection strings in appsettings.json, appsettings.Development.json, appsettings.Development.example.json, and AppDbContextFactory.cs.
- **Files modified:** `docker-compose.yml`, `src/Api/appsettings.json`, `src/Api/appsettings.Development.json`, `src/Api/appsettings.Development.example.json`, `src/Api/Data/AppDbContextFactory.cs`
- **Verification:** Postgres container started cleanly on port 5433; API connected, migrations applied, DevUser seeded
- **Committed in:** `151f945` (Task 3 commit)

**3. [Rule 3 - Blocking] Used Mvc.Testing instead of Microsoft.AspNetCore.Authentication**
- **Found during:** Task 1 (test project setup)
- **Issue:** `dotnet add package Microsoft.AspNetCore.Authentication` installs version 2.3.9 (legacy meta-package from ASP.NET Core 2.x), not the .NET 10 framework assembly needed to test auth handlers
- **Fix:** Removed the 2.x package; added `Microsoft.AspNetCore.Mvc.Testing` 10.0.5 which includes the full ASP.NET Core test infrastructure including auth handler testing utilities
- **Files modified:** `src/Api.Tests/StudyApp.Api.Tests.csproj`
- **Verification:** All 4 DevAuth tests pass with the correct package
- **Committed in:** `2fbb67f` (Task 1 commit)

---

**Total deviations:** 3 auto-fixed (3 blocking)
**Impact on plan:** All auto-fixes necessary for the plan to execute. Port change is an environment-specific adaptation. No scope creep.

## Issues Encountered
- `dotnet-ef` global tool was version 7.0.0 (installed previously for another project). Updated to 10.0.5 before generating migrations — otherwise the migration command would fail against net10.0 target framework.

## User Setup Required
Note: Postgres now listens on **port 5433** (not 5432) due to system PostgreSQL conflict. When connecting with a DB client (pgAdmin, DBeaver, etc.), use port 5433.

Connection string: `Host=localhost;Port=5433;Database=studyapp;Username=studyapp;Password=studyapp`

## Next Phase Readiness
- API starts cleanly with `dotnet run --project src/Api`
- All infrastructure services registered in DI and verified working
- DevAuth scheme fully tested — ready for protected endpoints in Phase 2
- `appsettings.Development.json` is gitignored; new developers should copy from `appsettings.Development.example.json`

## Self-Check: PASSED

- DevAuthHandler.cs: FOUND
- AppDbContext.cs: FOUND
- HealthController.cs: FOUND
- DevAuthHandlerTests.cs: FOUND
- appsettings.Development.example.json: FOUND
- Commit 2fbb67f: FOUND
- Commit bcde193: FOUND
- Commit 151f945: FOUND

---
*Phase: 01-foundation*
*Completed: 2026-03-17*
