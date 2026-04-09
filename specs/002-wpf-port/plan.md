# Implementation Plan: WPF Port — LiteDB.Studio Migration

**Branch**: `002-wpf-port` | **Date**: 2026-04-07 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/002-wpf-port/spec.md`

## Summary

Migrate LiteDB.Studio from WinForms to WPF using MVVM architecture, achieving feature parity for execution engine, result display, database explorer, code completion, transactions, file operations, debugger, and logging. All database interactions routed through `IDatabaseService` abstraction. Implementation uses CommunityToolkit.Mvvm for MVVM patterns, AvalonEdit for SQL editor, Serilog for structured logging (30-day rolling retention), and maintains behavioral parity with existing WinForms `ConnectionForm.cs`. Targets **LiteDB 5.x** exclusively. Single active DB connection at all times; multi-DB is explicitly out of scope.

## Technical Context

**Language/Version**: C# / .NET 9.0  
**Primary Dependencies**: CommunityToolkit.Mvvm (MVVM framework), AvalonEdit (SQL editor), Serilog + Serilog.Sinks.File (logging), LiteDB 5.x (database engine)  
**Storage**: LiteDB database files (user-provided paths); `%APPDATA%\LiteDB.Studio\settings.json` (app preferences); `%APPDATA%\Temp\LiteDB.Studio\log-YYYYMMDD.txt` (rolling logs, 30-day retention)  
**Testing**: xUnit (align with existing `LiteDB.Tests` project); mocking via NSubstitute  
**Target Platform**: Windows desktop (WPF)  
**Project Type**: Single WPF desktop application  
**Performance Goals**:
- Execute and render queries <1s for typical local DBs (<1000 rows); <3s for larger result sets (up to 10k rows)
- Editor keystroke render ≤50ms p95
- Completion provider latency ≤200ms local schema (≤500ms on-demand fetch)
- Automatic completion debounce ≤150ms
- Logging overhead <5% performance impact  
**Constraints**:
- UI must not freeze during long queries (cancellable async operations)
- Result limiting (default 1000 rows, per-tab `RowLimit` override) prevents UI freezes
- Logging is mandatory, thread-safe, and must not store passwords
- Single active database connection at all times
- `password` parameter to `ConnectAsync` MUST NOT be logged, stored, or retained beyond the connection attempt  
**Scale/Scope**: ~9 implementation phases plus logging and editor porting; primary ViewModels (`MainViewModel`, `TabViewModel`, `DatabaseTreeViewModel`); ~20-30 commands; integration with LiteDB 5.x engine and Serilog

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Requirement | Status | Notes |
|------------|--------|-------|
| All edits via `apply_patch` | ✓ PASS | Constitution requires minimal diffs; implementation will use apply_patch for all code changes |
| Consistency with existing C#/WPF patterns | ✓ PASS | Spec requires following existing style and CommunityToolkit.Mvvm conventions |
| ViewModel-First UI Design | ✓ PASS | Spec mandates MVVM-first; UI logic in ViewModels only; code-behind for event wiring only |
| Resources under LiteDB.Studio.Wpf/Resources with pack URIs | ✓ PASS | Spec specifies resource path and pack URI format |
| DB access through IDatabaseService/LiteDbService | ✓ PASS | Spec requires all DB interactions via service abstraction |
| Tests required for logical changes | ✓ PASS | All phases require unit tests for ViewModels and integration tests for LiteDbService |
| Logging using Serilog with file output | ✓ PASS | Constitution v1.2.1 requires structured logging; spec v1.1 details Serilog integration, file location, exception logging, and 30-day retention |
| Human-only commits | ✓ PASS | Constitution and spec both require human-performed commits; agents prepare patches only |
| PowerShell for agent scripts | ✓ PASS | Repository uses PowerShell scripts in .specify/scripts/powershell/ |

**Gate Result**: ✅ PASS — No violations; all constitution requirements align with spec

**Post-Phase-1 Re-Check** (2026-04-07 — updated after 19 clarification Q&As):

| Requirement | Status | Notes |
|------------|--------|-------|
| Contracts align with constitution | ✓ PASS | IDatabaseService uses abstraction pattern; ViewModels follow MVVM-first; `readOnly`/`password` added to ConnectAsync |
| Data model supports testability | ✓ PASS | QueryResult, ColumnInfo, and ViewModels defined with clear boundaries; mockable service |
| Resource conventions followed | ✓ PASS | IconUri properties use pack URI format; quickstart documents resource path |
| Logging integration planned | ✓ PASS | Serilog configuration with 30-day retention, mandatory exception logging, no password logging |
| Tests planned | ✓ PASS | Unit tests for ViewModels (mocked service); integration tests for LiteDbService (in-memory LiteDB 5.x databases) |
| Password security | ✓ PASS | ConnectAsync password never logged, stored in preferences, or retained in ViewModel state |
| Single connection enforcement | ✓ PASS | ConnectCommand runs full disconnect flow before opening new connection |

**Post-Phase-1 Gate Result**: ✅ PASS — All design artifacts satisfy constitutional requirements

## Project Structure

### Documentation (this feature)

```text
specs/002-wpf-port/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── IDatabaseService.md
│   └── ViewModels.md
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
LiteDB.Studio.Wpf/
├── Services/
│   ├── IDatabaseService.cs          # DB abstraction interface
│   ├── LiteDbService.cs             # LiteDB 5.x implementation
│   └── SqlCompletionProvider.cs     # AvalonEdit completion provider
├── ViewModels/
│   ├── MainViewModel.cs             # Main window ViewModel
│   ├── TabViewModel.cs              # Editor/result tab ViewModel
│   ├── DatabaseTreeViewModel.cs     # DB explorer root ViewModel
│   ├── DbTreeNode.cs                # Tree node ViewModel (IsSystemCollection flag)
│   └── DebuggerViewModel.cs         # Debugger ViewModel
├── Views/
│   ├── MainWindow.xaml/.cs          # Main window view
│   ├── DatabaseTreeView.xaml/.cs    # DB explorer tree view
│   └── DebuggerView.xaml/.cs        # Debugger view
├── Controls/
│   ├── ResultGrid.xaml/.cs          # Result grid control with virtualization
│   ├── ResultTextView.xaml/.cs      # JSON text result view
│   ├── ParametersView.xaml/.cs      # Query parameters view
│   ├── FindReplaceControl.xaml/.cs  # Find/Replace control
│   └── AvalonEditBehaviors.cs       # Attached properties for AvalonEdit MVVM binding
├── Converters/
│   └── BsonValueToStringConverter.cs
├── Resources/
│   └── Icons/                       # Icon resources (pack URIs)
├── Settings/
│   └── AppSettings.cs               # Persisted settings model (RowLimit, FontFamily, FontSize, TabSize, ThemeName)
└── Util/
    ├── Logging.cs                   # Serilog configuration (30-day retention)
    └── AvalonEditBehavior.cs        # Legacy shim (deprecated; delegates to AvalonEditBehaviors)

