# Implementation Plan: WPF Port — LiteDB.Studio Migration

**Branch**: `002-wpf-port` | **Date**: 2026-01-19 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/002-wpf-port/spec.md`

## Summary

Migrate LiteDB.Studio from WinForms to WPF using MVVM architecture, achieving feature parity for execution engine, result display, database explorer, code completion, transactions, file operations, and debugger. All database interactions routed through IDatabaseService abstraction. Implementation uses CommunityToolkit.Mvvm for MVVM patterns, AvalonEdit for SQL editor, and maintains behavioral parity with existing WinForms ConnectionForm.cs.

## Technical Context

**Language/Version**: C# / .NET 9.0 (or compatible with existing LiteDB.Studio.Wpf.csproj target)  
**Primary Dependencies**: CommunityToolkit.Mvvm (MVVM framework), AvalonEdit (SQL editor), LiteDB (database engine)  
**Storage**: LiteDB database files (user-provided paths); application preferences for settings  
**Testing**: xUnit (align with existing LiteDB.Tests project); mocking via NSubstitute  
**Target Platform**: Windows desktop (WPF)  
**Project Type**: Single WPF desktop application  
**Performance Goals**: Execute and render queries <1s for typical local DBs (<1000 rows); <3s for larger result sets (up to 10k rows)  
**Constraints**: UI must not freeze during long queries (cancellable async operations); result limiting prevents UI freezes  
**Scale/Scope**: ~7 migration phases; primary ViewModels (MainViewModel, TabViewModel, DatabaseTreeViewModel); ~20-30 commands; integration with existing LiteDB engine

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Requirement | Status | Notes |
|------------|--------|-------|
| All edits via `apply_patch` | ✓ PASS | Constitution requires minimal diffs; implementation will use apply_patch for all code changes |
| Consistency with existing C#/WPF patterns | ✓ PASS | Spec requires following existing style and CommunityToolkit.Mvvm conventions |
| ViewModel-First UI Design | ✓ PASS | Spec mandates MVVM-first; UI logic in ViewModels only |
| Resources under LiteDB.Studio.Wpf/Resources with pack URIs | ✓ PASS | Spec specifies resource path and pack URI format |
| DB access through IDatabaseService/LiteDbService | ✓ PASS | Spec requires all DB interactions via service abstraction |
| Tests required for logical changes | ✓ PASS | Spec Phase 7 requires unit tests for ViewModels and integration tests for LiteDbService |
| Human-only commits | ✓ PASS | Constitution and spec both require human-performed commits; agents prepare patches only |
| PowerShell for agent scripts | ✓ PASS | Repository uses PowerShell scripts in .specify/scripts/powershell/ |

**Gate Result**: ✅ PASS — No violations; all constitution requirements align with spec

**Post-Phase-1 Re-Check** (2026-01-19):

| Requirement | Status | Notes |
|------------|--------|-------|
| Contracts align with constitution | ✓ PASS | IDatabaseService uses abstraction pattern; ViewModels follow MVVM-first |
| Data model supports testability | ✓ PASS | QueryResult, ColumnInfo, and ViewModels defined with clear boundaries; mockable service |
| Resource conventions followed | ✓ PASS | IconUri properties use pack URI format; quickstart documents resource path |
| Tests planned | ✓ PASS | Unit tests for ViewModels (mocked service); integration tests for LiteDbService (ephemeral DBs) |

**Post-Phase-1 Gate Result**: ✅ PASS — Design artifacts (data-model.md, contracts, quickstart.md) satisfy all constitutional requirements

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
LiteDB.Studio.Wpf/
├── Services/
│   ├── IDatabaseService.cs          # DB abstraction interface
│   ├── LiteDbService.cs             # LiteDB-specific implementation
│   └── [other services]
├── ViewModels/
│   ├── MainViewModel.cs             # Main window ViewModel
│   ├── TabViewModel.cs              # Editor/result tab ViewModel
│   ├── DatabaseTreeViewModel.cs     # DB explorer root ViewModel
│   └── DbTreeNode.cs                # Tree node ViewModel
├── Views/
│   ├── MainWindow.xaml/.cs          # Main window view
│   ├── EditorTab.xaml/.cs           # Editor/result tab view
│   └── DatabaseTreeView.xaml/.cs    # DB explorer tree view
├── Controls/
│   ├── ResultGrid.xaml/.cs          # Result grid control with virtualization
│   └── [other custom controls]
├── Converters/
│   └── BsonValueToStringConverter.cs # BSON rendering converter
├── Resources/
│   ├── Icons/                       # Icon resources (pack URIs)
│   └── [other resources]
├── Util/
│   └── [utility classes]
└── App.xaml/.cs                     # Application entry point

LiteDB.Studio.Wpf.Tests/             # New test project
├── ViewModels/
│   ├── MainViewModelTests.cs
│   ├── TabViewModelTests.cs
│   └── DatabaseTreeViewModelTests.cs
└── Integration/
    └── LiteDbServiceTests.cs

LiteDB.Studio/                        # Existing WinForms project (reference only)
└── Forms/ConnectionForm.cs           # Behavioral parity reference
```

