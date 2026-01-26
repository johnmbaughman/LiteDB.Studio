# LiteDB.Studio.Wpf C# Coding Style Summary (for Copilot instructions)

This summary reflects the observed C# and XAML style in the WPF project and the workspace settings in [.editorconfig](.editorconfig).

## Formatting and layout (from .editorconfig)
- Indentation: 4 spaces, spaces (no tabs). CRLF line endings. No final newline.
- Braces: always use braces for control blocks; open brace on a new line (`csharp_new_line_before_open_brace = all`).
- Namespace style: file-scoped namespaces are used in code; settings allow block-scoped but current code uses file-scoped.
- `using` directives: prefer outside namespace; no forced grouping; `System` directives are not forced to be first.
- Spaces: standard C# spacing (space after commas, around binary operators; no spaces inside parentheses or before semicolons).
- Preserve single-line statements/blocks when they already exist.

## Naming and structure
- Classes, methods, properties, events: PascalCase; interfaces begin with `I`.
- Private fields: underscore prefix (e.g., `_dbService`).
- Prefer `string.Empty` over `""`.
- Use `readonly` fields where possible.
- Prefer explicit access modifiers on non-interface members.

## C# language preferences (from .editorconfig + observed usage)
- Use `var` for built-in types and when the type is apparent; avoid `var` elsewhere.
- Prefer predefined types (e.g., `int`, `string`) in locals, parameters, and members.
- Prefer object/collection initializers, null-coalescing, null-propagation, and simplified interpolation.
- Prefer pattern matching and switch expressions where they improve clarity.
- Parentheses in binary operators are used for clarity.
- Expression-bodied accessors/properties are OK; methods typically use block bodies.

## MVVM and WPF patterns (project-specific)
- MVVM via `CommunityToolkit.Mvvm`:
  - ViewModels derive from `ObservableObject`.
  - Use `[ObservableProperty]` for simple properties; classes using it are `partial`.
  - Commands use `RelayCommand`/`AsyncRelayCommand`; bind via `ICommand` in XAML.
- Keep UI logic out of code-behind; code-behind limited to wiring `DataContext`, dialog ownership, and UI event glue.
- Use `ObservableCollection<T>` for bindable collections and raise property changed for derived properties.
- Prefer `DataTemplate`, bindings, and converters; avoid dynamic UI creation in code-behind.

## Async, errors, and logging
- Async API shape uses `Task`/`Task<T>` with `CancellationToken` parameters.
- Guard clauses for invalid state (`ArgumentNullException`, `InvalidOperationException`).
- Error handling uses try/catch with concise user-facing messages; structured logging with `Serilog.Log`.

## Service and data access conventions
- Database access is abstracted behind `IDatabaseService`.
- Methods are sync or async depending on underlying work; sync wrappers call async with `GetAwaiter().GetResult()`.
- Use `Task.CompletedTask` where an async method does no real async work.

## XAML conventions
- Use bindings for commands/state; avoid code-behind for behavior.
- Pack URIs for resources and icons.
- `Window` and control layout mirrors WinForms layout where necessary, but remains binding-driven.

## Practical guidance for new C# code in this project
- Follow MVVM with `ObservableObject`, `[ObservableProperty]`, and `ICommand` bindings.
- Keep code-behind minimal; place logic in ViewModels and services.
- Use guard clauses and explicit error handling; log with `Serilog.Log`.
- Prefer clear, readable code over clever constructs; keep public APIs stable.
