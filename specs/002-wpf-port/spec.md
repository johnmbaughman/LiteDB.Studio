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

- All git commits and pushes that record repository history MUST be performed by a human. Agents are permitted to prepare patches (for example via `apply_patch`) and propose changes, but MUST NOT execute commits or pushes. Human reviewers MUST apply, review, sign, and push commits; CI may verify author/committer metadata as part of gating.

### Logging Requirements

As per constitution principle VI (Logging and Troubleshooting), the WPF port MUST implement structured logging using Serilog for mandatory file logging to support application troubleshooting and diagnostics.

- **Framework**: Use Serilog as the logging framework.
- **File Location**: Log files MUST be stored in the user's AppData temp folder under `LiteDB.Studio` subfolder (e.g., `%APPDATA%\Temp\LiteDB.Studio\`).
- **File Naming**: Use rolling file naming convention (e.g., `log-20260122.txt`, `log-20260123.txt`) to prevent single large files and manage log retention.
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

## Required Interfaces & Service Contracts

All database interactions MUST be routed through a database service abstraction (conceptually `IDatabaseService`). The spec describes required behaviors rather than exact language-specific signatures. Required behaviors:

- Connection lifecycle: the service MUST support connecting to and disconnecting from a database, and expose whether a connection is active.
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
- Extensible metadata: a key/value bag for future signals.

Notes:
- For extremely large result sets the implementation may expose a streaming API; the default service call returns the structured result to simplify UI rendering and testing.
- The presence of a limit indicator is required so the UI can surface an explicit message and offer export or re-run with a higher limit.

### ViewModel Contracts (required properties/commands)

MainViewModel (must expose):
- Tabs: an ordered collection of open editor/result tabs (`TabViewModel` instances).
- SelectedTab: the currently active tab.
- RunCommand: an asynchronous command that runs the current editor buffer or selection.
- ConnectCommand / DisconnectCommand: commands to manage DB connections.
- Transaction commands: begin/commit/rollback primitives surfaced as commands.
- File commands: open, save, and save-all commands.
- Tree refresh and snippet insertion commands.
- Tree: the root view model for the DB explorer.

TabViewModel (must expose):
- Title, Filename (optional), and IsModified state.
- Editor text and caret position information required for run/selection behaviors.
- LastResult and LastError: last execution result or error information (structured result and diagnostic messages).
- Tab-scoped Run and Close commands.
- Lazy-load flags for large result views (e.g., whether result rendering has been materialized).

DatabaseTreeViewModel / DbTreeNode:
- Nodes expose a display header, identity tag, optional icon URI, and child nodes.
- Nodes support an asynchronous LoadChildren action to lazily populate children.
- Node context actions map to commands on MainViewModel or node-scoped commands; actions include Open, Drop, Export, InsertSnippet.

## Migration Phases (prioritized user stories)

Phase 1 — Core Execution (MVP)
- Story: As a user, I can run SQL against a connected database and see results in a grid.
- Acceptance Criteria:
  - `RunCommand` executes the SQL or selection via `IDatabaseService.ExecuteAsync` with a `CancellationToken`.
  - Default result rows limited to 1000 (global default); each tab/session MAY override this limit; UI shows `LimitExceeded` indicator when hit.
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
  - Node context menu includes Open/Drop/Export/InsertSnippet actions; double-click inserts snippet into active editor.

Phase 4 — Editor Enhancements
- Story: As a user, I get code completion (Ctrl+Space), run selection (F5), and caret-aware run behavior.
- Acceptance Criteria:
  - AvalonEdit completion provider returns collection names and known functions from DB schema.
  - `RunCommand` runs selection when selection exists, else runs entire buffer.

Phase 5 — File Operations & UX
- Story: As a user, I can open/save SQL files, track modified state, and are prompted to save before closing.
- Acceptance Criteria:
  - `Filename` tracked; `IsModified` toggles on editor edits.
  - `SaveFileCommand` writes content to disk; failing writes surface errors and do not clear `IsModified`.

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

### Destructive Actions UX

- Always show a confirmation dialog for destructive operations (DROP collection, DELETE many, or other irreversible actions). The dialog must describe the operation and its scope (affected collection, query, or number of documents).
- Modifier behavior (Windows-like): if the user holds a modifier key (e.g., `Shift`) while invoking the destructive action, the operation MAY bypass or alter confirmation behavior to mirror Windows file-delete semantics (for example, `Shift+Delete` performs an immediate permanent delete). Implementations SHOULD surface clear affordances when modifier behavior is in effect (iconography or inline text) and may still show a compact confirmation if configured in settings.
- For bulk or high-risk actions, require the user to type a confirmation phrase (e.g., the collection name) before enabling the final confirm button.

## Edge Cases & Failure Handling

- Query cancellation: ensure cancellation releases DB resources and does not leave partial state; UI must surface cancellation as a distinct outcome from error.
- Connection loss during operation: surface a recoverable error, allow retry, and ensure UI does not crash or leak handles.
- Locked DB files: detect file locks and present actionable guidance (close competing processes, open read-only, or retry with backoff).
- Large results: avoid materializing extremely large result sets; provide streaming or pagination and ensure virtualization in UI components.
- Schema mismatch: gracefully handle unexpected/missing fields in result documents and show placeholder values rather than crashing.
- Concurrent updates: when two clients edit the same document, provide conflict detection and a clear resolution affordance (reload/overwrite/merge).
- Transaction failures: surface failure reason and ensure aborted transactions revert UI state; do not implicitly retry destructive operations.
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
- Default row limit is 1000 (global default stored in application preferences). Individual tabs/sessions MAY override this value transiently (per-tab/session override).

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
