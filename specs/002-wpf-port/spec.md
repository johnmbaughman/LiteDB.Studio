---
title: WPF Port — LiteDB.Studio Migration
created: 2026-01-22
spec_version: 1.1
short_name: wpf-port
number: 2
---

# Purpose

Migrate and complete the WPF port of LiteDB.Studio from WinForms to WPF (MVVM), achieving feature parity for execution engine, result display, DB explorer, code completion, transactions, file operations, and debugger. This specification sets constitution-level constraints, required service/ViewModel contracts, prioritized migration phases (user stories with acceptance criteria), testing and CI gating, and delivery rules that enforce repository conventions.

**Constitution-level constraints**
- Apply changes via `apply_patch` for all code edits; keep diffs minimal and focused.
- Preserve existing C#/.NET and WPF styles; follow MVVM-first: place UI logic in `ViewModel`s only.
- Maintain behavior parity with `ConnectionForm.cs` unless an explicit migration plan is recorded.
- Place icons/resources under `LiteDB.Studio.Wpf/Resources` and reference with pack URIs: `pack://application:,,,/LiteDB.Studio.Wpf;component/Resources/<name>.png`.
- All DB access must use `IDatabaseService` / `LiteDbService` lifecycle APIs.
- Use CommunityToolkit.Mvvm patterns (`ObservableObject`, `IAsyncRelayCommand`, etc.).
- Add unit and integration tests for changed logic; tests must run green in CI.
- Implement mandatory file logging using Serilog; log files stored in `%APPDATA%\Temp\LiteDB.Studio\` folder with rolling file naming and structured logging for troubleshooting.

- Human-only commits: see Conformance & Developer Rules section below for the authoritative statement.

### Logging Requirements

As per constitution principle VI (Logging and Troubleshooting), the WPF port MUST implement structured logging using Serilog for mandatory file logging to support application troubleshooting and diagnostics.

- **Framework**: Use Serilog as the logging framework.
- **File Location**: Log files MUST be stored in the user's AppData temp folder under `LiteDB.Studio` subfolder (e.g., `%APPDATA%\Temp\LiteDB.Studio\`).
- **File Naming**: Use rolling file naming convention (e.g., `log-20260122.txt`, `log-20260123.txt`) to prevent single large files and manage log retention.
- **Retention**: Retain the **last 30 days** of log files; files older than 30 days MUST be deleted automatically (configure via Serilog `retainedFileCountLimit: 30`).
- **Log Levels**: Capture events at Debug, Information, Warning, Error, and Fatal levels with appropriate filtering for production use.
- **Log Content**: Each log entry MUST include timestamps, log level, source context, and descriptive messages.
- **Exception Handling**: All exceptions MUST be caught and logged with the full exception message and stack trace to facilitate debugging and diagnostics.
- **Key Events**: Log application startup/shutdown, database connections, query executions, errors, and other significant operations.
- **Accessibility**: Log file location MUST be documented for users to access for troubleshooting purposes.
- **Performance**: Logging MUST not degrade application performance by more than 5%.
- **Thread Safety**: Logging operations MUST be thread-safe to handle concurrent access.

### Editor Porting: ICSharpCode.TextEditor → AvalonEdit

- Rationale: replace legacy `ICSharpCode.TextEditor` usages with `AvalonEdit` (ICSharpCode.AvalonEdit) for first-class WPF integration, improved performance, and active maintenance.
- Requirements:
  - Port all WPF editor usages from `ICSharpCode.TextEditor` to `AvalonEdit`'s `TextEditor` control. Do not introduce a hybrid mix of both editors in the WPF project.
  - Implement completion using AvalonEdit's `CompletionWindow`/`ICompletionData` patterns; the completion provider must consult `IDatabaseService` for schema and function suggestions.
  - Expose editor state (text, caret position, selection, IsModified) to `TabViewModel` via attached properties/behaviors; attached properties/behaviors are the preferred MVVM pattern — avoid view code-behind except for minimal view-only wiring.
  - Ensure run-selection and caret-aware run behaviors map to existing commands (F5, Ctrl+Enter, Run selection) and are unit-tested against `TabViewModel` behaviors.
  - Preserve editor features: syntax highlighting, undo/redo, find/replace hooks (if present), and configurability for tab size/font via `App` settings.
  - Add the required NuGet dependency reference for AvalonEdit (ICSharpCode.AvalonEdit) and document any additional packages required for completion or text templating.

- Acceptance criteria for the port:
  - All editor-related acceptance tests (completion, run selection, caret-aware run, IsModified tracking) pass using the new AvalonEdit integration.
  - No remaining references to `ICSharpCode.TextEditor` exist within `LiteDB.Studio.Wpf` sources; include a search/replace verification step in the PR checklist.
  - Performance of editor operations (typing, large buffer navigation, selection-run) meets the non-functional constraints in this spec.


## Scope

In scope:
- Execution engine (SQL execution, parameter binding, cancellation, result limiting)
- Result rendering (grid/text/parameters, Bson rendering, edit/commit)
- Database explorer tree with lazy loading and context actions
- Editor features: code completion, run selection, caret tracking
- File operations: load/save SQL, modified tracking, save-before-close
- Transactions and debugger features ported from WinForms implementations
- Tests (unit and integration) and CI gating

Out of scope for this spec:
- Full UI visual redesign (keep existing workflows and affordances)
- Cross-platform packaging or installers
- Export formats other than JSON (CSV, XML, Excel, etc. are deferred to a future spec)
- Simultaneous multi-database connections: only one database is active at a time; invoking `ConnectCommand` while a connection is already active MUST first execute `DisconnectCommand` (with unsaved-tab save prompts); multi-DB support is deferred to a future spec

## Required Interfaces & Service Contracts

All database interactions MUST be routed through a database service abstraction (conceptually `IDatabaseService`). The spec describes required behaviors rather than exact language-specific signatures. Required behaviors:

- Connection lifecycle: the service MUST support connecting to and disconnecting from a database, and expose whether a connection is active. `ConnectAsync` MUST accept a `readOnly` flag and an optional `password` parameter (`null` = no encryption). The service MUST expose an `IsReadOnly` property. When `IsReadOnly` is `true`: `ExecuteAsync` with a write statement MUST return a structured error result (not throw) with a "database opened read-only" message surfaced to `LastError` — `RunCommand` is NOT disabled by `IsReadOnly` in the UI. `UpdateDocumentFieldAsync` and all transaction primitives (`BeginTransactionAsync`, `CommitTransactionAsync`, `RollbackTransactionAsync`) MUST have CanExecute guarded by `!IsReadOnly` in the ViewModel so the UI disables those controls entirely. The `password` value MUST NOT be logged, stored in preferences, or held in memory beyond the duration of the connection attempt.
- Execution: the service MUST accept a query (or selection) and return a structured result containing rows and metadata; execution MUST support cancellation and surface execution diagnostics (time, warnings).
- Schema & discovery: the service MUST provide collection / schema discovery operations (collection names, system collections, collection schema) used to populate the DB explorer and completion providers.
- Document updates: the service MUST support targeted document updates (update a field in a document) triggered by UI cell edits.
- Transactions: the service MUST support transactional primitives (begin, commit, rollback, checkpoint) appropriate for the underlying DB engine.
- Resource safety: the service MUST release file and database handles when disconnected and on disposal.

Implementation note: exact method signatures and language-specific types are recorded in the implementation plan at `specs/002-wpf-port/plan.md`. This keeps the spec technology-agnostic while preserving an authoritative implementation contract in the plan.

### QueryResult (behavioral shape)

The service MUST return a structured query result that provides rows and metadata to the UI and tests. At a conceptual level the result contains:

- Rows: an ordered collection of result documents (the UI should treat these as records to render). 
- Columns: ordered column metadata (name, type family, optional display/format hints) to enable correct rendering in grids.
- Limit indicator: a boolean flag indicating that a configured row limit was reached and results were truncated.
- Row count: number of rows returned (useful when less than configured limit).
- Execution diagnostics: elapsed execution time and optional warnings/messages from the service.
- Extensible metadata: a key/value bag for future signals. MUST include an `IsDdl` boolean entry when the executed statement is a schema-changing DDL (`CREATE`/`DROP` collection or index); consumers use this to trigger automatic tree refresh.

Notes:
- For extremely large result sets the implementation may expose a streaming API; the default service call returns the structured result to simplify UI rendering and testing.
- The presence of a limit indicator is required so the UI can surface an explicit message and offer export or re-run with a higher limit.

### ViewModel Contracts (required properties/commands)

MainViewModel (must expose):
- Tabs: an ordered collection of open editor/result tabs (`TabViewModel` instances).
- SelectedTab: the currently active tab.
- RunCommand: an asynchronous command that runs the current editor buffer or selection.
- ConnectCommand / DisconnectCommand: commands to manage DB connections. Only one database connection is active at a time; if `ConnectCommand` is invoked while already connected, it MUST first run the disconnect flow (unsaved-tab save prompts) before opening the new connection. The full disconnect flow is: (1) if `TransactionActive`, show "An active transaction will be rolled back. Proceed?" — on confirm call `RollbackTransactionAsync`; on cancel abort the disconnect; (2) prompt to save any `IsModified` tabs; (3) call `DisconnectAsync`. On disconnect, **open tabs are NOT closed** — they remain visible with their last editor content and result data intact. All DB-dependent commands on those tabs (`RunCommand`, edit commit, tree actions) become disabled until reconnected. The status bar shows a disconnected indicator in place of connection info. `ConnectCommand` MUST offer read-only mode as an explicit user choice (e.g., a checkbox or toggle in the connection dialog) in addition to the default read-write mode; it MUST also offer read-only as a retry when a write-open fails due to a file lock. The connection dialog MUST include an optional masked password field for encrypted LiteDB databases; blank = no encryption. The password MUST NOT be persisted to preferences, logged, or retained in memory after the connection is established or fails.
- IsReadOnly: a boolean property mirroring `IDatabaseService.IsReadOnly`; all mutating commands (transaction begin/commit/rollback, write-query execution) MUST have CanExecute depend on `!IsReadOnly`.
- LastConnectedPath: the file path of the last successfully connected database, loaded from preferences on startup and used to populate the status bar reconnect link; `null` when no prior connection exists or after the user explicitly dismisses the link.
- Transaction commands: begin/commit/rollback primitives surfaced as commands.
- File commands: open, save, and save-all commands.
- Tree refresh and snippet insertion commands.
- Tree: the root view model for the DB explorer.

TabViewModel (must expose):
- Title, Filename (optional), and IsModified state. `IsModified` is `true` after any content change (typed or programmatic); `false` only after `OpenFileCommand` loads a file or `SaveFileCommand` successfully writes one.
- Editor text and caret position information required for run/selection behaviors.
- LastResult and LastError: last execution result or error information (structured result and diagnostic messages).
- RowLimit: an integer property (default: loaded from global app settings, typically 1000); user edits it via a numeric input in the tab toolbar; `ExecuteAsync` passes this value as the row cap for that execution.
- Tab-scoped Run and Close commands. `RunCommand.CanExecute` depends solely on `IsConnected` — it is NOT blocked by `IsReadOnly`. In read-only mode, SELECT queries execute normally; write queries (`INSERT`, `UPDATE`, `DELETE`, `DROP`, etc.) are rejected by `LiteDbService` with a descriptive "database opened read-only" error surfaced to `LastError`. This lets users run reads freely without hunting for a disabled button.
- Lazy-load flags for large result views (e.g., whether result rendering has been materialized).

DatabaseTreeViewModel / DbTreeNode:
- Nodes expose a display header, identity tag, optional icon URI, and child nodes.
- Nodes support an asynchronous LoadChildren action to lazily populate children.
- Node context actions map to commands on MainViewModel or node-scoped commands. User collection nodes expose: Open, Drop, Export, InsertSnippet. System collection nodes expose: Open, InsertSnippet only — Drop and Export are hidden (not present in the context menu) to prevent accidental metadata corruption. `DbTreeNode` MUST carry a `IsSystemCollection` flag to drive context menu visibility.
- Auto-refresh after DDL: after `ExecuteAsync` completes, `MainViewModel` inspects `QueryResult.Metadata` for an `IsDdl` flag (set by `LiteDbService` when the executed statement is a schema-changing DDL). If `true`, `DatabaseTreeViewModel.LoadRootNodesAsync` is called automatically to refresh the root collection list without user intervention.

## Migration Phases (prioritized user stories)

Phase 1 — Core Execution (MVP)
- Story: As a user, I can run SQL against a connected database and see results in a grid.
- Acceptance Criteria:
  - `RunCommand` executes the SQL or selection via `IDatabaseService.ExecuteAsync` with a `CancellationToken`.
  - Default result rows limited to 1000 (global default, configurable in the app settings panel); each tab exposes a numeric `RowLimit` input in the toolbar or result bar — changes take effect on the next query run. UI shows `LimitExceeded` indicator when hit.
  - Long-running queries are cancellable; cancellation releases DB resources.
  - Errors surface to `LastError` on the active `TabViewModel`.

Phase 2 — Result Display & Editing
- Story: As a user, I can inspect results (Grid/Text/Parameters), edit cell values, and persist edits back to the DB.
- Acceptance Criteria:
  - `BsonValueToStringConverter` renders BSON types consistently.
  - Grid uses virtualization; editing a cell invokes `UpdateDocumentFieldAsync` on commit.
  - After edit commit, the UI refreshes affected rows or marks tab dirty.

Phase 3 — Database Explorer
- Story: As a user, I can browse collections, indexes, system collections, and insert snippets by double-click.
- Acceptance Criteria:
  - Tree lazy-loads children and uses `GetCollectionNames`/`GetCollectionSchemaAsync`.
  - **User collection** nodes expose four context actions: Open, Drop, Export, InsertSnippet.
  - **System collection** nodes (`$users`, `$cols`, `$indexes`, etc.) expose two context actions only: Open and InsertSnippet. Drop and Export are **hidden** (not disabled) for system nodes to prevent accidental metadata corruption.
  - After any DDL query execution (`CREATE`/`DROP` collection or index), the tree root automatically refreshes via `LoadRootNodesAsync` without requiring manual `RefreshTreeCommand` invocation.
  - Export writes the collection to a user-selected `.json` file (UTF-8, pretty-printed); **JSON is the only supported export format for this release**. CSV and other formats are explicitly out of scope.

Phase 4 — Editor Enhancements
- Story: As a user, I get code completion (Ctrl+Space), run selection (F5), and caret-aware run behavior.
- Acceptance Criteria:
  - AvalonEdit completion provider returns collection names and known functions from DB schema.
  - `RunCommand` runs selection when selection exists, else runs entire buffer.

Phase 5 — File Operations & UX
- Story: As a user, I can open/save SQL files, track modified state, and are prompted to save before closing.
- Acceptance Criteria:
  - `Filename` tracked. `IsModified = true` is set by **any** change to editor content — user typing, snippet insertion via `InsertSnippetCommand`, or any other programmatic text modification. `IsModified` is reset to `false` only by two explicit operations: `OpenFileCommand` (after file content is loaded) and `SaveFileCommand` (after a successful write). Tests for `InsertSnippetCommand` MUST assert `IsModified = true` post-insertion.
  - `SaveFileCommand` writes content to disk; failing writes surface errors and do not clear `IsModified`.
  - On startup, if a last-used DB path is stored in preferences, the **status bar** displays a clickable reconnect link (e.g., "Reconnect to [filename]") that invokes `ConnectCommand` when clicked. No automatic connection is attempted — the link is the sole prompt. If the user ignores it, the app starts with a blank state. If the file is missing or inaccessible at click time, a non-blocking error notification replaces the link. SQL tabs do NOT persist across sessions — tabs start fresh on every launch.

Phase 6 — Transactions & Debugger
- Story: As a user, I can begin/commit/rollback transactions and use the Database Debugger tools.
- Acceptance Criteria:
  - Transaction commands call `BeginTransactionAsync`/`CommitTransactionAsync`/`RollbackTransactionAsync` on `IDatabaseService`.
  - Debugger features ported so breakpoints & step actions operate with parity to WinForms behavior.

Phase 7 — Testing & Polish
- Story: Before merge, tests exist and CI gates run green.
- Acceptance Criteria:
  - Unit tests for `MainViewModel`, `TabViewModel`, and `DatabaseTreeViewModel` using mocked `IDatabaseService`.
  - Integration tests for `LiteDbService` using in-memory databases; tests cover connect/execute/update/transactions.
  - Performance checks for grid virtualization and a memory/leak review for repeated open/close cycles.

## Success Criteria (measurable)
- Users can execute SQL and see results within 3s for typical local DBs (<10k rows).
- 95% of queries under 1000 rows return and render in under 1s on a dev machine.
- The main user flows (connect → run → edit → save) have automated tests that pass in CI.
- Result limiting prevents UI freezes; users are explicitly shown when limits are reached.

### Performance SLOs (editor & completion)
- Editor typing responsiveness: UI should render keystrokes within 50ms for typical local edits (no heavy background processing).
- Completion provider latency: show completion results within 200ms for local schema lookups (IDatabaseService caching enabled); up to 500ms acceptable when on-demand schema fetch is required.
- Completion invocation behavior: `Ctrl+Space` must return results within the latency targets; automatic trigger debounce should be ≤150ms to avoid interruptive suggestions.

## Testing & CI Requirements

- All non-trivial changes must include unit tests. Tests must mock `IDatabaseService` for ViewModel tests.
- Integration tests for `LiteDbService` must use in-memory databases and clean up automatically after run.
- CI must run `dotnet test` for all test projects and fail the build on failures.
- PR template must include Problem, Approach, Risks, Tests, Rollout/Rollback notes.

## Security & Input Validation

- Sanitize SQL inputs where user-provided parameters are bound; do not embed unescaped inputs.
- Fail closed on malformed connection strings or file paths; do not allow operations without confirmation for destructive actions (e.g., DROP).
- Database passwords entered in the connection dialog MUST NOT be logged, persisted to application preferences or settings files, or retained in ViewModel state after a connection attempt completes (success or failure). Treat password fields as ephemeral secrets.

### Destructive Actions UX

- Always show a confirmation dialog for destructive operations (DROP collection, DELETE many, or other irreversible actions). The dialog must describe the operation and its scope (affected collection, query, or number of documents).
- Modifier behavior (Windows-like): if the user holds a modifier key (e.g., `Shift`) while invoking the destructive action, the operation MAY bypass or alter confirmation behavior to mirror Windows file-delete semantics (for example, `Shift+Delete` performs an immediate permanent delete). Implementations SHOULD surface clear affordances when modifier behavior is in effect (iconography or inline text) and may still show a compact confirmation if configured in settings.
- For bulk or high-risk actions, require the user to type a confirmation phrase (e.g., the collection name) before enabling the final confirm button.

## Edge Cases & Failure Handling

- Query cancellation: ensure cancellation releases DB resources and does not leave partial state; UI must surface cancellation as a distinct outcome from error.
- Connection loss during operation: surface a recoverable error, allow retry, and ensure UI does not crash or leak handles.
- Locked DB files: detect file locks and present three actionable recovery options: (1) retry with backoff after the user closes the competing process, (2) open the database in **read-only mode** (first-class connection option — invokes `ConnectAsync(readOnly: true)`), or (3) cancel. When opened in read-only mode, the UI must display a persistent read-only indicator and disable all mutating commands.
- Large results: avoid materializing extremely large result sets; provide streaming or pagination and ensure virtualization in UI components.
- Schema mismatch: gracefully handle unexpected/missing fields in result documents and show placeholder values rather than crashing.
- Concurrent updates: when two clients edit the same document, provide conflict detection and a conflict resolution dialog with three options always present: **Reload** (discard in-flight edit, refresh from DB), **Overwrite** (persist the user's edit, last-writer-wins), and **Merge** (show user's edited value alongside the current DB value in a side-by-side field comparison — always available since both values are known at conflict detection time). The dialog MUST pre-select **Reload** as the safe default; the confirm button is only enabled once the user has acknowledged the selection. Overwrite and Merge require an explicit user action to select.
- Transaction failures: surface failure reason and ensure aborted transactions revert UI state; do not implicitly retry destructive operations.
- Transaction active at disconnect: if `DisconnectCommand` (or the implicit disconnect from `ConnectCommand`) is invoked while `TransactionActive = true`, show a blocking confirmation dialog: "An active transaction will be rolled back. Proceed with disconnect?" Confirming calls `RollbackTransactionAsync` then proceeds with disconnect; cancelling aborts the disconnect entirely and leaves the connection and transaction intact.
- Partial failures during multi-step operations (export, batch updates): report per-item failures and allow retry for failed subset.

## Conformance & Developer Rules (enforceable checks)

- All code edits committed via `apply_patch` in this repository.
- New or modified code must include unit tests; CI will block merges for missing tests.
- Resources: icons must be added under `LiteDB.Studio.Wpf/Resources` and referenced via pack URIs.
- ViewModels must own UI logic; code-behind only for wiring and view-only behaviors.

- All git commits and pushes that record repository history MUST be performed by a human. Agents may generate patches but must not perform commit or push operations; CI checks may enforce committer metadata.

## Assumptions

- The WinForms `ConnectionForm.cs` behavior is the authoritative source for connection UX.
- Existing repository uses .NET patterns compatible with `CommunityToolkit.Mvvm`.
- `LiteDbService` targets **LiteDB 5.x** (latest stable). Encryption is handled via `ConnectionString.Password`; in-memory databases for integration tests use `new LiteDatabase(":memory:")`; transaction primitives map to LiteDB 5's `ILiteDatabase.BeginTrans` / `Commit` / `Rollback` APIs.
- Default row limit is 1000 (global default stored in application preferences, editable via the app settings panel). Individual tabs expose a `RowLimit` numeric input in their toolbar; edits are per-tab and transient (not persisted across sessions).
- The last-used DB file path is stored in application preferences and used to offer connection restore on next startup. Tab content (editor text) is NOT persisted across sessions.

## Deliverables

- Spec file: specs/002-wpf-port/spec.md (this file)
- Quality checklist: specs/002-wpf-port/checklists/requirements.md
- Reference implementation tasks: updates to `IDatabaseService`, `LiteDbService`, `MainViewModel`, `TabViewModel`, `DatabaseTreeViewModel`, editor completion provider, result rendering controls, and tests.

## Next Steps

1. Create branch `002-wpf-port` using the repository script and the arguments in the feature JSON.
2. Implement Phase 1 with tests (see `specs/002-wpf-port/plan.md` for concrete signatures): Execute, `MainViewModel.RunCommand`, and basic grid rendering.
3. Iterate phases in priority order, opening focused PRs for each phase with required PR template fields.

---

## Clarifications

### Session 2026-01-19

- Q: Which `ExecuteAsync` return shape should the service use? → A: Option B — Rich `QueryResult` (fields: `Rows`, `Columns`, `LimitExceeded`, `RowCount`, `ExecutionTime`, `Warnings`, `Metadata`).
 - Q: How should destructive DB actions be confirmed? → A: Options A+B — show confirmation dialog AND require a modifier (e.g., Shift or "I understand" checkbox); bulk/high-risk actions require typed confirmation.
 - Q: Which scope should the default row limit use? → A: Global default (1000) with per-tab/session override.

### Session 2026-01-20

- Q: Which binding approach should be used to expose AvalonEdit editor state to `ViewModel`s? → A: Option A — Attached properties / behaviors (MVVM-friendly). 
  - Rationale: Keeps UI logic out of `ViewModel`s, is testable, and minimizes code-behind; implement attached properties or behaviors to surface `EditorText`, `CaretOffset`, `SelectionStart`, `SelectionLength`, and `IsModified` to `TabViewModel`.

### Session 2026-04-07

- Q: Should read-only connection be a first-class connection option or only a fallback when a write-open fails? → A: Option A — First-class: `ConnectAsync` accepts a `readOnly` flag; user can deliberately open any DB read-only; all mutating commands disable when `IsReadOnly = true`.
- Q: Should the app restore the previous session on startup (re-connect to last DB, re-open SQL tabs)? → A: Option B — Connection only: remember and offer to re-connect to the last-used DB path on startup; SQL tabs start fresh each launch.
- Q: What should the default pre-selected action be in the concurrent-update conflict resolution dialog? → A: Option A — Reload (safe default): dialog pre-selects "Reload from DB"; user must actively choose Overwrite or Merge; confirm button requires explicit acknowledgment.
- Q: Should collection export support formats beyond JSON? → A: Option B — JSON only (intentionally scoped); CSV and other formats are explicitly out of scope for this release.
- Q: Should the connection dialog include a password field for encrypted LiteDB databases? → A: Option A — Yes: `ConnectAsync` accepts an optional `password` parameter; dialog includes a masked password field; password is never logged, stored, or retained beyond the connection attempt.

### Session 2026-04-07 (continued)

- Q: On startup, should last-DB reconnect be auto-connect silently or a prompt? → A: Option B variant — status bar clickable link ("Reconnect to [filename]"); no dialog, no auto-connect; link invokes `ConnectCommand`; dismissed by ignoring or on successful connect; `MainViewModel.LastConnectedPath` drives visibility.
- Q: Which LiteDB major version should `LiteDbService` target? → A: Option A — LiteDB 5.x (latest stable); encryption via `ConnectionString.Password`; in-memory test DBs via `new LiteDatabase(":memory:")`; transactions via `BeginTrans`/`Commit`/`Rollback` APIs.
- Q: Should the WPF port support simultaneous multi-database connections? → A: Option B — Single connection only (explicitly out of scope); invoking `ConnectCommand` while connected runs the full disconnect flow first; multi-DB deferred to a future spec.
- Q: What happens to open tabs when `DisconnectCommand` is invoked? → A: Option B — Tabs remain open and inactive; last editor content and results are preserved; `RunCommand` and all DB-dependent commands disable until reconnected; status bar shows disconnected state.
- Q: In read-only mode, should `RunCommand` be enabled (allow reads, surface write errors) or fully disabled? → A: Option A — Enabled; `RunCommand.CanExecute` depends only on `IsConnected`, not `IsReadOnly`; write statements return a descriptive "database opened read-only" error via `LastError`; only `UpdateDocumentFieldAsync` and transaction commands are disabled by `IsReadOnly`.

### Session 2026-04-07 (continued 2)

- Q: What happens when `DisconnectCommand` is invoked while a transaction is active? → A: Option A — Warn and force rollback: show "An active transaction will be rolled back. Proceed?" dialog; confirming calls `RollbackTransactionAsync` then disconnects; cancelling aborts the disconnect entirely.
- Q: How should the per-tab row limit override be exposed? → A: Options A+B — toolbar numeric input bound to `TabViewModel.RowLimit` for per-tab transient override; global default (1000) configurable in app settings panel. Per-tab changes are not persisted.
- Q: Should the database explorer tree auto-refresh after DDL queries? → A: Option A — Auto-refresh: `LiteDbService` sets `IsDdl=true` in `QueryResult.Metadata` for schema-changing DDL; `MainViewModel` triggers `LoadRootNodesAsync` automatically after each such execution.
- Q: Should the Merge option in the concurrent-update dialog always appear or only conditionally? → A: Option A — Always shown; Merge renders user's edited value vs. current DB value side-by-side (always available at conflict detection time); the "if feasible" qualifier removed.

### Session 2026-04-07 (continued 3)

- Q: Does `InsertSnippetCommand` (programmatic text insertion) set `IsModified = true`? → A: Option A — Yes; any EditorText change (typed or programmatic) sets `IsModified = true`; only `OpenFileCommand` (load) and `SaveFileCommand` (successful save) reset it to `false`.
- Q: What is the log file retention policy? → A: Last 30 days; files older than 30 days deleted automatically via Serilog `retainedFileCountLimit: 30`.
- Q: Should system collection nodes in the DB explorer expose the same context menu as user collections? → A: Option B — Restricted: system nodes expose Open and InsertSnippet only; Drop and Export are hidden; `DbTreeNode.IsSystemCollection` drives visibility.
