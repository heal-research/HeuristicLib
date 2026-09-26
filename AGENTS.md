# AGENTS.md

Contributor contract for this repository.

Follow [docs/contributing/developer-guidelines.md](docs/contributing/developer-guidelines.md) for implementation rules, public API conventions and architectural decisions. Durable product and architectural principles live in [docs/contributing/design-goals.md](docs/contributing/design-goals.md).

Use [docs/guide/glossary.md](docs/guide/glossary.md) for canonical HeuristicLib terminology when editing code, docs, tests, examples, and plans. Prefer glossary terms over legacy or ad hoc wording unless a local context explicitly defines a narrower meaning.

## Repository map

- `src/HeuristicLib.Contracts`: small public contracts shared across the library.
- `src/HeuristicLib`: core algorithms, operators, search spaces, candidate representations, problems, random engines, and analysis primitives.
- `src/HeuristicLib.Experimental`: experimental problems, workflows, and integration-oriented features.
- `src/HeuristicLib.PythonInterop`: Python-specific integration code, adapters, and workflows.
- `test/HeuristicLib.Tests`: fast unit tests for core library behavior and invariants.
- `test/HeuristicLib.Tests.Experimental`: fast unit tests for experimental library behavior and invariants.
- `test/HeuristicLib.Tests.Scenarios`: broader workflow, execution, and data-backed scenarios that may span core, experimental, and integration packages.
- `test/HeuristicLib.Tests.ApiUsageSpecs`: executable API usage specs; see `test/README.md` before editing specs.
- `analyzers`: Roslyn analyzers and code fixes for repository-specific API usage rules.
- `docs`: user-facing and design documentation.
- `samples`: runnable C# sample applications, one project per sample, referencing the projects in `src` so an API change breaks them here first.
- `python-samples`: Python demonstrations and notebooks. Not part of the solution, so nothing builds, formats or tests them.
- `.agents/skills`: agent skills for Codex; see "Agent skills" below.
- `.claude`: Claude Code project settings, which enable the `dotnet` plugin, and identical copies of the shared skills in `.agents/skills`.

Test-suite placement guidance lives in `test/README.md`.

## Validation commands

- Restore dependencies with `dotnet restore`.
- Build with `dotnet build --configuration Release --no-restore`.
- Run the selected test scope with `dotnet test --configuration Release --no-restore`.
- Check whitespace with `dotnet format whitespace ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore`.
- Check code style with `dotnet format style ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore --severity warn`.
- Check analyzer fixes with `dotnet format analyzers ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore --severity error`.
- CI currently runs restore, release build, release tests, formatting verification, and package creation. Formatting verification is currently non-blocking in CI, so do not treat a green CI format job as proof that formatting is clean.

### Test execution strategy

Use the narrowest test scope that provides confidence for the current change and prefer fast feedback during ordinary development.

1. During implementation, run focused tests from `HeuristicLib.Tests` with an appropriate test filter.
2. After meaningful core changes, run the complete `HeuristicLib.Tests` project. This is the primary test suite and the default broad validation during development.
3. Run `HeuristicLib.Tests.ApiUsageSpecs` when changing public APIs, authoring patterns, documentation examples or intended usage.
4. Run `HeuristicLib.Tests.Experimental` when changing experimental code or when core changes may affect experimental consumers.
5. Run `HeuristicLib.Tests.Scenarios` when changing scenario behavior, validating a broad workflow or performing final validation near completion. Scenario tests are often time consuming, so consider their value and timing carefully before running them.
6. Run the complete solution test suite only for final validation of substantial public API, shared invariant or cross-project changes.

Do not run scenario tests repeatedly during ordinary implementation iterations. Prefer the core, API usage and experimental test projects for faster results and feedback.

## Agent skills

The repository ships general-purpose agent skills, copied verbatim from their sources; `.agents/skills/README.md` lists them. Where a skill's instructions conflict with this file, the developer guidelines or the glossary, the repository wins. Adjust skill behavior in this section instead of editing the copied skills.

Claude Code gets `csharp-refactoring` and the C# language server from the `dotnet` plugin, which `.claude/settings.json` enables once the folder is trusted. Codex has no language server, so the skill falls back to compiler-checked edits there.

### Repository overrides

- Glossary, for `domain-modeling` and `grill-with-docs`: `docs/guide/glossary.md` is the domain glossary. Challenge terms against it and record resolved terms there, using its status labels and entry format. Entries may name the type that represents a concept. Do not create `CONTEXT.md` or `CONTEXT-MAP.md`.
- Decisions, for `domain-modeling` and `grill-with-docs`: do not create ADRs or `docs/adr/`. Record a settled decision in the plan of the change it belongs to under `plans/`. Approaches that were tried and rejected go to "Discussed, tried, and rejected" in `plans/developer-backlog.md`. Contributor and architecture docs describe the current state of the code, not decision history.
- Review scope, for `two-axis-review`: changes are usually left uncommitted, so review the working tree against the fixed point, `git diff $(git merge-base <fixed-point> HEAD)`, and include untracked files from `git status --short`.
- Review sources, for `two-axis-review`: there is no issue tracker, so skip `docs/agents/issue-tracker.md` and `/setup-matt-pocock-skills`. The spec is the branch's plan in `plans/`, usually `plans/<branch>.md`. The standards are this file, the developer guidelines, the design goals, the glossary and, for tests, `test/README.md`.
- Compatibility, for `csharp-refactoring`: backward compatibility is not a design goal (developer guidelines § 1.1). A requested public rename, move or removal is an authorized breaking change. Migrate every in-repository caller, including tests, samples and documentation snippets, and add no `[Obsolete]` shims, forwarding members or `[TypeForwardedTo]` attributes.
- Validation, for all skills: use the validation commands and test execution strategy above instead of a skill's generic build and test commands.
