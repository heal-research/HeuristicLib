# Test suite guide

This folder contains the repository's executable test suites.

Use the project split to keep the fast unit test loop separate from broader execution scenarios.

## Where new tests go

- `HeuristicLib.Tests`: fast unit tests for core types, operators, algorithms and invariants owned by the core assembly.
- `HeuristicLib.Tests.Experimental`: fast unit tests for experimental types and invariants owned by the experimental assembly.
- `HeuristicLib.Tests.Scenarios`: broader workflow tests, execution composition tests, data backed checks and other scenarios that may span the main, experimental and integration libraries.
- `HeuristicLib.Tests.ApiUsageSpecs`: executable usage shape specs that document intended public API usage.
- `HeuristicLib.Tests.Docs`: executable examples published in the documentation through source imports.

Choose `HeuristicLib.Tests.ApiUsageSpecs` when the main value of the test is that it communicates intended public API usage clearly, compiles cleanly and runs in normal test flow.

Typical API usage spec characteristics:

- not an ordinary assertion heavy unit test
- expresses intended usage clearly, compiles cleanly and runs in normal test flow
- may contain only a few assertions when API shape and ergonomics are the main point
- keeps each spec focused on one user story and one intended usage flow
- does not remove seemingly unused setup, locals or intermediate values when they help show the intended usage flow
- prefers realistic names and explicit setup over compressed test helpers when the extra lines make the intended API usage clearer
- may keep current state and desired state specs side by side when that clarifies the refactoring path
- leaves detailed edge cases and invariant checks to the unit test projects

## Examples published in the guides

`HeuristicLib.Tests.Docs` owns executable walkthroughs whose named C# regions are included directly by VitePress. Organize the source by guide topic, such as `Extending`. Keep implementation and usage code in those source files; use `<<< path/to/Example.cs#region-name{csharp}` in the guide rather than copying the code. Tests keep their attributes and assertions outside the displayed regions. Use named regions rather than line numbers so ordinary source edits do not select the wrong lines.

Choose this project when the published example is the primary artifact. Use `HeuristicLib.Tests.ApiUsageSpecs` for intended API flows and inference checks that are useful independently of a guide. Keep one executable source for each published example; add a separate API usage spec only when it covers an additional contract.

The documentation suite references the core library and supplies only the test framework's global imports. Examples declare their own library imports and use public consumer APIs. Add other library dependencies only when an example needs them.

The documentation suite compiles and exercises these examples, and the documentation build checks their imported paths and region names. This connection applies only to imported source; a separate fenced code block is not validated merely because a similar usage spec exists.

```powershell
dotnet test --project test/HeuristicLib.Tests.Docs/HeuristicLib.Tests.Docs.csproj --configuration Release --no-restore
npm run docs:build
```

## Unit tests versus scenarios

Choose a unit test project when the test is primarily about one local behavior and should stay cheap in the normal development loop.

Typical unit test characteristics:

- narrow scope and clear ownership by one assembly
- small in memory setup
- focused assertions about behavior or invariants
- deterministic and quick to run

Choose `HeuristicLib.Tests.Scenarios` when the test is mainly about a composed workflow rather than one local invariant.

Typical scenario characteristics:

- exercises multiple subsystems together
- uses realistic datasets, execution composition or external integration points
- validates longer running or more story shaped execution flows
- may be slower and broader than unit tests, so it should not be treated as the inner development loop

When both views matter, keep the narrow invariant in a unit test project and add one representative end to end check in `HeuristicLib.Tests.Scenarios`.

## Validation order

Use the narrowest test scope that provides confidence:

1. Run focused `HeuristicLib.Tests` filters during implementation.
2. Run the complete `HeuristicLib.Tests` project after meaningful core changes.
3. Run `HeuristicLib.Tests.ApiUsageSpecs` for public API, authoring or usage changes.
4. Run `HeuristicLib.Tests.Docs` and build the documentation for changes to published examples.
5. Run `HeuristicLib.Tests.Experimental` for experimental changes or affected consumers.
6. Run `HeuristicLib.Tests.Scenarios` for broad workflow validation or once near completion.
7. Run the complete solution test suite for final validation of substantial cross project changes.

Scenario tests are often time consuming. Prefer the faster core, API usage and experimental projects for ordinary feedback.
