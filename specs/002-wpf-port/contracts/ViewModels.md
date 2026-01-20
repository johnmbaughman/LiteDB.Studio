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

### Commands

| Command | Type | Parameters | Preconditions | Behavior | Postconditions |
|---------|------|------------|---------------|----------|----------------|
| RunCommand | IAsyncRelayCommand | None | IsConnected = true; SelectedTab != null | Execute query in active tab | Tab.LastResult or Tab.LastError set |
| ConnectCommand | IAsyncRelayCommand | None | IsConnected = false | Show connection dialog; call service.ConnectAsync | IsConnected = true; Tree populated |
| DisconnectCommand | IAsyncRelayCommand | None | IsConnected = true | Call service.DisconnectAsync | IsConnected = false; Tree cleared |
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
- **ConnectCommand**: Shows connection dialog (file picker or connection string); validates input; calls service.ConnectAsync; on success, populates Tree root nodes and sets CurrentDatabase
- **DisconnectCommand**: Confirms if unsaved tabs exist; calls service.DisconnectAsync; clears Tree; sets CurrentDatabase = null
- **Transaction commands**: Enable/disable based on TransactionActive state; surface errors in message box
- **File commands**: Use standard file dialogs; filter for .sql files; track Filename and IsModified
- **RefreshTreeCommand**: Clears Tree.RootNodes; reloads via service.GetCollectionNamesAsync and GetSystemCollectionNamesAsync
- **InsertSnippetCommand**: Triggered by tree node double-click or context menu; inserts text at caret

### Initialization

```
Constructor dependencies: IDatabaseService
Initialization:
  - Tabs = new ObservableCollection<TabViewModel>()
  - Tree = new DatabaseTreeViewModel(databaseService)
  - Subscribe to service.ConnectionStateChanged → update IsConnected, CurrentDatabase
  - Subscribe to service.TransactionStateChanged → update TransactionActive
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

### Commands

| Command | Type | Parameters | Preconditions | Behavior | Postconditions |
|---------|------|------------|---------------|----------|----------------|
| RunCommand | IAsyncRelayCommand | None | databaseService.IsConnected = true | Execute query; update LastResult or LastError | LastResult or LastError set; IsResultLoaded = true |
| CloseCommand | IRelayCommand | None | None | Prompt save if IsModified; notify parent to remove tab | Tab closed |

### Behavior Notes

- **RunCommand**:
  - If SelectionLength > 0: execute selected text
  - Else: execute entire EditorText
  - Clear LastResult and LastError before execution
  - Call databaseService.ExecuteAsync(query, cancellationToken)
  - On success: set LastResult; clear LastError; set IsResultLoaded = false (defer grid rendering)
  - On error: set LastError with user-friendly message; clear LastResult
  - On cancellation: set LastError = "Cancelled by user"
- **CloseCommand**: If IsModified, show confirmation dialog; if user confirms save, trigger SaveFileCommand; remove tab from parent MainViewModel.Tabs
- **EditorText PropertyChanged**: Set IsModified = true (unless loading from file)

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

### Commands

| Command | Type | Parameters | Preconditions | Behavior | Postconditions |
|---------|------|------------|---------------|----------|----------------|
| LoadChildrenCommand | IAsyncRelayCommand | None | IsLoaded = false | Load children via service; set IsLoaded = true | Children populated |
| DropCommand | IAsyncRelayCommand | None | Node represents collection | Show confirmation; call service.ExecuteAsync("DROP COLLECTION ...") | Collection dropped; parent refreshes |
| ExportCommand | IAsyncRelayCommand | None | Node represents collection | Show file picker; export collection to JSON | File written |
| InsertSnippetCommand | IRelayCommand | None | Node represents collection or field | Notify MainViewModel to insert snippet | Snippet inserted in active tab |

### Behavior Notes

- **LoadChildrenCommand**:
  - Called on first expand (when IsExpanded changes from false to true and IsLoaded = false)
  - For collection nodes: call service.GetCollectionSchemaAsync(collectionName); create child nodes for fields/indexes
  - Set IsLoaded = true to prevent redundant loads
- **DropCommand**: Show confirmation dialog with collection name; require typed confirmation for user collections; call service.ExecuteAsync with DROP statement; on success, notify parent to refresh tree
- **ExportCommand**: Show SaveFileDialog; export collection documents to JSON file; show progress for large collections
- **InsertSnippetCommand**: Generate snippet text (e.g., "db.collectionName.find()"); notify MainViewModel.InsertSnippetCommand with snippet text

### Initialization

```
Constructor parameters: header, tag, iconUri, databaseService
Initialization:
  - Header = header
  - Tag = tag
  - IconUri = iconUri
  - Children = new ObservableCollection<DbTreeNode>()
  - IsLoaded = false
  - IsExpanded = false
```

## Acceptance Criteria

### MainViewModel
- [ ] RunCommand executes query in SelectedTab and updates LastResult or LastError
- [ ] ConnectCommand shows dialog, connects, populates Tree, sets IsConnected = true
- [ ] DisconnectCommand disconnects, clears Tree, sets IsConnected = false
- [ ] Transaction commands manage TransactionActive state via service
- [ ] File commands create/load/save tabs with correct Filename and IsModified state
- [ ] NewTabCommand creates empty tab; CloseTabCommand prompts save if modified

### TabViewModel
- [ ] RunCommand executes selection if present, else entire buffer
- [ ] RunCommand updates LastResult on success, LastError on error
- [ ] EditorText changes set IsModified = true
- [ ] CloseCommand prompts save if IsModified

### DatabaseTreeViewModel
- [ ] LoadRootNodesAsync populates RootNodes with collections
- [ ] Clear removes all nodes

### DbTreeNode
- [ ] LoadChildrenCommand lazy-loads children on first expand
- [ ] DropCommand shows confirmation and drops collection
- [ ] ExportCommand exports collection to JSON file
- [ ] InsertSnippetCommand inserts snippet in active tab
