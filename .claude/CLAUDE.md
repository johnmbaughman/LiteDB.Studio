---
applyTo: '**'
---

# Claude Operating Guidelines — LiteDB.Studio

## Interaction
- Ask for missing context when correct output depends on it (runtime, constraints, file paths).
- Propose a short plan for broad/ambiguous requests; wait for confirmation before heavy changes.
- Prefer small, incremental edits with minimal diffs; highlight impacted files and rationale.
- Provide one primary solution; mention alternatives only when trade-offs materially differ.
- All agent tools must use PowerShell; Python is not reliably available.
- Create PowerShell script tools in `.github/tools`.
- **Never modify the `LiteDB` project** (third-party library).

## Code Quality
- Match existing style and patterns; don't introduce unrelated paradigms.
- Keep code simple and readable; optimize only when necessary.
- Self-explaining names; brief comments only where non-obvious.
- Avoid dead code, excessive abstraction, and speculative hooks.
- Fully clean up duplicated text or stale code when modifying files.

## Testing
- Propose targeted tests (unit/behavioral) covering success, failure, and edge cases.
- Clear Arrange-Act-Assert structure; deterministic tests only.
- Never include secrets/credentials; use configuration mechanisms.
- Always complete a build before running tests; fix issues first.

## Security & Privacy
- Validate and sanitize all external inputs; prefer safe defaults.
- Least privilege; fail closed; avoid insecure algorithms/APIs.
- No PII or secrets in logs; errors include actionable context only.

## Dependencies
- Prefer existing standard library or repo utilities.
- New dependency: justify why, note license/size/attack surface, suggest internal alternative.

## Error Handling & Observability
- Use established error/return patterns and logging abstraction.
- Messages aid debugging; avoid noisy or redundant logs.

## Performance
- Call out big-O or resource concerns; propose a simpler baseline first.
- Use repo conventions for I/O, concurrency, and retries.

## Reviews, Commits & PRs
- Reviews: correctness, safety, tests, performance, readability, docs.
- PR descriptions: problem, approach, risks, tests, rollout/rollback — concise bullets.
- Commit messages: imperative, scoped, concise.

## Output Style
- Code-first answers with brief explanations.
- Fenced code blocks with language hints.
- Next steps only when directly helpful.

## Don'ts
- Don't invent missing details.
- Don't introduce breaking changes or public API shifts without approval.
- Don't add external services/SDKs, tracking, or telemetry without opt-in.

## When Unsure
- State assumptions explicitly; ask one or two focused clarifying questions.

---

## C# & XAML Style (LiteDB.Studio.Wpf)

### Formatting
- 4-space indentation, spaces only, CRLF, no final newline.
- Braces always required; open brace on a new line.
- File-scoped namespaces.
- `using` directives outside namespace; no forced System-first grouping.

### Naming
- PascalCase for classes, methods, properties, events. `I` prefix for interfaces.
- Private fields: underscore prefix (`_dbService`).
- `string.Empty` over `""`. `readonly` fields where possible.
- Explicit access modifiers on non-interface members.

### C# Language
- `var` for built-in types and when type is apparent; avoid elsewhere.
- Predefined types (`int`, `string`) in locals, parameters, and members.
- Prefer object/collection initializers, null-coalescing (`??`), null-propagation (`?.`), simplified interpolation.
- Pattern matching and switch expressions where clarity improves.
- Expression-bodied accessors/properties OK; methods use block bodies.

### MVVM & WPF
- ViewModels derive from `ObservableObject` (CommunityToolkit.Mvvm).
- `[ObservableProperty]` for simple properties; class must be `partial`.
- Commands: `RelayCommand`/`AsyncRelayCommand`; bind via `ICommand` in XAML.
- Code-behind limited to: wiring `DataContext`, dialog ownership, UI event glue.
- `ObservableCollection<T>` for bindable collections.
- Prefer `DataTemplate`, bindings, and converters; avoid dynamic UI creation in code-behind.

### Async, Errors & Logging
- `Task`/`Task<T>` with `CancellationToken` parameters.
- Guard clauses: `ArgumentNullException`, `InvalidOperationException`.
- Try/catch with concise user-facing messages; structured logging via `Serilog.Log`.

### Services & Data Access
- Database access abstracted behind `IDatabaseService`.
- Sync wrappers use `.GetAwaiter().GetResult()`.
- `Task.CompletedTask` where no real async work is done.

### XAML
- Pack URIs for resources and icons.
- Binding-driven layout; avoid code-behind for behavior.
