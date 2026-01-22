# Implementation Plan: WPF Port — LiteDB.Studio Migration

**Branch**: `002-wpf-port` | **Date**: 2026-01-22 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/002-wpf-port/spec.md`

## Summary

Migrate LiteDB.Studio from WinForms to WPF using MVVM architecture, achieving feature parity for execution engine, result display, database explorer, code completion, transactions, file operations, debugger, and logging. All database interactions routed through IDatabaseService abstraction. Implementation uses CommunityToolkit.Mvvm for MVVM patterns, AvalonEdit for SQL editor, Serilog for structured logging, and maintains behavioral parity with existing WinForms ConnectionForm.cs.

## Technical Context

**Language/Version**: C# / .NET 9.0 (or compatible with existing LiteDB.Studio.Wpf.csproj target)  
**Primary Dependencies**: CommunityToolkit.Mvvm (MVVM framework), AvalonEdit (SQL editor), Serilog (logging framework), LiteDB (database engine)  
**Storage**: LiteDB database files (user-provided paths); application preferences for settings; log files in %APPDATA%\Temp\LiteDB.Studio\  
**Testing**: xUnit (align with existing LiteDB.Tests project); mocking via NSubstitute  
**Target Platform**: Windows desktop (WPF)  
**Project Type**: Single WPF desktop application  
**Performance Goals**: Execute and render queries <1s for typical local DBs (<1000 rows); <3s for larger result sets (up to 10k rows); logging adds <5% performance overhead  
**Constraints**: UI must not freeze during long queries (cancellable async operations); result limiting prevents UI freezes; logging is mandatory and thread-safe  
**Scale/Scope**: ~7 migration phases plus logging implementation; primary ViewModels (MainViewModel, TabViewModel, DatabaseTreeViewModel); ~20-30 commands; integration with existing LiteDB engine and Serilog

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
| Logging using Serilog with file output | ✓ PASS | Constitution v1.2.1 requires structured logging with mandatory exception handling; spec v1.1 details Serilog integration, file location, and exception logging with message and stack trace |
| Human-only commits | ✓ PASS | Constitution and spec both require human-performed commits; agents prepare patches only |
| PowerShell for agent scripts | ✓ PASS | Repository uses PowerShell scripts in .specify/scripts/powershell/ |

**Gate Result**: ✅ PASS — No violations; all constitution requirements align with spec

**Post-Phase-1 Re-Check** (2026-01-19):

| Requirement | Status | Notes |
|------------|--------|-------|
| Contracts align with constitution | ✓ PASS | IDatabaseService uses abstraction pattern; ViewModels follow MVVM-first |
| Data model supports testability | ✓ PASS | QueryResult, ColumnInfo, and ViewModels defined with clear boundaries; mockable service |
| Resource conventions followed | ✓ PASS | IconUri properties use pack URI format; quickstart documents resource path |
| Logging integration planned | ✓ PASS | Logging configuration and setup included in Phase 1 design artifacts, including mandatory exception handling with message and stack trace logging |
| Tests planned | ✓ PASS | Unit tests for ViewModels (mocked service); integration tests for LiteDbService (in-memory databases) |

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
│   ├── Logging.cs                   # Serilog configuration and setup
│   └── [utility classes]
└── App.xaml/.cs                     # Application entry point with logging initialization

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
- Testing: xUnit with NSubstitute; unit tests for ViewModels (mocked service); integration tests for LiteDbService (in-memory databases)
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

## Editor Porting Tasks (ICSharpCode.TextEditor → AvalonEdit)

This section captures concrete implementation steps introduced by the updated spec which mandates porting existing editor usages from `ICSharpCode.TextEditor` to `AvalonEdit`.

1. Add dependency
    - Add NuGet package reference: `ICSharpCode.AvalonEdit` to `LiteDB.Studio.Wpf.csproj`.
    - Document any optional packages required for advanced completion or templates in `quickstart.md`.

2. Editor MVVM binding (preferred: attached properties/behaviors)
    - Implement attached properties / behaviors that expose AvalonEdit editor state to `TabViewModel`: `EditorText`, `CaretOffset`/`LineColumn`, `SelectionStart`, `SelectionLength`, `IsModified`.
    - Attached properties/behaviors are the preferred MVVM-friendly pattern; avoid view code-behind except for minimal view-only wiring.

3. Completion provider
    - Implement completion using AvalonEdit's `CompletionWindow` and `ICompletionData` patterns.
    - Create `EditorCompletionService` that queries `IDatabaseService` for schema, collection names, and function suggestions.
    - Wire completion to `Ctrl+Space` and automatic triggers where appropriate; unit-test completion provider logic against mocked `IDatabaseService`.

4. Run-selection & caret-aware behavior
    - Ensure `TabViewModel.RunCommand` executes selection when a selection exists, otherwise executes full buffer.
    - Map keyboard shortcuts (F5, Ctrl+Enter) in the View to invoke the `RunCommand` on the `TabViewModel`.
    - Add unit tests to assert selection/run behavior and caret-aware execution.

5. Preserve editor features
    - Implement or retain syntax highlighting, undo/redo, find/replace hooks, and configurable tab size/font via application settings.
    - Ensure large-buffer performance (typing, navigation) remains within performance goals.

6. Tests & CI checks
    - Add unit tests in `LiteDB.Studio.Wpf.Tests/ViewModels/TabViewModelTests.cs` for `IsModified`, selection-run behaviors, and adapter bindings (mock `IDatabaseService`).
    - Add integration test(s) to exercise the editor-driven execution flow using `LiteDbService` with in-memory databases.

7. PR checklist / verification
    - Include an automatic verification step in the PR checklist (or CI job) that runs a repository search for `ICSharpCode.TextEditor` references and fails the check if any remain within `LiteDB.Studio.Wpf` sources.
    - Document the verification step in `specs/002-wpf-port/checklists/requirements.md`.

8. Migration cadence
    - Implement port incrementally per-story: start with Phase 1 (core execution) by adding lightweight attached properties that bind AvalonEdit `TextEditor` to `TabViewModel` (expose text/caret/selection/IsModified). Then progressively add completion, advanced editor features, and configuration in Phase 4 (Editor Enhancements).

Developer Notes:
- Prefer minimal changes and keep the existing `LiteDB.Studio` WinForms project untouched; the WPF project should choose AvalonEdit exclusively for editor components.
- Prefer attached properties/behaviors for binding AvalonEdit to `ViewModel`s; if an adapter is required for a specific scenario, keep it thin and well-tested.


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