LiteDB.Studio.Wpf.Tests/
├── ViewModels/
│   ├── MainViewModelTests.cs
│   ├── TabViewModelTests.cs
│   ├── DatabaseTreeViewModelTests.cs
│   └── DbTreeNodeTests.cs
├── Controls/
│   ├── AvalonEditBehaviorsTests.cs
│   ├── ResultGridTests.cs
│   ├── FindReplaceTests.cs
│   └── UndoRedoTests.cs
├── Integration/
│   └── LiteDbServiceTests.cs
└── Performance/
    ├── GridPerformanceTests.cs
    ├── MemoryLeakTests.cs
    ├── CompletionLatencyTests.cs
    ├── CompletionCachingTests.cs
    └── EditorTypingLatencyTests.cs

LiteDB.Studio/                        # Existing WinForms project (reference only)
└── Forms/ConnectionForm.cs           # Behavioral parity reference
```

**Structure Decision**: Single WPF application with MVVM structure. Existing `LiteDB.Studio.Wpf/` project contains partial WPF implementation; migration completes ViewModels, Views, Services, and adds comprehensive tests.

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
- BSON rendering: Custom `BsonValueToStringConverter`
- Tree lazy loading: `LoadChildrenAsync` with `IsLoaded` flag
- Testing: xUnit with NSubstitute; unit tests for ViewModels (mocked service); integration tests for LiteDbService (in-memory LiteDB 5.x databases)
- Error handling: Structured errors in `LastError`; confirmation dialogs for destructive actions
- Resources: Icons under `LiteDB.Studio.Wpf/Resources` with pack URIs
- Performance: Target <1s for queries <1000 rows; Stopwatch for `ExecutionTime`
- **LiteDB 5.x**: target latest stable 5.x; encryption via `ConnectionString.Password`; in-memory tests via `new LiteDatabase(":memory:")`
- **Logging retention**: 30 days via Serilog `retainedFileCountLimit: 30`

**Outcome**: All technical unknowns resolved; ready for design phase.

---

### Phase 1: Design & Contracts ✅ COMPLETE (updated 2026-04-07)

**Artifacts**:
- [data-model.md](data-model.md) — Core entities (updated with `RowLimit`, `LastConnectedPath`, `IsReadOnly`, `IsSystemCollection`, `IsDdl` metadata)
- [contracts/IDatabaseService.md](contracts/IDatabaseService.md) — Database service contract (updated with `readOnly`/`password` on `ConnectAsync`, `IsReadOnly` property, `IsDdl` in Metadata)
- [contracts/ViewModels.md](contracts/ViewModels.md) — ViewModel contracts (updated with all clarified properties/commands)
- [quickstart.md](quickstart.md) — Phase-by-phase implementation guide

**Key Deliverables**:
- `QueryResult` structure: Rows, Columns, LimitExceeded, RowCount, ExecutionTime, Warnings, Metadata (`IsDdl` flag for DDL auto-refresh)
- `IDatabaseService.ConnectAsync(connectionString, readOnly, password, cancellationToken)` — LiteDB 5.x; password never logged/stored
- `IDatabaseService.IsReadOnly` property — mutating operations disabled when true
- `MainViewModel`: Tabs, RunCommand, ConnectCommand (single-connection flow with transaction-rollback guard), `IsReadOnly`, `LastConnectedPath`
- `TabViewModel`: EditorText, `RowLimit` (per-tab override, transient), RunCommand (`CanExecute` depends on `IsConnected` only), `IsModified` (set by any content change; reset only by OpenFile/SaveFile)
- `DbTreeNode`: `IsSystemCollection` flag; system nodes expose Open+InsertSnippet only; user nodes expose Open+Drop+Export+InsertSnippet
- DDL auto-refresh: `MainViewModel` watches `QueryResult.Metadata["IsDdl"]` after each execution and calls `DatabaseTreeViewModel.LoadRootNodesAsync` automatically
- Disconnect flow: (1) warn+rollback if `TransactionActive`; (2) save prompts for modified tabs; (3) `DisconnectAsync` — tabs remain open but inactive

**Post-Phase-1 Constitution Check**: ✅ PASS

**Outcome**: All design artifacts updated; contracts reflect 19 clarification decisions; ready for implementation.

---

## Editor Porting Tasks (ICSharpCode.TextEditor → AvalonEdit)

1. Add NuGet: `ICSharpCode.AvalonEdit` to `LiteDB.Studio.Wpf.csproj`
2. Implement `AvalonEditBehaviors.cs` attached properties: `EditorText` (two-way), `CaretOffset`, `SelectionStart`, `SelectionLength`, `IsModified`, `ShowCompletionCommand`
3. Completion provider: `SqlCompletionProvider` implementing `ICompletionData`; queries `IDatabaseService` for schema; caching with TTL and schema-change invalidation; debounce ≤150ms
4. Run-selection & caret-aware: `TabViewModel.RunCommand` prefers selection; F5 and Ctrl+Enter both bind to same `RunCommand`
5. Preserve: syntax highlighting, undo/redo, find/replace (`FindReplaceControl`), tab size/font via `AppSettings`
6. CI check: `.github/scripts/verify-no-icsharpcodetexteditor.ps1` fails PR if `ICSharpCode.TextEditor` references remain in WPF sources
7. **IsModified rule**: any `EditorText` change (typed or programmatic, including `InsertSnippetCommand`) sets `IsModified = true`; only `OpenFileCommand` (load) and `SaveFileCommand` (successful save) reset it to `false`

---

## Key Clarification Decisions (reference — full log in spec.md)

| Decision | Resolution |
|----------|-----------|
| LiteDB version | 5.x (latest stable) |
| ConnectAsync signature | `ConnectAsync(string path, bool readOnly, string? password, CancellationToken ct)` |
| Read-only mode | First-class option (connection dialog + locked-file retry); `IsReadOnly` property; mutating commands disabled |
| `RunCommand` in read-only | Enabled; SELECT runs normally; writes return structured "read-only" error via `LastError` |
| Encryption | `ConnectionString.Password`; password never logged, stored, or retained |
| Multi-DB connections | Single connection only; explicitly out of scope |
| Tabs on disconnect | Remain open, become inactive; re-enabled on reconnect |
| Disconnect with active transaction | Warn dialog → confirm calls `RollbackTransactionAsync` → disconnect; cancel aborts disconnect |
| Session restore | Status bar clickable link "Reconnect to [filename]"; no auto-connect; `LastConnectedPath` property |
| Row limit override | Global default in settings panel; per-tab `RowLimit` input in toolbar (transient, not persisted) |
| DDL auto-refresh | `IsDdl` flag in `QueryResult.Metadata`; `MainViewModel` triggers `LoadRootNodesAsync` automatically |
| Conflict resolution | Reload (default pre-selected) / Overwrite / Merge (always shown; side-by-side field comparison) |
| Export format | JSON only; CSV and other formats explicitly out of scope |
| System collection menu | Open + InsertSnippet only; Drop and Export hidden; `DbTreeNode.IsSystemCollection` drives visibility |
| IsModified rule | Any content change sets true; OpenFile load and SaveFile success reset to false |
| Log retention | 30 days (`retainedFileCountLimit: 30`) |

---

## Next Steps

1. **Implement Phase 1 MVP** (`IDatabaseService`, `LiteDbService`, `TabViewModel.RunCommand`, `ResultGrid`)
2. **Open first PR**: Phase 1 Core Execution with unit tests for TabViewModel and integration tests for LiteDbService
3. **Iterate phases**: Phase 2–9 in priority order per tasks.md
4. **Human commits only**: Agent prepares patches; human reviews, commits, and pushes per constitution

---

## References

- Feature spec: [spec.md](spec.md)
- Research: [research.md](research.md)
- Data model: [data-model.md](data-model.md)
- Contracts: [contracts/IDatabaseService.md](contracts/IDatabaseService.md), [contracts/ViewModels.md](contracts/ViewModels.md)
- Quickstart: [quickstart.md](quickstart.md)
- Constitution: [.specify/memory/constitution.md](../../.specify/memory/constitution.md)
- Branch: `002-wpf-port`
