---
applyTo: '**.csproj, **/*.cs, **/*.xaml, **/*.sln'
---

# LiteDB.Studio.Wpf — GitHub Copilot Instructions

These instructions are intended to help contributors understand, build, run, and extend the WPF port of LiteDB.Studio.

## Context
- This workspace contains a WPF port of the original WinForms LiteDB Studio. Recent work added:
  - Extraction and inclusion of the application icon from `LiteDB.Studio/Forms/MainForm.resx` into `LiteDB.Studio.Wpf/Resources`.
  - A WPF `ConnectionManagerWindow` + `ConnectionManagerViewModel` that mirrors the WinForms `ConnectionForm` UX and populates `LiteDB.ConnectionString` exactly as the WinForms code.
  - Wiring from `MainViewModel.ConnectAsync()` to show the WPF connection dialog and call `IDatabaseService.ConnectAsync()`.
  - A tree view (`DatabaseTree`) populated with a `DbTreeNode` VM that mirrors the WinForms `tvwDatabase` (System node, collections, and system collections like `$cols`).
  - Node icons copied into `LiteDB.Studio.Wpf/Resources` and bound via pack URIs; a `GrayableImage` helper is used to render them.
  - Double-click and right-click context-menu behavior on tree nodes that insert SQL snippets into the query editor and optionally run them.
  - Context menu actions implemented: Query, Count, Explain plan (inserts EXPLAIN), Indexes (reads `$indexes` if present), Export to JSON, Analyze (placeholder), Rename, Drop collection.

## Files changed (key)
- `LiteDB.Studio.Wpf/Resources/*` — images (database.png, table.png, page_white_gear.png, etc.)
- `LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs` — connection flow, tree build, collection helpers (count, indexes, rename, drop, export)
- `LiteDB.Studio.Wpf/ViewModels/DbTreeNode.cs` — tree node VM with `Header`, `Tag`, `Icon`, `Children`
- `LiteDB.Studio.Wpf/Views/ConnectionManagerWindow.xaml` and .xaml.cs — WPF connection dialog
- `LiteDB.Studio.Wpf/MainWindow.xaml` & `MainWindow.xaml.cs` — TreeView binding, templates, double-click & context-menu handlers

## Build & run (developer checklist)
1. Build the solution (Windows, requires .NET SDK):

   `dotnet build LiteDB.Studio.Wpf\LiteDB.Studio.Wpf.csproj -c Debug`

2. Run the WPF app:

   `dotnet run --project "LiteDB.Studio.Wpf\LiteDB.Studio.Wpf.csproj" --configuration Debug`
3. Use the toolbar `Connect` button to open the `Connection Manager`. Connect to a `.db` file.

4. Inspect the left tree: root (database) -> System -> collections. Double-click a collection to insert a SQL snippet into the query editor. Right-click a node to access the context menu (Query, Count, Explain plan, Indexes, Export to JSON, Rename, Drop collection).

## Testing & verification
- Manual integration test: open a real `.db` file and confirm the following:
  - Tree populates with the database name, System node, and collections.
  - Icons appear next to nodes (database, folder, table, system entries).
  - Double-click inserts the expected SQL snippet into the editor.
  - Context menu actions perform the expected behaviors (Count shows a dialog; Export prompts Save; Rename and Drop prompt and update tree).

## Known limitations & TODOs
- `Analyze` is a placeholder and needs implementation.
- Collation combobox population in the connection dialog is a TODO (populate available cultures/sorts like the WinForms form).
- Some index information is read from a system collection (`$indexes`) as a best-effort; refine if the underlying LiteDB API provides an index enumeration.
- Tests: there are currently no automated UI tests; add integration tests or headless tests for `MainViewModel` logic.

## Guidelines for contributors
- Preserve behavioral parity with the original WinForms logic in `LiteDB.Studio/Forms/ConnectionForm.cs` when modifying connection mapping.
- Keep UI logic in ViewModels; avoid code-behind changes unless strictly UI-related (dialogs, event wiring).
- When adding icons, place them under `LiteDB.Studio.Wpf/Resources` and reference with pack URIs: `pack://application:,,,/LiteDB.Studio.Wpf;component/Resources/<name>.png`.

## Quick pointers (developer notes)
- Tree node model: `LiteDB.Studio.Wpf.ViewModels.DbTreeNode` (Header, Tag, Icon, Children).
- Editor tabs: `MainViewModel.Tabs` uses `TabViewModel` objects; `AddSqlSnippet()` adds SQL to the current/new tab.
- Database service: `LiteDB.Studio.Wpf.Services.IDatabaseService` and `LiteDbService` implement connection lifecycle.
