# Phase 5: Study Experience - Research

**Researched:** 2026-04-04
**Domain:** React frontend (tabbed UI, multi-step guided session, Mermaid.js), ASP.NET Core (read endpoints, SectionProgress entity + migration)
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Section entry & navigation**
- Generation-ready modules show a "Study" link/button on ModuleDetailPage that navigates to `/study/:moduleId`
- `/study/:moduleId` — dedicated study page listing all sections (not part of the setup/upload flow)
- Section list: simple list rows, each showing section title + progress badge + two action buttons: "Browse" and "Study"
- "Browse" navigates to `/study/:moduleId/sections/:sectionId` (reference mode)
- "Study" navigates to `/study/:moduleId/sections/:sectionId/session` (guided session)

**Reference mode layout**
- `/study/:moduleId/sections/:sectionId` uses tabbed layout: Study Guide | Flashcards | Quiz | Concept Map
- Tabs implemented via shadcn/ui Tabs component (add to project)
- Study Guide tab: prose sections with headers — Direct Answer at top (highlighted/bold), then High-Yield Details, Key Tables, Must-Know Numbers, Sources; no collapsible cards
- Flashcards tab: list of all cards — front always visible, back hidden with a "Reveal" toggle per card; clicking reveals back inline
- Quiz tab: all questions shown as a readable list (front/answer pairs for reference, no interactive scoring in this mode)
- Concept Map tab: Mermaid.js rendered inline as SVG diagram; hide this tab entirely when section has no concept map

**Guided session**
- `/study/:moduleId/sections/:sectionId/session` — full-page dedicated route, back button exits to section detail or list
- Step 1 — Preview: study guide content displayed (same as Study Guide tab), with a "Start Flashcards →" button at the bottom
- Step 2 — Flashcards: one card at a time; front shown first → click card or "Flip" button to reveal back → click "Next →" to advance; each card marked reviewed when Next is clicked
- Step 3 — Quiz: one question at a time; student selects from multiple choice; immediate feedback after selection (green = correct, red = wrong); "Next Question →" to advance
- Completion screen: shows quiz score + completion message + "Back to Section List" button; section marked done automatically on completion screen render

**Progress visibility**
- Per-section progress shown on section list rows as small badges: e.g. "5 cards reviewed" and "Quiz ✓" when complete
- Progress tracked server-side in a `SectionProgress` table: `cardsReviewed` (int, incremented per card flip-through in session) + `quizCompleted` (bool, set true on completion screen)
- No module-level progress summary — section-level only

### Claude's Discretion
- Exact card flip animation (CSS transform vs instant reveal)
- Mermaid.js version and initialization approach
- Loading/skeleton states for tab content
- Exact badge styling and color choices for progress indicators
- Error state messaging for failed content loads
- Whether "Browse" and "Study" are separate buttons or a split button

### Deferred Ideas (OUT OF SCOPE)
- Module-level progress dashboard (STUDY-V2-02)
- Spaced repetition scheduling (STUDY-V2-01)
- Student-editable flashcards/quiz
- Swipe interaction for flashcards
- Per-section regeneration button
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| STUDY-01 | User can browse reorganized lecture sections and their study guide pages in reference mode | Reference mode route + tabbed UI + Study Guide tab rendering StudyGuide JSON fields |
| STUDY-02 | User can view flashcards for any section in reference mode | Flashcards tab rendering Flashcard entities with inline Reveal toggle |
| STUDY-03 | User can view concept maps for algorithmic sections in reference mode | Concept Map tab rendering ConceptMap.MermaidSyntax via Mermaid.js; hidden when absent |
| STUDY-04 | User can launch a guided study session for a section (preview → flashcards → quiz → done) | Session route + 4-step state machine (preview, flashcards, quiz, completion) |
| STUDY-05 | App tracks basic progress per section (cards reviewed, quizzes completed) | SectionProgress EF entity + migration + POST /sections/:id/progress endpoint + badge display |
</phase_requirements>

---

## Summary

Phase 5 is almost entirely frontend work layered on top of data that already exists in the database (StudyGuide, Flashcard, QuizQuestion, ConceptMap entities were all created in Phase 4). The backend work is minimal: three new read endpoints, one new progress upsert endpoint, one new EF entity (`SectionProgress`), and one migration. There are no background jobs, no LLM calls, and no new Python skills.

