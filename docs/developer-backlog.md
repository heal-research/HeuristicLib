# Developer Backlog

This file is the short living tracker for doc, API-spec, and refactoring follow-up.

It is intentionally narrow: it should track only partial and unfinished follow-up.

## Partly addressed, but still open

These items have meaningful progress behind them, but should still stay on the backlog until the remaining work is done:

- desired-state experiment API, beside the current-state experiment specs
- desired-state analysis API, beside the current-state analysis specs
- runtime-graph invariants within one top-level execution
- role-aware mating and parent selection pressure differences
- decide and document one common rule for record configuration style: primary constructor parameters versus explicit init-only properties, including naming and when each form is allowed
- audit all solution candidate types used as `TGenotype` and refactor the model so solution candidates are immutable everywhere
- improve project and assembly layering so the solution structure better matches responsibility boundaries
- reduce namespace sprawl so common usage requires fewer `using` statements, while keeping a clear folder-level code organization
- move to a more standard C# formatting baseline, including a more conventional editor configuration and CI enforcement that fails pull requests on formatting violations

Why these are only partial today:

- the experiment and analysis specs cover the current API well, but there is not yet a separate desired-state API story
- the analyzer docs are much clearer now, but the broader desired-state analysis spec story is still open
- immutability is now an explicit design rule, but that is stronger as policy than as a completed whole-repo audit

## Still not taken care of

These are still real open items:

- multi-objective short-path usage
- island-style population workflow
- ALPS-style layered workflow
- richer experiment scenarios
- symbolic-regression validation scenarios
- dynamic-problem extension workflow
- meta-optimization by running an inner algorithm inside `Problem.Evaluate(...)`
- clear separation between population topology and per-member side metadata as an executable example story
- decide and apply a clearer name for the current definition/execution-instance split. The design intent is not to change the core mechanics, but to present the existing architecture more clearly: user-facing `Algorithm` and `Operator` objects are reusable configuration objects, while a run activates them into stateful lifecycle objects. Evaluate replacing "definition" with "configuration" or "config" in docs, and replacing "execution instance" / "instance" with a term such as "runtime" if it better communicates that this object owns per-run mutable state. The documentation should also make the authoring story explicit: ordinary contributors usually implement logic on the configuration type through state-based base classes (`Mutator<TExecutionState>`, `Evaluator<TExecutionState>`, `IterativeAlgorithm<..., TExecutionState>`, ...), while direct runtime implementation remains the advanced escape hatch.
- re-evaluate the state-based algorithm authoring model. The current design places algorithm step logic on definition classes and passes resolved runtime dependencies through `TExecutionState`. This is convenient for simple operators, but for algorithms such as genetic algorithms it may be less natural than a first-class runtime/instance authoring model, because algorithm behavior wants to live near resolved executable dependencies. Compare keeping state-based authoring, restoring runtime-centered algorithm authoring, or supporting both with clear guidance.
- bring back first-class inner terminators for algorithms where users naturally expect them, especially iterative algorithms such as genetic algorithms. The current `IterativeAlgorithm` stream is effectively unbounded, and common termination such as max iterations is available through `TerminatableAlgorithm.WithMaxIterations(...)`, but a GA should still be able to expose a normal `Terminator`/termination configuration as part of its own API. Keep `TerminatableAlgorithm` only as an additional composition tool for algorithms that genuinely do not own termination, not as the only way to make ordinary iterative algorithms finite.
- clean up the test-suite organization and conventions: standardize test naming, file names, folders, and arrange/act/assert structure; use Shouldly consistently across all test projects, including extension tests; delete or restore commented-out tests; rethink `Explicit = true` for long-running tests and decide whether categories, traits, or separate projects should distinguish fast TDD/unit tests from manually triggered regression, smoke, and performance checks; CI should normally run all tests, but may run expensive regression groups only after the fast unit-test group passes; add a coverage-reporting baseline that highlights weak coverage for public APIs and important invariants
- add explicit ownership-taking factory methods for genotype containers such as `RealVector`, `IntegerVector`, and similar types, following the explicit static-factory style rather than constructor overloads; the goal is to let callers that already own the backing storage transfer it without another allocation or copy, and random generation should be one of the first places to adopt this once the API shape is decided
- expand `IRandomNumberGenerator` and the concrete random engines so they expose the raw primitive outputs and data widths needed to build statistically sound and efficient higher-level sampling APIs; in particular, bounded integer generation should eventually be reworked on top of integer-domain primitives rather than `NextDouble()` scaling

## Still failing the current design rules

These are not just theoretical backlog items; the repository still contains active mismatches with current design guidance:

- remove all `Debug.Assert` usage and any other debug-versus-release behavioral divergence from library code, and replace correctness checks with runtime invariants, type constraints, or tests

At the moment there are still `Debug.Assert` calls in symbolic tree operators, so this item is not done.

## Source material

The active guiding docs remain:

- [design-goals.md](design-goals.md)
- [requirements.md](requirements.md)

## Keep or delete?

Keep this file only while it helps maintainers decide what still needs examples, refactoring, or cleanup.

Delete it once most remaining items have either:

- become concrete spec files, or
- moved into normal issue tracking or another active planning system

If it starts reading like a second architecture document again, cut it down.
