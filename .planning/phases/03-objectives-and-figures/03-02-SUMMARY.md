---
phase: 03-objectives-and-figures
plan: "02"
subsystem: database
tags: [efcore, migrations, postgres, python, models, csharp]

requires:
  - phase: 03-01
    provides: Wave 0 test stubs for Skills, Figures, and Extraction (compile clean before models exist)
  - phase: 02-document-ingestion
    provides: Document, Module, Chunk EF entities; AppDbContext; HasConversion<string>() pattern

provides:
  - Figure EF entity (DocumentId FK, S3Key, Keep, PageNumber, Caption, LabelType, ManifestMetadataJson)
  - Section EF entity (ModuleId FK, HeadingLevel, HeadingText, Content, SourcePageRefsJson, SortOrder)
  - ExtractionStatus enum (NotStarted, Queued, Processing, Ready, Failed)
  - Module extended with ExtractionStatus, DocxS3Key, ExtractionError, Sections collection
  - Document extended with Figures collection
  - AddFiguresAndSections EF Core migration applied to Postgres
  - Python stub scripts at src/skills/lecture-extractor-extracted/ (extract_images.py, lecture_extractor.py)

affects: [03-03, 03-04, 03-05, figure-extraction, lecture-extraction, worker-jobs]

tech-stack:
  added: []
  patterns:
    - "ExtractionStatus stored as string column via HasConversion<string>() — same pattern as DocumentStatus"
    - "Python skill stubs print deterministic JSON to stdout, ignore input args — subprocess-safe for Worker integration"
    - "Figures scoped to Document (not Module); Sections scoped to Module — reflects extraction ownership boundary"

key-files:
  created:
    - src/Api/Models/ExtractionStatus.cs
    - src/Api/Models/Figure.cs
    - src/Api/Models/Section.cs
    - src/Api/Migrations/20260318203429_AddFiguresAndSections.cs
    - src/skills/lecture-extractor-extracted/extract_images.py
    - src/skills/lecture-extractor-extracted/lecture_extractor.py
    - src/skills/lecture-extractor-extracted/requirements.txt
  modified:
    - src/Api/Models/Module.cs
    - src/Api/Models/Document.cs
    - src/Api/Data/AppDbContext.cs

key-decisions:
  - "Figures FK to Document (not Module directly) — figure extraction runs per-document; Module owns Sections from DOCX extraction"
  - "ExtractionStatus HasConversion<string>() matches DocumentStatus pattern for DB readability"
  - "Python stubs use only stdlib — no requirements.txt deps needed for stub mode"

patterns-established:
  - "New enum-backed status fields use HasConversion<string>() in OnModelCreating"
  - "Python skill stubs ignore sys.argv input and return deterministic output for safe local testing"

requirements-completed: [FIG-01, FIG-02, FIG-03, LEXT-03, LEXT-04, SKILL-02]

duration: 2min
completed: 2026-03-18
---

# Phase 3 Plan 02: Figures/Sections EF Entities, Migration, and Python Stubs Summary

**Figure and Section EF entities with AddFiguresAndSections migration, extended Module with ExtractionStatus, and Python subprocess stub scripts for figure/lecture extraction**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-18T20:33:33Z
- **Completed:** 2026-03-18T20:35:30Z
- **Tasks:** 2
- **Files modified:** 10

## Accomplishments

- Added Figure, Section, ExtractionStatus models matching plan spec exactly; extended Module and Document with new collections/properties
- Applied AddFiguresAndSections EF Core migration to local Postgres (Figures and Sections tables created)
- Created Python stub scripts at src/skills/lecture-extractor-extracted/ — both print deterministic valid JSON and pass all tests

## Task Commits

Each task was committed atomically:

1. **Task 1: Add Figure, Section, ExtractionStatus models and extend Module** - `defb486` (feat)
2. **Task 2: Run EF migration and create Python stub scripts** - `9c1e925` (feat)

## Files Created/Modified

- `src/Api/Models/ExtractionStatus.cs` - Enum: NotStarted, Queued, Processing, Ready, Failed
- `src/Api/Models/Figure.cs` - Figure entity with DocumentId FK, S3Key, Keep, PageNumber, Caption, LabelType, ManifestMetadataJson
- `src/Api/Models/Section.cs` - Section entity with ModuleId FK, HeadingLevel, HeadingText, Content, SourcePageRefsJson, SortOrder
- `src/Api/Models/Module.cs` - Extended with ExtractionStatus, DocxS3Key, ExtractionError, Sections collection
- `src/Api/Models/Document.cs` - Extended with ICollection<Figure> Figures
- `src/Api/Data/AppDbContext.cs` - DbSet<Figure>, DbSet<Section>; Figure/Section OnModelCreating config; ExtractionStatus HasConversion<string>()
- `src/Api/Migrations/20260318203429_AddFiguresAndSections.cs` - EF Core migration (Figures, Sections tables)
- `src/skills/lecture-extractor-extracted/extract_images.py` - Stub: prints figure manifest JSON with 2 deterministic figures
- `src/skills/lecture-extractor-extracted/lecture_extractor.py` - Stub: prints sections JSON with 2 deterministic sections
- `src/skills/lecture-extractor-extracted/requirements.txt` - Empty (stdlib only)

## Decisions Made

- Figures attach to Document (not Module) because figure extraction is a per-document operation; Sections attach to Module because lecture extraction reads the DOCX at module level
- ExtractionStatus follows the same HasConversion<string>() pattern as DocumentStatus for readable string columns in Postgres
- Python stubs use only stdlib so requirements.txt stays empty and scripts run anywhere without pip install

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Self-Check: PASSED

All 7 key files confirmed present. Task commits defb486 and 9c1e925 verified in git log.

## Next Phase Readiness

- Figure, Section, ExtractionStatus entities are in the DB — Wave 1 Worker job implementations (03-03, 03-04) can now reference these types
- Python stubs at expected paths — Worker subprocess calls will succeed in stub mode without Python ML dependencies
- Full test suite green (23 passed, 10 skipped Wave 0 stubs — expected until implementation plans run)

---
*Phase: 03-objectives-and-figures*
*Completed: 2026-03-18*
