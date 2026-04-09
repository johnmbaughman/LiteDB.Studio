# ViewModel Contracts

**Purpose**: Define required properties, commands, and behaviors for ViewModels; ensure UI bindings and testability

## MainViewModel

### Properties

| Property | Type | Access | Description | Validation |
|----------|------|--------|-------------|------------|
| Tabs | ObservableCollection<TabViewModel> | Get | Open editor/result tabs | Non-null |
| SelectedTab | TabViewModel? | Get/Set | Currently active tab | Exists in Tabs or null |
| IsConnected | bool | Get | Database connection state | Derived from IDatabaseService.IsConnected |
| CurrentDatabase | string? | Get | Connected DB file path | Valid path or null |
| Tree | DatabaseTreeViewModel | Get | DB explorer root | Non-null |
| TransactionActive | bool | Get | Transaction in progress | Derived from IDatabaseService.TransactionActive |
| IsReadOnly | bool | Get | Connection is read-only | Derived from IDatabaseService.IsReadOnly; false when not connected |
| LastConnectedPath | string? | Get | File path of last successful connection | Persisted in AppSettings; drives status bar reconnect link |

### Commands

| Command | Type | Parameters | Preconditions | Behavior | Postconditions |
|---------|------|------------|---------------|----------|----------------|
| RunCommand | IAsyncRelayCommand | None | IsConnected = true; SelectedTab != null (NOT blocked by IsReadOnly) | Execute query in active tab | Tab.LastResult or Tab.LastError set |
| ConnectCommand | IAsyncRelayCommand | None | None (runs full disconnect flow first if already connected) | Show connection dialog (file picker, read-only toggle, optional password); if TransactionActive warn+rollback; prompt save for modified tabs; call service.ConnectAsync | IsConnected = true; IsReadOnly set; LastConnectedPath updated; Tree populated |
| DisconnectCommand | IAsyncRelayCommand | None | IsConnected = true | If TransactionActive: warn+rollback; prompt save for modified tabs; call service.DisconnectAsync; clear Tree; tabs remain open but inactive | IsConnected = false; Tree cleared |
| BeginTransactionCommand | IAsyncRelayCommand | None | IsConnected = true; TransactionActive = false | Call service.BeginTransactionAsync | TransactionActive = true |
| CommitTransactionCommand | IAsyncRelayCommand | None | TransactionActive = true | Call service.CommitTransactionAsync | TransactionActive = false |
| RollbackTransactionCommand | IAsyncRelayCommand | None | TransactionActive = true | Call service.RollbackTransactionAsync | TransactionActive = false |
| OpenFileCommand | IRelayCommand | None | None | Show file picker; create/select tab; load SQL file | New/selected tab with file content; IsModified = false |
| SaveFileCommand | IRelayCommand | None | SelectedTab != null; IsModified = true | Save tab content to file | IsModified = false; Filename set |
| SaveAllCommand | IRelayCommand | None | Any tab IsModified = true | Save all modified tabs | All tabs IsModified = false |
| RefreshTreeCommand | IAsyncRelayCommand | None | IsConnected = true | Reload tree root nodes | Tree refreshed with current schema |
| InsertSnippetCommand | IRelayCommand | string snippet | SelectedTab != null | Insert snippet at caret position | EditorText updated; IsModified = true |
| NewTabCommand | IRelayCommand | None | None | Create new empty tab; add to Tabs | New tab added; SelectedTab = new tab |
| CloseTabCommand | IRelayCommand | TabViewModel tab | tab in Tabs | Prompt save if modified; remove from Tabs | Tab removed |

### Behavior Notes

- **RunCommand**: Delegates to SelectedTab.RunCommand; updates SelectedTab.LastResult or LastError
- **ConnectCommand**: Shows connection dialog (file picker + read-only checkbox + optional password field); if already connected, runs full disconnect flow first (transaction rollback guard → save prompts → disconnect); calls service.ConnectAsync(path, readOnly, password, ct); on success, populates Tree root nodes, sets CurrentDatabase, IsReadOnly, and updates LastConnectedPath; password is not stored/logged
- **DisconnectCommand**: If TransactionActive, shows warn dialog → on confirm calls RollbackTransactionAsync → disconnect; on cancel aborts disconnect; prompts save for modified tabs; calls service.DisconnectAsync; clears Tree; tabs remain open but inactive (commands disabled until reconnect); sets CurrentDatabase = null
- **Transaction commands**: Enable/disable based on TransactionActive state; surface errors in message box
- **File commands**: Use standard file dialogs; filter for .sql files; track Filename and IsModified
- **RefreshTreeCommand**: Clears Tree.RootNodes; reloads via service.GetCollectionNamesAsync and GetSystemCollectionNamesAsync; also called automatically by MainViewModel when QueryResult.Metadata["IsDdl"] = true after any tab execution
- **DDL auto-refresh**: After each RunCommand completes, MainViewModel checks LastResult.Metadata["IsDdl"]; if true, calls DatabaseTreeViewModel.LoadRootNodesAsync automatically
- **Status bar reconnect link**: When not connected and LastConnectedPath is non-null, status bar shows clickable "Reconnect to [filename]" link that invokes ConnectCommand pre-populated with LastConnectedPath
- **InsertSnippetCommand**: Triggered by tree node double-click or context menu; inserts text at caret

