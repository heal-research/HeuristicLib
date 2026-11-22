# Test suite guide

## Where new tests go

- `HeuristicLib.Tests`: fast unit tests for types, operators, algorithms and invariants of the core assembly.
- `HeuristicLib.Tests.Experimental`: fast unit tests for the experimental assembly.
- `HeuristicLib.Tests.Scenarios`: composed workflows, execution composition, data-backed checks and integration across the main, experimental and integration libraries.
- `HeuristicLib.Tests.ApiUsageSpecs`: executable specs of intended public API usage.
- `HeuristicLib.Tests.Docs`: executable examples imported into the published documentation.

A unit test covers one local behavior with small in-memory setup, focused assertions and quick deterministic execution. A scenario exercises several subsystems, realistic data or external integration and may be slow. When both views matter, keep the invariant in a unit test and add one representative end-to-end check to `HeuristicLib.Tests.Scenarios`.

## API usage specs

Choose `HeuristicLib.Tests.ApiUsageSpecs` when the test's main value is showing intended public API usage. A spec:

- covers one user story and one usage flow, and may contain only a few assertions;
- keeps setup, locals and intermediate values that show the flow, even if they look unused;
- prefers realistic names and explicit setup over compressed helpers;
- may keep current-state and desired-state specs side by side when that clarifies a refactoring;
- leaves edge cases and invariants to the unit test projects.

## Examples published in the guides

`HeuristicLib.Tests.Docs` owns walkthroughs whose named C# regions VitePress includes with `<<< path/to/Example.cs#region-name{csharp}`. Organize sources by guide topic, such as `Extending`, and keep test attributes and assertions outside the displayed regions. Use named regions, not line numbers. Keep one executable source per published example; add an API usage spec only for an additional contract.

The suite references only the core library and the test framework's global imports. Examples declare their own imports, use public consumer APIs and add other library dependencies only when needed. Only imported source is validated: the suite compiles and runs it, and the documentation build checks the imported paths and region names. A separate fenced code block is not validated.

## Validation order

Use the narrowest scope that gives confidence:

1. Focused `HeuristicLib.Tests` filters during implementation.
2. The complete `HeuristicLib.Tests` project after meaningful core changes.
3. `HeuristicLib.Tests.ApiUsageSpecs` for public API, authoring or usage changes.
4. `HeuristicLib.Tests.Docs` and `npm run docs:build` for changes to published examples.
5. `HeuristicLib.Tests.Experimental` for experimental changes or affected consumers.
6. `HeuristicLib.Tests.Scenarios` for broad workflow validation or near completion; these runs are slow.
7. The complete solution test suite for final validation of substantial cross-project changes.
