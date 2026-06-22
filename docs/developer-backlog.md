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
- revisit whether core and experimental unit tests should remain separate projects or be merged into one unit-test package; the current compromise keeps separate test projects for reporting while using stable `HEAL.HeuristicLib.Tests` namespaces so promotion from experimental to core does not require namespace churn
- reconsider `ExperimentalAttribute` once the main package has a meaningful stable API baseline; while HeuristicLib is still early alpha and all public APIs may change, package-level experimental opt-in is likely enough and avoids annotation ceremony that users may simply suppress wholesale
- define a concrete namespace and source-layout ergonomics target from real examples: reduce the `using` lists in `docs/getting-started.md`, `docs/overview.md`, and the symbolic-regression examples by deciding which user-facing namespaces should be flattened while keeping source folders detailed where useful; include a cleanup pass over operator folders so base types, cross-cutting instrumentation, and genotype-specific implementations are organized consistently without necessarily forcing one folder to equal one namespace
- improve the main `README.md` as the repository and NuGet front door: add install commands, a compact representative code example, a package overview, links to usage guides, and a visible example of what HeuristicLib can do, such as the Python interactive demonstrator GIF
- move to a more standard C# formatting baseline, including a more conventional editor configuration and CI enforcement that fails pull requests on formatting violations
- actively pay down solution warning debt and define a staged warning policy: fix the existing compiler, analyzer, and test warnings intentionally instead of normalizing them, then ratchet toward warning-clean builds and stricter CI enforcement in steps that avoid drowning active work in noise

Why these are only partial today:

- the experiment and analysis specs cover the current API well, but there is not yet a separate desired-state API story
- the analyzer docs are much clearer now, but the broader desired-state analysis spec story is still open
- immutability is now an explicit design rule, but that is stronger as policy than as a completed whole-repo audit

## Still not taken care of

These are still real open items:

- multi-objective short-path usage
- island-style population workflow
- fully implement ALPS-style age-layered workflow; the current ALPS algorithm still behaves like a simple single-layer evolutionary loop with a regular generation budget
- add an offspring-selection genetic algorithm implementation
- richer experiment scenarios
- symbolic-regression redesign follow-up, tracked in the [symbolic regression redesign plan](../plans/symbolic-regression-redesign-plan.md)
- dynamic-problem extension workflow
- meta-optimization by running an inner algorithm inside `Problem.Evaluate(...)`
- start building a glossary for the domain concepts the library is about, covering both heuristic optimization concepts and HeuristicLib-specific terms; consider using documentation interrogation workflows such as [grill-with-docs](https://github.com/mattpocock/skills/blob/main/skills/engineering/grill-with-docs/SKILL.md) to extract unclear, overloaded, or underdefined terminology from the existing docs and API specs
- clear separation between population topology and per-member side metadata as an executable example story
- decide and apply a clearer name for the current definition/execution-instance split. The design intent is not to change the core mechanics, but to present the existing architecture more clearly: user-facing `Algorithm` and `Operator` objects are reusable configuration objects, while a run activates them into stateful lifecycle objects. Evaluate replacing "definition" with "configuration" or "config" in docs, and replacing "execution instance" / "instance" with a term such as "runtime" if it better communicates that this object owns per-run mutable state. The documentation should also make the authoring story explicit: ordinary contributors usually implement logic on the configuration type through state-based base classes (`Mutator<TExecutionState>`, `Evaluator<TExecutionState>`, `IterativeAlgorithm<..., TExecutionState>`, ...), while direct runtime implementation remains the advanced escape hatch.
- re-evaluate the state-based algorithm authoring model. The current design places algorithm step logic on definition classes and passes resolved runtime dependencies through `TExecutionState`. This is convenient for simple operators, but for algorithms such as genetic algorithms it may be less natural than a first-class runtime/instance authoring model, because algorithm behavior wants to live near resolved executable dependencies. See the plan in [../plans/operator-authoring-base-classes.md](../plans/operator-authoring-base-classes.md).
- decide whether algorithm results should remain plain final/search states or whether HeuristicLib should introduce a distinct completion-result object that can carry the final state plus lifecycle metadata such as completion, early stopping, cancellation, failure, or stop reason; this is a larger result-model design decision and should not be hidden inside the termination overhaul
- rethink whether `IAlgorithm<...>` should require an `Evaluator` property. Several meta-algorithm wrappers only forward the inner algorithm's evaluator to satisfy the interface, which suggests evaluator access may be a convenience of current algorithm families rather than a true requirement of every algorithm abstraction.
- decide the broader run/resume/continue naming scheme and decide whether completion results should carry typed stop reasons; this should become its own API naming and lifecycle-result design discussion rather than remaining tied to the termination overhaul branch
- re-discuss algorithm builder APIs before extending them further: decide whether builders should become a polished, first-class configuration style with full feature parity, better type inference, and clear naming, or whether the library should phase them out in favor of object initializers, static factories, and small helper APIs
- add static factory methods for algorithms where constructors force callers to spell generic arguments that should be inferable from supplied collaborators; meta-algorithms such as `CycleAlgorithm.Create(...)` and `PipelineAlgorithm.Create(...)` are the most visible examples, but the convention should be considered across algorithm APIs
- reconsider the naming scheme for  and problem operator base classes. Decide whether common independent authoring bases should use the short main type name, with names such as `PopulationMutator`, `PopulationMutator` or similar reserved for less common variants whose semantics genuinely depend on the full population or batch.
- revisit where scalar problem batch execution should live. `SingleSolutionProblem` currently implements the batch `IProblem` contract by parallelizing scalar evaluation and therefore exposes `MaxDegreeOfParallelism`, but this leaks execution policy into a problem type. Moving this to `ProblemEvaluator` would require either a scalar problem contract, separate evaluator types or a type check with conditional parallelism semantics.
- clean up the test-suite organization and conventions: standardize test naming, file names, folders, and arrange/act/assert structure; use Shouldly consistently across all test projects, including extension tests; delete or restore commented-out tests; rethink `Explicit = true` for long-running tests and decide whether categories, traits, or separate projects should distinguish fast TDD/unit tests from manually triggered regression, smoke, and performance checks; CI should normally run all tests, but may run expensive regression groups only after the fast unit-test group passes; add a coverage-reporting baseline that highlights weak coverage for public APIs and important invariants
- migrate HeuristicLab operators and their unit tests in a structured way; track operator mappings, missing operators, test migration status, and intentional behavior/API differences in the [HeuristicLab operator migration overview](../plans/heuristiclab_operator_migration_overview.md)
- add a Python package bootstrap story for `HEAL.HeuristicLib.PythonInterop`: during early GitHub-based installs, the Python package should generate a tiny temporary `.csproj` that references the matching NuGet package and runs `dotnet publish` into a user cache, so Python users do not need checked-in generated DLLs. Later, replace or complement this with CI-built PyPI wheels that already contain the published .NET payload for normal Python installs.
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