### Initialization

```
Constructor dependencies: IDatabaseService
Initialization:
  - Tabs = new ObservableCollection<TabViewModel>()
  - Tree = new DatabaseTreeViewModel(databaseService)
  - Subscribe to service.ConnectionStateChanged → update IsConnected, IsReadOnly, CurrentDatabase
  - Subscribe to service.TransactionStateChanged → update TransactionActive
  - Load LastConnectedPath from AppSettings
  - Create initial empty tab
```

## TabViewModel

### Properties

| Property | Type | Access | Description | Validation |
|----------|------|--------|-------------|------------|
| Title | string | Get/Set | Tab display title | Non-empty |
| Filename | string? | Get/Set | Path to saved SQL file | Valid path or null |
| IsModified | bool | Get | Unsaved changes indicator | Set to true on EditorText change |
| EditorText | string | Get/Set | SQL editor content | May be empty |
| CaretOffset | int | Get/Set | Caret position in editor | >= 0; <= EditorText.Length |
| SelectionStart | int | Get/Set | Selection start offset | >= 0 |
| SelectionLength | int | Get/Set | Selection length | >= 0 |
| LastResult | QueryResult? | Get | Most recent query result | May be null |
| LastError | string? | Get | Last execution error message | May be null |
| IsResultLoaded | bool | Get/Set | Lazy-load flag for result view | Initial: false |
| RowLimit | int? | Get/Set | Per-tab row limit override (transient, not persisted) | >= 1; null uses global AppSettings default (1000) |

### Commands

| Command | Type | Parameters | Preconditions | Behavior | Postconditions |
|---------|------|------------|---------------|----------|----------------|
| RunCommand | IAsyncRelayCommand | None | databaseService.IsConnected = true | Execute query; update LastResult or LastError | LastResult or LastError set; IsResultLoaded = true |
| CloseCommand | IRelayCommand | None | None | Prompt save if IsModified; notify parent to remove tab | Tab closed |

### Behavior Notes

- **RunCommand**:
  - CanExecute: databaseService.IsConnected = true (NOT blocked by IsReadOnly)
  - If SelectionLength > 0: execute selected text
  - Else: execute entire EditorText
  - Row limit: use RowLimit if set; else use AppSettings.RowLimit (default 1000)
  - Clear LastResult and LastError before execution
  - Call databaseService.ExecuteAsync(query, cancellationToken)
  - On success: set LastResult; clear LastError; set IsResultLoaded = false (defer grid rendering)
  - On error (including read-only write attempt): set LastError with user-friendly message; clear LastResult
  - On cancellation: set LastError = "Cancelled by user"
- **CloseCommand**: If IsModified, show confirmation dialog; if user confirms save, trigger SaveFileCommand; remove tab from parent MainViewModel.Tabs
- **IsModified rule**: ANY change to EditorText (typed, programmatic, InsertSnippet) sets IsModified = true; ONLY OpenFileCommand (file load) and SaveFileCommand (successful save) reset IsModified = false

### Initialization

```
Constructor dependencies: IDatabaseService
Initialization:
  - Title = "Untitled"
  - EditorText = ""
  - IsModified = false
  - LastResult = null
  - LastError = null
  - IsResultLoaded = false
  - RowLimit = null (uses global AppSettings default)
```

## DatabaseTreeViewModel

### Properties

| Property | Type | Access | Description | Validation |
|----------|------|--------|-------------|------------|
| RootNodes | ObservableCollection<DbTreeNode> | Get | Top-level tree nodes | Non-null |

### Behavior

- **LoadRootNodesAsync**: Call service.GetCollectionNamesAsync and GetSystemCollectionNamesAsync; create DbTreeNode for each collection (user collections and system collections in separate parent nodes or flat list)
- **Clear**: RootNodes.Clear(); used on disconnect or refresh

### Initialization

```
Constructor dependencies: IDatabaseService
Initialization:
  - RootNodes = new ObservableCollection<DbTreeNode>()
  - If IsConnected, call LoadRootNodesAsync
```

## DbTreeNode

### Properties

