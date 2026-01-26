# LiteDB.Studio.Wpf — Copilot C# Instruction Summary (Mashup)

This document consolidates the three provided reports into a single, actionable guide for writing C# code that matches the LiteDB.Studio.Wpf codebase.

## 1. Project Context
- **Framework**: .NET 9.0 (`net9.0-windows`)
- **UI**: WPF
- **Architecture**: MVVM
- **MVVM Toolkit**: `CommunityToolkit.Mvvm`
- **DI**: `Microsoft.Extensions.DependencyInjection`
- **Logging**: `Serilog`
- **Editor**: `AvalonEdit`
- **Nullable**: enabled; **ImplicitUsings**: enabled

## 2. Formatting Rules (from .editorconfig)
- **Indentation**: 4 spaces, no tabs.
- **Line endings**: CRLF.
- **Braces**: Allman (open brace on new line).
- **Newline before**: `catch`, `else`, `finally`.
- **Namespace**: file‑scoped for new files.
- **Usings**: outside namespace; no forced grouping or system-first sorting.
- **Spacing**: standard C# spacing (space after commas, around binary operators; none inside parentheses).
- **No `this.` qualification** for fields/properties/methods.

## 3. Naming Conventions
- **PascalCase**: classes, methods, properties, events, enums, constants.
- **Interfaces**: `I` prefix (e.g., `IDatabaseService`).
- **Private fields**: `_camelCase`.
- **Locals/params**: `camelCase`.
- **Async methods**: suffix `Async`.
- **Commands**: suffix `Command`.
- Prefer `string.Empty` over `""`.

## 4. C# Language Preferences
- Prefer **built‑in types** (`string`, `int`, `bool`).
- Use `var` when the type is apparent or built‑in; avoid when unclear.
- Prefer object/collection initializers, null‑coalescing, null‑propagation.
- Pattern matching and switch expressions where they improve clarity.
- Expression‑bodied **properties/accessors** are OK; **methods** typically use block bodies.

## 5. MVVM Patterns (CommunityToolkit.Mvvm)
- ViewModels inherit from `ObservableObject` and are **partial**.
- Use `[ObservableProperty]` for simple properties.
- Use manual properties or partial `OnXChanged` when custom logic is needed.
- Commands:
  - `RelayCommand` for sync
  - `AsyncRelayCommand` for async
- Keep UI logic out of code‑behind; code‑behind only for wiring and UI‑specific events.

## 6. Async/Await and Cancellation
- Async APIs return `Task`/`Task<T>` and accept `CancellationToken`.
- Pass tokens to downstream calls; check cancellation in loops.
- Use `Task.CompletedTask` for synchronous async implementations.
- Avoid `.Result`/`.Wait()` except for controlled sync wrappers like `Dispose()`.

## 7. Services & DI
- Abstract services behind interfaces (e.g., `IDatabaseService`).
- Constructor inject dependencies; guard with `ArgumentNullException`.
- Use guard clauses for invalid state (`InvalidOperationException`).
- Wrap external calls with try/catch and log.

## 8. Logging (Serilog)
- Use structured logging with named properties.
- Pass exceptions as the first argument.
- Avoid logging secrets/PII.

## 9. WPF / XAML Conventions
- Use bindings for commands/state; minimize code‑behind.
- Use Pack URIs for resources/icons.
- Prefer `ObservableCollection<T>` for bindable lists.
- Use attached behaviors for non‑bindable controls (e.g., AvalonEdit).

## 10. Quick Checklist for New C# Code
- [ ] File‑scoped namespace
- [ ] 4‑space indentation, Allman braces
- [ ] `_camelCase` private fields, PascalCase public API
- [ ] No `this.` qualification
- [ ] ViewModels: `ObservableObject` + `[ObservableProperty]`
- [ ] Commands: `RelayCommand` / `AsyncRelayCommand`
- [ ] Async methods include `CancellationToken` and use `await`
- [ ] Guard clauses for null/invalid state
- [ ] Structured Serilog logging
