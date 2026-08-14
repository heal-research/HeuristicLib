# Test suite guide

This folder contains the repository's executable test suites.

Use the project split to keep the fast unit test loop separate from broader execution scenarios.

## Where new tests go

- `HeuristicLib.Tests`: fast unit tests for core types, operators, algorithms and invariants owned by the core assembly.
- `HeuristicLib.Tests.Experimental`: fast unit tests for experimental types and invariants owned by the experimental assembly.
- `HeuristicLib.Tests.Scenarios`: broader workflow tests, execution composition tests, data backed checks and other scenarios that may span the main, experimental and integration libraries.
- `HeuristicLib.Tests.ApiUsageSpecs`: executable usage shape specs that document intended public API usage.

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
4. Run `HeuristicLib.Tests.Experimental` for experimental changes or affected consumers.
5. Run `HeuristicLib.Tests.Scenarios` for broad workflow validation or once near completion.
6. Run the complete solution test suite for final validation of substantial cross project changes.

Scenario tests are often time consuming. Prefer the faster core, API usage and experimental projects for ordinary feedback.