The frontend requires three new pages (StudyPage listing sections, SectionDetailPage with tabs, SessionPage with step machine) and registration of three new React Router v6 routes. The most technically interesting pieces are: (1) the Tabs component must be added via `npx shadcn add tabs` — the project uses `base-nova` style which pulls from `@base-ui/react/tabs`, which is already installed as a transitive dep; (2) Mermaid.js must be installed (`npm install mermaid`) and initialized carefully to avoid double-renders; (3) the session page requires a simple integer step machine with local `useState`.

**Primary recommendation:** Add `SectionProgress` migration first (Wave 0), build backend endpoints in one plan, then build frontend pages in two or three focused plans. No libraries are speculative — `@base-ui/react/tabs` is already bundled and `mermaid` is a single package install.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| @base-ui/react/tabs | (already installed — part of @base-ui/react ^1.3.0) | Accessible tabs primitive | Project uses base-nova shadcn style; tabs component added via `npx shadcn add tabs` which wraps this |
| mermaid | ^11.x (latest stable) | Render Mermaid syntax as inline SVG | GEN-04 stores MermaidSyntax in ConceptMap; Mermaid.js is the canonical renderer |
| React Query (@tanstack/react-query ^5) | already installed | Fetch section content per tab | Same pattern as all other data in project |
| React Router v6 (react-router-dom ^7) | already installed | New routes for study pages | Already used throughout |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| lucide-react | already installed | Icons (e.g., ChevronRight, Check) | CTAs and completion indicators in session |
| class-variance-authority + tailwind-merge | already installed | Styling variants for progress badges | Already used in badge.tsx |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| @base-ui/react Tabs | Hand-rolled `<div>` tab UI | @base-ui/react is already a dep; using it gets ARIA roles, keyboard nav, focus management for free |
| mermaid npm package | mermaid CDN script | CDN breaks CSP and Vite dev; npm package works with Vite bundling |
| mermaid npm package | react-mermaid2 | react-mermaid2 is a thin wrapper with less maintenance; direct mermaid init gives more control over resize/reinit |

**Installation:**
```bash
# From src/Frontend/
npm install mermaid
# Then add Tabs component (picks up @base-ui/react/tabs already installed):
npx shadcn add tabs
```

---

## Architecture Patterns

### Recommended Project Structure
```
src/Frontend/src/
├── pages/
│   ├── StudyPage.tsx             # /study/:moduleId — section list
│   ├── SectionDetailPage.tsx     # /study/:moduleId/sections/:sectionId — tabbed reference
│   └── SessionPage.tsx           # /study/:moduleId/sections/:sectionId/session
├── api/
│   └── study.ts                  # new API client for study endpoints
└── components/ui/
    └── tabs.tsx                  # added via: npx shadcn add tabs

src/Api/
├── Controllers/
│   └── StudyController.cs        # GET sections, GET study content, POST progress
├── Models/
│   └── SectionProgress.cs        # new EF entity
└── Migrations/
    └── YYYYMMDD_AddSectionProgress.cs
```

### Pattern 1: @base-ui/react Tabs (already installed, add via shadcn)

**What:** shadcn `add tabs` generates `src/components/ui/tabs.tsx` which wraps `@base-ui/react/tabs` with Tailwind classes, consistent with how badge.tsx and button.tsx wrap `@base-ui/react/badge` and `@base-ui/react/button`.

**When to use:** Reference mode SectionDetailPage — all four content tabs.

**Example (from @base-ui/react API pattern):**
```tsx
// After `npx shadcn add tabs` generates src/components/ui/tabs.tsx
// Usage in SectionDetailPage:
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';

<Tabs defaultValue="study-guide">
  <TabsList>
    <TabsTrigger value="study-guide">Study Guide</TabsTrigger>
    <TabsTrigger value="flashcards">Flashcards</TabsTrigger>
    <TabsTrigger value="quiz">Quiz</TabsTrigger>
    {section.hasConceptMap && (
      <TabsTrigger value="concept-map">Concept Map</TabsTrigger>
    )}
  </TabsList>
  <TabsContent value="study-guide"><StudyGuideTab ... /></TabsContent>
  <TabsContent value="flashcards"><FlashcardsTab ... /></TabsContent>
  <TabsContent value="quiz"><QuizTab ... /></TabsContent>
  {section.hasConceptMap && (
    <TabsContent value="concept-map"><ConceptMapTab ... /></TabsContent>
  )}
</Tabs>
```

