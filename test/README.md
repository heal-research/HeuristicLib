# Test Suite Guide

This folder contains the repository's executable test suites.

Use the project split to keep the fast unit-test loop separate from broader runtime scenarios.

## Where New Tests Go

- `HeuristicLib.Core.Tests`: fast unit tests for core types, operators, algorithms, and invariants owned by the core assembly.
- `HeuristicLib.Extensions.Tests`: fast unit tests for extension-specific types and invariants owned by the extensions assembly.
- `HeuristicLib.Scenarios`: broader workflow tests, runtime composition tests, data-backed checks, and other scenarios that may span core and extensions.
- `HeuristicLib.ApiUsageSpecs`: executable usage-shape specs that document intended public API usage.

Choose `HeuristicLib.ApiUsageSpecs` when the main value of the test is that it communicates intended public API usage clearly, compiles cleanly, and runs in normal test flow.

Typical API usage spec characteristics:

- not an ordinary assertion-heavy unit test
- expresses intended usage clearly, compiles cleanly, and runs in normal test flow
- may contain only a few assertions when API shape and ergonomics are the main point
- keeps each spec focused on one user story and one intended usage flow
- does not remove seemingly unused setup, locals, or intermediate values when they help show the intended usage flow
- prefers realistic names and explicit setup over compressed test helpers when the extra lines make the intended API usage clearer
- may keep current-state and desired-state specs side by side when that clarifies the refactoring path
- leaves detailed edge cases and invariant checks to the unit-test projects

## Unit Tests vs. Scenarios

Choose a unit-test project when the test is primarily about one local behavior and should stay cheap in the normal TDD loop.

Typical unit-test characteristics:

- narrow scope and clear ownership by one assembly
- small in-memory setup
- focused assertions about behavior or invariants
- deterministic and quick to run

Choose `HeuristicLib.Scenarios` when the test is mainly about a composed workflow rather than one local invariant.

Typical scenario characteristics:

- exercises multiple subsystems together
- uses realistic datasets, runtime composition, or external integration points
- validates longer-running or more story-shaped execution flows
- may be slower and broader than unit tests, so it should not be treated as the inner TDD test cycle

When both views matter, keep the narrow invariant in the unit-test project and add one representative end-to-end check in `HeuristicLib.Scenarios`.