| Property | Type | Access | Description | Validation |
|----------|------|--------|-------------|------------|
| Header | string | Get/Set | Display text | Non-empty |
| Tag | object? | Get/Set | Identity/metadata (collection name, node type) | May be null |
| IconUri | string? | Get/Set | Pack URI to icon resource | Valid pack URI or null |
| Children | ObservableCollection<DbTreeNode> | Get | Child nodes | Non-null |
| IsLoaded | bool | Get/Set | Lazy-load state | Initial: false |
| IsExpanded | bool | Get/Set | UI expansion state | Bindable |
| IsSystemCollection | bool | Get | Node is a system collection (_chunks, _files, etc.) | Immutable after construction; drives context menu visibility |

### Commands

| Command | Type | Parameters | Preconditions | Behavior | Postconditions |
|---------|------|------------|---------------|----------|----------------|
| LoadChildrenCommand | IAsyncRelayCommand | None | IsLoaded = false | Load children via service; set IsLoaded = true | Children populated |
| DropCommand | IAsyncRelayCommand | None | Node represents user collection (IsSystemCollection = false) | Show confirmation; call service.ExecuteAsync("DROP COLLECTION ...") | Collection dropped; parent refreshes |
| ExportCommand | IAsyncRelayCommand | None | Node represents user collection (IsSystemCollection = false) | Show file picker; export collection to JSON (JSON only) | File written |
| InsertSnippetCommand | IRelayCommand | None | Node represents collection or field | Notify MainViewModel to insert snippet | Snippet inserted in active tab; IsModified = true |

### Behavior Notes

- **LoadChildrenCommand**:
  - Called on first expand (when IsExpanded changes from false to true and IsLoaded = false)
  - For collection nodes: call service.GetCollectionSchemaAsync(collectionName); create child nodes for fields/indexes
  - Set IsLoaded = true to prevent redundant loads
- **DropCommand**: Only available when IsSystemCollection = false; show confirmation dialog with typed collection name verification; call service.ExecuteAsync with DROP statement; on success, notify parent to refresh tree
- **ExportCommand**: Only available when IsSystemCollection = false; show SaveFileDialog (JSON only; CSV and other formats out of scope); export collection documents to JSON file; show progress for large collections
- **InsertSnippetCommand**: Available for both user and system collections; generate snippet text (e.g., "db.collectionName.find()"); notify MainViewModel.InsertSnippetCommand with snippet text; sets IsModified = true on the active tab
- **Context menu visibility**: IsSystemCollection = true → show Open + InsertSnippet only; Drop and Export hidden; IsSystemCollection = false → show Open + Drop + Export + InsertSnippet

### Initialization

```
Constructor parameters: header, tag, iconUri, isSystemCollection, databaseService
Initialization:
  - Header = header
  - Tag = tag
  - IconUri = iconUri
  - IsSystemCollection = isSystemCollection
  - Children = new ObservableCollection<DbTreeNode>()
  - IsLoaded = false
  - IsExpanded = false
```

## Acceptance Criteria

### MainViewModel
- [ ] RunCommand executes query in SelectedTab and updates LastResult or LastError; CanExecute depends only on IsConnected (not IsReadOnly)
- [ ] ConnectCommand shows dialog with read-only toggle and password field; runs full disconnect flow if already connected; calls ConnectAsync(path, readOnly, password, ct); password not logged or stored
- [ ] ConnectCommand with active transaction: warns user, calls RollbackTransactionAsync, then disconnects before reconnecting
- [ ] DisconnectCommand: warns if TransactionActive → rollback; prompts save for modified tabs; tabs remain open but inactive after disconnect
- [ ] IsReadOnly and LastConnectedPath updated on connect; status bar reconnect link shown when disconnected
- [ ] After any RunCommand: if LastResult.Metadata["IsDdl"] = true, calls DatabaseTreeViewModel.LoadRootNodesAsync automatically
- [ ] Transaction commands manage TransactionActive state; BeginTransactionCommand disabled when IsReadOnly = true
- [ ] File commands create/load/save tabs with correct Filename and IsModified state

### TabViewModel
- [ ] RunCommand executes selection if present, else entire buffer; uses RowLimit if set, else AppSettings default
- [ ] RunCommand updates LastResult on success, LastError on error (including read-only write errors)
- [ ] ANY EditorText change (typed, programmatic, or InsertSnippet) sets IsModified = true
- [ ] ONLY OpenFileCommand (file load) and SaveFileCommand (successful save) reset IsModified = false
- [ ] CloseCommand prompts save if IsModified

### DatabaseTreeViewModel
- [ ] LoadRootNodesAsync populates RootNodes with user collections and system collections
- [ ] Clear removes all nodes

### DbTreeNode
- [ ] LoadChildrenCommand lazy-loads children on first expand
- [ ] IsSystemCollection = true: context menu shows Open + InsertSnippet only; Drop and Export hidden
- [ ] IsSystemCollection = false: context menu shows Open + Drop + Export + InsertSnippet
- [ ] DropCommand shows typed confirmation and drops user collection
- [ ] ExportCommand exports user collection to JSON file only
- [ ] InsertSnippetCommand inserts snippet in active tab; available for both user and system collections
