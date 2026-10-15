# Developer backlog

This file is the short living tracker for doc, API-spec, and refactoring follow-up.

It is intentionally narrow: it should track only partial and unfinished follow-up. [Discussed, tried, and rejected](#discussed-tried-and-rejected) is the one exception, and holds settled questions that should not be reopened without new evidence.

## Partly addressed, but still open

These items have meaningful progress behind them, but should still stay on the backlog until the remaining work is done:

- decide whether the current mutable single use `AlgorithmRun` and `ExperimentRun` lifecycle should remain or be replaced by separate immutable setup and running types. Any replacement must preserve the direct fluent API while making repeated execution and analyzer attachment after execution impossible through the available types
- define the intended serialization boundary for experiments. Grid configurators and trial analyzer selectors and factories are runtime setup delegates, while materialized algorithm, operator and analyzer configurations remain serialization friendly in principle. Decide whether experiment configurations themselves need a persistable representation
- extend experiment composition only from concrete user needs. Deferred candidates include per trial problem and initial state creation, heterogeneous algorithm comparison, benchmark experiments across problem instances and dedicated individual cancellation handles
- reconsider typed hierarchical trial paths only if composed tuple keys and `RandomForkPath` metadata prove insufficient. Do not introduce untyped path tags that require casts
- execution-graph invariants within one run
- separate operation concurrency capability from requested execution policy. A future run-level execution context should let users select sequential or concurrent operation execution once, while operators and other execution boundaries declare the concurrency they safely allow. Define how the effective policy is constrained, how it reaches run-scoped instances through the execution registry without ambient state, how stateless operator bases capture run-specific settings, how problem evaluation participates, how per-trial experiment concurrency remains separate and whether maximum concurrency is per batch or shared across a whole run
- identify concrete run-scoped operator controls beyond mutation strength before expanding the existing control-resolution conveniences or introducing a generic parameter framework. Gaussian's `IMutationStrengthControl` and raw-control projection establish configured/current value ownership and preserve adaptation when advice is applied directly to that selected configuration. Configured wrapper chains can still hide the control; the capability-preservation bug below remains open. Any further control needs its own algorithm consumer and authoring example
- role-aware mating and parent selection pressure differences
- decide and document one common rule for record configuration style: primary constructor parameters versus explicit init-only properties, including naming and when each form is allowed
- design sensible creator, crossover and mutator recommendations for the remaining search spaces and for problems with domain specific preferences. `PermutationSearchSpace` and `TravelingSalesmanProblem` prove the recommendation mechanism and are enough for the current ergonomics work. Do not add recommendations mechanically. Each choice needs evidence that it is a useful starting point for that search space or problem, plus documentation that accepting a recommendation affects behavior and reproducibility. Decide at the same time whether reproducibility metadata should record which operators came from recommendations rather than explicit choices, since changing a recommendation changes results for everyone who accepted it
- audit all candidate types used as `TCandidate` and refactor the model so candidates are immutable everywhere
- revisit whether core and experimental unit tests should remain separate projects or be merged into one unit-test package; the current compromise keeps separate test projects for reporting while using stable `HEAL.HeuristicLib.Tests` namespaces so promotion from experimental to core does not require namespace churn
- reconsider `ExperimentalAttribute` once the main package has a meaningful stable API baseline; while HeuristicLib is still early alpha and all public APIs may change, package-level experimental opt-in is likely enough and avoids annotation ceremony that users may simply suppress wholesale
- improve the main `README.md` as the repository and NuGet front door: add install commands, a compact representative code example, a package overview, links to usage guides, and a visible example of what HeuristicLib can do, such as the Python interactive demonstrator GIF
- move to a more standard C# formatting baseline, including a more conventional editor configuration and CI enforcement that fails pull requests on formatting violations. The rules settled while cleaning up the arity branch are: the expression-body arrow ends the previous line; block indentation is always a multiple of four, while column alignment is left alone; a `where` constraint aligns with the base list, both at declaration + 4; a continuation indents four from the line that opened it rather than aligning to a column, and a wrapped construct breaks at its first element; a parameter list breaks after the opening parenthesis or not at all, never between parameters. Two decisions were deliberately deferred and should be taken with this item. First, whether a multi-line parameter list should close with `)` on its own line, which would make the start of a `where` block easier to see, rather than attached to the last parameter as it is now. Second, whether the roughly 990 remaining mid-expression continuation lines that sit at an indent of `4n + 2` should be normalized; the declaration-level cases are already fixed. Note that none of these rules is enforced by anything today: `dotnet format` accepts every variant, and Rider normalizes some of them but also rewraps long signatures in a way this repository does not follow, so reconciling `.editorconfig` with Rider's wrapping settings is part of this item
- actively pay down solution warning debt and define a staged warning policy: fix the existing compiler, analyzer, and test warnings intentionally instead of normalizing them, then ratchet toward warning-clean builds and stricter CI enforcement in steps that avoid drowning active work in noise

Why these are only partial today:

- immutability is now an explicit design rule, but that is stronger as policy than as a completed whole-repo audit

## Still not taken care of

These are still real open items:

- at the release that consolidates Contracts into the main package, manually deprecate the previously published `HEAL.HeuristicLib.Contracts` package on nuget.org and point consumers to `HEAL.HeuristicLib`. The project and package output have been removed from the repository; the published package still needs this release action. Consumers of the former package must reference the main package, and its former public types now have the main assembly's identity
- normalize `src/HeuristicLib/Problems/TestFunctions/ZDT/ZDT1.cs` and `ZDT5.cs` to `Zdt1.cs` and `Zdt5.cs`, matching their type names. Record these case-only renames explicitly in Git on case-insensitive filesystems

- fix rejection of a competing `AlgorithmRun.Stream()` call without failing the active execution. `BeginExecutionSegment` rejects the second call, but `CreateStream` catches that exception and calls `ExecutionFailed`. Add a regression test that retains the first stream, rejects the second, and then completes the original execution with the expected lifecycle. This was identified by code inspection during the layers overhaul and is outside that branch's structural scope
- define and fix `AlgorithmRun` lifecycle and cleanup when the underlying enumerator's `DisposeAsync` throws. On natural completion, `Track` sets `ended` before awaiting disposal, so a disposal exception can leave the run `Running`. Add a throwing-disposal regression test, establish the terminal failure and exception contract, and check explicit terminal cancellation cleanup as part of the same focused fix. This was identified by code inspection; the current characterization tests do not cover it

- fix loss of algorithm-facing execution capabilities through wrappers. This is a known correctness bug, not only an authoring caveat: wrappers retain the ordinary operator role while hiding optional controls, so `EvolutionStrategy` can silently stop adapting Gaussian mutation strength. `PipelineMutator.Create(gaussian)` hides the control directly; `gaussian.Named("exploration")` forwards it until advice on the Gaussian child, including a broad mutator pointcut, supplies a child execution without `IMutationStrengthControl`. A deterministic one-dimensional strategy with initial strength 3 produces candidates `0, -1.5, -2.5` without that child inspection and `0, -1.5, -3` with counting advice. Raw-control projection solves advice applied directly to the selected Gaussian, not arbitrary configured wrapper chains. Design a capability-preservation contract that respects wrapper semantics and keeps operations on the advised execution; no general solution has been selected. Cover configured compositions, transparent naming, child and outer advice, broad pointcuts and contextual rebinding when implementing the fix. Current behavior and the limitation are documented in [Operator composition](../docs/guide/extending/operator-composition.md#known-capability-loss)

- evaluate wider use of tested source imports for documentation examples. Placement is settled: `HeuristicLib.Tests.Docs` owns documentation examples, organized by guide topic, while `HeuristicLib.Tests.ApiUsageSpecs` owns independent public API usage and inference specs. The complete stateful-composition and custom-advice walkthroughs live in the Docs project's `Extending` folder and are included through named VitePress source regions; the solution test run compiles and exercises them without duplicated tests. Other fenced examples still have no automatic connection to compiled code, and previous migrations left broken examples in the guides. Decide which existing guide and README examples should adopt this approach, how partial, illustrative and external-prerequisite examples are identified, and whether extracting code fences into a compile-only project has a useful remaining role. Preserve one source for each published example and its executable validation; copied tests do not validate independently maintained Markdown
- replace the two numeric optimization examples made only of comments in `SymbolicRegressionRedesignSpecs`. The tests currently end in a type existence assertion, so they stay green without compiling or executing the authoring patterns described in their bodies. Turn the intended API shapes into executable usage specs or remove the claims
- introduce solution equivalence as a concept distinct from candidate value equality. Resolve it from the problem first, then the search space and finally `EqualityComparer<TCandidate>.Default`. Prefer behavior members such as `AreEquivalent` and `GetEquivalenceHashCode` over a stored comparer property, because search spaces are records and a comparer instance would affect value equality and serialization. Provide an `IEqualityComparer<TCandidate>` adapter for set and dictionary consumers. Audit `RemoveDuplicatesInterceptor`, Pareto front extraction, diversity and genealogy analysis and future evaluation caches before landing the contract. An explicitly supplied comparer remains the highest priority override
- reduce the number of equivalent public API entry points. Several subsystems currently expose the same outcome through constructors, static helpers, extension methods and fluent convenience methods. Analysis is one example: users can construct analyzers directly or reach equivalent analyzers through several helper layers, making it unclear which path is canonical. Algorithm construction, run creation, execution, completion and streaming similarly offer overlapping routes that need a deliberate review. For each workflow, choose one clear canonical entry point and retain only a small convenience layer where it removes meaningful type-inference or setup friction. Remove, internalize or stop promoting alternatives that merely provide another spelling for the same operation. Judge the resulting API by whether a new user can identify the intended path without first comparing several equivalent choices.
- define how genealogy represents repeated candidate values with different derivations in one generation. `GenealogyGraph.AddConnection` currently throws on the second value unless it equals one of its parents. A three-generation NSGA-II run with real-vector alpha/beta crossover, population 10, and seed 42 exposed this while validating explicit analysis ordering. It predates the trace changes and requires a decision about occurrence identity, not merely replacing dictionary insertion.
- decide whether interceptor traces should infer their search state. `IInterceptor<TCandidate>` carries none, so `Analyzer.Trace(interceptor, …)` with a lambda and `Measurement.Candidates(interceptor)` need it named. An extension on the `Interceptor<…>` authoring base already infers it for `TracePopulationQuality` and `TracePopulationCandidates`; the same could cover the `Analyzer.Trace` overloads if a use appears

- multi-objective short-path usage
- generalize `PipelineAlgorithm` so consecutive algorithms may use different search state types
- add explicit state transformations between `PipelineAlgorithm` stages, including transformations where the input and output state types are the same
- introduce a probability based selection abstraction that separates selection from mappings such as fitness to probability and rank to probability
- rework the objective system around the glossary terms `objective value`, `objective vector`, `objective direction`, and `objective directions`: keep the conceptual model open enough for single-objective and multi-objective cases, and clarify when an algorithm/operator requires exactly one objective value, a total comparer over objective vectors, or multi-objective comparison semantics. Pareto front and hypervolume analysis stay Experimental until this settles dominance, objective directions and total comparers
- complete the measurement and aggregation catalog. Shipped today: measurements for candidates, objective vectors and crossover offspring; the aggregations `Best`, `BestSoFar`, `BestMedianWorst`, `MinMeanMax`, `Count`, `Single` and `Readings` with their candidate-carrying variants; and experimental hypervolume and pairwise population similarity. Still intended: objective-component and feasibility measurements; encoding-specific measurements beside their encodings, such as expression tree length, depth, variable set and symbol set, permutation edges and positions, and real-vector per-dimension values; standard deviation, count-matching, histogram, frequency-map, distinct-count and non-dominated-set aggregations; running worst and cumulative sum and count across observations; and a transposed view that reads a keyed series such as a frequency map as one line per key without a second storage shape. Decide at the same time whether generic numeric measurements infer cleanly or the catalog standardizes on `double`. Naming and package placement follow the [analyzer architecture](../docs/contributing/architecture/analyzers.md) page
- measure what observation costs before it gains more roles or advice kinds. Observation records are classes allocated once per observed operator call, and each `Observe` adds its own wrapper, so several analyzers at one boundary form a chain. Benchmark class records against readonly structs, including copy and stack costs, and whether merging the observers at one boundary into one wrapper pays. An unobserved operator already pays nothing, and that must stay true
- revisit resolver/advice performance if a concrete workload makes setup, deferred activation or operation-advice overhead worth investigating. Comparative timing/allocation measurements remain deferred and are not a completion gate for the resolver/aspect rework. No comparative cost claim has been established for the current resolver. Benchmark infrastructure follows the accepted [performance regression benchmarking plan](performance-regression-benchmarking.md)
- design any additional matching conveniences and advice-kind helpers as separate review packages. Reference/type matching, runtime predicate matching, And/Or/Not composition, custom pointcuts, exact named-node matching and role-preserving named wrappers are described in [execution resolution](../docs/contributing/architecture/execution-resolution.md#pointcuts-advice-and-join-points). The single `Advice` concept provides around behavior, including algorithm `RunStreamingAsync` wrapping and the ability to change execution logic. Separate `Before`, `AfterReturning`, `AfterThrowing` and `After` APIs, built-in retry helpers, dedicated algorithm lifecycle boundaries, attribute-specific conveniences, wildcards and caller-path selection remain deferred. Their presence here does not authorize implementation.

  Retain the configured `Interceptor` for now and defer its replacement by around-iteration advice. Future exploration must preserve serializable configuration behavior and the internal `step -> Transform -> IsTerminalState -> yield` boundary, with the transformed state feeding the next step. A wrapper around the output stream can replace a state seen by its consumer but cannot change the inner iterator's retained next-step state or undo its termination decision. Equivalent advice needs an explicit typed boundary inside the iteration loop that consumer-defined algorithms can implement. Compare a deduplication example and a stateful transformation shared by two algorithms, preserving next-step input, termination, RNG, pausing, ordering and shared state. One possible direction is a persistable behavior description prepared into runtime advice, rather than making callback-bearing runtime aspects configuration values.

  Broader advice-kind APIs need explicit failure and continuation semantics: what happens when advice throws, whether proceeding zero or several times is supported, and what finally/throwing callbacks observe under suppression, replacement or retry. Expanded algorithm lifecycle advice must distinguish stream creation, enumeration start, each yield, normal completion, exception/cancellation and early disposal. Pausing retains the iterator and is distinct from disposal. These are future API design questions; the existing typed wrappers do not establish a separate lifecycle callback contract.

  Caller-path selection is also deferred. It needs an explicit model of caller edges or execution occurrences, including shared configurations and deferred child construction. An acceptance case is a genetic algorithm and a hill climber sharing one stateful mutator configuration and prepared state: advice selecting only hill-climber calls must preserve the shared mutation counter and RNG behavior while excluding genetic-algorithm calls. Configuration selection alone cannot identify that caller edge. Names and path syntax are possible interfaces, not selected designs. Simple name/property pointcuts do not require that feature.

  An optional future extension could persist an algorithm together with configured aspect descriptions. Compare an outer configuration with an algorithm wrapper only if that capability is requested. General aspects may change behavior; algorithms/operators may install them internally; inspection and analysis preserve the observed computation. No persistable wrapper or conversion of runtime aspects/advice/pointcuts to records has been selected.

- design provenance. Genealogy reconstructs candidate identity by value through an equality comparer, because no identity survives an operator boundary. Candidate identity across operators, a first-class lineage model and operator attribution, including making the dispatch decision inside `ChooseOne*` operators visible so operator success rates become measurable, were deferred from the analysis rework as their own work. Relates to the solution-equivalence and repeated-genealogy-values items above
- settle `PopulationSimilarity` before promoting it. It has no behavior tests, excludes the diagonal without saying so, and reports a minimum of 1, a maximum of 0 and a mean of NaN for a single-member population
- decide whether epoch work needs exact recording. `EpochWorkTrace.PerEpoch` derives staleness from an evaluation clock and the epoch clock, which does not describe a caching evaluator. A counter on the problem would be exact, but it puts into the problem's state a metric that only the coarser update policies and parallel evaluation produce
- two analysis footguns are documented rather than guarded: an aggregation that mutates and returns one object can still be put in an ordinary trace, which then stores that one object many times, and a retention object shared by two traces couples their counting. Add checks when either bites

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
- add cross-cutting authoring API rules to the architecture tests in `HeuristicLib.Tests/Architecture`, such as returning the most concrete accessible execution node type from protected creation methods. The removed `PipelineAlgorithm_FactoryExposesItsConcretePublicInstanceType` unit test is the first concrete example to restore there as a generalized architecture rule rather than a Pipeline-specific test. These rules concern the compiled public API, so evaluate ArchUnitNET against plain reflection, which `RoleContractArityTests` and `AssemblyDependencyTests` already use, when the first one is restored. Layer dependency and ownership checks stay on Roslyn: ownership depends on source folders that compiled IL does not record, and the check compiles Experimental from source without a project reference from the core tests
- add a Roslyn analyzer and code fix for missing type inference helpers on public operator configurations. Detect when direct construction requires generic arguments that could be inferred through a static `Create(...)`, `For(problem, ...)` or `For(algorithm, ...)` helper, generate the appropriate companion helper and cover the intended syntax with API usage specs. Limit this analyzer to operators because the standard algorithms already provide their construction helpers. Do not duplicate the same rule in architecture tests
- redesign composite search space and composite genotype construction so common composition does not require callers to repeat every candidate and search space type argument. Preserve the fully typed relationship between each candidate part and its search space without relying on untyped component collections
- revisit where scalar problem batch execution should live. `SingleSolutionProblem` currently implements the batch `IProblem` contract by adapting scalar evaluation and therefore exposes its own concurrency setting, but this leaks execution policy into a problem type. Moving it to `ProblemEvaluator` would require either a scalar problem contract, separate evaluator types or a type check with conditional concurrency semantics
- reconsider the naming scheme for problem operator base classes. Decide whether common independent authoring bases should use the short main type name, with names such as `PopulationMutator` reserved for less common variants whose semantics genuinely depend on the full population or batch
- clean up the test-suite organization and conventions: standardize test naming, file names, folders, and arrange/act/assert structure; use Shouldly consistently across all test projects, including extension tests; delete or restore commented-out tests; rethink `Explicit = true` for long-running tests and decide whether categories, traits, or separate projects should distinguish fast TDD/unit tests from manually triggered regression, smoke, and performance checks; CI should normally run all tests, but may run expensive regression groups only after the fast unit-test group passes; add a coverage-reporting baseline that highlights weak coverage for public APIs and important invariants
- migrate HeuristicLab operators and their unit tests in a structured way; track operator mappings, missing operators, test migration status, and intentional behavior/API differences in the [HeuristicLab operator migration overview](heuristiclab_operator_migration_overview.md)
- add a Python package bootstrap story for `HEAL.HeuristicLib.PythonInterop`: during early GitHub-based installs, the Python package should generate a tiny temporary `.csproj` that references the matching NuGet package and runs `dotnet publish` into a user cache, so Python users do not need checked-in generated DLLs. Later, replace or complement this with CI-built PyPI wheels that already contain the published .NET payload for normal Python installs.
- split optional third-party dependencies out of the main package only when a consumer needs a lighter one. Known obstacles: CsvHelper is used only by `DataFrame.Csv.cs`, a part of the partial `DataFrame`, so a CSV package first needs a reader API outside that type; Parlot is imported project-wide through `global using static Parlot.Fluent.Parsers` for `InfixExpressionParser`; symbolic-regression fitting calls the internal `NumericParameterFitter` and `LevenbergMarquardt`, so a machine-learning split must decide their accessibility instead of making them public to compile; and MathNet is also used by `HyperVolumeCalculator` in `Objectives`, so moving machine learning alone would not remove it from the main package
- add explicit ownership-taking factory methods for candidate containers such as `RealVector`, `IntegerVector`, and similar types, following the explicit static-factory style rather than constructor overloads; the goal is to let callers that already own the backing storage transfer it without another allocation or copy, and random generation should be one of the first places to adopt this once the API shape is decided
- decide whether operators should expose a consistent caller-provided output-buffer or result-memory API. Avoid one-off `Memory<T>` parameters on individual operators. If this becomes necessary for allocation-sensitive workflows, design it as a library-wide operator convention that clearly states who owns the resulting storage and how immutable candidate containers are created from it.
- benchmark and, if worthwhile, reduce batching overhead in lightweight operators such as `InversionMutator`, including per-call captured delegates and per-item RNG forks. General and variable-width operations should retain deterministic child RNGs per logical item or key. For operators with a proven fixed number of primitive draws, evaluate deterministic fixed-width random-decision planning in logical item order as an allocation-saving specialization. Results must remain independent of workers, partitions, scheduling, concurrency limits, and CPU-core count. Do not require all RNG implementations to become counter-based or random-access-capable solely for this optimization.
- expand `IRandomNumberGenerator` and the concrete random engines so they expose the raw primitive outputs and data widths needed to build statistically sound and efficient higher-level sampling APIs; in particular, bounded integer generation should eventually be reworked on top of integer-domain primitives rather than `NextDouble()` scaling
- decide whether execution needs a problem dependent validation phase after the execution graph is resolved and before algorithm work begins. `NumericParameterFittingRefiner` is the first concrete case: enabling numeric parameter fitting while the symbolic expression search space allows operations the differentiable compiler cannot lower should fail before candidate creation, random draws or evaluation. A future design must find the right validation owner, reach nested operators and delayed child execution graphs, preserve configuration and execution node ownership and avoid adding a broad operator lifecycle hook solely for this one case. Until then the refiner reports the incompatibility when it encounters an affected candidate
- decide how cancellation reaches operators. No operator role signature carries a `CancellationToken`, so an operator wrapping a long-running computation cannot observe cancellation and cannot pass one to a component that accepts it. `NumericParameterFittingRefiner` is the first concrete case: `NumericParameterFitter` takes a token and checks it inside the Levenberg-Marquardt solve, but the refiner has none to give and passes `default`, so a solve over a large population cannot be interrupted. Decide whether cancellation belongs in the role signatures, in a run-scoped execution context reaching instances through the execution registry, or on the execution node itself; the answer interacts with the separate run-level execution-policy item above, since both concern run-scoped state that must reach operators without ambient statics. Consider that most operators are short and would only forward a token they never check
- find a way to reduce the operator boilerplate that a role-contract change has to touch. The problem is unsolved, and both approaches tried so far are recorded in [Discussed, tried, and rejected](#discussed-tried-and-rejected): [typed operator invocation](#typed-operator-invocation) and [generated operator families](#generated-operator-families). Do not re-propose either without the new evidence those entries require.

  The evaluator contract change from `EvaluatedCandidate<TCandidate>` to `ObjectiveVector` (RF-2 of the symbolic-regression parameter-fitting work; see [symbolic-regression decisions](symbolic-regression-decisions.md)) is the clearest measurement available so far. It changed the single most fundamental property of an operator role — what its execution node returns — and is worth reading from two directions at once, because the pair is the actual finding.

  **How far it spread.** The change touched 55 files, +255/-393. Split by whether a file has any semantic stake in what an evaluator does:

  | Area                                                                  | Files | Lines     |
  | --------------------------------------------------------------------- | ----- | --------- |
  | Evaluator role: contract, arity ladders, topologies                   | 14    | +68/-174  |
  | Surrounding algorithms, analysis hooks, experimental operators, tests | 38    | +147/-213 |

  About three quarters of the affected files changed only because a signature moved.

  **How little it reached.** Inside the evaluator role itself, roughly 6% of the source changed. The role spans about 1080 lines; the return-type change touched 68 added and 106 removed lines, and about 30 of those were the identical one-line signature edit repeated across `Evaluator`, `StatelessEvaluator`, `StatefulEvaluator`, `SingleCandidateEvaluator`, the wrapping and multi topologies, and the instrumentation pair. Per file it is starker still: `CountingEvaluator` and `DurationMeasuringEvaluator` changed one line each out of 70 and 86, and `RelativeQualityEvaluator` two out of 108.

  Genuine design work amounted to four edits: removing `RepeatingEvaluator.CandidateComparer`, deleting `IteratedEvaluator`, dropping `CachingEvaluator`'s candidate-substitution behavior, and adding the batch pairing helper. Everything else was mechanical.

  Taken together: the change was broad but shallow. The ~94% of role source that survived untouched — constructors, generic constraint lists, arity re-declarations, factory companions, fluent helpers, nested execution-node classes — is exactly the code that never expressed what an evaluator does, and the 38 outside files are coupled to the role's signature rather than its behavior. Any future proposal should be measured against this shape: it must shrink the mechanical fraction without reintroducing a generic invocation path or a generated public API, and handwritten source must remain fully supported. Record the commit hash here once this work is committed so the measurement can be re-derived.

## Discussed, tried, and rejected

### Binding as the public runtime type family

The execution-factory naming discussion on 2026-09-27 found `IExecutionBinding` accurate for the runtime object's responsibility, but the user rejected binding as the public type vocabulary because the word is overloaded. The selected family is `IConfigurationNode` / `IExecutionNode`, with role-specific `...Execution` types and unchanged domain names for configurations. Binding can remain descriptive terminology inside the resolver design; it does not introduce a parallel public contract. The [glossary](../docs/guide/glossary.md#execution-node) defines the current terminology.

### Prohibiting all child-scope module installation

The DI/AOP scope discussion considered requiring every module to be declared at a common ancestor and allowing child scopes only to inherit modules (option B). This was not implemented and is not the selected direction: it removes useful child-local observations, including an inner observer that receives only the inner calls while an outer observer receives all calls. Restricting modules alone also leaves the same binding problem for configuration-origin budgets.

Typed execution factories support child-local aspects while preserving shared logical state. Dependencies bind in their factory owner's state with the requesting aspects, so adding an inner observer does not reset state or change existing parent bindings. These are implemented contracts described in [execution resolution](../docs/contributing/architecture/execution-resolution.md#state-and-construction).

### Retaining fixed composite bindings with rejection (option A)

Selected on 2026-09-26, then superseded on 2026-09-27 after comparing shared-state binding designs. Option A would retain an ancestor composite's existing children and reject reuse when a requesting scope required observations those children would bypass. Its comprehensive detection policy for opaque/deferred dependencies had not been implemented or settled.

Typed factories support new scope-specific child bindings while preserving logical execution state, as described in [execution resolution](../docs/contributing/architecture/execution-resolution.md#state-and-construction). Do not restore the superseded guard or silently omit required observations when reuse is difficult. Reopen only if a concrete requirement cannot be met by contextual binding over shared state.

### Implicit source resolution for generated wrappers

Production behavior at `c6957cbc`, rejected on 2026-10-13. A generated wrapper obtained the execution it enclosed by resolving its original source configuration, which the resolver temporarily redirected to the already-built inner chain. Internal compositions reused the same redirection.

It hides the wrapper's input behind an ordinary-looking `Resolve` call, so resolving the source means different things inside and outside a wrapper. The resolver needs interception state in every construction frame and rules for when the redirection must not leak into extra children. An explicit typed `inner` gives the same composition without either. Reopen only if explicit input cannot express a wrapper whose source it must locate itself.

### Supplying the inner execution through every factory

Proposed and rejected on 2026-10-13. Every factory would receive an optional supplied execution with the configuration it stands for. Wrapping and multi bases would take it for matching child references, and the resolver would check that it was taken.

It treated the symptom of building advice through ordinary child resolution rather than the cause. Wrappers already prepare state once and wrap a supplied child execution, so advice applies that hook directly to the built execution. Ordinary factories stay unchanged, and the check, the extra parameter and the private advice states disappear. Reopen only if advice must resolve children of its own.

### Separate ordinary and supplied construction callbacks

Tried in the redesign's disposable authoring fixture and superseded on 2026-10-13. A factory received an `ordinary` constructor for configured use, a `supplied` constructor for an already-built `inner`, and an `acceptsSource` predicate deciding which source it could receive.

The two constructors duplicated the same node construction, and the predicate restated what the factory's own child references already determine. One construction callback that receives the supplied execution as an explicit argument covers both routes. Reopen only for a factory whose ordinary and supplied construction genuinely differ beyond where the child comes from.

### Separate public advice declarations and aspects

Extracted during the resolver redesign and superseded on 2026-10-14. `AdviceDeclaration` represented one pointcut/wrapper-recipe rule, while `Aspect` grouped declarations and installed other aspects. The separate public authoring concept made users distinguish an aspect from the rule it declares.

Each rule needs independent identity for preparation caching, but the advice instance provides that identity. An aspect stores its pointcut/advice pairs as tuples, so no public declaration type is needed. The standalone declaration type was removed rather than hidden or renamed. Reopen only if a concrete rule responsibility cannot be expressed by a pointcut/advice pair.

### Separate direct-advice and installed-aspect collections

Used during the resolver redesign and superseded on 2026-10-14. `Advise` added rules to one list, while `Install` added existing aspects to another. Collection applied direct rules before installed aspects regardless of call order, including across scope roots. This preserved an origin-priority policy at the cost of two authoring paths, two snapshots and separate collection passes.

The user selected one ordered list. Attach order is application order, and first occurrence determines a shared advice's position. There is no direct-versus-installed priority. Reopen only if a concrete behavior requires origin priority that cannot be expressed by explicit attach order. See [ordering and shared advice](../docs/contributing/architecture/execution-resolution.md#ordering-and-shared-advice).

### Composite aspects

Implemented as rule-or-composition `Aspect` with nested installation, and proposed as `PipelineAspect` with `Aspect.Combine`; rejected on 2026-10-14. Aspect hierarchies needed lazy declaration, recursive flattening, cycle handling and a lock in every aspect.

A resolution scope holds one flat list of pointcut/advice pairs instead. Combining aspects concatenates their lists, and shared clocks apply once because their advice instances are removed as duplicates by reference. Reopen only if an ordering or sharing requirement cannot be expressed by a flat list.

### Advice expressed as wrapper configurations

Implemented during the resolver redesign and rejected on 2026-10-14. A recipe mapped each selected configuration to a wrapper configuration whose child was never used; the resolver borrowed the wrapper's construction hook through a role-specific type test and public static bridges.

It mixed the configuration graph with woven behavior and hid the advice behind a configuration. Advice now wraps executions directly through one advice base per role. Reopen only if advice must itself appear in the configuration graph.

### Static advice hooks on execution roles and factory objects

Assessed during the aspect redesign. Moving advice preparation to static hooks on execution-role interfaces failed with `CS8705` for an execution implementing two roles. Replacing delegate factories with factory objects would change the common factory contract and add allocations.

The role's resolution extension already knows the run's types and supplies the typed advice-preparation callback. This keeps ordinary delegate factories and supports consumer-defined roles without another factory abstraction. Reopen only for a concrete role requirement the callback cannot express; any alternative must support an execution implementing several roles. See [pointcuts, advice and join points](../docs/contributing/architecture/execution-resolution.md#pointcuts-advice-and-join-points).

### Advice-based configured wrappers

Proposed during the aspect redesign and rejected on 2026-10-14. A generic configured wrapper per role, such as `AdvisedMutator(child, advice)`, would let one behavior serve both as a configured operator and as advice.

Instead, instrumentation such as counting, timing and observing is advice only, and behavior-changing wrappers such as caching and limits are operators only, so no behavior needs both forms. Reopen only if a behavior is needed both in configurations and as woven advice.

### Resolver-owned keyed child retention

Production behavior at `c6957cbc`, rejected on 2026-10-13. `GetOrCreateChildScope(key, declare)` kept a child's state and its once-only declarations under a reference key on the owner, and rebuilt a scope around them for each requesting context. Cycle's reuse mode and both operator budgets used it.

It coupled state retention with declarations and made the resolver own callers' retention keys. `With(aspect)` adds advice without new state, so budgets need no retention slot. Cycle prepares a `ChildScopeSelector` that owns its activation policy: `Fresh` creates child-local state per activation, `Retained` keeps child-local state by child configuration reference within that prepared Cycle, and `Shared` uses the parent's state. Separate Cycle preparations have separate retained slots, while all policies can reuse factories already owned by ancestors. Retained states keep no requesting aspects; each activation supplies its current aspects. Reopen only if caller-owned state cannot express a concrete retention requirement. See [child activation policies](../docs/contributing/architecture/execution-resolution.md#child-activation-policies).

### Generated wrappers with children of their own

Production behavior at `c6957cbc`, rejected on 2026-10-13. A generated wrapper could resolve additional children, with a private selection scope and a rule that its own registration skipped them while other registrations still saw them.

No production recipe used it. Advice now wraps exactly its target, so it never resolves anything, and the private scopes and self-exclusion rule disappear. Real work belongs in the configuration, where node selectors and budgets can see it. Reopen only for advice that must resolve another configured operator.

### Per-context node caches in the resolver

Production behavior at `c6957cbc`, rejected on 2026-10-13. Each observation context cached its completed nodes and construction faults, children without new declarations reused ancestor nodes, and any added declaration forced the affected nodes to be rebuilt.

Resolution runs once per run, or once per activation in a meta-algorithm, while operator execution dominates runtime. Persistent state lives with the configuration's factory, so the cache only preserved node identity and fault timing, at the cost of fault records, unchanged-parent lookup and the rebuild rule. Reopen only if measurements show repeated resolution as a material cost; even then, add a cache that does not change behavior.

### Shared configurable run host

Tried and removed during the layers overhaul in September 2026. `RunHost` wrapped a lifecycle enum and lock, accepted a transition table and exception-message prefix, and exposed its lock back to the runs. The callers still made the actual lifecycle decisions. The extraction introduced a generic state-machine mechanism without taking ownership of execution or scheduling.

Algorithm runs retain resumable execution; experiment runs coordinate independent trials and stop permanently on early disposal. Their lifecycle rules and diagnostics stay with the corresponding run. Shared state names and similar guarded assignments do not justify a common host. Keep algorithm lifecycle and enumerator ownership together; composition and continuation wrappers are not required merely to divide the class. Revisit a shared host only if there is a concrete shared execution responsibility it can fully own.

Approaches that were evaluated, in some cases prototyped, and decided against. They are recorded here so that a settled question is not proposed, prototyped and rejected a second time. Each entry states what was tried, what decided it, and what would have to change to reopen it.

Unlike the open items above, these are settled. Rules that follow from them live in [developer guidelines](../docs/contributing/developer-guidelines.md) and the relevant topic pages; this section holds only the reasoning.

### Renaming Interceptor only to make room for AOP

Discussed and rejected during the DI/AOP design review in September 2026. A rename to `StateTransformer` was proposed to avoid using `Interceptor` for both the existing state-transforming operator and future AOP behavior. The AOP design can use advice terminology, so the assumed naming collision does not require a rename.

Neither `Interceptor` nor `StateTransformer` communicates the precise invocation boundary: after a successful iteration step, before termination evaluation and publication, with the transformed state feeding the next iteration. Renaming would therefore leave the stated clarity problem unresolved while changing the whole role family and its callers. Retain `Interceptor`. Reconsider its name only if a concrete responsibility or unavoidable ambiguity changes; adopting AOP alone is insufficient. Whether typed algorithm advice should replace the role remains a separate [open advice follow-up](#still-not-taken-care-of).

### Internally derived configurations as selectable sources

Discussed on 2026-10-09 and 2026-10-10 after selector registration (4c.2) exposed that `GeneticAlgorithm`, `NSGA2` and ALPS resolve `Mutator.AppliedAtRate(rate)` as an ordinary source, so broad mutator selectors also matched the derived `ChooseOneMutator` and `NoChangeMutator.Instance`. What was measured then depended on how an algorithm implements its rate rather than on what the user configured.

On 2026-10-13 algorithm-owned logic was selected: an algorithm resolves only its configured parameters and applies behavior derived from a parameter, such as a mutation rate, in its own execution code. This rule is documented in [developer guidelines § 4.3](../docs/contributing/developer-guidelines.md). GA, NSGA2 and ALPS route offspring with the same `WeightedRouter` that `ChooseOneMutator` uses, so the rate logic exists once. Rejected alternatives:

- **Internal compositions**, implemented in `c6957cbc`. The algorithm derived `Mutator.AppliedAtRate(MutationRate)` and the resolver built it around the configured mutator, hidden from selectors, in an isolated scope with an internal-source rule and a per-owner table. Each redesign attempt needed another resolver mechanism for it: source interception, a supplied inner execution or owner-applied advice. Algorithm-owned logic needs none.
- **Keep derived configurations as visible sources.** Selection and measurement would follow algorithm internals that the user cannot see on the configuration and that may change without notice.
- **Execution-level composition helpers**, such as `AppliedAtRate` on a resolved mutator execution. Every configuration-level helper would need an execution-level twin for every role.
- **Removing `MutationRate` from the algorithms**, leaving explicit `Mutator = m.AppliedAtRate(r)`. Users expect a mutation-rate parameter on a genetic algorithm, and a builder parameter without a matching algorithm property is inconsistent. Parameters are kept as given and interpreted by the algorithm, as in scikit-learn.
- **A transparent resolve that only hides a derived configuration**, without isolation or the plumbing rule. Hidden nodes could share state with the user's graph or do unmeasured work.

The list-level approach was first rejected as duplicating the rate logic on a third representation. A shared router removes that duplication, and its cost is a few lines per algorithm instead of a resolver feature. An algorithm that resolves a configuration it builds itself only makes that configuration visible to node selectors; the developer guidelines forbid it, and no tooling enforces it. Reopen only if several algorithms need the same derived behavior and the shared router cannot express it.

### Separate node-selector filtering through Where

The first node-selector draft used `Where(predicate)`. During the September 2026 composition review it was replaced by `And(predicate)`: a selector builds a definition rather than returning an enumerable, and a predicate condition is already intersection with a predicate selector. The overload constructs that selector and delegates to the same `And(selector)` implementation. Keeping both spellings would add a second public name for the same operation. Reconsider only if predicate filtering acquires a responsibility different from Boolean intersection; LINQ familiarity alone does not require another spelling.

### Typed operator invocation

Prototyped during the operator rework in August 2026 and rolled back.

Cross-cutting concerns such as duration measurement, observation, choosing one child and pipelining would have been centralized behind one typed execution signature, `(TInput, TContext) -> TOutput`, with generic meta-operator bases invoking children through a shared `Invoke`.

It failed because C#'s nominal interface model still requires a role-specific configuration and execution node for every role. A generic pipeline containing mutators cannot become an `IMutator` merely because its type arguments are mutators, and CRTP preserves a self type but cannot add interface membership. The duration-measuring prototype moved only the small exception-safe timing block into the shared base, while constructors, configuration properties, static factories, fluent extensions, the nested execution node and role forwarding all remained; the generic base declarations and the context bridge replaced at least as much code as they removed. An isolated pass-through benchmark measured 2.39 ns for the role-specific one-stage pipeline against 12.84 ns for the typed one, both at zero allocation.

Role execution nodes therefore expose their named operation — `Mutate`, `Cross`, `Select`, `Evaluate` — and no second generic invocation path. Do not reintroduce a generic `Invoke`, a problem context carrier, default interface bridges between an invocation method and role methods, or generic wrapping and multi bases shared across roles. `IOperator<TExecution>` is unrelated to this rejection and is retained: it expresses which execution-node role a configuration creates and carries no input, context or output model. Advice plumbing is also outside it: a wrapper that applies advice, observing or otherwise, may erase callback types internally, because it wraps role execution nodes rather than replacing their contracts.

Reopening this would need a language or runtime mechanism that lets one generic implementation satisfy several nominal role contracts without a per-role leaf. Current C# has none.

### Algorithm construction through builders, problem witnesses and value-dependent recommendations

Settled during the API ergonomics work in August 2026, after each variant was prototyped against the real library.

`GeneticAlgorithmBuilder` and its siblings were phased out rather than polished into a first-class API. The record plus a static companion covers every case the builders did: `Create(...)` where the caller supplies the required operators, `For(anchor, ...)` where a problem or search space supplies the candidate type and recommends missing required operators, and `with` for incremental changes. Algorithm defaults still supply optional settings in both factory forms. What the builders offered that nothing replaces is mutable statement-by-statement configuration spread across a method body; porting the call sites made them shorter, not longer.

Passing the problem to `Create(...)` purely as a type witness was measured and dropped. The required operators already infer `TCandidate`, so `Create(...)` returns `GeneticAlgorithm<TCandidate>` without a problem argument. Use `For(problem, ...)` when the factory should inspect problem and search space recommendations. A problem parameter on `Create(...)` would otherwise be inert.

Value-dependent recommendations remain a capability but are not current built in behavior. `TryCreateRecommendedOperator` is an instance method and can inspect current values. Returning `false` with `null` declines for the current state and lets the next source participate. A rule such as "length under twenty picks a swap mutator, otherwise an inversion mutator" would still publish an arbitrary threshold as public API and needs evidence before adoption.

Reopening any of these needs a concrete case the current shape cannot express: a configuration that genuinely requires mutation across many statements, an inference failure that only an extra witness parameter fixes, or a value-dependent recommendation whose threshold is derived rather than chosen.

### Generated operator families

Decided against on 2026-08-13, after the typed-invocation rework left source generation open as an alternative.

A Roslyn incremental generator, or a deterministic one-shot scaffolding command, would have emitted the repetitive parts of an operator family: configuration and execution-node arity ladders, stateless and stateful bases, wrapping and multi topologies, construction companions, and one adapter per role for each cross-cutting concern.

It was rejected because new operator roles are expected to be rare and concern adapters are not purely mechanical. Applicability, lifecycle, result shape, naming and construction differ per role, as duration measurement recording failed calls in `finally` and counting recording only successful ones already shows. A production generator would need a stable generator contract, semantic discovery, diagnostics, collision handling, generated-source tests, IDE verification, packaging, external-consumer tests and ongoing compiler compatibility work. Generated public API would also be harder to navigate, refactor and review, a template change could reshape a broad public API with no ordinary source diff at each type, and one generator defect would reproduce across the entire matrix. Coding agents absorb the same repetitive work while the result stays ordinary reviewable source.

Operator families are therefore ordinary checked-in C# source; see [operator implementation](../docs/contributing/architecture/operator-implementation.md#scaffolding-roles-and-cross-cutting-concerns) for the workflow and its guardrails.

Reopening this would need measured evidence that ordinary source has become a material maintenance burden — frequent new roles or concerns, recurring matrix omissions, or repeated synchronization work whose semantics have stabilized into a small declarative model. Any proposal must compare its benefit against the agent-assisted workflow rather than against handwriting everything, and must keep handwritten source a fully supported path. Reopen it through an explicit decision rather than through isolated generator experiments.

### Parallel observation systems

Built during the analysis rework in September 2026 and removed again within it.

Two mechanisms once gave analysis a system of its own beside the execution model. A parallel observer API, `ObserveWith` with observable configurations and public `I...Observer` interfaces, offered a second way to react to the same boundary. A public supply hierarchy, `ICandidateReadings`, `IObjectiveReadings`, `IEvaluatedPopulation` and `IDerivation`, let contravariance pick valid pairs of measurement and boundary; it named type-system machinery as if it were domain language and hid which observation a measurement actually read.

Both were removed. Analysis uses the execution model's own mechanism, decorations installed into the run's resolution scope, and measurements read the typed observation of the role they observe. Do not reintroduce an observation system that runs beside execution modules, or a capability hierarchy whose only purpose is to make overload resolution select valid combinations.

### Trace storage structures

Measured during the analysis rework in September 2026, at one million samples unless stated.

`List<T>.Add` costs 1.9 ns, or 17.3 ns under a lock, which is 17 ms per million appends. Copying to publish costs 1.4 ms for a thousand samples polled a thousand times and 111 ms for a hundred thousand samples polled a thousand times. `ImmutableList<T>` gives free snapshots but needed 467 ms and 1241 MB to append a million samples, because it allocates rebalancing nodes on the operation a trace performs most. A lock-free append-only log appended the same in 5.5 ms and 23 MB and published in constant time, but its prototype had a bug in its barrier-sensitive code.

Traces keep a list under a lock and copy it when a snapshot is taken. Reopen only when profiling shows snapshot copying to be a real cost, and then compare the append-only log, not `ImmutableList<T>`.

### Detecting observation sources that never resolve

Tried during the analysis rework in September 2026 and removed.

An observation source selected by reference is not matched when a configuration is copied with `with` after an analyzer was attached, so its trace stays empty without a word. Failing at run creation cannot work: aspects are attached before resolution, and a meta-algorithm may resolve a child only later in the run. Failing after execution comes too late to save that run. A queryable read was built instead, a flag each observation module set when it was resolved. It was removed because it covered observation modules and no other decoration, and relied on every module author remembering to set it.

Reopen only for a concrete diagnostic need. Any replacement should track matching in the resolver's common advice-application path and account for deferred resolution, rather than require each analyzer to report consumption. Empty pointcut selections are valid, so define an opt-in expectation before diagnosing an unmatched selection. Current matching behavior is described in [execution resolution](../docs/contributing/architecture/execution-resolution.md#pointcuts-advice-and-join-points).

### Analyzer cleanup contracts

Removed during the analysis rework in September 2026.

The earlier framework gave every analyzer a run state, `IAnalyzerRunState`, which the run created and released, and dynamic analysis added `IDisposable` to it to unsubscribe from the dynamic problem's evaluation event. A public registration lifetime with a release hook was then drafted to make that cleanup explicit.

Neither survived. The only analyzer that ever acquired anything was dynamic analysis, and only because the problem published its evaluations through an event. Once the problem owned its batch boundary and the event went away, no analyzer needed releasing, so the concept was removed rather than reshaped. Reopen only for an analyzer that acquires an external resource.

### Namespaces, folders or assemblies by layer

Discussed after the layers overhaul in September 2026: whether the six layers should become top-level namespaces, grouping folders or separate assemblies, for example one home for the domain vocabulary now spread over `SearchSpaces`, `Objectives`, `Random` and `Problems`.

Layers and namespaces answer different questions. Layers direct dependencies for contributors; namespaces are where users find a concept. The layers cut through the concept namespaces instead of grouping them: `Algorithms` and `Operators` hold L1 contracts beside L3 implementations, `Execution` spans four layers, and each encoding is a vertical slice of one representation. A layer-first namespace would make users import two namespaces for one concept, and a grouping folder above a namespace folder contradicts the rule that grouping folders sit beneath it. Assemblies fail harder: configuration creation names `ResolutionScope` and resolution names configurations, so L1 and L2 would reference each other. A hosting assembly would have to take `AlgorithmExtensions` and `RunTraceExtensions` with it, because both reference `AlgorithmRun` from the main assembly's `Algorithms` and `Analysis`. The Contracts assembly had just been folded into the main one for similar reasons. The architecture test enforces the dependency rules that matter, including a floor rule for domain vocabulary. Unlike an assembly boundary it checks only those rules, so each rule needs a probe that shows it fails.

Reopen only if a layer gains an audience of its own, such as a separately distributed package with a consumer that must not see the layers above it.

## Source material

The active guiding docs remain:

- [design-goals.md](../docs/contributing/design-goals.md)
- [developer-guidelines.md](../docs/contributing/developer-guidelines.md)
- [requirements.md](../docs/contributing/requirements.md)

## Keep or delete?

Keep this file only while it helps maintainers decide what still needs examples, refactoring, or cleanup.

Delete it once most remaining items have either:

- become concrete spec files, or
- moved into normal issue tracking or another active planning system

If it starts reading like a second architecture document again, cut it down.

[Discussed, tried, and rejected](#discussed-tried-and-rejected) outlives the tracker. When this file goes away, move that section somewhere durable rather than deleting it; its whole purpose is to stop settled questions from being reopened by someone who was not there.
