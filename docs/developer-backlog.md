# Developer Backlog

This file is the short living tracker for doc, API-spec, and refactoring follow-up.

It is intentionally narrow: it should track only partial and unfinished follow-up.

## Partly addressed, but still open

These items have meaningful progress behind them, but should still stay on the backlog until the remaining work is done:

- decide whether the current mutable single use `AlgorithmRun` and `ExperimentRun` lifecycle should remain or be replaced by separate immutable setup and running types. Any replacement must preserve the direct fluent API while making repeated execution and analyzer attachment after execution impossible through the available types
- define the intended serialization boundary for experiments. Grid configurators and trial analyzer selectors and factories are runtime setup delegates, while materialized algorithm, operator and analyzer configurations remain serialization friendly in principle. Decide whether experiment configurations themselves need a persistable representation
- extend experiment composition only from concrete user needs. Deferred candidates include per trial problem and initial state creation, heterogeneous algorithm comparison, benchmark experiments across problem instances and dedicated individual cancellation handles
- reconsider typed hierarchical trial paths only if composed tuple keys and `RandomForkPath` metadata prove insufficient. Do not introduce untyped path tags that require casts
- desired-state analysis API, beside the current-state analysis specs
- execution-graph invariants within one run
- separate operation concurrency capability from requested execution policy. A future run-level execution context should let users select sequential or concurrent operation execution once, while operators and other execution boundaries declare the concurrency they safely allow. Define how the effective policy is constrained, how it reaches run-scoped instances through the execution registry without ambient state, how stateless operator bases capture run-specific settings, how problem evaluation participates, how per-trial experiment concurrency remains separate and whether maximum concurrency is per batch or shared across a whole run
- work out run-scoped mutable operator parameters as a deliberate authoring capability and present the pattern clearly to users. Define naming and ownership for immutable configured values versus mutable current instance values, identify examples beyond mutation strength and decide how coordinating algorithms discover and control these parameters without prematurely introducing a generic parameter framework
- design how operator wrappers and compositions preserve specialized execution instance capabilities such as variable mutation strength without adding role specific type checks and boilerplate to every general wrapper. Currently wrapping or composing `GaussianMutator` hides `IVariableStrengthMutatorInstance` from `EvolutionStrategy` and disables mutation strength adaptation
- role-aware mating and parent selection pressure differences
- decide and document one common rule for record configuration style: primary constructor parameters versus explicit init-only properties, including naming and when each form is allowed
- audit all candidate types used as `TCandidate` and refactor the model so candidates are immutable everywhere
- revisit whether core and experimental unit tests should remain separate projects or be merged into one unit-test package; the current compromise keeps separate test projects for reporting while using stable `HEAL.HeuristicLib.Tests` namespaces so promotion from experimental to core does not require namespace churn
- reconsider `ExperimentalAttribute` once the main package has a meaningful stable API baseline; while HeuristicLib is still early alpha and all public APIs may change, package-level experimental opt-in is likely enough and avoids annotation ceremony that users may simply suppress wholesale
- define a concrete namespace and source-layout ergonomics target from real examples: reduce the `using` lists in `docs/getting-started.md`, `docs/overview.md`, and the symbolic-regression examples by deciding which user-facing namespaces should be flattened while keeping source folders detailed where useful; include a cleanup pass over operator folders so base types, cross-cutting instrumentation, and candidate-specific implementations are organized consistently without necessarily forcing one folder to equal one namespace
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
- rework the objective system around the glossary terms `objective value`, `objective vector`, `objective direction`, and `objective directions`: keep the conceptual model open enough for single-objective and multi-objective cases, and clarify when an algorithm/operator requires exactly one objective value, a total ordering over objective vectors, or multi-objective comparison semantics
- island-style population workflow
- fully implement ALPS-style age-layered workflow; the current ALPS algorithm still behaves like a simple single-layer evolutionary loop with a regular generation budget
- add an offspring-selection genetic algorithm implementation
- richer experiment scenarios
- symbolic-regression validation scenarios
- dynamic-problem extension workflow
- meta-optimization by running an inner algorithm inside `Problem.Evaluate(...)`
- apply remaining glossary terminology to public API and code names where the current names still encode legacy concepts, while keeping namespace and folder cleanup in separate branches
- clear separation between population topology and per-member side metadata as an executable example story
- decide whether algorithm results should remain plain final/search states or whether HeuristicLib should introduce a distinct completion-result object that can carry the final state plus lifecycle metadata such as completion, early stopping, cancellation, failure, or stop reason; this is a larger result-model design decision and should not be hidden inside the termination overhaul
- consider an optional encoded-problem adapter pattern for reusing an existing problem with different candidate encodings without adding a separate phenotype generic parameter throughout the core algorithm and operator APIs. The adapter should own the candidate search space and decode candidates into the existing problem's searched/domain representation before evaluation. Include a concrete example, preferably grammatical evolution, where an integer-vector candidate is decoded into an expression tree and evaluated by an existing symbolic-regression-style problem.
- decide the broader run/resume/continue naming scheme and decide whether completion results should carry typed stop reasons; this should become its own API naming and lifecycle-result design discussion rather than remaining tied to the termination overhaul branch
- re-discuss algorithm builder APIs before extending them further: decide whether builders should become a polished, first-class configuration style with full feature parity, better type inference, and clear naming, or whether the library should phase them out in favor of object initializers, static factories, and small helper APIs
- introduce a dedicated architecture test suite and evaluate ArchUnitNET as its foundation. Cover assembly dependency directions and cross-cutting authoring API rules such as returning the most concrete accessible execution instance type from protected creation methods. The removed `PipelineAlgorithm_FactoryExposesItsConcretePublicInstanceType` unit test is the first concrete example to restore there as a generalized architecture rule rather than a Pipeline-specific test
- add static factory methods for algorithms where constructors force callers to spell generic arguments that should be inferable from supplied collaborators; meta-algorithms such as `CycleAlgorithm.Create(...)` and `PipelineAlgorithm.Create(...)` are the most visible examples, but the convention should be considered across algorithm APIs
- clean up the test-suite organization and conventions: standardize test naming, file names, folders, and arrange/act/assert structure; use Shouldly consistently across all test projects, including extension tests; delete or restore commented-out tests; rethink `Explicit = true` for long-running tests and decide whether categories, traits, or separate projects should distinguish fast TDD/unit tests from manually triggered regression, smoke, and performance checks; CI should normally run all tests, but may run expensive regression groups only after the fast unit-test group passes; add a coverage-reporting baseline that highlights weak coverage for public APIs and important invariants
- migrate HeuristicLab operators and their unit tests in a structured way; track operator mappings, missing operators, test migration status, and intentional behavior/API differences in the [HeuristicLab operator migration overview](../plans/heuristiclab_operator_migration_overview.md)
- add a Python package bootstrap story for `HEAL.HeuristicLib.PythonInterop`: during early GitHub-based installs, the Python package should generate a tiny temporary `.csproj` that references the matching NuGet package and runs `dotnet publish` into a user cache, so Python users do not need checked-in generated DLLs. Later, replace or complement this with CI-built PyPI wheels that already contain the published .NET payload for normal Python installs.
- add explicit ownership-taking factory methods for candidate containers such as `RealVector`, `IntegerVector`, and similar types, following the explicit static-factory style rather than constructor overloads; the goal is to let callers that already own the backing storage transfer it without another allocation or copy, and random generation should be one of the first places to adopt this once the API shape is decided
- decide whether operators should expose a consistent caller-provided output-buffer or result-memory API. Avoid one-off `Memory<T>` parameters on individual operators. If this becomes necessary for allocation-sensitive workflows, design it as a library-wide operator convention that clearly states who owns the resulting storage and how immutable candidate containers are created from it.
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
