---
phase: 1
slug: foundation
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-17
---

# Phase 1 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xunit (dotnet new xunit) — Wave 0 installs |
| **Config file** | src/Api.Tests/StudyApp.Api.Tests.csproj — Wave 0 creates |
| **Quick run command** | `dotnet test src/Api.Tests/ --no-build -v q` |
| **Full suite command** | `dotnet test --no-build -v q` |
| **Estimated runtime** | ~5 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build src/Api && dotnet build src/Worker` (compile check)
- **After every plan wave:** Run `docker compose up -d && curl http://localhost:5000/health`
- **Before `/gsd:verify-work`:** Full suite must be green + all manual smoke tests passing
- **Max feedback latency:** ~10 seconds (build check)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 1-01-01 | 01 | 0 | AUTH-01 | unit | `dotnet test --filter "DevAuth"` | ❌ W0 | ⬜ pending |
| 1-01-02 | 01 | 1 | INFRA-01 | smoke | `docker compose ps` shows healthy | ❌ manual | ⬜ pending |
| 1-01-03 | 01 | 1 | INFRA-02 | smoke | `curl http://localhost:5000/health` | ❌ manual | ⬜ pending |
| 1-01-04 | 01 | 1 | INFRA-03 | smoke | Worker starts without error (exit code 0) | ❌ manual | ⬜ pending |
| 1-01-05 | 01 | 1 | INFRA-04 | smoke | Browser loads `http://localhost:5173` | ❌ manual | ⬜ pending |
| 1-01-06 | 01 | 1 | INFRA-05 | smoke | `docker exec postgres psql -U studyapp -c "\dt hangfire.*"` | ❌ manual | ⬜ pending |
| 1-01-07 | 01 | 1 | INFRA-06 | smoke | API startup log shows S3 registered | ❌ manual | ⬜ pending |
| 1-01-08 | 01 | 1 | INFRA-07 | smoke | `dotnet ef database update` exits 0 | ❌ manual CLI | ⬜ pending |
| 1-01-09 | 01 | 1 | AUTH-01 | unit | `dotnet test --filter "DevAuth"` | ❌ W0 | ⬜ pending |
| 1-01-10 | 01 | 1 | AUTH-02 | smoke | `docker exec postgres psql -U studyapp -c 'SELECT id FROM "Users"'` | ❌ manual | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `src/Api.Tests/StudyApp.Api.Tests.csproj` — xunit test project scaffold
- [ ] `src/Api.Tests/Auth/DevAuthHandlerTests.cs` — unit tests for DevAuthHandler (AUTH-01, no DB needed)

*These must exist before Wave 1 tasks that implement DevAuthHandler.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| docker compose up starts Postgres + MinIO healthy | INFRA-01 | Requires Docker daemon running | Run `docker compose up -d && docker compose ps` — both services show `healthy` |
| Worker Service starts without error | INFRA-03 | Separate process, no HTTP endpoint in Phase 1 | Run `dotnet run --project src/Worker` — no exception in first 5s of output |
| Frontend loads in browser and calls API | INFRA-04 | Browser interaction required | Run `npm run dev` in src/Frontend, open `http://localhost:5173` — page loads, health check widget shows API status |
| Hangfire schema created in Postgres | INFRA-05 | Requires live Postgres | After API starts: `docker exec studyapp-postgres-1 psql -U studyapp -c "\dt hangfire.*"` — shows tables |
| MinIO S3 client connects | INFRA-06 | Requires live MinIO container | API startup logs show no S3 connection error; MinIO console at `http://localhost:9001` is accessible |
| EF migrations apply cleanly | INFRA-07 | Requires live Postgres + EF CLI | Run `dotnet ef database update --project src/Api` — exits 0, `Users` table exists |
| DevUser row exists after migration | AUTH-02 | Requires live Postgres | `docker exec studyapp-postgres-1 psql -U studyapp -c 'SELECT * FROM "Users"'` — shows 1 row with Guid `00000000-0000-0000-0000-000000000001` |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
