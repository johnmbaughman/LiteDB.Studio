# MVVM DI Standardization Plan (LiteDB.Studio.Wpf + LiteDB.Studio.Mvvm)

## Goals
- Standardize on DI-based view/viewmodel instantiation.
- Remove manual `new` viewmodel creation and static service lookups.
- Keep changes minimal, scoped to WPF + MVVM projects only (do not touch LiteDB).

## Scope (Projects)
- LiteDB.Studio.Wpf
- LiteDB.Studio.Mvvm

## Non-Goals **MANDATORY**
- **No changes** inside the LiteDB or LiteDB.Studio project trees.
- No functional UI redesigns.
- No broad MVVM framework rewrites.

---

## Current State Summary (Key Findings)
- **Shell pipeline uses DI**: `ConfigureUi<MainWindow, MainViewModel>` registers `ShellView`, `ShellViewModel`, `MainWindow`, and `MainViewModel` as singletons.
- **Manual instantiation exists**:
  - `MainViewModel` creates `DatabaseTreeViewModel` with `new`.
  - `DatabaseTreeViewModel` uses `LiteDbStudioApplication.ShellContentView` (static service lookup).
  - `ConnectionManagerViewModel` is created in XAML and also in code (duplicate creation path).

---

## Standardization Plan (Minimal Refactors)

### 1) Register missing viewmodels/services in DI
**Why**: Ensure all viewmodels are created by DI.

**Files**
- LiteDB.Studio.Wpf/App.xaml.cs

**Changes**
- Add DI registrations for:
  - `DatabaseTreeViewModel`
  - `ConnectionManagerViewModel`

**Example (conceptual)**
- `services.AddSingleton<DatabaseTreeViewModel>();`
- `services.AddTransient<ConnectionManagerViewModel>();` (transient is fine for dialogs)

---

### 2) Inject `DatabaseTreeViewModel` into `MainViewModel`
**Why**: Remove manual construction and let DI manage the graph.

**Files**
- LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs

**Changes**
- Update constructor signature:
  - From: `MainViewModel(IDatabaseService dbService)`
  - To: `MainViewModel(IDatabaseService dbService, DatabaseTreeViewModel tree)`
- Assign `Tree = tree;` instead of `new DatabaseTreeViewModel(dbService);`
- Keep existing event hookup:
  - `Tree.InsertSnippetRequested += (_, snippet) => InsertSnippet(snippet);`

---

### 3) Remove static service access in `DatabaseTreeViewModel`
**Why**: Avoid static service lookup and keep DI pure.

**Files**
- LiteDB.Studio.Wpf/ViewModels/DatabaseTreeViewModel.cs

**Changes**
- Change constructor signature to accept the shell content view or viewmodel:
  - Option A: `DatabaseTreeViewModel(IDatabaseService databaseService, IShellContentView shellContentView)`
  - Option B: `DatabaseTreeViewModel(IDatabaseService databaseService, IShellContentViewModel shellContentViewModel)`
- Replace `base(LiteDbStudioApplication.ShellContentView)` with injected dependency.

**Preferred**: Option A (`IShellContentView`) since `ShellViewModel` expects a view instance.

---

### 4) Standardize dialog viewmodel creation (Connection Manager)
**Why**: One creation path; no XAML instantiation.

**Files**
- LiteDB.Studio.Wpf/Views/ConnectionManagerWindow.xaml
- LiteDB.Studio.Wpf/ViewModels/MainViewModel.cs

**Changes**
- Remove XAML DataContext for `ConnectionManagerViewModel`.
- In `MainViewModel.ConnectAsync()`:
  - Resolve `ConnectionManagerViewModel` via DI (inject `IServiceProvider` or a small factory service).
  - Assign `win.DataContext = resolvedVm;`

**Minimal approach**
- Inject `IServiceProvider` into `MainViewModel` and call `GetRequiredService<ConnectionManagerViewModel>()` when opening the dialog.

---

### 5) Decide on ViewFactory usage (optional)
**Why**: Currently `ViewFactory` exists but isn’t used in WPF.

**Option A (Minimal)**
- Keep DI-only constructor injection and leave ViewFactory unused.

**Option B (Consistent mapping)**
- Register view/viewmodel pairs using `AddView<TView, TViewModel>` and resolve views via `IViewFactory`.

**Recommendation**
- Option A for minimal changes now.
- Revisit Option B later if dynamic view creation is needed.

---

## Suggested Implementation Order
1) Register missing DI services (Step 1).
2) Update `MainViewModel` constructor (Step 2).
3) Update `DatabaseTreeViewModel` constructor (Step 3).
4) Remove XAML DataContext and resolve `ConnectionManagerViewModel` via DI (Step 4).
5) (Optional) Decide on ViewFactory usage (Step 5).

---

## Expected Impact
- Cleaner DI graph with fewer static service calls.
- Single instantiation path per viewmodel.
- Easier testing and predictable lifetime behavior.

---

## Notes / Constraints
- Do not modify LiteDB project.
- Preserve existing behavior; refactor only construction patterns.
- Keep UI logic in viewmodels; keep code-behind minimal.