### Pattern 2: Mermaid.js Initialization

**What:** Mermaid must be initialized once and `mermaid.render()` called imperatively inside a `useEffect`. The key pitfall is double-render in React 18 StrictMode.

**When to use:** ConceptMapTab component only.

**Example:**
```tsx
// Source: mermaid official docs (mermaid.js.org)
import mermaid from 'mermaid';
import { useEffect, useRef, useState } from 'react';

mermaid.initialize({ startOnLoad: false, theme: 'default' });

export function MermaidDiagram({ syntax }: { syntax: string }) {
  const id = useRef(`mermaid-${Math.random().toString(36).slice(2)}`);
  const [svg, setSvg] = useState<string>('');

  useEffect(() => {
    mermaid.render(id.current, syntax).then(({ svg }) => setSvg(svg));
  }, [syntax]);

  return <div dangerouslySetInnerHTML={{ __html: svg }} />;
}
```

**Note:** `mermaid.initialize()` should be called once at module level, not inside the component. Use `dangerouslySetInnerHTML` — this is safe here because the SVG is generated by Mermaid from our own stored syntax (not user-supplied HTML).

### Pattern 3: Session Step Machine

**What:** Simple `useState<'preview' | 'flashcards' | 'quiz' | 'done'>` plus index counters for card/question position.

**When to use:** SessionPage exclusively.

```tsx
type SessionStep = 'preview' | 'flashcards' | 'quiz' | 'done';

const [step, setStep] = useState<SessionStep>('preview');
const [cardIndex, setCardIndex] = useState(0);
const [cardFlipped, setCardFlipped] = useState(false);
const [questionIndex, setQuestionIndex] = useState(0);
const [selectedAnswer, setSelectedAnswer] = useState<string | null>(null);
const [correctCount, setCorrectCount] = useState(0);
```

### Pattern 4: SectionProgress API Client (mirrors modules.ts)

```tsx
// src/api/study.ts — new file following modules.ts pattern
import client from './client';

export interface SectionSummary {
  id: string;
  headingText: string;
  sortOrder: number;
  cardsReviewed: number;
  quizCompleted: boolean;
}

export const study = {
  listSections: (moduleId: string) =>
    client.get<SectionSummary[]>(`/modules/${moduleId}/sections`).then(r => r.data),
  getStudyGuide: (sectionId: string) =>
    client.get(`/sections/${sectionId}/study-guide`).then(r => r.data),
  getFlashcards: (sectionId: string) =>
    client.get(`/sections/${sectionId}/flashcards`).then(r => r.data),
  getQuiz: (sectionId: string) =>
    client.get(`/sections/${sectionId}/quiz`).then(r => r.data),
  getConceptMap: (sectionId: string) =>
    client.get(`/sections/${sectionId}/concept-map`).then(r => r.data),
  upsertProgress: (sectionId: string, body: { cardsReviewed?: number; quizCompleted?: boolean }) =>
    client.post(`/sections/${sectionId}/progress`, body).then(r => r.data),
};
```

### Pattern 5: SectionProgress Entity (mirrors ExtractionRun pattern)

