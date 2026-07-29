# AGENTS.md

Contributor contract for this repository. Durable architectural rationale lives in [docs/design-goals.md](docs/design-goals.md).

Use [docs/glossary.md](docs/glossary.md) for canonical HeuristicLib terminology when editing code, docs, tests, examples, and plans. Prefer glossary terms over legacy or ad hoc wording unless a local context explicitly defines a narrower meaning.

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

## XML API documentation

Add XML documentation deliberately. Do not add summaries that only restate a type, member or parameter name. Prefer clear names and signatures while the API is evolving.

Use XML documentation only when it communicates a nonobvious contract, invariant, lifecycle rule, failure behavior, algorithm detail or usage constraint that belongs directly on the API. Put broader design guidance and examples in `docs` instead.

## Nullability and defensive validation

Treat nullable reference type annotations as repository contracts. Do not add defensive runtime null checks for nonnullable parameters, properties or collection elements. Handle null only when a type is explicitly nullable or when code operates at an untyped external boundary.

Do not systematically validate `ImmutableArray<T>.IsDefault`. Assume immutable array parameters are initialized unless a specific API defines default as meaningful or validation is required by a concrete domain invariant.

Do not add validation solely to defend against `null!`, disabled nullable analysis, reflection or other deliberate contract bypasses.

## Collection ownership

Core configurations and durable value objects use snapshot semantics. Accept retained finite ordered inputs as `IReadOnlyList<T>`, immediately snapshot them into `ImmutableArray<T>` and expose owned immutable collections as `ImmutableArray<T>`. Later changes to caller-owned input collections must not affect an existing configuration or durable value. Keep transient operation batches on `IReadOnlyList<T>` and use mutable collection returns only when mutation or ownership transfer is intentional.

## Validation commands

- Restore dependencies with `dotnet restore`.
- Build with `dotnet build --configuration Release --no-restore`.
- Run the selected test scope with `dotnet test --configuration Release --no-restore`.
- Check formatting with `dotnet format ./HEAL.HeuristicLib.sln --verify-no-changes --no-restore --severity error`.
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
