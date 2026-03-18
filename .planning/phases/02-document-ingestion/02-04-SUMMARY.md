---
phase: 02-document-ingestion
plan: "04"
subsystem: worker-providers
tags: [tdd, llm-providers, gemini, anthropic, stub-mode, di, env-vars]

# Dependency graph
requires:
  - phase: 02-document-ingestion
    plan: "01"
    provides: "NuGet packages (Google.GenAI, Anthropic), Worker→Api ProjectReference"
  - phase: 02-document-ingestion
    plan: "02"
    provides: "Worker Program.cs foundation, ProviderConfig placeholder"

provides:
  - "IVisionProvider interface: Task<string> ExtractTextAsync(byte[] imageBytes, string mimeType)"
  - "IGenerationProvider interface: Task<string> GenerateAsync(string prompt, IEnumerable<string> sourceChunks)"
  - "StubVisionProvider: returns '[Figure: vision extraction not available in stub mode]' — no API calls"
  - "StubGenerationProvider: returns deterministic '[Stub] Generated content for: {prompt}' — no API calls"
  - "GeminiVisionProvider: Google.GenAI Client, model from Gemini:VisionModel config"
  - "ClaudeGenerationProvider: Anthropic IAnthropicClient, model from Claude:Model config"
  - "GeminiGenerationProvider: Google.GenAI Client, generation with source chunk context"
  - "ProviderRegistration.AddProviders() extension: env-var-driven DI switch for both providers"
  - "Stub mode works with no API keys configured"

affects:
  - 02-05-extraction (IngestionJob will inject IVisionProvider)
  - 02-06-worker-jobs (VisionExtractionJob and generation jobs inject providers)
  - 02-07-module-ui (stub mode enables local dev without API keys)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "ProviderRegistration extension method: testable DI switch via env vars"
    - "Stub providers: no-op implementations that return deterministic content without API calls"
    - "Real providers: constructor-inject SDK client + IConfiguration for model ID"
    - "TDD: RED test stubs replaced with real tests; GREEN implementations created"

key-files:
  created:
    - src/Worker/Providers/IVisionProvider.cs
    - src/Worker/Providers/StubVisionProvider.cs
    - src/Worker/Providers/IGenerationProvider.cs
    - src/Worker/Providers/StubGenerationProvider.cs
    - src/Worker/Providers/GeminiVisionProvider.cs
    - src/Worker/Providers/ClaudeGenerationProvider.cs
    - src/Worker/Providers/GeminiGenerationProvider.cs
    - src/Worker/ProviderRegistration.cs
  modified:
    - src/Worker/Program.cs
    - src/Api.Tests/StudyApp.Api.Tests.csproj
    - src/Api.Tests/Providers/VisionProviderTests.cs
    - src/Api.Tests/Providers/GenerationProviderTests.cs
    - src/Api.Tests/Providers/ProviderConfigTests.cs

key-decisions:
  - "Google.GenAI SDK uses Client (not GoogleAI) as the main entry point — confirmed via build-time type resolution"
  - "Api.Tests gains ProjectReference to Worker so provider unit tests can reference Worker types directly"
  - "ProviderRegistration extension method lives in StudyApp.Worker namespace for testability — tests call new ServiceCollection().AddProviders(config)"
  - "ProviderConfig singleton (from plan 02-02) replaced by AddProviders() — cleaner DI, no config-carrier object"
  - "ClaudeGenerationProvider injects IAnthropicClient (interface) not AnthropicClient (concrete) for testability"
  - "Model IDs from IConfiguration: Gemini:VisionModel, Gemini:GenerationModel, Claude:Model — not hardcoded"

# Metrics
duration: 11min
completed: 2026-03-18
---

# Phase 2 Plan 04: Vision and Generation Provider Abstraction Layer Summary

**IVisionProvider and IGenerationProvider interfaces with stub and real implementations, ProviderRegistration extension for env-var-driven DI, and 6 TDD tests all passing — stub mode requires no API keys**

## Performance

- **Duration:** ~11 min
- **Started:** 2026-03-18T03:33:28Z
- **Completed:** 2026-03-18T03:44:28Z
- **Tasks:** 2 task commits (RED+GREEN combined for interfaces/stubs, then ProviderRegistration)
- **Files modified:** 12

## Accomplishments