```csharp
// src/Api/Models/SectionProgress.cs
namespace StudyApp.Api.Models;

public class SectionProgress
{
    public Guid Id { get; set; }
    public Guid SectionId { get; set; }
    public Section Section { get; set; } = null!;
    public Guid UserId { get; set; }
    public int CardsReviewed { get; set; } = 0;
    public bool QuizCompleted { get; set; } = false;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

AppDbContext needs: `public DbSet<SectionProgress> SectionProgresses => Set<SectionProgress>();` plus `OnModelCreating` config with unique index on `(SectionId, UserId)`.

### Anti-Patterns to Avoid
- **Calling `mermaid.initialize()` inside `useEffect`:** Causes re-initialization on every render. Call it once at module level.
- **Using `mermaid.contentLoaded()` or `mermaid.run()`:** These scan the DOM for `.mermaid` class elements; the explicit `mermaid.render(id, syntax)` API is more predictable in React.
- **Fetching all tab content eagerly:** Each tab's content should use a separate `useQuery` with `enabled: activeTab === 'flashcards'` (or React Query's lazy fetching pattern) — avoids unnecessary API calls when user only opens Study Guide tab.
- **One progress row per card flip instead of upsert:** Progress is an upsert (one row per section+user). Increment `cardsReviewed` by total count when "Next" is clicked on each card — a single POST per card advance is fine, or batch on step transition.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Accessible tabs with keyboard nav | Custom div-based tabs | `npx shadcn add tabs` (wraps @base-ui/react/tabs) | ARIA roles, roving tabindex, focus management — already in @base-ui/react which is installed |
| Mermaid SVG rendering | Custom AST → SVG transformer | `mermaid` npm package | Mermaid syntax is complex; custom rendering would be unmaintainable |
| Multiple-choice quiz state | Custom form with validation | Simple `useState` + conditional classNames | Quiz is a single-step interaction; no form library needed |

**Key insight:** @base-ui/react is already installed as a project dep (powers badge.tsx, button.tsx). The Tabs primitive is in the same package — `npx shadcn add tabs` just generates a styled wrapper.

---

## Common Pitfalls

### Pitfall 1: Mermaid Double-Render in React StrictMode
**What goes wrong:** `mermaid.render()` is called twice in dev mode (StrictMode double-invokes effects). The second call may fail or produce a duplicate SVG id warning.
**Why it happens:** React 18 StrictMode mounts → unmounts → remounts in dev.
**How to avoid:** Use a cleanup function in `useEffect` and generate unique IDs with `useId()` or `useRef`.
**Warning signs:** Console errors like "Element with id already exists" or blank diagram on first load.

### Pitfall 2: Fetching Content Before Tab is Active
**What goes wrong:** All four `useQuery` calls fire on mount, triggering 4 API calls even if user only needs Study Guide.
**Why it happens:** All queries enabled by default.
**How to avoid:** Pass `enabled: activeTab === 'flashcards'` to each query, or restructure so each TabsContent renders its data-fetching component only when active (React Query's `staleTime` covers the re-fetch case).

### Pitfall 3: Progress Count Reset on Re-entry
**What goes wrong:** Student re-enters session; `cardsReviewed` resets to 0 on completion because POST replaces rather than accumulates.
**Why it happens:** Backend upsert replaces whole row.
**How to avoid:** Backend POST should accept a delta or an explicit field list. The decision is to increment `cardsReviewed` — track total cards in that session and POST the new total (max of existing + session count), not reset to session count.

### Pitfall 4: Concept Map Tab Visible When No Concept Map Exists
**What goes wrong:** Tab renders but content area is empty or errors.
**Why it happens:** `getConceptMap` returns 404 when section has no concept map.
**How to avoid:** `SectionSummary` from `GET /modules/:id/sections` should include a `hasConceptMap: bool` field. The frontend conditionally renders the tab only when `hasConceptMap === true`. This requires the sections list endpoint to JOIN on ConceptMaps.

### Pitfall 5: Missing Route in App.tsx Causes Silent 404
**What goes wrong:** Navigation to `/study/:moduleId/sections/:sectionId/session` renders blank page.
**Why it happens:** React Router v6 requires all routes to be registered in the router config.
**How to avoid:** Add all three routes to App.tsx in the same plan that creates the pages.

### Pitfall 6: EF Migration Not Applied Before Testing
**What goes wrong:** `POST /sections/:id/progress` fails with "table sectionprogresses does not exist".
**Why it happens:** New SectionProgress migration must be applied.
**How to avoid:** Wave 0 plan creates the migration; execution plans include `dotnet ef database update` in verification steps.

---

## Code Examples

### Backend: StudyController endpoint skeleton
```csharp
// Source: matches ModulesController.cs pattern in this codebase
[ApiController]
[Route("")]
[Authorize]
public class StudyController : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /modules/{moduleId}/sections
    [HttpGet("modules/{moduleId:guid}/sections")]
    public async Task<IActionResult> GetSections(Guid moduleId)
    {
        // verify module belongs to user, return sections with progress joined
    }

    // GET /sections/{sectionId}/study-guide
    [HttpGet("sections/{sectionId:guid}/study-guide")]
    public async Task<IActionResult> GetStudyGuide(Guid sectionId) { ... }

    // GET /sections/{sectionId}/flashcards
    [HttpGet("sections/{sectionId:guid}/flashcards")]
    public async Task<IActionResult> GetFlashcards(Guid sectionId) { ... }

    // GET /sections/{sectionId}/quiz
    [HttpGet("sections/{sectionId:guid}/quiz")]
    public async Task<IActionResult> GetQuiz(Guid sectionId) { ... }

    // GET /sections/{sectionId}/concept-map
    [HttpGet("sections/{sectionId:guid}/concept-map")]
    public async Task<IActionResult> GetConceptMap(Guid sectionId) { ... }

    // POST /sections/{sectionId}/progress
    [HttpPost("sections/{sectionId:guid}/progress")]
    public async Task<IActionResult> UpsertProgress(Guid sectionId, [FromBody] ProgressRequest req) { ... }
}
```

### Backend: SectionProgress OnModelCreating config
```csharp
// In AppDbContext.OnModelCreating
modelBuilder.Entity<SectionProgress>(entity =>
{
    entity.HasKey(sp => sp.Id);
    entity.HasIndex(sp => new { sp.SectionId, sp.UserId }).IsUnique();
    entity.HasOne(sp => sp.Section)
          .WithMany()
          .HasForeignKey(sp => sp.SectionId)
          .OnDelete(DeleteBehavior.Cascade);
});
```

### Frontend: Session step machine skeleton
```tsx
// SessionPage.tsx
export default function SessionPage() {
  const { moduleId, sectionId } = useParams<{ moduleId: string; sectionId: string }>();
  const [step, setStep] = useState<'preview' | 'flashcards' | 'quiz' | 'done'>('preview');
  const [cardIndex, setCardIndex] = useState(0);
  const [cardFlipped, setCardFlipped] = useState(false);
  const [questionIndex, setQuestionIndex] = useState(0);
  const [selectedAnswer, setSelectedAnswer] = useState<string | null>(null);
  const [correctCount, setCorrectCount] = useState(0);

  const progressMutation = useMutation({
    mutationFn: (body: { cardsReviewed?: number; quizCompleted?: boolean }) =>
      study.upsertProgress(sectionId!, body),
  });

  function handleNextCard(cards: Flashcard[]) {
    progressMutation.mutate({ cardsReviewed: cardIndex + 1 });
    if (cardIndex + 1 < cards.length) {
      setCardIndex(i => i + 1);
      setCardFlipped(false);
    } else {
      setStep('quiz');
    }
  }

  // completion screen auto-marks done
  useEffect(() => {
    if (step === 'done') {
      progressMutation.mutate({ quizCompleted: true });
    }
  }, [step]);
  // ...
}
```

### Frontend: Mermaid initialization
```tsx
// src/components/MermaidDiagram.tsx
import mermaid from 'mermaid';
import { useEffect, useId, useState } from 'react';

