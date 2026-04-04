# Phase 5: Study Experience - Context

**Gathered:** 2026-04-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Students can browse generated study materials freely (reference mode) or complete a guided study session per section. Phase 5 adds the study-facing UI on top of the already-generated content from Phase 4. Backend work is limited to the SectionProgress entity and read endpoints for generated content.

</domain>

<decisions>
## Implementation Decisions

### Section entry & navigation
- Generation-ready modules show a "Study" link/button on ModuleDetailPage that navigates to `/study/:moduleId`
- `/study/:moduleId` — dedicated study page listing all sections (not part of the setup/upload flow)
- Section list: simple list rows, each showing section title + progress badge + two action buttons: "Browse" and "Study"
- "Browse" navigates to `/study/:moduleId/sections/:sectionId` (reference mode)
- "Study" navigates to `/study/:moduleId/sections/:sectionId/session` (guided session)

### Reference mode layout
- `/study/:moduleId/sections/:sectionId` uses tabbed layout: **Study Guide | Flashcards | Quiz | Concept Map**
- Tabs implemented via shadcn/ui Tabs component (add to project)
- **Study Guide tab**: prose sections with headers — Direct Answer at top (highlighted/bold), then High-Yield Details, Key Tables, Must-Know Numbers, Sources; no collapsible cards
- **Flashcards tab**: list of all cards — front always visible, back hidden with a "Reveal" toggle per card; clicking reveals back inline
- **Quiz tab**: all questions shown as a readable list (front/answer pairs for reference, no interactive scoring in this mode)
- **Concept Map tab**: Mermaid.js rendered inline as SVG diagram; hide this tab entirely when section has no concept map

### Guided session feel
- `/study/:moduleId/sections/:sectionId/session` — full-page dedicated route, back button exits to section detail or list
- **Step 1 — Preview**: study guide content displayed (same as Study Guide tab), with a "Start Flashcards →" button at the bottom
- **Step 2 — Flashcards**: one card at a time; front shown first → click card or "Flip" button to reveal back → click "Next →" to advance; each card marked reviewed when Next is clicked
- **Step 3 — Quiz**: one question at a time; student selects from multiple choice; immediate feedback after selection (green = correct, red = wrong); "Next Question →" to advance
- **Completion screen**: shows quiz score (e.g. "3 of 4 correct") + completion message + "Back to Section List" button; section marked done automatically on completion screen render

### Progress visibility
- Per-section progress shown on section list rows as small badges: e.g. "5 cards reviewed" and "Quiz ✓" when complete
- Progress tracked server-side in a `SectionProgress` table: `cardsReviewed` (int, incremented per card flip-through in session) + `quizCompleted` (bool, set true on completion screen)
- No module-level progress summary — section-level only (module dashboard is STUDY-V2-02, explicitly deferred)

### Claude's Discretion
- Exact card flip animation (CSS transform vs instant reveal)
- Mermaid.js version and initialization approach
- Loading/skeleton states for tab content
- Exact badge styling and color choices for progress indicators
- Error state messaging for failed content loads
- Whether "Browse" and "Study" are separate buttons or a split button

</decisions>

<specifics>
## Specific Ideas

- Reference mode is non-linear — student should never be forced into a flow; they pick any tab in any order
- Guided session is linear: preview → flashcards → quiz → done; no skipping steps
- In guided flashcard mode, "Next" is what marks a card reviewed (not clicking Flip) — so a student has to at least attempt the card before advancing counts
- Completion screen marks the section as done automatically — student doesn't need to click a separate "Mark Done" button

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Badge` component (`src/Frontend/src/components/ui/badge.tsx`): reuse for progress badges on section list
- `Button` component: all CTAs in session and reference mode
- `modules.ts` API client: add section/progress endpoints here following existing pattern
- `useQuery` + `refetchInterval` pattern: reuse for any live data (though study content is static once generated)
- `ModuleDetailPage` routing pattern: new pages follow same React Router v6 pattern

### Established Patterns
- React Router v6: new routes added in `App.tsx`
- React Query: all server state via `useQuery`/`useMutation`
- Axios + hand-written TypeScript types in `src/Frontend/src/api/types.ts`
- shadcn/ui components installed as-needed per phase
- Status lifecycle pattern (`HasConversion<string>()`) — `SectionProgress` is simpler (no status enum, just counts + bool)

### Integration Points
- `ModuleDetailPage`: add "Browse Study Materials" link/button when `generationStatus === 'Ready'`
- `App.tsx`: add routes for `/study/:moduleId`, `/study/:moduleId/sections/:sectionId`, `/study/:moduleId/sections/:sectionId/session`
- New API endpoints needed: `GET /modules/:id/sections` (list sections with study content), `GET /sections/:id/study-guide`, `GET /sections/:id/flashcards`, `GET /sections/:id/quiz`, `GET /sections/:id/concept-map`, `POST /sections/:id/progress` (upsert progress)
- `SectionProgress` EF entity: new migration, FK to Section + UserId

</code_context>

<deferred>
## Deferred Ideas

- Module-level progress dashboard (STUDY-V2-02) — explicitly out of scope for MVP
- Spaced repetition scheduling (STUDY-V2-01) — deferred; basic count tracking is sufficient
- Student-editable flashcards/quiz — raised in Phase 4 deferred ideas, still future phase
- Swipe interaction for flashcards — mobile polish, future enhancement
- Per-section regeneration button — deferred from Phase 4

</deferred>

---

*Phase: 05-study-experience*
*Context gathered: 2026-04-04*