- Two provider interfaces (`IVisionProvider`, `IGenerationProvider`) with correct signatures per plan spec
- Stub implementations return exact strings from CONTEXT.md decisions — no API calls, no keys required
- Three real provider implementations (`GeminiVisionProvider`, `ClaudeGenerationProvider`, `GeminiGenerationProvider`) compile and register correctly — model IDs from config, not hardcoded
- `ProviderRegistration.AddProviders()` extension method replaces the ProviderConfig singleton from plan 02-02 — fully testable, env-var driven
- Worker Program.cs updated to single-line `builder.Services.AddProviders(builder.Configuration)`
- 6 tests across VisionProviderTests, GenerationProviderTests, ProviderConfigTests — all pass

## Task Commits

Each task was committed atomically:

1. **Interfaces, stubs, and real provider implementations** - `400cd7b` (feat)
2. **ProviderRegistration extension and DI wiring tests** - `bd7dea6` (feat)

## Files Created/Modified

- `src/Worker/Providers/IVisionProvider.cs` - Interface: ExtractTextAsync(byte[], string) → Task<string>
- `src/Worker/Providers/IGenerationProvider.cs` - Interface: GenerateAsync(string, IEnumerable<string>) → Task<string>
- `src/Worker/Providers/StubVisionProvider.cs` - Returns "[Figure: vision extraction not available in stub mode]"
- `src/Worker/Providers/StubGenerationProvider.cs` - Returns deterministic "[Stub] Generated content for: {prompt}"
- `src/Worker/Providers/GeminiVisionProvider.cs` - Google.GenAI Client, model from Gemini:VisionModel config
- `src/Worker/Providers/ClaudeGenerationProvider.cs` - Anthropic IAnthropicClient, model from Claude:Model config
- `src/Worker/Providers/GeminiGenerationProvider.cs` - Google.GenAI Client, generation with chunk context
- `src/Worker/ProviderRegistration.cs` - AddProviders() extension: env-var switch for vision + generation providers
- `src/Worker/Program.cs` - Replaced ProviderConfig singleton with AddProviders() call
- `src/Api.Tests/StudyApp.Api.Tests.csproj` - Added Worker ProjectReference for provider tests
- `src/Api.Tests/Providers/VisionProviderTests.cs` - Test: stub returns exact placeholder string
- `src/Api.Tests/Providers/GenerationProviderTests.cs` - Tests: stub returns non-empty [Stub] string; is deterministic
- `src/Api.Tests/Providers/ProviderConfigTests.cs` - Tests: VISION_PROVIDER=stub, GENERATION_PROVIDER=stub, missing → defaults to stub

## Decisions Made

- Google.GenAI SDK 1.5.0 uses `Client` as the main class (not `GoogleAI`) — confirmed at build time; linter auto-corrected the initial `GoogleAI` type reference
- `Api.Tests → Worker` ProjectReference added so provider tests can reference `StudyApp.Worker.Providers` types directly without duplicating them
- `ProviderConfig` singleton replaced by `ProviderRegistration.AddProviders()` — the extension method pattern makes the switch logic testable without a running Host
- `IAnthropicClient` (interface) injected into `ClaudeGenerationProvider` rather than the concrete `AnthropicClient` to support mocking in future tests
- Model IDs placed in `IConfiguration` (keys: `Gemini:VisionModel`, `Gemini:GenerationModel`, `Claude:Model`) with sensible defaults

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added Worker ProjectReference to Api.Tests**
- **Found during:** Task 1 (writing VisionProviderTests)
- **Issue:** `StudyApp.Worker.Providers` namespace inaccessible from Api.Tests — tests couldn't compile
- **Fix:** Added `<ProjectReference Include="..\Worker\StudyApp.Worker.csproj" />` to Api.Tests.csproj
- **Files modified:** `src/Api.Tests/StudyApp.Api.Tests.csproj`
- **Commit:** `400cd7b`

**2. [Rule 1 - Bug] Google.GenAI Client type name correction**
- **Found during:** Task 1 (building GeminiVisionProvider)
- **Issue:** Plan's RESEARCH.md references `GoogleAI` as the main class, but Google.GenAI 1.5.0 uses `Client`
- **Fix:** Used `Client` type in GeminiVisionProvider and GeminiGenerationProvider constructors (linter auto-applied)
- **Files modified:** `src/Worker/Providers/GeminiVisionProvider.cs`
- **Commit:** `400cd7b`

**Total deviations:** 2 auto-fixed (Rule 3 + Rule 1)
**Impact on plan:** Both required for correctness; no scope creep.

## Self-Check: PASSED