mermaid.initialize({ startOnLoad: false });

export function MermaidDiagram({ syntax }: { syntax: string }) {
  const id = useId().replace(/:/g, '');  // mermaid ids must be alphanumeric
  const [svg, setSvg] = useState('');

  useEffect(() => {
    let cancelled = false;
    mermaid.render(`m${id}`, syntax).then(({ svg }) => {
      if (!cancelled) setSvg(svg);
    });
    return () => { cancelled = true; };
  }, [syntax, id]);

  return <div dangerouslySetInnerHTML={{ __html: svg }} />;
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `mermaid.contentLoaded()` DOM scan | `mermaid.render(id, syntax)` imperative API | Mermaid v9+ | Works correctly in React without DOM manipulation |
| Radix UI Tabs | @base-ui/react Tabs | shadcn base-nova style (this project) | Already installed; tabs.tsx component added via `npx shadcn add tabs` |
| React Router v5 | React Router v7 (react-router-dom ^7) | This project | Route definitions in `createBrowserRouter`, not `<Switch>` |

**Deprecated/outdated:**
- `mermaid.init()`: Deprecated in v10+; use `mermaid.render()` or `mermaid.run()` instead.
- Radix UI primitives: Not used in this project — `@base-ui/react` is the primitive layer.

---

## Open Questions

1. **shadcn `tabs` component exact generated output for base-nova style**
   - What we know: `npx shadcn add tabs` reads `components.json` and generates a `tabs.tsx` wrapping `@base-ui/react/tabs` with Tailwind classes matching the base-nova theme
   - What's unclear: The exact generated classNames until the command runs
   - Recommendation: Run `npx shadcn add tabs` in Wave 0 (setup plan); treat the generated file as canonical

2. **Progress increment strategy: per-card POST vs batch on step transition**
   - What we know: CONTEXT.md says "each card marked reviewed when Next is clicked"
   - What's unclear: Whether one POST per card click is acceptable vs batching all at quiz-transition
   - Recommendation: One POST per card advance keeps backend simple; for a study tool with low traffic this is fine

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + Microsoft.AspNetCore.Mvc.Testing 10.0.5 |
| Config file | `src/Api.Tests/StudyApp.Api.Tests.csproj` |
| Quick run command | `dotnet test src/Api.Tests --filter "Category=Study" --no-build` |
| Full suite command | `dotnet test src/Api.Tests` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| STUDY-01 | `GET /modules/:id/sections` returns sections with progress fields | Integration | `dotnet test src/Api.Tests --filter "StudyEndpointTests"` | ❌ Wave 0 |
| STUDY-01 | `GET /sections/:id/study-guide` returns StudyGuide JSON fields | Integration | `dotnet test src/Api.Tests --filter "StudyEndpointTests"` | ❌ Wave 0 |
| STUDY-02 | `GET /sections/:id/flashcards` returns flashcard list | Integration | `dotnet test src/Api.Tests --filter "StudyEndpointTests"` | ❌ Wave 0 |
| STUDY-03 | `GET /sections/:id/concept-map` returns 200 when concept map exists | Integration | `dotnet test src/Api.Tests --filter "StudyEndpointTests"` | ❌ Wave 0 |
| STUDY-03 | `GET /sections/:id/concept-map` returns 404 when no concept map | Integration | `dotnet test src/Api.Tests --filter "StudyEndpointTests"` | ❌ Wave 0 |
| STUDY-04 | Session page routing (manual UI flow verification) | Manual | n/a — UAT plan | n/a |
| STUDY-05 | `POST /sections/:id/progress` creates SectionProgress row | Integration | `dotnet test src/Api.Tests --filter "StudyEndpointTests"` | ❌ Wave 0 |
| STUDY-05 | `POST /sections/:id/progress` upserts (not duplicates) | Integration | `dotnet test src/Api.Tests --filter "StudyEndpointTests"` | ❌ Wave 0 |
| STUDY-05 | Section list includes cardsReviewed + quizCompleted from progress | Integration | `dotnet test src/Api.Tests --filter "StudyEndpointTests"` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test src/Api.Tests --no-build`
- **Per wave merge:** `dotnet test src/Api.Tests`
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `src/Api.Tests/Study/StudyEndpointTests.cs` — covers STUDY-01, STUDY-02, STUDY-03, STUDY-05
- [ ] `src/Api.Tests/Study/StudyProgressTests.cs` — upsert logic for STUDY-05
- [ ] `src/Api/Models/SectionProgress.cs` — new entity
- [ ] EF migration: `dotnet ef migrations add AddSectionProgress --project src/Api`
- [ ] Frontend: `npx shadcn add tabs` — generates `src/Frontend/src/components/ui/tabs.tsx`
- [ ] Frontend: `npm install mermaid` — in `src/Frontend/`

---

## Sources

### Primary (HIGH confidence)
- Codebase read (AppDbContext.cs, ModulesController.cs, StudyGuide.cs, Flashcard.cs, QuizQuestion.cs, ConceptMap.cs, Section.cs, Module.cs, modules.ts, ModuleDetailPage.tsx, button.tsx, badge.tsx, GenerationTriggerTests.cs) — existing patterns verified directly
- `components.json` — confirmed base-nova style with @base-ui/react primitives
- `package.json` — confirmed @base-ui/react ^1.3.0 installed; mermaid NOT installed
- `node_modules/@base-ui/react/tabs/index.parts.d.ts` — confirmed Tabs.Root, Tabs.Tab, Tabs.List, Tabs.Panel, Tabs.Indicator API

### Secondary (MEDIUM confidence)
- Mermaid.js imperative render API: `mermaid.render(id, syntax)` returning `{ svg }` — standard pattern documented at mermaid.js.org; consistent with mermaid v10/v11

### Tertiary (LOW confidence)
- Exact generated classNames from `npx shadcn add tabs` for base-nova style — not verified without running the command; must run in Wave 0

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all deps verified in node_modules and package.json
- Architecture: HIGH — controller/entity/migration patterns read directly from codebase
- Mermaid integration: MEDIUM — API pattern is well-established but exact version/init nuances should be verified when installing
- Tabs component output: LOW for exact classNames, HIGH for API shape (read from @base-ui/react types)

**Research date:** 2026-04-04
**Valid until:** 2026-05-04 (stable libraries; @base-ui/react and mermaid are not fast-moving)
