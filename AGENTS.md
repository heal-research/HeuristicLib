# AGENTS.md

Contributor contract for this repository. Durable architectural rationale lives in [docs/design-goals.md](docs/design-goals.md).

## Repository map

- `src/HeuristicLib.Abstractions`: small public contracts shared across the library.
- `src/HeuristicLib.Core`: core algorithms, operators, search spaces, genotypes, problems, random engines, and analysis primitives.
- `src/HeuristicLib.Extensions`: extension problems, workflows, and integration-oriented features.
- `test/HeuristicLib.Core.Tests`: fast unit tests for core library behavior and invariants.
- `test/HeuristicLib.Extensions.Tests`: fast unit tests for extension library behavior and invariants.
- `test/HeuristicLib.Scenarios`: broader workflow, runtime, and data-backed scenarios that may span core and extensions.
- `test/HeuristicLib.ApiUsageSpecs`: executable API usage specs; see `test/README.md` before editing specs.
- `analyzers`: Roslyn analyzers and code fixes for repository-specific API usage rules.
- `docs`: user-facing and design documentation.
- `examples`: runnable examples and external-language demonstrations.

Test-suite placement guidance lives in `test/README.md`.

## Project posture

- HeuristicLib is in an early alpha stage.
- Backward compatibility is not a design goal yet.
- Current code and docs describe the current implementation, not a frozen target architecture.
- Public APIs and OOP structure may change when a materially better design is justified.

## Design rules

- Favor SOLID design and clear responsibility boundaries.
- Design for the pit of success.
- Prefer strong static typing, explicit invariants, and compile-time safety over conventions or late runtime checks.
- Keep abstractions small, composable, and explicit.
- Keep behavior-affecting dependencies such as randomness, time, caches, and execution context explicit.
- Avoid unnecessary magic runtime machinery, global registries, and opaque indirection.
- Optimize for human users first, with clear naming and predictable contracts.
- Do not treat current folders, assemblies, namespaces, or package names as the intended final architecture.

## Changing the design

- Start with a concrete problem, not stylistic preference alone.
- Compare meaningful alternatives before committing to a new direction.
- Prefer replacing a weak API with a stronger one over preserving compatibility with weak patterns.
- Justify design changes with clear usage examples.
- Update docs, examples, and tests when the architectural story changes.

## Workflow expectations

- Design public APIs to be easy to use correctly and hard to use incorrectly.
- Keep examples small, explicit, and representative of the intended usage style.
- Add or update tests when behavior or invariants change.
- Prefer removing accidental complexity over preserving familiar but weak patterns.
- Keep documentation aligned with code.

## Validation commands

- Restore dependencies with `dotnet restore`.
- Build with `dotnet build --configuration Release --no-restore`.
- Run tests with `dotnet test --configuration Release --no-restore`.
- Check formatting with `dotnet format ./HEAL.HeuristicLib.sln --verify-no-changes --no-restore --severity error`.
- CI currently runs restore, release build, release tests, formatting verification, and package creation. Formatting verification is currently non-blocking in CI, so do not treat a green CI format job as proof that formatting is clean.

Prefer targeted test runs while iterating, then run the narrowest command that gives confidence for the changed area. For public API, shared invariants, or cross-project changes, run the full release test command when feasible.
