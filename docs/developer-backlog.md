# Developer Backlog

This file is the short living tracker for doc, API-spec, and refactoring follow-up.

It is intentionally narrow: it should track only partial and unfinished follow-up. [Discussed, tried, and rejected](#discussed-tried-and-rejected) is the one exception, and holds settled questions that should not be reopened without new evidence.

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

- restructure the developer guidelines into a coherent, numbered hierarchy that separates document authority, contributor decision-making, architectural rules, contract and validation rules, public API design, implementation conventions, and enforcement. Treat the guide as policy for both human contributors and coding agents: normative decisions must be explicit, easy to locate and reference, and clearly distinguished from rationale and examples so agents can identify, explain, and warn about proposed code or documentation that conflicts with an established guideline. Preserve the guideline as the canonical policy document linked from `AGENTS.md`; perform the initial structural pass without intentionally changing policy, then review duplication and move topic-specific explanations only in a separate editorial pass
- multi-objective short-path usage
- generalize `PipelineAlgorithm` so consecutive algorithms may use different search state types
- add explicit state transformations between `PipelineAlgorithm` stages, including transformations where the input and output state types are the same
- introduce a probability based selection abstraction that separates selection from mappings such as fitness to probability and rank to probability
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
- add a Roslyn analyzer and code fix for missing type inference helpers on public operator configurations. Detect when direct construction requires generic arguments that could be inferred through a static `Create(...)`, `For(problem, ...)` or `For(algorithm, ...)` helper, generate the appropriate companion helper and cover the intended syntax with API usage specs. Start with operators. Reconsider algorithms after their builder and factory design is settled. Do not duplicate the same rule in architecture tests
- consider static factory methods for concrete algorithms where constructors force callers to spell generic arguments that should be inferable from supplied collaborators. Keep this work aligned with the separate decision about builders and object initializer based configuration
- redesign composite search space and composite genotype construction so common composition does not require callers to repeat every candidate and search space type argument. Preserve the fully typed relationship between each candidate part and its search space without relying on untyped component collections
- revisit where scalar problem batch execution should live. `SingleSolutionProblem` currently implements the batch `IProblem` contract by adapting scalar evaluation and therefore exposes its own concurrency setting, but this leaks execution policy into a problem type. Moving it to `ProblemEvaluator` would require either a scalar problem contract, separate evaluator types or a type check with conditional concurrency semantics
- decide where problem-specific operators belong in the source layout. Operators currently fall into three tiers with genuinely different reuse, and all three sit under `Operators/<Role>/` today: general role machinery usable with any candidate and problem (`PipelineRefiner`, `ImprovementCheckingRefiner`, the authoring bases and instrumentation pairs); candidate-specific operators bound to one genotype but any problem over it (`Operators/Mutators/SymbolicExpressionMutators`); and problem-specific operators bound to one problem type (`ConstantOptimizationRefiner`, which requires `SymbolicRegressionProblem` and is the case that raised this). Browsing `Operators/Refiners` therefore mixes universal infrastructure with a leaf only symbolic-regression users can instantiate, and the same will recur for every problem-specific operator. Options include keeping role-first grouping everywhere and accepting domain leaves in general namespaces, colocating problem-specific operators with their problem such as `Problems/DataAnalysis/Regression/Operators`, or separating the tiers by folder while keeping role-first namespaces. Note that colocation would contradict the recorded ownership decision that the operator role remains the primary namespace grouping, so that decision has to be revisited rather than quietly broken. This is the placement taxonomy behind the broader layout item above about organizing operator folders consistently
- reconsider the naming scheme for problem operator base classes. Decide whether common independent authoring bases should use the short main type name, with names such as `PopulationMutator` reserved for less common variants whose semantics genuinely depend on the full population or batch
- clean up the test-suite organization and conventions: standardize test naming, file names, folders, and arrange/act/assert structure; use Shouldly consistently across all test projects, including extension tests; delete or restore commented-out tests; rethink `Explicit = true` for long-running tests and decide whether categories, traits, or separate projects should distinguish fast TDD/unit tests from manually triggered regression, smoke, and performance checks; CI should normally run all tests, but may run expensive regression groups only after the fast unit-test group passes; add a coverage-reporting baseline that highlights weak coverage for public APIs and important invariants
- migrate HeuristicLab operators and their unit tests in a structured way; track operator mappings, missing operators, test migration status, and intentional behavior/API differences in the [HeuristicLab operator migration overview](../plans/heuristiclab_operator_migration_overview.md)
- add a Python package bootstrap story for `HEAL.HeuristicLib.PythonInterop`: during early GitHub-based installs, the Python package should generate a tiny temporary `.csproj` that references the matching NuGet package and runs `dotnet publish` into a user cache, so Python users do not need checked-in generated DLLs. Later, replace or complement this with CI-built PyPI wheels that already contain the published .NET payload for normal Python installs.
- add explicit ownership-taking factory methods for candidate containers such as `RealVector`, `IntegerVector`, and similar types, following the explicit static-factory style rather than constructor overloads; the goal is to let callers that already own the backing storage transfer it without another allocation or copy, and random generation should be one of the first places to adopt this once the API shape is decided
- decide whether operators should expose a consistent caller-provided output-buffer or result-memory API. Avoid one-off `Memory<T>` parameters on individual operators. If this becomes necessary for allocation-sensitive workflows, design it as a library-wide operator convention that clearly states who owns the resulting storage and how immutable candidate containers are created from it.
- benchmark and, if worthwhile, reduce batching overhead in lightweight operators such as `InversionMutator`, including per-call captured delegates and per-item RNG forks. General and variable-width operations should retain deterministic child RNGs per logical item or key. For operators with a proven fixed number of primitive draws, evaluate deterministic fixed-width random-decision planning in logical item order as an allocation-saving specialization. Results must remain independent of workers, partitions, scheduling, concurrency limits, and CPU-core count. Do not require all RNG implementations to become counter-based or random-access-capable solely for this optimization.
- expand `IRandomNumberGenerator` and the concrete random engines so they expose the raw primitive outputs and data widths needed to build statistically sound and efficient higher-level sampling APIs; in particular, bounded integer generation should eventually be reworked on top of integer-domain primitives rather than `NextDouble()` scaling
- decide how cancellation reaches operators. No operator role signature carries a `CancellationToken`, so an operator wrapping a long-running computation cannot observe cancellation and cannot pass one to a component that accepts it. `ConstantOptimizationRefiner` is the first concrete case: `ConstantOptimizer` takes a token and checks it inside the Levenberg-Marquardt solve, but the refiner has none to give and passes `default`, so a solve over a large population cannot be interrupted. Decide whether cancellation belongs in the role signatures, in a run-scoped execution context reaching instances through the execution registry, or on the execution instance itself; the answer interacts with the separate run-level execution-policy item above, since both concern run-scoped state that must reach operators without ambient statics. Consider that most operators are short and would only forward a token they never check
- find a way to reduce the operator boilerplate that a role-contract change has to touch. The problem is unsolved, and both approaches tried so far are recorded in [Discussed, tried, and rejected](#discussed-tried-and-rejected): [typed operator invocation](#typed-operator-invocation) and [generated operator families](#generated-operator-families). Do not re-propose either without the new evidence those entries require.

  The evaluator contract change from `EvaluatedCandidate<TCandidate>` to `ObjectiveVector` (RF-2 of the [symbolic-regression constant-optimization plan](../plans/symbolic-regression-constant-optimization-plan.md)) is the clearest measurement available so far. It changed the single most fundamental property of an operator role — what its execution instance returns — and is worth reading from two directions at once, because the pair is the actual finding.

  **How far it spread.** The change touched 55 files, +255/-393. Split by whether a file has any semantic stake in what an evaluator does:

  | Area | Files | Lines |
  | --- | --- | --- |
  | Evaluator role: contract, arity ladders, topologies | 14 | +68/-174 |
  | Surrounding algorithms, analysis hooks, experimental operators, tests | 38 | +147/-213 |

  About three quarters of the affected files changed only because a signature moved.

  **How little it reached.** Inside the evaluator role itself, roughly 6% of the source changed. The role spans about 1080 lines; the return-type change touched 68 added and 106 removed lines, and about 30 of those were the identical one-line signature edit repeated across `Evaluator`, `StatelessEvaluator`, `StatefulEvaluator`, `SingleCandidateEvaluator`, the wrapping and multi topologies, and the instrumentation pair. Per file it is starker still: `CountingEvaluator` and `DurationMeasuringEvaluator` changed one line each out of 70 and 86, and `RelativeQualityEvaluator` two out of 108.

  Genuine design work amounted to four edits: removing `RepeatingEvaluator.CandidateComparer`, deleting `IteratedEvaluator`, dropping `CachingEvaluator`'s candidate-substitution behavior, and adding the batch pairing helper. Everything else was mechanical.

  Taken together: the change was broad but shallow. The ~94% of role source that survived untouched — constructors, generic constraint lists, arity re-declarations, factory companions, fluent helpers, nested execution-instance classes — is exactly the code that never expressed what an evaluator does, and the 38 outside files are coupled to the role's signature rather than its behavior. Any future proposal should be measured against this shape: it must shrink the mechanical fraction without reintroducing a generic invocation path or a generated public API, and handwritten source must remain fully supported. Record the commit hash here once this work is committed so the measurement can be re-derived.

## Discussed, tried, and rejected

Approaches that were evaluated, in some cases prototyped, and decided against. They are recorded here so that a settled question is not proposed, prototyped and rejected a second time. Each entry states what was tried, what decided it, and what would have to change to reopen it.

Unlike the open items above, these are settled. Rules that follow from them live in [developer guidelines](developer-guidelines.md) and the relevant topic pages; this section holds only the reasoning.

### Typed operator invocation

Prototyped during the operator rework in August 2026 and rolled back.

Cross-cutting concerns such as duration measurement, observation, choosing one child and pipelining would have been centralized behind one typed execution signature, `(TInput, TContext) -> TOutput`, with generic meta-operator bases invoking children through a shared `Invoke`.

It failed because C#'s nominal interface model still requires a role-specific configuration and execution instance for every role. A generic pipeline containing mutators cannot become an `IMutator` merely because its type arguments are mutators, and CRTP preserves a self type but cannot add interface membership. The duration-measuring prototype moved only the small exception-safe timing block into the shared base, while constructors, configuration properties, static factories, fluent extensions, the nested execution instance and role forwarding all remained; the generic base declarations and the context bridge replaced at least as much code as they removed. An isolated pass-through benchmark measured 2.39 ns for the role-specific one-stage pipeline against 12.84 ns for the typed one, both at zero allocation.

Role execution instances therefore expose their named operation — `Mutate`, `Cross`, `Select`, `Evaluate` — and no second generic invocation path. Do not reintroduce a generic `Invoke`, a problem context carrier, default interface bridges between an invocation method and role methods, or generic wrapping and multi bases shared across roles. `IOperator<TExecutionInstance>` is unrelated to this rejection and is retained: it expresses which execution-instance role a configuration creates and carries no input, context or output model.

Reopening this would need a language or runtime mechanism that lets one generic implementation satisfy several nominal role contracts without a per-role leaf. Current C# has none.

### Generated operator families

Decided against on 2026-08-13, after the typed-invocation rework left source generation open as an alternative.

A Roslyn incremental generator, or a deterministic one-shot scaffolding command, would have emitted the repetitive parts of an operator family: configuration and execution-instance arity ladders, stateless and stateful bases, wrapping and multi topologies, construction companions, and one adapter per role for each cross-cutting concern.

It was rejected because new operator roles are expected to be rare and concern adapters are not purely mechanical. Applicability, lifecycle, result shape, naming and construction differ per role, as duration measurement recording failed calls in `finally` and counting recording only successful ones already shows. A production generator would need a stable generator contract, semantic discovery, diagnostics, collision handling, generated-source tests, IDE verification, packaging, external-consumer tests and ongoing compiler compatibility work. Generated public API would also be harder to navigate, refactor and review, a template change could reshape a broad public API with no ordinary source diff at each type, and one generator defect would reproduce across the entire matrix. Coding agents absorb the same repetitive work while the result stays ordinary reviewable source.

Operator families are therefore ordinary checked-in C# source; see [operator authoring](operator-authoring.md#scaffolding-roles-and-cross-cutting-concerns) for the workflow and its guardrails.

Reopening this would need measured evidence that ordinary source has become a material maintenance burden — frequent new roles or concerns, recurring matrix omissions, or repeated synchronization work whose semantics have stabilized into a small declarative model. Any proposal must compare its benefit against the agent-assisted workflow rather than against handwriting everything, and must keep handwritten source a fully supported path. Reopen it through an explicit decision rather than through isolated generator experiments.

## Source material

The active guiding docs remain:

- [design-goals.md](design-goals.md)
- [developer-guidelines.md](developer-guidelines.md)
- [requirements.md](requirements.md)

## Keep or delete?

Keep this file only while it helps maintainers decide what still needs examples, refactoring, or cleanup.

Delete it once most remaining items have either:

- become concrete spec files, or
- moved into normal issue tracking or another active planning system

If it starts reading like a second architecture document again, cut it down.

[Discussed, tried, and rejected](#discussed-tried-and-rejected) outlives the tracker. When this file goes away, move that section somewhere durable rather than deleting it; its whole purpose is to stop settled questions from being reopened by someone who was not there.