**Structure Decision**: Single WPF application with MVVM structure. Existing `LiteDB.Studio.Wpf/` project contains partial WPF implementation; migration will complete ViewModels, Views, Services, and add comprehensive tests. New test project `LiteDB.Studio.Wpf.Tests` created for unit and integration tests.

## Complexity Tracking

No constitutional violations. Complexity tracking not required.

---

## Phase Execution Summary

### Phase 0: Outline & Research ✅ COMPLETE

**Artifacts**:
- [research.md](research.md) — Technology decisions and best practices

**Key Decisions**:
- MVVM framework: CommunityToolkit.Mvvm (source generators, async commands)
- SQL editor: AvalonEdit (syntax highlighting, completion support)
- Grid virtualization: WPF DataGrid with VirtualizingStackPanel
- Async patterns: async/await with CancellationToken throughout
- BSON rendering: Custom BsonValueToStringConverter
- Tree lazy loading: LoadChildrenAsync with IsLoaded flag
- Testing: xUnit with NSubstitute; unit tests for ViewModels (mocked service); integration tests for LiteDbService (ephemeral DBs)
- Error handling: Structured errors in LastError; confirmation dialogs for destructive actions
- Resources: Icons under LiteDB.Studio.Wpf/Resources with pack URIs
- Performance: Target <1s for queries <1000 rows; Stopwatch for ExecutionTime

**Outcome**: All technical unknowns resolved; ready for design phase.

---

### Phase 1: Design & Contracts ✅ COMPLETE

**Artifacts**:
- [data-model.md](data-model.md) — Core entities (QueryResult, ColumnInfo, TabViewModel, MainViewModel, DbTreeNode)
- [contracts/IDatabaseService.md](contracts/IDatabaseService.md) — Database service contract (connection, execution, schema, updates, transactions)
- [contracts/ViewModels.md](contracts/ViewModels.md) — ViewModel contracts (properties, commands, behaviors)
- [quickstart.md](quickstart.md) — Phase-by-phase implementation guide

**Agent Context Updated**:
- GitHub Copilot context file created: `.github/agents/copilot-instructions.md`
- Technologies added: C# / .NET 9.0, CommunityToolkit.Mvvm, AvalonEdit, LiteDB

**Key Deliverables**:
- QueryResult structure: Rows, Columns, LimitExceeded, RowCount, ExecutionTime, Warnings, Metadata
- IDatabaseService methods: ConnectAsync, ExecuteAsync, GetCollectionNamesAsync, GetCollectionSchemaAsync, UpdateDocumentFieldAsync, transaction primitives
- MainViewModel: Tabs, RunCommand, ConnectCommand, file commands, transaction commands
- TabViewModel: EditorText, RunCommand (execute selection or buffer), LastResult/LastError
- DbTreeNode: Lazy loading with LoadChildrenCommand, context actions (Drop, Export, InsertSnippet)

**Post-Phase-1 Constitution Check**: ✅ PASS — All design artifacts satisfy constitutional requirements.

**Outcome**: Data model, service contracts, and ViewModel contracts defined; implementation guide created; ready to proceed to implementation (next: `/speckit.tasks` to generate task breakdown).

---

## Next Steps

1. **Run `/speckit.tasks`** to generate prioritized task breakdown from this plan (creates `tasks.md`)
2. **Start Phase 1 implementation**: Implement `IDatabaseService`, `LiteDbService`, `TabViewModel.RunCommand`, and ResultGrid
3. **Open first PR**: Phase 1 MVP (core execution) with unit tests for TabViewModel and integration tests for LiteDbService
4. **Iterate phases**: Phase 2 (editing), Phase 3 (tree), Phase 4 (completion), Phase 5 (file ops), Phase 6 (transactions/debugger), Phase 7 (tests/polish)

---

## References

- Feature spec: [spec.md](spec.md)
- Research: [research.md](research.md)
- Data model: [data-model.md](data-model.md)
- Contracts: [contracts/IDatabaseService.md](contracts/IDatabaseService.md), [contracts/ViewModels.md](contracts/ViewModels.md)
- Quickstart: [quickstart.md](quickstart.md)
- Constitution: [.specify/memory/constitution.md](../../.specify/memory/constitution.md)
- Branch: `002-wpf-port`
