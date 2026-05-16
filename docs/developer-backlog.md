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
- improve project and assembly layering so the solution structure better matches responsibility boundaries; the current [package restructuring plan](../plans/package_restructuring_plan.md) assumes separate `HEAL.HeuristicLib` and `HEAL.HeuristicLib.Experimental` projects/NuGet packages, but the NuGet distribution strategy is still open: reconsider whether `HEAL.HeuristicLib` should ship both stable and experimental assemblies in one NuGet package, or whether stable and experimental APIs should live in a single project/assembly with experimental status communicated only through namespace placement and `ExperimentalAttribute`; the core tradeoff is install-time opt-in versus code-level opt-in and discoverability
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
- start building a glossary for the domain concepts the library is about, covering both heuristic optimization concepts and HeuristicLib-specific terms; consider using documentation interrogation workflows such as [grill-with-docs](https://github.com/mattpocock/skills/blob/main/skills/engineering/grill-with-docs/SKILL.md) to extract unclear, overloaded, or underdefined terminology from the existing docs and API specs
- clear separation between population topology and per-member side metadata as an executable example story
- decide and apply a clearer name for the current definition/execution-instance split. The design intent is not to change the core mechanics, but to present the existing architecture more clearly: user-facing `Algorithm` and `Operator` objects are reusable configuration objects, while a run activates them into stateful lifecycle objects. Evaluate replacing "definition" with "configuration" or "config" in docs, and replacing "execution instance" / "instance" with a term such as "runtime" if it better communicates that this object owns per-run mutable state. The documentation should also make the authoring story explicit: ordinary contributors usually implement logic on the configuration type through state-based base classes (`Mutator<TExecutionState>`, `Evaluator<TExecutionState>`, `IterativeAlgorithm<..., TExecutionState>`, ...), while direct runtime implementation remains the advanced escape hatch.
- re-evaluate the state-based algorithm authoring model. The current design places algorithm step logic on definition classes and passes resolved runtime dependencies through `TExecutionState`. This is convenient for simple operators, but for algorithms such as genetic algorithms it may be less natural than a first-class runtime/instance authoring model, because algorithm behavior wants to live near resolved executable dependencies. Compare keeping state-based authoring, restoring runtime-centered algorithm authoring, or supporting both with clear guidance.
- bring back first-class inner terminators for algorithms where users naturally expect them, especially iterative algorithms such as genetic algorithms. The current `IterativeAlgorithm` stream is effectively unbounded, and common termination such as max iterations is available through `TerminatableAlgorithm.WithMaxIterations(...)`, but a GA should still be able to expose a normal `Terminator`/termination configuration as part of its own API. Keep `TerminatableAlgorithm` only as an additional composition tool for algorithms that genuinely do not own termination, not as the only way to make ordinary iterative algorithms finite.
- clean up the test-suite organization and conventions: standardize test naming, file names, folders, and arrange/act/assert structure; use Shouldly consistently across all test projects, including extension tests; delete or restore commented-out tests; rethink `Explicit = true` for long-running tests and decide whether categories, traits, or separate projects should distinguish fast TDD/unit tests from manually triggered regression, smoke, and performance checks; CI should normally run all tests, but may run expensive regression groups only after the fast unit-test group passes; add a coverage-reporting baseline that highlights weak coverage for public APIs and important invariants
- migrate HeuristicLab operators and their unit tests in a structured way; track operator mappings, missing operators, test migration status, and intentional behavior/API differences in the [HeuristicLab operator migration overview](../plans/heuristiclab_operator_migration_overview.md)
- add explicit ownership-taking factory methods for genotype containers such as `RealVector`, `IntegerVector`, and similar types, following the explicit static-factory style rather than constructor overloads; the goal is to let callers that already own the backing storage transfer it without another allocation or copy, and random generation should be one of the first places to adopt this once the API shape is decided
- decide whether operators should expose a consistent caller-provided output-buffer or result-memory API. Avoid one-off `Memory<T>` parameters on individual operators. If this becomes necessary for allocation-sensitive workflows, design it as a library-wide operator convention that clearly states who owns the resulting storage and how immutable genotype containers are created from it.
- expand `IRandomNumberGenerator` and the concrete random engines so they expose the raw primitive outputs and data widths needed to build statistically sound and efficient higher-level sampling APIs; in particular, bounded integer generation should eventually be reworked on top of integer-domain primitives rather than `NextDouble()` scaling

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
