<!--
Sync Impact Report
- Version change: 1.2.0 → 1.2.1
- Modified sections: none
- Modified principles: VI. Logging and Troubleshooting (added mandatory exception handling guidance)
- Added sections: none
- Templates requiring updates: ✅ updated (no changes required - templates already reference constitution principles generically)
	- .specify/templates/plan-template.md ✅ updated
	- .specify/templates/spec-template.md ✅ updated
	- .specify/templates/tasks-template.md ✅ updated
	- .specify/templates/agent-file-template.md ✅ updated
	- .specify/templates/checklist-template.md ✅ updated
- Runtime docs to review: README.md ✅ updated (no changes required - no specific logging details in README)
- Follow-up TODOs: none (amendment is clarificatory and does not require migration or enforcement changes beyond existing logging implementation tasks)
-->

# LiteDB.Studio.Wpf Constitution

## Core Principles

### I. Minimal, Focused Changes (NON-NEGOTIABLE)
All edits MUST be applied via `apply_patch` and kept as minimal diffs. Changes MUST fix the root cause rather than apply surface-level workarounds. Avoid large rewrites unless a clear, documented migration plan exists.

### II. Consistency with Existing Patterns
Code MUST follow existing C# and WPF style and repository conventions. Do not introduce new paradigms or libraries without justification. Preserve naming, threading, and async conventions used across `LiteDB.Studio.Wpf`.

### III. ViewModel-First UI Design
UI logic MUST reside in ViewModels (for example `MainViewModel`, `DbTreeNode`). Code-behind is only permitted for UI wiring, event hookup, or platform-specific plumbing. Maintain behavioral parity with the WinForms implementation where specified. Use `CommunityToolkit.Mvvm` only for the MVVM framework.

### IV. Resource & Service Conventions
Icons and other static resources MUST be added under `LiteDB.Studio.Wpf/Resources` and referenced with pack URIs: `pack://application:,,,/LiteDB.Studio.Wpf;component/Resources/<name>.png`. Database lifecycle MUST be handled through `IDatabaseService` / `LiteDbService` abstractions.

### V. Tests, Validation, and Safety
Any logical change MUST include targeted unit or integration tests that cover normal, edge, and failure cases. Run relevant tests before formatting or committing changed files. Do NOT include secrets in code. All external inputs MUST be validated/sanitized and the system SHOULD fail closed on invalid inputs.

### VI. Logging and Troubleshooting
Logging MUST be implemented using a structured logging framework to capture application events, errors, and performance metrics. Troubleshooting information MUST be accessible through logs, UI feedback, and diagnostic tools. All exceptions MUST be caught and logged with the full exception message and stack trace to facilitate debugging and diagnostics. Specific logging levels, formats, and troubleshooting features will be defined in the spec, plan, and tasks.

## Additional Constraints
- Agent scripts and automation for this repository SHOULD use PowerShell when a script is required.  
- Commit messages MUST be imperative, scoped and concise (e.g., `fix: update DbTreeNode icon handling`).  
- Pull requests MUST include: Problem, Approach, Risks, Tests, and Rollout/Rollback notes in the description.
- All git commits and pushes that record repository history MUST be performed by a human. Agents are permitted to prepare patches (for example via `apply_patch`) and propose changes, but MUST NOT execute commits or pushes. Human reviewers MUST apply, review, sign, and push commits; CI may verify author/committer metadata as part of gating.

## Development Workflow
- Use `apply_patch` for file edits; ensure patches use the repository's diff format and preserve indentation/style.  
- Prefer small, incremental changes with clear commit boundaries.  
- Keep UI behavior aligned with `LiteDB.Studio/Forms/ConnectionForm.cs` for connection mapping and semantics unless explicitly refactored with a migration plan.  
- Place new icons in `LiteDB.Studio.Wpf/Resources` and reference them via pack URIs.  
- Add tests for any changed logic; integration tests are encouraged for `MainViewModel` behaviors that interact with `IDatabaseService`.

## Governance
Amendments to this constitution require: a documented rationale, a short migration plan for affected work, and a PR that references this constitution change. The project follows semantic versioning for governance: MAJOR for incompatible governance changes, MINOR for added principles or sections, PATCH for wording/typo fixes. Compliance checks (PR template, CI checks) SHOULD verify required tests and that `apply_patch` was used for non-trivial edits.

**Version**: 1.2.1 | **Ratified**: 2026-01-19 | **Last Amended**: 2026-01-22
