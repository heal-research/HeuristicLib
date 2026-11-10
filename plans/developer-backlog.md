# Developer backlog

This file tracks unfinished work and unresolved requirements. Sections describe the next kind of action, not implementation approval or a promise to create an issue. Concrete problems can still require a semantic decision before a fix; conditional ideas remain undecided until their trigger is met.

The [backlog review](developer-backlog-review.md) records evidence, original item identifiers and applied cleanup. Related entries may share context without sharing one implementation scope. [Discussed, tried, and rejected](#discussed-tried-and-rejected) preserves settled decisions and their conditions for reconsideration.

## Concrete problems and bounded follow-ups

### Correctness and verification

- define how genealogy represents repeated candidate values with different derivations in one generation. `GenealogyGraph.AddConnection` currently throws on the second value unless it equals one of its parents. A three-generation NSGA-II run with real-vector alpha/beta crossover, population 10, and seed 42 exposed this while validating explicit analysis ordering. It predates the trace changes and requires a decision about occurrence identity, not merely replacing dictionary insertion. Keep the fix independently scoped from a general provenance framework; coordinate occurrence semantics with provenance and solution equivalence without making either entire design a prerequisite.

- **Population similarity edge cases.** Define empty/singleton and diagonal semantics before promoting Experimental population similarity. The current aggregation excludes the diagonal and produces minimum 1, maximum 0 and mean NaN for a singleton. Construction coverage is not numerical behavior coverage: specify the similarity contract and add focused behavior tests after that decision.

- give a terminator the same random stream wherever it is placed. Since Package 2b of the [operator role parameterization plan](operator-role-parameterization-rework.md), an iterative algorithm passes its own terminator the iteration's generator, after the step and the interceptor have drawn from it, while `TerminatedBy` passes a fork of the run's generator under a dedicated key and then one fork per yielded state. A terminator that draws therefore can stop the same seeded run at different states as `GeneticAlgorithm.Terminator` and through `TerminatedBy`; the built-in terminators draw nothing. A per-state stream derived from the iteration generator under a fixed key would make these two placements agree, but equal derivations would give nested drawing terminators equal streams, and the same placement question applies to other operators. Solve it with a general decision on how operators obtain stable random streams, such as named operator keys, rather than a terminator-specific key. The acceptance case is a drawing terminator producing the same run in both placements. The flaw is documented in [Reproducible randomness](../docs/guide/execution/randomness.md#streams-that-algorithms-pass-to-operators)

### Documentation, tests and maintenance

- normalize `src/HeuristicLib/Problems/TestFunctions/ZDT/ZDT1.cs` and `ZDT5.cs` to `Zdt1.cs` and `Zdt5.cs`, matching their type names. Record these case-only renames explicitly in Git on case-insensitive filesystems

- evaluate wider use of tested source imports for documentation examples. Placement is settled: `HeuristicLib.Tests.Docs` owns documentation examples, organized by guide topic, while `HeuristicLib.Tests.ApiUsageSpecs` owns independent public API usage and inference specs. The complete stateful-composition and custom-advice walkthroughs live in the Docs project's `Extending` folder and are included through named VitePress source regions; the solution test run compiles and exercises them without duplicated tests. Other fenced examples still have no automatic connection to compiled code, and previous migrations left broken examples in the guides. Decide which existing guide and README examples should adopt this approach, how partial, illustrative and external-prerequisite examples are identified, and whether extracting code fences into a compile-only project has a useful remaining role. Preserve one source for each published example and its executable validation; copied tests do not validate independently maintained Markdown

- **Misleading numeric-optimization specs.** Remove the two comment-only examples in `SymbolicRegressionRedesignSpecs` or replace them with distinct current refiner workflows beyond `NumericParameterFittingSpecs`. Type-existence assertions do not compile or execute the claimed examples. Keep this bounded fix independently reviewable within test-convention cleanup; do not revive the obsolete optimizing-evaluator design.

- audit maintained and legacy candidate types used as `TCandidate` for mutation and aliasing risks. Immutability is an explicit design rule, not a completed whole-repository audit; existing ownership-taking vector factories do not close it. Identify remaining gaps and decide migration or retirement per representation before implementation

- apply remaining glossary terminology to public API and code names where the current names still encode legacy concepts, while keeping namespace and folder cleanup in separate branches

- **Authoring rules not yet enforced.** Inventory accepted rules missing from `RoleContractArityTests`, assembly/layer tests and existing analyzers, then add checks suited to each rule. Operators, algorithms and consumer-defined roles use `CreateExecutor` and explicit state requests; `HLib0002` and `HLib0003` cover every configuration, but the repository analyzers analyze only the main library, not Experimental. The historical protected-creation/concrete-return test is not a current rule to restore mechanically. Use reflection for compiled API shape and Roslyn for source ownership/dependencies; evaluate another test library only for a demonstrated gap, and avoid duplicate enforcement with the proposed inference-helper analyzer.

- **Test-convention cleanup.** Audit concrete violations of the [test-suite guide](../test/README.md): naming, filenames, folders, arrange/act/assert structure, consistent Shouldly use (including extension tests), and commented-out tests. Keep the numeric-optimization placeholder fix independently reviewable. Preserve settled unit/scenario/API/docs responsibilities; this cleanup does not reopen the suite split.

- **Coverage-reporting baseline.** Establish a useful view of weak coverage in public workflows and important invariants, and record how it will inform test selection. Choose tools and thresholds from that purpose; coverage reporting is separate from reorganizing test projects.

- **Warning baseline and ratchet.** Measure remaining compiler, analyzer and test warnings by diagnostic and suite, fix or justify them, then extend the existing staged policy. Nullable and selected XML-documentation diagnostics are already errors in `Directory.Build.props`; do not describe enforcement as absent or claim warning-clean builds without measuring them.

- **Contracts package release check.** Verify the published `HEAL.HeuristicLib.Contracts` package's deprecation status and the replacement release on nuget.org; repository consolidation alone establishes neither. If still needed, deprecate it and point consumers to `HEAL.HeuristicLib` as a release action. Consumers must reference the main package, and former Contracts types have the main assembly's identity. External status remains unverified.

- **HeuristicLab migration inventory.** Refresh the [operator/test migration overview](heuristiclab_operator_migration_overview.md) against current code and deliberate omissions before selecting the next batch. Its Boolean-operator absence claim is stale (`RandomBoolVectorCreator`, `BitUniformCrossover`, `BitFlipMutator` and `BitSwapMutator` exist), and its symbolic-expression mapping predates the maintained `ExpressionTree` model. Record operator mappings, behavioral differences and test parity separately; class existence does not prove migrated behavior or coverage.

### Measurements and current baselines

- **Observation cost.** Measure current allocations and wrapper overhead through the [benchmarking plan](performance-regression-benchmarking.md). Compare class records with readonly structs and separate subscriptions with a combined observer under matched clocks, ordering, failure and observation semantics. Preserve the unobserved path and measure its cost rather than assuming a universal zero-cost result. Share infrastructure with batching and resolver measurements; keep conclusions independent.

- **Lightweight batching cost.** Measure current batching, per-item RNG forks and output allocation using the [benchmarking plan](performance-regression-benchmarking.md); `SingleCandidateMutator` already uses static callbacks with explicit state. Evaluate fixed-width draw planning only for proven primitive-draw counts with identical deterministic semantics. Keep variable-width operations on logical-item/key child RNGs; results must remain independent of workers, partitions, scheduling, concurrency limits and CPU-core count. Do not require counter-based or random-access RNGs solely for this optimization. Coordinate raw RNG sampling and terminator streams without assuming shared performance conclusions.

- **Residual operator boilerplate.** Rebaseline the change surface for a representative role-contract edit after the accepted [operator unification work](operator-role-parameterization-rework.md), separating semantic work from mechanical repetition. The historical evaluator change is retained in the [review evidence](developer-backlog-review.md#historical-evaluator-change-evidence-b67); it does not describe today’s implementation. Preserve rejection boundaries against generic invocation on role executors and generated operator families; handwritten authoring remains required. Any further design needs a measured current burden.

## Requirements and design questions

### Runs, execution and experiments

- **Unify configuration and advice construction APIs.** Revisit a common public API to reduce surface, as requested on 2026-10-28. The 2026-10-29 revision removes resource lookup from advice. Configurations resolve declared children and resources, while advice receives concrete runtime dependencies through constructors. Revisit whether fewer public types can preserve this responsibility boundary. A proposal that also permits child resolution from advice must make those dependencies visible to preflight validation and define which advice applies to introduced children, including recursion behavior. Compare reducing context types with preserving that responsibility boundary. The [lifetime plan](operator-state-lifetimes.md#closure-follow-ups-accepted-on-2026-10-28) records the current decision.

- **Explicit resources and executor private state.** Refined option 1.2 was selected on 2026-10-27 in the [operator state lifetime plan](operator-state-lifetimes.md) and implemented with resource handles, which the developer marked provisional on 2026-11-03. Executors own private state. Lifetime binding through `Sharing` replaced the cache and limit handles and Cycle executor reuse, and the developer accepted the resolution scope as the default sharing unit, not yet implemented. The accounting handles, `RunResources` and the run setup supply remain until the operator count and duration terminators move to counting and timing wrapper configurations. Automatic disposal is excluded, and resources needing deterministic cleanup remain caller owned. Operator authors own concurrent access correctness, with caching as the documentation example. Strict shared concurrent budgets, serialization and checkpoints are deferred. Sharing across runs is deferred under [scenarios awaiting a concrete need](#scenarios-awaiting-a-concrete-need). The 2026-10-29 revision removes resource lookup from all advice wrapping contracts. Public API unification remains a later discussion.

- **Run lifecycle, results and naming.** Decide whether final/search states plus run metadata suffice or a completion-result object should carry typed stop reasons and lifecycle information. Preserve the distinctions between pause/resume, terminal completion, early stopping, cancellation and failure. Algorithm runs can resume, while experiment runs have terminal scheduling behavior. Review run/resume/continue names after settling those semantics. Separating setup from execution was accepted on 2026-10-28 through an optional `CreateRun` setup callback with fluent resource and aspect attachment. The [run setup decision](operator-state-lifetimes.md#run-setup-decision-accepted-on-2026-10-28) records the implemented contract and validation. This combines the former result and naming entries and remains separate from the termination overhaul.

- **Execution policy and batching ownership.** Define how a run requests sequential or concurrent execution while operator batches declare capabilities and trials retain separate scheduling. `SingleCandidateMutator` and `SingleSolutionProblem` currently own concurrency settings; scalar problem batching belongs within this investigation. Decide effective limits, per-batch versus shared run budgets and how policy reaches executors through explicit resolution/state ownership while preserving deterministic RNG. Compare keeping scalar adaptation in the problem with moving it to evaluation; scalar contracts, separate evaluators and conditional type checks are alternatives to assess, not accepted machinery. Coordinate cancellation transport while retaining its independently scoped long-running-operation requirement.

- decide how cancellation reaches operators. No operator role signature carries a `CancellationToken`, so an operator wrapping a long-running computation cannot observe cancellation and cannot pass one to a component that accepts it. `NumericParameterFittingRefiner` is the first concrete case: `NumericParameterFitter` takes a token and checks it inside the Levenberg-Marquardt solve, but the refiner has none to give and passes `default`, so a solve over a large population cannot be interrupted. Decide whether cancellation belongs in the role signatures, in a run-scoped execution context reaching instances through resolution and explicit state ownership, or on the executor itself; the answer interacts with the separate run-level execution-policy item above, since both concern run-scoped state that must reach operators without ambient statics. Consider that most operators are short and would only forward a token they never check. Keep this cancellation requirement separate from the broader concurrency-policy investigation; share transport decisions where necessary

- decide whether execution needs a problem dependent validation phase after the execution graph is resolved and before algorithm work begins. `NumericParameterFittingRefiner` is the first concrete case: enabling numeric parameter fitting while the symbolic expression search space allows operations the differentiable compiler cannot lower should fail before candidate creation, random draws or evaluation. A future design must find the right validation owner, reach nested operators and delayed child executor graphs, preserve configuration and executor ownership and avoid adding a broad operator lifecycle hook solely for this one case. Until then the refiner reports the incompatibility when it encounters an affected candidate

- **Pipeline stage transitions.** Define explicit transitions between `PipelineAlgorithm` stages, including different search-state types and same-type transformations. The current contract has one `TSearchState` for all stages. Specify typed transition inputs/outputs and what the composed stream exposes before implementation; split child work only when an accepted design yields independent deliverables.

- **Delegates in analysis definitions.** Accepted temporarily on 2026-11-04 to keep the operator unification branch in scope; see the [lifetime plan](operator-state-lifetimes.md#advice-and-scope-layers-under-discussion-since-2026-11-04). Trace definitions may hold value functions and callbacks, such as `mutator.Invocations.Trace(o => o.Offspring.Count)` or a caller's log, and `Advised` places them into the algorithm configuration. This contradicts developer guidelines § 4.8: delegates are not persistable, so an algorithm with such analysis attached cannot be persisted, and whether it can depends on the attached analysis. Redesign candidate discussed on the same day: configurations hold only value definitions, with trace values as immutable strategies such as `TraceValue.BestQuality`; delegates, including PythonInterop's Python callbacks, attach through a run-time-only list on `CreateRun` whose delegate definitions the compiler keeps out of `Advised`; observation sources become plain values of a configuration reference and an operation. Allowing no delegates at all was rejected because it rules out Python callbacks. Decide together with experiment persistence.

- **Time taken from the run.** `Duration` nodes, duration terminators and budgets hold a `TimeProvider`, a documented exception to the data rule (D53 in the [core simplification plan](core-simplification-plan.md#decided-in-round-21)). The run could supply the time instead. `ResolutionScope.Create(TimeProvider)` already exists and is only internal.

- **Checks and guidance against common mistakes.** Rethink what machinery could prevent common analysis and placement mistakes, such as a troubleshooting guide or a check for known misconceptions. Open cases after [D57](core-simplification-plan.md#decided-in-round-21): a terminator holding a counter that was never attached never stops, an undeclared child hides what is placed below it, and an analyzer repeated across separate `AnalyzedBy` calls records twice, which is accepted because a dedupe would bring back a rule phase 3 removes. Under D36 a clock shared across separate calls also ticks twice, and a trace on the original target of `Counting` reads the counter before the call. A Roslyn analyzer could report a discarded result of `AnalyzedBy` or `Counting` at compile time.

- **One rewrite that also takes budgets and wrappers.** Under [D58](core-simplification-plan.md#decided-in-round-21), only analyzers share one call, so a budget before `AnalyzedBy` makes `ga.Iterations` stale and gives D32's error. A generalized call accepting anything that carries substitutions would remove that order. It needs a substitution-bundle form for every budget and wrapper API, and it gives up the concrete return type of budgets. It also exposes list order whenever kinds mix: `[limit, trace]` gives `Budget(T(ga))`, the opposite direction to reading a fluent chain, and a wrong order gives no error, while the order of calls it removes gives a loud one. It extends `AnalyzedBy` without breaking it. Reconsider if the count taken during phase 3 steps 1 and 3 shows budget-then-analyze is common.

- **Reaching racing contenders from outside.** Under D3 an outer analyzer cannot reach what `AlgBuilder` creates, and a type or predicate selection passes over it without an error. The [migration notes](core-simplification-plan.md#migration-notes-for-origindynamic-racing) give the existing recipe. Reconsider only for a case that recipe cannot express.

- **Experiment persistence.** Support serializable parameter grids through runtime metadata with typed read and immutable replacement operations over ordinary configuration properties; see [Serializable parameter grids for experiments](configuration-parameters-and-experiment-persistence.md) for the problem, proposed approach, open decisions and acceptance criteria. Define construction from data, conditional alternatives, graph-reference handling and reproducibility. Coordinate persisted analysis with the runtime-only delegate boundary. Serialization friendliness is not a verified round-trip contract.

- **String-based configuration and remote experiment setup.** Decided on 2026-11-05: the [core simplification plan](core-simplification-plan.md) builds the declared parameter system and uses it for in-process experiment setup, as the foundation for this. Truly string-based configuration, such as experiments received through a REST API or loaded from a file, is a separate later task on its own branch. That task covers external formats, construction from data, type identifiers, value conversion, path syntax, and persisted recipes.

- decide which controls, if any, a configured composition can meaningfully expose. Transparent naming and invocation wrappers already forward controls through `ExecutorControls` and `IExecutorControlProvider`; preserve that contract. A configured pipeline exposes its own controls and must not inherit an arbitrary child's mutation strength. Require a concrete composition and algorithm consumer before changing that behavior. Current semantics are documented in [Operator composition](../docs/guide/extending/operator-composition.md#specialized-executor-capabilities). The historical Gaussian/counting-advice reproduction remains to be rechecked before its specific regression is marked closed; its reported values are retained in [backlog review B20](developer-backlog-review.md#b20--capabilities-through-wrappers-line-39)

### Algorithms, representations and analysis

- **Problem components and decomposition.** Build proper support for splitting a composite problem into parts. Two cases differ, with `SubProblem` and `ProblemComponent` as provisional names. A subproblem has its own evaluation, as the tour and packing parts of a Traveling Thief problem can. A problem component has a search space and instance data but no evaluation of its own, as with two symbolic regression expressions that only produce predictions when combined. The literature calls such problems non-separable, or [multi-component with interdependence](https://cs.adelaide.edu.au/~zbyszek/Papers/TTP.pdf), and [cooperative coevolution](https://cs.gmu.edu/~mpotter/abstract_ecj00.html) evaluates a subcomponent together with collaborators from the other parts, often kept in a context vector. Fixing the collaborators turns a component into a subproblem. Today `NoProblem` stands in for a component, with an `Evaluate` that throws and a `ZeroObjective`, and `CompositeSearchSpace` only combines two search spaces. `IPartialSolutionProblem` means incomplete candidates in constructive search, so "partial" should not name this concept. Decide the component contract, how operators read its instance data, how evaluation in context works, and how composite genotypes map onto the parts. This is out of scope for the [core simplification plan](core-simplification-plan.md), whose problem binding must not block it.

- **Mating contract and offspring counts.** Reconcile the [mating plan](mating-contract.md) with current selection/crossover behavior and locate acceptance evidence before adding a mating-arrangement role. The plan calls for `PopulationSize - Elites` offspring and twice that many selected parents; the current GA selects `2 * PopulationSize` parents, pairs them with `ToParentPairs`, evaluates the offspring and then applies elitism. Resolve this discrepancy explicitly. Preserve ordered parent roles, role-specific selection pressure and the plan's count/order examples; distinguish implemented pairing from the proposed arrangement abstraction.

- **Solution equivalence.** Decide its semantics separately from candidate value equality and genealogy occurrence identity. The existing proposal resolves equivalence from the problem, then search space, then `EqualityComparer<TCandidate>.Default`, with an explicitly supplied comparer taking precedence. It favors `AreEquivalent` and `GetEquivalenceHashCode` members plus an `IEqualityComparer<TCandidate>` adapter over a stored comparer affecting record equality and serialization. Verify acceptance before treating that shape as selected. Audit duplicate removal, Pareto extraction, diversity, genealogy and future evaluation caches; coordinate provenance without making them one implementation task.

- **Provenance and operator attribution.** Define occurrence identity and derivation across operator boundaries from concrete workflows. Genealogy currently reconstructs identity by value; routing attribution for `ChoosePerInvocation` and `ChoosePerItem` is a separate success-rate analysis requirement. Coordinate the repeated-genealogy-values defect and solution equivalence, while keeping a focused genealogy fix possible without first building a general lineage framework.

- introduce a probability based selection abstraction that separates selection from mappings such as fitness to probability and rank to probability

- **Objective semantics and promotion criteria.** Audit remaining inconsistencies in single-objective requirements, Pareto dominance, objective directions and total ordering. Current APIs already use `ObjectiveDirections`, `ObjectiveVector` and `MultiObjective`; ranking requests a total order through `RequireTotalOrder`. Identify concrete remaining gaps rather than restarting terminology work. Define acceptance examples and promotion criteria for Experimental Pareto-front and hypervolume analysis, including consumers requiring one value, a total order or only dominance.

- island-style population workflow

- fully implement ALPS-style age-layered workflow; the current ALPS algorithm still behaves like a simple single-layer evolutionary loop with a regular generation budget

- add an offspring-selection genetic algorithm implementation

- **API entry-point clarity.** Identify current workflows where equivalent spellings cause ambiguity, then remove only redundancy that adds no inference or setup value. Constructors, inference factories and useful fluent helpers are deliberately supported by developer guidelines § 8.6. Review algorithm construction, run setup/execution and analysis through representative current callers; an audit is not blanket authorization to remove APIs.

- **Analysis functions and derived views.** Inventory missing workflows against observation records, `Analyzer.Trace` functions, `Ranking` and `BestSoFar`. Counting, collection and projection can already be expressed in trace functions; do not recreate removed measurement strategies or `Readings` source types. Check remaining needs for objective-component/feasibility reads; expression length, depth, variable and symbol sets; permutation edges/positions; real-vector dimensions; standard deviation, count-matching, histograms, frequency maps, distinct counts and non-dominated sets; running worst/sum/count; and keyed-series views. Existing expression metrics and ordinary functions may cover parts of this list. Keep numeric-type inference versus standardizing on `double` undecided and follow [analyzer architecture](../docs/contributing/architecture/analyzers.md) for ownership and naming.

- **Inference-helper analyzer proposal.** Define when public operator construction benefits from an inferable `Create(...)`, `For(problem, ...)` or `For(algorithm, ...)` helper before implementing an analyzer/code fix. Specify false positives, safe generated output and API usage examples against current generic operation slots and developer guidelines § 8.6. Limit the proposed rule to operators and avoid duplicating it in architecture tests; existing algorithm helpers are not a mandate to generate every possible companion.

- **Composite construction inference.** Start from representative composite search-space and genotype callers repeating candidate/space type arguments. Compare inference helpers with a larger representation change before choosing a design. Preserve static relationships between each candidate part and search space; no untyped component collection workaround is approved.

- **Authoring-base naming.** Identify a concrete confusion between batch roles and single-candidate convenience bases against the current hierarchy in developer guidelines § 8.4. Compare names only after that example is clear; reserving names such as `PopulationMutator` for batch-dependent variants is a proposal, not an accepted naming rule.

- add a Python package bootstrap story for `HEAL.HeuristicLib.PythonInterop`: during early GitHub-based installs, the Python package should generate a tiny temporary `.csproj` that references the matching NuGet package and runs `dotnet publish` into a user cache, so Python users do not need checked-in generated DLLs. Later, replace or complement this with CI-built PyPI wheels that already contain the published .NET payload for normal Python installs.

- expand `IRandomNumberGenerator` and the concrete random engines so they expose the raw primitive outputs and data widths needed to build statistically sound and efficient higher-level sampling APIs; in particular, bounded integer generation should eventually be reworked on top of integer-domain primitives rather than `NextDouble()` scaling

### Contributor and test policy

- decide and document one common rule for record configuration style: primary constructor parameters versus explicit init-only properties, including naming and when each form is allowed

- **Formatting policy and enforcement.** Reconcile `.editorconfig`, `dotnet format` and Rider with agreed wrapping rules, measure current violations and decide which checks should become blocking; CI formatting checks currently continue on error. Preserve the settled conventions: the expression-body arrow ends the preceding line; block indentation uses multiples of four while column alignment is left alone; `where` aligns with the base list at declaration + 4; continuations indent four from the opening line and break at their first element; parameter lists break after the opening parenthesis or not at all. Two choices remain open: whether a multiline parameter list closes with `)` on its own line, and whether mid-expression continuation indents of `4n + 2` should be normalized. The old violation count is historical, not a current baseline.

- **Expensive-test execution policy.** Inventory current long-running and `Explicit = true` tests, then decide which belong in normal CI and which require explicit regression, smoke or performance runs. Compare traits/categories with separate suites only for a demonstrated need. Preserve the test guide's fast-to-broad feedback order; CI already builds before testing. Do not assume all expensive tests should be excluded or introduce a new suite split without acceptance.

## Conditional ideas and reconsideration triggers

These entries are retained for later selection. Regrouping them does not accept a feature, reject an idea or establish a missing capability; vague placeholders still need a concrete scenario before issue drafting.

### Scenarios awaiting a concrete need

- extend experiment composition only from concrete user needs. Deferred candidates include per trial problem and initial state creation, heterogeneous algorithm comparison, benchmark experiments across problem instances and dedicated individual cancellation handles. For each proposed scenario, identify the gap against existing experiment specs and materialization behavior and state the expected outcome before adding implementation work

- execution-graph invariants within one run

- **Configuration graph check.** A later validation pass over the declared parameters of the [core simplification plan](core-simplification-plan.md) could report common wrong configurations: a `Sharing` entry its subtree never uses, a stateful child left unshared under a shared configuration, such as a mutation strength under a shared mutator, a strength the strategy's mutator does not read, and a budget whose counter has no writer below it. Stateless children make some of these false positives, and delegates hide children, so decide how a node marks that it holds state and whether findings are errors or hints before implementing it

- **Node-level configuration checks.** `algorithm.Validate(problem)`, which `CreateRun` calls by default, walks the graph through `SearchConfigurationValidation` and reports two things: a declared invariant the search space contradicts, and a node not written for the run's types. A node cannot add a check of its own, so a setting that makes no sense, such as an empty `CycleAlgorithm`, fails only when `CreateExecutor` builds it, which for a stage inside a cycle can be well into the run, and a nonpositive budget is only documented. Decide whether nodes get a general `Validate` member that this walk calls, so that the check runs up front and also elsewhere, for example when a grid materializes its cases, and whether the `CreateExecutor` guards then move there. This is the per-node counterpart of the configuration graph check above, and it builds on the same walk once phase 2 of the core simplification plan moves it to declared parameters. Raised on 2026-11-06 from the empty-cycle check.

- sharing one live object across runs. Deferred on 2026-11-03 when run setup supply (`AddResource`, `AddResourcePerTrial`, `SuppliedResourceHandle`) was removed without replacement; see [system spec Q7](operator-unification-system-spec.md#q7-must-a-caller-be-able-to-share-one-live-object-across-runs). Candidates are a warm cache across repetitions, a global budget across an experiment, one sink for many runs, knowledge carried across runs and externally owned services. One sink and external services are common and caller owned; the other three give up trial independence. Name a concrete workflow and decide whether a caller-owned reference may enter a configuration before proposing a mechanism

- live per-trial tagging of observations in experiments, for example concurrent trials writing to one external sink tagged with each trial's case. `AddAspectPerTrial`, released on `main`, served this by creating one aspect per trial; the developer accepted its removal on 2026-11-04, and per-trial results are read from each trial's run afterwards ([lifetime plan, design review](operator-state-lifetimes.md#design-review-on-2026-11-04)). Candidate shape: a definition builds its per-run part from a factory that receives the run's context, which for an experiment trial includes its case, using the per-run construction that run-bound results already need

- transforming the state between Cycle turns or cycles, such as migration or re-seeding between inner algorithms. Turn and cycle interceptor slots were proposed and dropped on 2026-11-04 because observation no longer needs them; see the [aspects plan](aspects-as-automatic-composition.md#trace-collection-separated-from-attachment-accepted-on-2026-11-04). If they are added, Cycle passes every inner state through and yields the interceptor's result as one extra state when it differs from the state it received; holding back one state was rejected because it delays every state by one inner step

- multi-objective short-path usage

- symbolic-regression validation scenarios

- dynamic-problem extension workflow

- clarify whether a broader meta-optimization workflow is needed beyond the existing Experimental `MetaOptimizationProblem`, which already runs an inner algorithm inside `Evaluate` and has a scenario test. Name a concrete missing workflow before proposing additional work; the original implementation-existence requirement is fulfilled

- clear separation between population topology and per-member side metadata as an executable example story

- consider an optional encoded-problem adapter pattern for reusing an existing problem with different candidate encodings without adding a separate phenotype generic parameter throughout the core algorithm and operator APIs. The adapter should own the candidate search space and decode candidates into the existing problem's searched/domain representation before evaluation. Include a concrete example, preferably grammatical evolution, where an integer-vector candidate is decoded into an expression tree and evaluated by an existing symbolic-regression-style problem.

- **Substitution bundles beyond a flat list, and an analysis interface on top.** `ISubstitutionBundle` (working name, [D59](core-simplification-plan.md#decided-in-round-21)) is a flat, ordered list in `Execution`, and analyzers, counters and durations implement it, so analysis builds on substitution. Two extensions were considered and deferred on 2026-11-10. A composite bundle, which holds bundles, pays off only once a bundle must act as a unit beyond its order, such as applying only within a subtree or only when its first entry matches; until then nesting means nothing a flattened list lacks. An `IAnalyzer` extending the bundle, which `AnalyzedBy` would take, needs a member of its own, such as an analyzer's result type once phase 6 retires `Instrument` and analyzers become nodes. Before that it would be an empty marker, and it could not live in `Analysis`, because `Instrument` and the clocks sit in lower layers. Revisit the bundle's name at the same point.

### Optional capabilities and packaging

- reconsider typed hierarchical trial paths only if composed tuple keys and `RandomForkPath` metadata prove insufficient. Do not introduce untyped path tags that require casts

- identify concrete run-scoped operator controls beyond mutation strength before expanding the existing control-resolution conveniences or introducing a generic parameter framework. Any further control needs its own algorithm consumer and authoring example, using the existing control-provider contract. Define configured composition semantics explicitly; see the composition-control question under runs and execution

- design sensible creator, crossover and mutator recommendations for the remaining search spaces and for problems with domain specific preferences. `PermutationSearchSpace` and `TravelingSalesmanProblem` prove the recommendation mechanism and are enough for the current ergonomics work. Do not add recommendations mechanically. Each choice needs evidence that it is a useful starting point for that search space or problem, plus documentation that accepting a recommendation affects behavior and reproducibility. Decide at the same time whether reproducibility metadata should record which operators came from recommendations rather than explicit choices, since changing a recommendation changes results for everyone who accepted it

- revisit whether core and experimental unit tests should remain separate projects or be merged into one unit-test package; the current compromise keeps separate test projects for reporting while using stable `HEAL.HeuristicLib.Tests` namespaces so promotion from experimental to core does not require namespace churn

- reconsider `ExperimentalAttribute` once the main package has a meaningful stable API baseline; while HeuristicLib is still early alpha and all public APIs may change, package-level experimental opt-in is likely enough and avoids annotation ceremony that users may simply suppress wholesale

- decide whether epoch work needs exact recording. `EpochWorkTrace.PerEpoch` derives staleness from an evaluation clock and the epoch clock, which does not describe a caching evaluator. A counter on the problem would be exact, but it puts into the problem's state a metric that only the coarser update policies and parallel evaluation produce

- two analysis footguns are documented rather than guarded: an aggregation that mutates and returns one object can still be put in an ordinary trace, which then stores that one object many times, and a retention object shared by two traces couples their counting. Add checks when either bites

- **Shipping the analyzers and checking parameter declarations.** Take this up in its own issue and branch after [the core simplification](core-simplification-plan.md#decided-in-round-19), which moved it out of that branch. Nothing checks yet that a configuration declares its parameters. A forgotten child makes a walk treat the node as a leaf, so a selection by type or predicate skips that subtree without an error, and external authors have the same risk (D2). The analyzers do not reach them today: `HeuristicLib.csproj` references the analyzer projects with `PrivateAssets="all"`, and Experimental and PythonInterop do not run them.

  A trial on 2026-11-06, reverted afterwards, showed the packaging can work. Both DLLs went into `analyzers/dotnet/cs` of `HEAL.HeuristicLib`, the project references between the three libraries took `PrivateAssets="none"` so that the generated dependency no longer excludes analyzers, and a consumer of any single package got `HLib0003` from a local feed. The analyzers' Roslyn reference went from 5.9.0 to 5.0.0, which the developer accepts, because `global.json` allows SDK 10.0.100 and a compiler older than the reference skips the analyzer with only CS9057; this was not run on such an SDK. The form of the trial is not acceptable: `None` items naming `bin\$(Configuration)\netstandard2.0` depend on the folder layout, and the analyzer references were repeated in each project.

  Options to compare, none tried yet: a pack target that adds the resolved `@(Analyzer)` items as `TfmSpecificPackageFile` through `TargetsForTfmSpecificContentInPackage`, so that no output path is named; a separate analyzer project that packs itself and that the library depends on; and one shared declaration of the analyzer references, such as `src/Directory.Build.props`.

  The rule drafted for this, not built and not yet accepted: report a property declared on a type implementing `IConfigurationNode` that is an instance property, not private, and stored (a backing field or a `set` or `init` accessor), unless `GetParameters()` on that type reaches a parameter with that name through `Parameters`. Exempt are static members, computed properties, private properties, overrides and compiler-generated members. Still to decide: its severity, whether test projects run it, and whether it also reports an override that drops `base.GetParameters()` and a private member typed by a configuration. It does not catch a declaration whose getter reads the wrong property. It should also report `Parameter<X, T>` where `T` is a bare type parameter, because that value is read as a child when it closes over a configuration and is usually data, which [`SettingParameter`](core-simplification-plan.md#decided-in-round-20) declares.

  The developer decided on 2026-11-06 that detecting a missing parameter is a job for this later analyzer, which settles D2's choice of form. Detection has to read source at compile time, because a walk sees only what is declared and the library uses no reflection to find members. Until then a forgotten declaration goes undetected: the child still runs and its own `Resolve` still checks it when it is built, but it loses the pre-flight check and cannot be reached by a substitution. The rule also cannot see a child inside a settings object or a private field, or a `GetParameters()` too unusual to read from source.

- **Generating parameter declarations.** Phase 2 of [the core simplification](core-simplification-plan.md) showed how much hand-written declaration every configuration carries: about 3 lines per parameter, plus the `Parameters` collection and the `GetParameters()` override, on about 250 node types. The developer noted on 2026-11-06 that a source generator should probably write these at a later stage. It would also make a forgotten declaration impossible for the configurations it covers, so compare it with the analyzer above before building either. Questions to answer first: how an author marks the few properties that are not parameters, or a slot with a `ChildExecution` forward or a reference list; how generated code handles records nested in generic types and the `new` on `Parameters`; whether the generator reaches external authors through the package, which meets the same packaging question as the analyzers; and whether the static `XParameter` members stay readable and navigable when generated.

- **Optional dependency extraction.** Reconsider a package split only when a consumer needs lighter dependencies. Refresh ownership first: CSV is part of partial `DataFrame`; the expression parser imports Parlot locally; fitting uses internal `NumericParameterFitter` and `LevenbergMarquardt`; hypervolume also depends on MathNet. Compare a coherent split with the current package. Do not make internal APIs public merely to satisfy a split or carry forward the obsolete project-wide Parlot-import obstacle.

- decide whether operators should expose a consistent caller-provided output-buffer or result-memory API. Avoid one-off `Memory<T>` parameters on individual operators. If this becomes necessary for allocation-sensitive workflows, design it as a library-wide operator convention that clearly states who owns the resulting storage and how immutable candidate containers are created from it.

### Matching and advice extensions

- **Reusable aspect configuration and placement, under discussion.** The developer requested declarative resource sharing and nested aspect placement for algorithms, operators and generic configuration nodes. The proposal and independent review are recorded in [operator rework section 11](operator-role-parameterization-rework.md#11-aspect-configuration-rework-and-independent-review), with [lifetime and analyzer ownership notes](operator-state-lifetimes.md#aspect-configuration-lifetime-review-on-2026-10-29). Transparent application selection, selected typed views, description identity, lazy dependency construction and analyzer migration remain recommendations rather than accepted implementation scope. Keep the rejected universal `Wrap` resource parameter and live resources in reusable configurations rejected. This discussion does not authorize serialization, arbitrary caller path selection or a runtime advice cache.

  The subsequent [simplicity checkpoint](operator-role-parameterization-rework.md#simplicity-review-and-governing-design-goal) replaces the recommendation to proceed with that whole framework. Common operations must stay simple and complex workflows should compose existing configurations, aspects and resources. Transparent application exceptions and a new analyzer lifecycle are no longer the preferred starting point. An optional budget handle or a configured composition of existing advice remains a candidate to investigate, not an accepted API. The developer prefers convenient instrumentation extensions over a separate instrumentation system.

- **Additional pointcut conveniences.** Require a concrete wildcard or attribute-selection example before expanding matching. Reference/type/predicate/Boolean/exact-name matching and custom pointcuts already exist; see [execution resolution](../docs/contributing/architecture/execution-resolution.md#pointcuts-advice-and-join-points). Caller-path selection is a separate occurrence-model question.

- **Additional advice kinds and lifecycle callbacks.** Investigate separate before/after-returning/after-throwing/finally APIs only for a demanded workflow. Specify advice failures, zero or multiple proceeds, exception suppression/replacement/retry and what callbacks observe. For algorithm lifecycles distinguish stream creation, enumeration start, yields, normal completion, exception/cancellation and early disposal; pause retains the iterator. Existing around wrappers do not select these extra contracts.

- **Advice-level retry convenience.** Configured `Retry` and its tests already exist; a separate advice-level helper needs a concrete use case and explicit retry/failure semantics. Do not track generic retry support as absent.

- **Interceptor replacement, deferred.** Retain the configured `Interceptor` for now and defer its replacement by around-iteration advice. Future exploration must preserve serializable configuration behavior and the internal `step -> Transform -> IsTerminalState -> yield` boundary, with the transformed state feeding the next step. A wrapper around the output stream can replace a state seen by its consumer but cannot change the inner iterator's retained next-step state or undo its termination decision. Equivalent advice needs an explicit typed boundary inside the iteration loop that consumer-defined algorithms can implement. Compare a deduplication example and a stateful transformation shared by two algorithms, preserving next-step input, termination, RNG, pausing, ordering and shared state. One possible direction is a persistable behavior description prepared into runtime advice, rather than making callback-bearing runtime aspects configuration values.

- **Caller-path selection, deferred.** Caller-path selection is also deferred. It needs an explicit model of caller edges or execution occurrences, including shared configurations and deferred child construction. An acceptance case is a genetic algorithm and a hill climber sharing one stateful mutator configuration and prepared state: advice selecting only hill-climber calls must preserve the shared mutation counter and RNG behavior while excluding genetic-algorithm calls. Configuration selection alone cannot identify that caller edge. Names and path syntax are possible interfaces, not selected designs. Simple name/property pointcuts do not require that feature.

- **Persisted aspect descriptions.** An optional future extension could persist an algorithm together with configured aspect descriptions. Compare an outer configuration with an algorithm wrapper only if that capability is requested. General aspects may change behavior; algorithms/operators may install them internally; inspection and analysis preserve the observed computation. No persistable wrapper or conversion of runtime aspects/advice/pointcuts to records has been selected. Coordinate experiment persistence; runtime advice persistence is not assumed.

### Performance follow-ups

- **Resolver/advice performance.** Revisit setup, deferred activation or advice overhead only when a concrete workload warrants it. The [operator plan](operator-role-parameterization-rework.md) contains historical performance checkpoints; they do not establish every later revision's cost. Establish a fresh baseline and compare matched alternatives using the [benchmarking plan](performance-regression-benchmarking.md). Comparative resolver measurements remain deferred and are not retroactive completion gates for accepted reworks.

- revisit the call context every operation's invocation repeats. Each operation's private invocation struct implements `Random`, `SearchSpace` and `Problem` as three one-line properties, because general invocation wrappers see only the invocation and a struct has no base class. The sixth review of the [operator role parameterization plan](operator-role-parameterization-rework.md) kept them: passing the three to `IInvocationWrapper.Invoke` beside the invocation repeats them in every wrapper signature, a struct bundling them is the context carrier that developer guidelines § 4.19 excludes, and a class invocation with a base allocates on every call, against the zero-allocation requirement. A benchmark of a class invocation through the § 8 harness would show what dropping that requirement costs. This is conditional evidence gathering: the accepted decision keeps struct invocations and explicit call context. A class-invocation measurement would not itself approve relaxing the allocation requirement or introducing a context carrier.

## Discussed, tried, and rejected

### Finding a configuration copied by an earlier substitution

Discussed under [D32 and D33](core-simplification-plan.md#decided-in-round-11) and rejected on 2026-11-06. A reference that an earlier substitution copied stays an error, and the error names the routes that already exist.

- **"This configuration, wherever it is now" as the default.** A supplied configuration would match the exact reference, or else the one reachable node equal to it once added wrappers are ignored. A throwaway model confirmed that this is pure and that chains work in either order. The developer rejected it as the default, because a user who supplies an operator means that exact object. It also takes a separately built equal twin for a target missing from the graph, and it no longer finds a parent whose child was replaced.
- **Value equality as the default.** It fixes the wrong case, because a copied ancestor differs from its original by value exactly because its child changed. It also widens a match silently: slots left at their defaults are value-equal even in differently configured algorithms, and two counters with no settings compare equal.
- **A weak table from copy to original.** It finds a parent whose child was replaced, but every substitution writes to a global table, so a substitution is no longer pure, and a substitution and the same graph written by hand, equal as data, behave differently.
- **An identity field on each node.** It is copied by `with` and breaks record equality.
- **A result type carrying the map from copy to original.** It needs a second overload of every call and gives `var` the wrong type.

### Analyzer placement alternatives rejected on 2026-11-08

Discussed under [round 21](core-simplification-plan.md#decided-in-round-21) of the core simplification.

- **Counters only from `Counting(..., out)`, with passive clock definitions for traces.** A trace on the original target sits inside the counter and reads it before the call, and `ga.Iterations` stops matching once an operator was wrapped. Separate clock definitions avoid the first, but add a second way to count and need another counter for a total.
- **Installing everything in one chain with a selector, `ga.AnalyzedBy(g => g.Evaluator..., out var best)`.** It would make forgetting impossible and name targets from the current graph, but a wrapper around the root is no longer the concrete algorithm, so the chain loses its type after the first iteration analyzer (D10).
- **One simultaneous pass that orders wrappers by kind.** Every substitution would match only the input graph, and the library would sort the wrappers at each node. It removes a later selection matching a wrapper from the same call, which D28 allows on purpose, and it reverses D3 and D28. Users would also have two ordering regimes: a hidden one by kind, and list order for wrappers that change behaviour.
- **Analyzer placement looking through clock wrappers (`IClockWrapper`).** Built in step 1 of phase 3 and removed the same day. A placement went around the wrappers on its exact target that declared a clock, and skipped a clock already there, so separate `AnalyzedBy` calls could share one. It took a public interface on six wrappers, a second kind of substitution and a wrapper equality check, and it made clocks the one exception to "each call places what it lists". Only tests of the rule itself depended on it, and D58 already tells users to make one call.
- **Ticking a counter before the call and taking it back on failure.** This makes the order irrelevant for operators, but an iteration counted before the state exists stops an iteration terminator one iteration short.

### Simplification proposals rejected on 2026-11-05

Proposed by the [simplification reports](simplification/), reviewed on 2026-11-04 and rejected by the developer on 2026-11-05. The [core simplification plan](core-simplification-plan.md) records what is being done instead.

- **Hand-written per-role decorators instead of `IOperation` and `IInvocationWrapper`.** This brings back an explosion of decorators: five wrappers over twelve operations is already 60 classes, and external operations would have to write their own. Reopen only if generic wrappers can no longer serve a required operation.
- **LINQ-style termination and interception on the state stream.** Termination is a fundamental part of an algorithm's configuration, so it must be in the configuration graph, and the same holds for interception definitions. A stream operator also stops only the outer stream, and a stream `Select` cannot feed its result into the next step. Callers may still filter a stream they consume.
- **No delegates in configurations under any circumstances.** Configurations being data is a fundamental guideline, held very highly, but not an unbreakable rule. Documented exceptions are acceptable where there is a technical need, such as Python interoperability.
- **Single-pass compile without scopes.** It cannot express budgets restarting per turn with chosen state kept across turns (S6, S7), and its compiler still needs a table and a sharing rule.
- **Progress only through search state, `IProgress` or metrics.** It loses genealogy, offspring histograms, cached versus real evaluations and custom clocks.
- **Nine roles reduced to four shapes.** It leaves no place for creators, replacers, terminators, interceptors or move operations, and closes roles to consumers.
- **An `ExecutionContext` parameter on operations.** Excluded by developer guidelines § 4.19. Cancellation remains its own open item.
- **Algorithm as an operator.** Retry, counting and choice do not fit a lazy stream.
- **Removing multi-operation configurations.** `Neighborhood` uses them, and they were retained by decision on 2026-11-04.
- **`Initialize` and `Step` as the algorithm contract, removing Cycle.** Cycle, Pipeline and racing yield several states per call, and `IterativeAlgorithm` already offers steps.
- **Predicate-plus-factory decorators in setup closures.** Applied during resolution, they rename advice while keeping its rules, and restore run setup. A substitution that uses predicates before resolution is a different thing and is accepted.
- **A `Name` property as configuration identity.** Names are not unique. Use format-level identifiers in a persisted form.

### Resource lookup on every advice wrapping hook

Implemented during the resource handle migration and rejected on 2026-10-29 after review of its public API cost. Passing `RunResources` through every role bridge, observation and custom advice extended the whole wrapping hierarchy for the counting and duration advice that actually needed lookup. Advice receives concrete runtime dependencies through constructors instead. Per run setup supplies the same instance under a handle when a terminator or other configuration must resolve it. Measurement alone needs no resource binding. The handle based aspect overloads are removed. Reusable handle based aspects would require a separately accepted binding design that preserves lazy creation, deferred aspect contexts and advice identity. No binding or preparation mechanism is introduced by this revision.

### Observation points as a separate observation concept

Accepted on 2026-10-26 in the [operator unification plan](operator-role-parameterization-rework.md), implemented for `CycleAlgorithm` in Package 5 and rejected on 2026-11-04. A configuration declared named points for progress that no invocation or iteration shows, resolved them as children and reported records to them. The concept existed only for observation, every `with` copy of a configuration received new points, and unused points still resolved and received records.

Observation attaches to executor calls instead, recorded in the [lifetime plan](operator-state-lifetimes.md#advice-and-scope-layers-under-discussion-since-2026-11-04). The moments once planned for points map to calls: Cycle turns, `PipelineAlgorithm` stages, the contenders `DynamicRacingAlgorithm` starts and drops, `Retry` attempts and `ChoosePerInvocation` picks are calls of child executors. Two positions of one reference are told apart by using distinct references. The point where a budget runs out is a stop reason, which belongs to [run lifecycle and results](#runs-execution-and-experiments). Reopen only for an internal moment that is neither an executor call nor a yielded state and cannot reasonably become an operator slot.

### Explicit run builder alongside CreateRun setup

Considered on 2026-10-28 and not selected. The accepted [run setup decision](operator-state-lifetimes.md#run-setup-decision-accepted-on-2026-10-28) uses `CreateRun` with an optional setup callback whose methods can remain fluent. A separate `CreateRunBuilder().AddResource(...).AddAspect(...).Build()` entry point adds an independently usable builder and a separate build operation without a demonstrated need. Reconsider only when a concrete workflow needs to retain incomplete setup or build multiple runs from it, with explicit rules for sharing random generators, supplied resources and analyzer instances. This decision concerns run creation, not the internal scope builder or reusable algorithm configurations.

### Live resource instances in reusable configurations

Considered as option 1.1 in the [operator state lifetime review](operator-state-lifetimes.md#review-findings) and not selected on 2026-10-27. Repeating or copying a configuration would also share its live cache or counter. Independent trials would require application code to reconstruct resources and configuration graphs per trial, and grids could silently reuse cached results across settings.

The selected direction keeps explicit handles in configurations and live instances in run preparation. Reconsider live instances as ordinary configuration properties only if reusable configurations producing independent executions is deliberately abandoned. This decision concerns the proposed resource mechanism, not a claim that all existing problem or strategy references are pure values.

### Runtime objects replacing configurations and executors

Considered as option 2 in the [operator state lifetime review](operator-state-lifetimes.md#review-findings) and not selected for this change on 2026-10-27. Direct authoring would be simpler, but fresh turns, parameter grids, deferred observation and automatic advice weaving still need construction and identity mechanisms. Binding to concrete run types also remains necessary.

Move private state into executors while retaining the configuration/executor distinction. Reconsider merging those types only for a broader product decision that explicitly changes experiment construction, observation attachment and reusable configuration semantics.

### Generic private state sharing through composition declarations or wrappers

Considered during the [operator state lifetime discussion](operator-state-lifetimes.md#earlier-alternatives) and not selected on 2026-10-27. An enclosing `SharedOperators` declaration or an operator `.Shared()` wrapper would still require selection of a lifetime owner for arbitrary private state and reconstruction around context specific children and advice.

The selected direction exposes coherent resources through handles and leaves other data private to executors. Reconsider generic private state sharing only if a concrete required workflow cannot be expressed through an explicit resource and the additional ownership machinery is justified. Do not reintroduce this mechanism as a resource registry keyed by configuration.

**Overridden by the developer on 2026-11-03.** The handle mechanism proved invasive: it added a concept, gave every resource two forms (a live object or a handle), spread through operator configurations and moved resource management into `AlgorithmRun`. The developer selected lifetime binding instead, recorded in the [operator state lifetime plan](operator-state-lifetimes.md#lifetime-binding-through-sharing-accepted-on-2026-11-03), and it is implemented as explicit `Sharing`. The two objections above are answered as follows:

- **Lifetime owner.** The node on which `Sharing` is called owns the lifetime. Its executor determines how long the shared executors live, so constructing that node again, for a new Cycle turn or another run, creates new ones. No first requester and no global policy chooses.
- **Reconstruction around context specific children and advice.** The shared executor is constructed once, in the sharing node's context, with the children and advice visible there. Each request then applies its own advice around that bare executor, exactly as it would around a fresh one, so sharing never changes which advice wraps a use or in which order. Advice installed below the sharing point does not reach inside the shared executor; this is accepted as a placement rule.

Each sharing layer keys its table by configuration reference, but only for the configurations it lists. Nothing is shared implicitly, every other configuration is still constructed fresh for each use, and the table is not a run-wide resource registry.

### Published callbacks obtained from the resolution scope

Implemented during Package 5 on 2026-10-26, then rejected in review. `ObservationSource.Published` installed internal `PublishedObservationAdvice` that wrapped nothing, while `ResolutionScope.GetObserver<T>(this)` returned an action. A node could not distinguish several observation points with the same value type, and resolution needed exceptions for this advice. Named observation points as ordinary child configurations replace it. Storing a point directly on a record was also rejected because `with` copies would share it; the accepted weak identity cache gives each configuration reference its own named points.

### Binding as the public runtime type family

The execution-factory naming discussion on 2026-09-27 found `IExecutionBinding` accurate for the runtime object's responsibility, but the user rejected binding as the public type vocabulary because the word is overloaded. The selected family is `IConfigurationNode` / `IExecutor`, with operation-specific `...Executor` interfaces and unchanged domain names for configurations. Binding can remain descriptive terminology inside the resolver design; it does not introduce a parallel public contract. The [glossary](../docs/guide/glossary.md#executor) defines the current terminology.

### Prohibiting all child-scope module installation

The DI/AOP scope discussion considered requiring every module to be declared at a common ancestor and allowing child scopes only to inherit modules (option B). This was not implemented and is not the selected direction: it removes useful child-local observations, including an inner observer that receives only the inner calls while an outer observer receives all calls. Restricting modules alone also leaves the same binding problem for configuration-origin budgets.

Resolution builds an executor for every request with the requesting scope's aspects, while scoped state is shared by configuration reference. Children resolve in their owner's state with the requesting aspects, so adding an inner observer does not reset state or change existing parent bindings. These are implemented contracts described in [execution resolution](../docs/contributing/architecture/execution-resolution.md#state-and-construction).

### Retaining fixed composite bindings with rejection (option A)

Selected on 2026-09-26, then superseded on 2026-09-27 after comparing shared-state binding designs. Option A would retain an ancestor composite's existing children and reject reuse when a requesting scope required observations those children would bypass. Its comprehensive detection policy for opaque/deferred dependencies had not been implemented or settled.

Building executors per request supports new scope-specific child bindings while preserving scoped state, as described in [execution resolution](../docs/contributing/architecture/execution-resolution.md#state-and-construction). Do not restore the superseded guard or silently omit required observations when reuse is difficult. Reopen only if a concrete requirement cannot be met by contextual binding over shared state.

### Implicit source resolution for generated wrappers

Production behavior at `c6957cbc`, rejected on 2026-10-13. A generated wrapper obtained the execution it enclosed by resolving its original source configuration, which the resolver temporarily redirected to the already-built inner chain. Internal compositions reused the same redirection.

It hides the wrapper's input behind an ordinary-looking `Resolve` call, so resolving the source means different things inside and outside a wrapper. The resolver needs interception state in every construction frame and rules for when the redirection must not leak into extra children. An explicit typed `inner` gives the same composition without either. Reopen only if explicit input cannot express a wrapper whose source it must locate itself.

### Supplying the inner execution through every factory

Proposed and rejected on 2026-10-13. Every factory would receive an optional supplied execution with the configuration it stands for. Wrapping and multi bases would take it for matching child references, and the resolver would check that it was taken.

It treated the symptom of building advice through ordinary child resolution rather than the cause. Wrappers already prepare state once and wrap a supplied child executor, so advice applies that hook directly to the built execution. Ordinary factories stay unchanged, and the check, the extra parameter and the private advice states disappear. Reopen only if advice must resolve children of its own.

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

Narrowly reopened by [operator role parameterization](operator-role-parameterization-rework.md): configuration-owned `OperatorExecutorFactory` state supports several checked operation/run views, including multi-operation executions. Package 1 exercises this through the production resolver. Static wrapping members on variant executor interfaces remain rejected. Outcome: Package 2a replaced the configuration-owned factory state with a `CreateExecutor` hook called for every build and scoped state kept by configuration reference. The static members live on the operation, `IOperation<TSelf>`, rather than on executor interfaces, and no factory objects remain.

### Advice-based configured wrappers

Proposed during the aspect redesign and rejected on 2026-10-14. A generic configured wrapper per role, such as `AdvisedMutator(child, advice)`, would let one behavior serve both as a configured operator and as advice.

Instead, instrumentation such as counting, timing and observing is advice only, and behavior-changing wrappers such as caching and limits are operators only, so no behavior needs both forms. Reopen only if a behavior is needed both in configurations and as woven advice.

**Reopened by the developer on 2026-11-03 and 2026-11-04.** The accepted [aspects as automatic composition](aspects-as-automatic-composition.md) contract requires every behavior an aspect installs to be expressible through explicit configuration composition, and the selected direction for the operator count and duration terminators makes counting and timing ordinary wrapper configurations ([lifetime plan, detail 4](operator-state-lifetimes.md#lifetime-binding-through-sharing-accepted-on-2026-11-03)). The reopening concerns one behavior available in both forms; whether that is a generic advised wrapper per role, as rejected above, or one wrapper per concern remains part of the aspects design.

### Resolver-owned keyed child retention

Production behavior at `c6957cbc`, rejected on 2026-10-13. `GetOrCreateChildScope(key, declare)` kept a child's state and its once-only declarations under a reference key on the owner, and rebuilt a scope around them for each requesting context. Cycle's reuse mode and both operator budgets used it.

It coupled state retention with declarations and made the resolver own callers' retention keys. `With(aspect)` adds advice without new state, so budgets need no retention slot. Cycle prepares a `ChildScopeSelector` that owns its activation policy: `Fresh` creates child-local state per activation, `Retained` keeps child-local state by child configuration reference within that prepared Cycle, and `Shared` uses the parent's state. Separate Cycle preparations have separate retained slots, while all policies can reuse factories already owned by ancestors. Retained states keep no requesting aspects; each activation supplies its current aspects. Reopen only if caller-owned state cannot express a concrete retention requirement. See [child activation policies](../docs/contributing/architecture/execution-resolution.md#child-activation-policies).

### Generated wrappers with children of their own

Production behavior at `c6957cbc`, rejected on 2026-10-13. A generated wrapper could resolve additional children, with a private selection scope and a rule that its own registration skipped them while other registrations still saw them.

No production recipe used it. Advice now wraps exactly its target, so it never resolves anything, and the private scopes and self-exclusion rule disappear. Real work belongs in the configuration, where node selectors and budgets can see it. Reopen only for advice that must resolve another configured operator.

### Per-context node caches in the resolver

Production behavior at `c6957cbc`, rejected on 2026-10-13. Each observation context cached its completed nodes and construction faults, children without new declarations reused ancestor nodes, and any added declaration forced the affected nodes to be rebuilt.

Resolution runs once per run, or once per activation in a meta-algorithm, while operator execution dominates runtime. Persistent state lives with the configuration's factory, so the cache only preserved node identity and fault timing, at the cost of fault records, unchanged-parent lookup and the rebuild rule. Reopen only if measurements show repeated resolution as a material cost; even then, add a cache that does not change behavior.

**Partly overridden by the developer on 2026-11-03.** A [resolution scope is now the sharing unit](operator-state-lifetimes.md#default-sharing-the-scope-is-the-unit-accepted-on-2026-11-03): within one scope, each configuration reference resolves to one executor. Performance is not the motive. Users expect a reference repeated within one algorithm to be one executor with its private data. Unlike the rejected cache, the table belongs to the scope rather than to each observation context. It has no fault records and no rebuild rule. A request from another advice context is an error, not a rebuild.

### Building shared executors in the sharing scope itself

Discussed and rejected on 2026-11-04, while implementing step 1 of the [operator state lifetimes plan](operator-state-lifetimes.md#step-1-decisions-accepted-on-2026-11-04). A shared configuration is built on its first request, usually from a Cycle turn, after the sharing scope has child scopes. Built literally in the sharing scope, a shared budget adds its advice there, which the guard against changing a scope with child scopes rejects. Relaxing the guard would let the shared budget count calls made in sibling turns, and whether it could still be added would depend on the order of the turns. Rejecting sharing for configurations that add advice drops the accepted `Cycle(limitedA, B).Sharing(limitedA)` scenario. Each shared configuration is instead built in a child scope of its own. Reopen if that ruling is simplified, for example together with a different budget mechanism.

### Aspect parameters on scope creation

Tried and removed on 2026-11-04 in step 1 of the [operator state lifetimes plan](operator-state-lifetimes.md#step-1-decisions-accepted-on-2026-11-04). `ResolutionScope.Create(aspects)` and `CreateChildScope(aspects)` applied their aspects in listed order beside `AddAspect`, which wraps closest, so node authors had two ways to add advice with opposite orders. `AddAspect` is the only scope-level way, and users attach advice through `Advised`.

### Sharing a configuration the scope has already resolved

Tried and removed on 2026-11-04 in step 1 of the [operator state lifetimes plan](operator-state-lifetimes.md#step-1-decisions-accepted-on-2026-11-04). `Share` kept the executor the scope had already resolved, so whether a later `Share` took effect depended on the order of resolution. It is an error instead. Reopen only for a meta-algorithm that cannot share before it resolves.

### Bridging run setup to instruments

Discussed and rejected on 2026-11-04 for step 2 of the [operator state lifetimes plan](operator-state-lifetimes.md#step-2-decisions-accepted-on-2026-11-04). Letting run setup attach instruments would have kept callers on run setup one step longer, but every reader changes anyway, from `trace.Snapshot()` to `run.Result(trace).Snapshot()`, and step 3 deletes run setup. New types under temporary names beside the old traces and clocks would have added a second implementation of each concept. Sources, traces, clocks and retention change in place instead.

### Advice that selects the node adding it

Discussed on 2026-11-04 for step 2 of the [operator state lifetimes plan](operator-state-lifetimes.md#step-2-decisions-accepted-on-2026-11-04). `Advised` adds its advice while it is built, after its own resolution started, so advice selecting `Advised` itself, such as a pointcut over every algorithm, was rejected as advice for an already resolved configuration. Keeping that error would make a broad pointcut impossible at the root of a run once run setup is gone. Making `Advised` no pointcut target contradicts spec Q3, which counts it like every other node. Resolving its child in a child scope would make wrapping change sharing. The accepted rule excludes only the adder from its own advice. Reopen if a node must observe its own calls through advice it adds itself.

### Shared configurable run host

Tried and removed during the layers overhaul in September 2026. `RunHost` wrapped a lifecycle enum and lock, accepted a transition table and exception-message prefix, and exposed its lock back to the runs. The callers still made the actual lifecycle decisions. The extraction introduced a generic state-machine mechanism without taking ownership of execution or scheduling.

Algorithm runs retain resumable execution; experiment runs coordinate independent trials and stop permanently on early disposal. Their lifecycle rules and diagnostics stay with the corresponding run. Shared state names and similar guarded assignments do not justify a common host. Keep algorithm lifecycle and enumerator ownership together; composition and continuation wrappers are not required merely to divide the class. Revisit a shared host only if there is a concrete shared execution responsibility it can fully own.

Approaches that were evaluated, in some cases prototyped, and decided against. They are recorded here so that a settled question is not proposed, prototyped and rejected a second time. Each entry states what was tried, what decided it, and what would have to change to reopen it.

Unlike the open items above, these are settled. Rules that follow from them live in [developer guidelines](../docs/contributing/developer-guidelines.md) and the relevant topic pages; this section holds only the reasoning.

### Renaming Interceptor only to make room for AOP

Discussed and rejected during the DI/AOP design review in September 2026. A rename to `StateTransformer` was proposed to avoid using `Interceptor` for both the existing state-transforming operator and future AOP behavior. The AOP design can use advice terminology, so the assumed naming collision does not require a rename.

Neither `Interceptor` nor `StateTransformer` communicates the precise invocation boundary: after a successful iteration step, before termination evaluation and publication, with the transformed state feeding the next iteration. Renaming would therefore leave the stated clarity problem unresolved while changing the whole role family and its callers. Retain `Interceptor`. Reconsider its name only if a concrete responsibility or unavoidable ambiguity changes; adopting AOP alone is insufficient. Whether typed algorithm advice should replace the role remains a separate [open advice follow-up](#matching-and-advice-extensions).

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

### Synchronous policy engine and retry-specific adapter hooks

Rejected during the operation-parameterization rework on 2026-10-17. `SynchronousPolicy`, then `ISynchronousPolicy`, generalized retry into a call engine with child selection, RNG and candidate-work result metadata. Its replacement, `RetryWrapper`, put a concrete feature on every operation adapter and closed new wrapper behavior to external assemblies. Use the smaller `IInvocationWrapper` contract instead; the entry below describes its current struct form. Separate success/lifecycle adapter profiles were also removed; they repeated execution code and calculated candidate counts for consumers that discarded them. New whole-invocation wrappers require no adapter changes. Whole-call choice uses the same contract through `ChoosePerCall`, implemented in Package 2b. Raw measurements remain local under `artifacts/operator-role-package2`.

### Delegate call wrappers, construction carriers and per-operator advice preparation

Implemented in Packages 1 and 2 of the operation-parameterization rework and replaced in Package 2a on 2026-10-18. Invocation wrappers turned a typed child-call delegate into another delegate, so each advice layer made three indirect calls and packed the arguments into a tuple. Optional lifecycle callbacks (`IInvocationCallbacks`, `InvocationNotifier`, `CallbackInvocationWrapper`) aggregated child, callback and cleanup failures. Construction went through checked carriers (`ExecutorFactory`, `WrapperExecutorFactory`), view-keyed caches, `IExecutorWithControls` pairs and per-owner advice wrapper factories with five type parameters. A small timing advice needed an inner factory, adapter generics, a callback wrapper and a callback struct.

A prepared operator now constructs its executor directly; a struct `IInvocationWrapper` is compiled into one wrapping executor per layer and calls `IInvocation.Proceed()`; executors forward their own controls; and operator advice checks before construction and wraps after it, without per-operator preparation. Accepted consequences: wrappers use ordinary `try`/`finally` failure semantics; an advice selecting several operators keeps its own per-operator data; reusing advice across independent roots no longer restarts its data; advice data does not follow Fresh or Retained child scopes. Reopen per-operator advice preparation only for a concrete scenario that cannot keep its data on the advice object or move into a configured operator, such as advice-driven adaptive operator selection that must restart per root.

### Typed operator invocation

Prototyped during the operator rework in August 2026 and rolled back.

Cross-cutting concerns such as duration measurement, observation, choosing one child and pipelining would have been centralized behind one typed execution signature, `(TInput, TContext) -> TOutput`, with generic meta-operator bases invoking children through a shared `Invoke`.

It failed because C#'s nominal interface model still requires a role-specific configuration and executor for every role. A generic pipeline containing mutators cannot become an `IMutator` merely because its type arguments are mutators, and CRTP preserves a self type but cannot add interface membership. The duration-measuring prototype moved only the small exception-safe timing block into the shared base, while constructors, configuration properties, static factories, fluent extensions, the nested executor and role forwarding all remained; the generic base declarations and the context bridge replaced at least as much code as they removed. An isolated pass-through benchmark measured 2.39 ns for the role-specific one-stage pipeline against 12.84 ns for the typed one, both at zero allocation.

Role executors therefore expose their named operation — `Mutate`, `Cross`, `Select`, `Evaluate` — and no second generic invocation path. Do not reintroduce a generic `Invoke`, a problem context carrier, default interface bridges between an invocation method and role methods, or generic wrapping and multi bases shared across roles. `IOperator<TOperation, TCandidate>` is unrelated to this rejection: it names the operation a configuration performs and carries no input, context or output model. Advice plumbing is also outside it: a wrapper that applies advice, observing or otherwise, may erase callback types internally, because it wraps role executors rather than replacing their contracts.

Reopening this would need a language or runtime mechanism that lets one generic implementation satisfy several nominal role contracts without a per-role leaf. Current C# has none.

The [operator role parameterization plan](operator-role-parameterization-rework.md) changes the consuming-slot premise to `IOperator<TOperation, TCandidate>` and reopens shared configuration/advice adapters on that basis. Package 1 adds descriptor adapters while preserving named execution methods. It does not restore universal execution invocation, source generation or advice expressed as configured wrappers; retry and consumer migrations must still justify their cost.

Re-examined on 2026-10-19 for Package 2b. A context argument in executor signatures remains rejected. Instead, every operation ends with its random number generator, search space and problem, and invocation wrappers can read and replace the random number generator and proceed on another executor of the same operation. Typed access to the search space and problem for generic wrappers was not adopted: such a wrapper lacks the run's types, an executor accepts only the run's exact problem type, and typed role advice already covers those arguments. Reopen that access only for a concrete generic wrapper that can produce a value of the run's type.

Outcome of the rework (2026-10-26): every configuration slot takes `IOperator<TOperation, TCandidate>`. An operation implements `IOperation<TSelf>` and supplies one invocation struct and one wrapping executor; general advice, naming, retry, per-invocation choice, counting and timing apply through them to built-in and external operations alike. Named operation methods remain, and no universal `Invoke`, context carrier or source generation was added.

### Candidate-work capability interface

Discussed on 2026-10-19 while settling the invocation contract for Package 2b. Moving `CountCandidateWork` from `IInvocation<TResult>` to a separate `ICandidateWorkInvocation<TResult>` would remove the throwing default. A local probe showed that the cast in a counting wrapper allocates nothing on .NET 10. It was not adopted: it adds a public type and a cast for authors of candidate-counting wrappers without removing the duplicated statement, because advice checks `SupportsCandidateWork` before any invocation exists. Deriving that flag from the invocation type would add a type parameter to every operation adapter. Defaults were selected instead: `SupportsCandidateWork` is false and `CountCandidateWork` throws unless an operation with candidate work overrides both. Reopen only if candidate work must vary per call or the duplicated statement causes real defects.

### Cast-free `ProceedWith`

Discussed on 2026-10-19 after implementing Package 2b. Each adapter's invocation casts the executor passed to `ProceedWith` from `IExecutor` to its executor interface, because generic wrappers such as `ChoosePerCall` hold their children as `IExecutor` and cannot name that interface. Adding the executor type to `IInvocation` changes every wrapper signature and still leaves the cast in the choice. Holding all children in a typed adapter array behind `ProceedWith(index)` slows the single-executor path and adds a choice-specific adapter member, which § 8.11 excludes. `Unsafe.As` turns a wrong executor into memory corruption. A protected helper on `OperationExecutorAdapter<TExecutor>` would hide the cast from descriptor authors at the cost of another extension member. The cast stays: only descriptor authors write it, once per operation, and it runs only when a wrapper passes an executor. Reopen only if measurement shows it matters on the choice path.

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

### Generic operator observations with type-erased call data

Implemented in the second Package 3 review of the [operator role parameterization plan](operator-role-parameterization-rework.md) and replaced in its third review on 2026-10-21. One `OperatorObservation<TOperation, TCandidate>` held each call's invocation and result as objects, and per-operation extension properties in `Inspection` read them through internal arguments interfaces and casts.

C# has no associated types. A descriptor such as `Mutation` is not generic in the candidate, so neither it nor `IOperator<Mutation, TCandidate>` can name a call's typed data, and one generic observation must convert it at run time. The generic type also could not declare a capability for some operations only. Measurements therefore listed the operations they read, which closed them to external operations (developer guidelines § 3.3), and `Analyzer` adapted the states interceptors return. Each operation now defines a typed observation, reached through its `Calls` handle. Reopen only if C# gains associated types or the per-operation observation code becomes a real maintenance burden.

### Per-operation trace overloads instead of observation sources

Discussed on 2026-10-21. Static extension members on `Analyzer`, declared once per operation, infer the typed observation and would keep `Analyzer.Trace(crossover, o => …)`; a compiled probe confirmed the value and measurement forms. Operator content must not reference Analysis, so they would need a layering exception. Every operation would also declare six forwarding overloads, one per trace form for one source and for a list, which made 54 for the built-in operations. They would forward to a public handle that external operations need anyway, and a new trace form would mean editing every operation. The `Calls` and `Iterations` handles were chosen instead.

### Input and result types on operation descriptors

Discussed on 2026-10-21. A descriptor `IOperationDescriptor<TSelf, TIn, TOut>` cannot state a candidate-dependent type: `Mutation` has no `TCandidate` in scope for `IReadOnlyList<TCandidate>`. Candidate-generic descriptors would require `IOperator<Mutation<RealVector>, RealVector>` in every slot, because C# does not infer a type argument from a constraint. Typed arguments and results live on the operation adapter base instead, which exists per run type. Reopen only if C# gains associated types.

### IObservable and Rx-style observation operators

Discussed on 2026-10-21. Observation sources resemble cold observables: `Calls` describes a stream of calls, readings resemble `Select`, best-so-far resembles `Scan`, and `Analyzer.Trace` and `Observing` resemble `Subscribe`. The library uses this only to explain the design.

Implementing `IObservable<T>` was rejected. Observation attaches through wrappers placed in the configuration before a run builds executors, and a `Subscribe` returning `IDisposable` would be a second way to attach to the same boundary; see parallel observation systems above. Rx's contract does not fit either. There is no completion, because runs pause and resume and one instrument can serve several runs. Failed calls are not observed. Delivery is synchronous, inside the call and after the clocks advance, which schedulers would break. A general operator set would add public surface and a delegate call per element on hot paths. `calls.Observing(subject.OnNext)` feeds an Rx subject without a library dependency. Reopen only for a concrete need that handles cannot express, such as combining sources over time.

### Measurements as separate strategies

Removed in the third Package 3 review on 2026-10-21. `IMeasurement<TInput, TValue>` records were immutable value strategies, and one measurement could read several kinds of source, such as evaluator calls and population states. Typed observations and handles replace them: a handle names its readings, as in `evaluator.Calls.ObjectiveVectors`, and `Read` builds custom ones, so `Analyzer.Trace` drops from fourteen overloads to four. Nothing relied on measurement value equality. Capability interfaces such as `IResultObservation<TResult>` or an offspring observation were considered to keep measurements open to external operations; readings on the handle made them unnecessary, and an offspring measurement named an evolutionary concept in a general mechanism. Reopen only if analysis definitions must be compared or serialized independently of their source.

### Readings and projections on observation sources

Implemented in the third and fourth Package 3 reviews and replaced in the fifth on 2026-10-23. `Readings<TValue>` was a second kind of source: a handle projected each observation into values plus the run's objective directions, through `Read` or a named reading such as `crossover.Calls.Offspring` or `evaluator.Calls.ObjectiveVectors`. `IProblemObservation<out TProblem>` let `Read` reach every record's objective. One source type with a `Reading<TValue>` value and a `Select` projection was also considered.

Objective directions matter only for ranking evaluation results, yet every reading carried them. Named readings of offspring or selections only collected values that a function on the record reaches directly. A projection operator also invites `Where` and the rest of LINQ, which points back to Rx; see IObservable and Rx-style observation operators above. Sources deliver their records, traces read them through a function, and ranking receives objective directions explicitly from that function, usually from the observed call’s problem. Reopen only if traces must combine sources of different observation types in practice.

### Operation arguments behind a shared invocation

Implemented in the third Package 3 review on 2026-10-22 and replaced on 2026-10-23. `OperationExecutorAdapter<TExecutor, TArguments, TResult>` built every operation's invocation around a private arguments struct implementing `IOperationArguments<TExecutor, TResult>` or `IItemOperationArguments<TExecutor, TItemResult>`. It saved about ten lines per operation, mutation shrinking from 67 to 57, but added two public interfaces, a third adapter base class and one more layer per call, and per-item choice boxed the arguments. Each operation now implements `IInvocation<TResult>` directly, and its operation type replaces the per-operation adapter classes. Reopen only if the repeated invocation code causes real defects.

### Spans in operator contracts

Discussed in the sixth review of the [operator role parameterization plan](operator-role-parameterization-rework.md) on 2026-10-24, after routed executors began handing each child an `ArraySegment` over the grouped inputs instead of a new array. Executor methods taking `ReadOnlySpan<T>` instead of `IReadOnlyList<T>` would avoid such copies everywhere, but a span cannot be stored, captured by a lambda, enumerated with LINQ, held across `await` or an iterator, or passed through Python interop. Observation records keep a call's inputs, so every observed call would copy its batch, and invocations holding the arguments would have to become ref structs. Contracts keep `IReadOnlyList<T>`; an implementation may hand on an `ArraySegment` over an array it owns, or use spans internally in hot loops. A sweep of production code found no other slice copy an `ArraySegment` replaces: the remaining ones slice sorted or lazy sequences, or inputs that need not be arrays. Reopen only if a benchmarked workflow shows list-based batches as its bottleneck; caller-provided output storage is the separate output-buffer item under optional capabilities.

### Untyped execution state on configuration contracts

Discussed in the factory review on 2026-10-23. Configurations would have offered `object? CreateExecutionState()` and `CreateExecutor(scope, object? state)`, with typed bases casting the state, to remove the factory object between a configuration and its executors. The developer rejected `object` in every configuration's public contract, even behind bases. Typed state variants of the bases avoided the cast, but leaves would have needed one variant per role. Each configuration now requests its own state from the scope of its build by passing itself, `scope.GetExecutionState(this, create)`, which the sixth review of the [operator role parameterization plan](operator-role-parameterization-rework.md) chose over a scope that remembers the configuration it builds. Reopen only if a configuration cannot pass itself to that request.

### Telling ranking where a record's problem is

Discussed in the factory review on 2026-10-23 and 2026-10-24. Ranking needs objective directions, which each record already holds through its call's problem, but generic trace code cannot read a property that no shared type declares. These routes were rejected:

- A `problem` function on every source, accepted first, which made every source definition know about problems although only ranking uses them.
- A shared `Observation` base record holding search space and problem untyped. A required base forces consumer-defined roles, such as a cooling schedule, to pass a problem their calls never use; an optional one is `IProblemObservation` again.
- Taking the problem from the run, which is wrong when one run calls operators with different problems, as `DynamicRacingAlgorithm` does; a context parameter beside every record, which puts the problem into the core observation path; and an ambient current call, which is hidden global state.
- Restarting best-so-far, or comparing the old best under the new directions, when the objective changes. The comparison reports neither the best overall nor the best since the change, and a restart silently alternates when two objectives interleave in one source.

Ranking now takes the directions as an argument, passed by the trace's function. Reopen only if direct ranking traces become common enough that passing `o.Problem.Objective` is a burden.

### Cached move operators on neighborhoods

Discussed and rejected on 2026-10-21, while making advice on a neighborhood's move operators reach the resolved instance.

A neighborhood handed out a new `NeighborhoodCreator`, `NeighborhoodApplier` or `NeighborhoodEvaluator` on every property read. Caching them in the neighborhood does not work cleanly, because a neighborhood is a record. A cached operator referring back to its neighborhood joins value equality, which then recurses, and `with` copies an operator that still refers to the original. Avoiding both needs hand-written equality, hashing and a copy constructor on the base, plus thread-safe lazy initialization.

A neighborhood is instead its own creator, applier and evaluator; see the move follow-up in the [operator role parameterization plan](operator-role-parameterization-rework.md).

### Trace storage structures

Measured during the analysis rework in September 2026, at one million samples unless stated.

`List<T>.Add` costs 1.9 ns, or 17.3 ns under a lock, which is 17 ms per million appends. Copying to publish costs 1.4 ms for a thousand samples polled a thousand times and 111 ms for a hundred thousand samples polled a thousand times. `ImmutableList<T>` gives free snapshots but needed 467 ms and 1241 MB to append a million samples, because it allocates rebalancing nodes on the operation a trace performs most. A lock-free append-only log appended the same in 5.5 ms and 23 MB and published in constant time, but its prototype had a bug in its barrier-sensitive code.

Traces keep a list under a lock and copy it when a snapshot is taken. Reopen only when profiling shows snapshot copying to be a real cost, and then compare the append-only log, not `ImmutableList<T>`.

### Detecting observation sources that never resolve

Tried during the analysis rework in September 2026 and removed.

An observation source selected by reference is not matched when a configuration is copied with `with` after an analyzer was attached, so its trace stays empty without a word. Failing at run creation cannot work: aspects are attached before resolution, and a meta-algorithm may resolve a child only later in the run. Failing after execution comes too late to save that run. A queryable read was built instead, a flag each observation module set when it was resolved. It was removed because it covered observation modules and no other decoration, and relied on every module author remembering to set it.

Under eager substitution, [D57](core-simplification-plan.md#decided-in-round-21) takes another route for analyzers: `run.Result` walks the declared graph, which covers every wrapper and asks nothing of analyzer authors beyond declaring parameters, and it stands aside at delegate slots to account for deferred construction.

Reopen only for a concrete diagnostic need. Any replacement should track matching in the resolver's common advice-application path and account for deferred resolution, rather than require each analyzer to report consumption. Empty pointcut selections are valid, so define an opt-in expectation before diagnosing an unmatched selection. Current matching behavior is described in [execution resolution](../docs/contributing/architecture/execution-resolution.md#pointcuts-advice-and-join-points).

### Analyzer cleanup contracts

Removed during the analysis rework in September 2026.

The earlier framework gave every analyzer a run state, `IAnalyzerRunState`, which the run created and released, and dynamic analysis added `IDisposable` to it to unsubscribe from the dynamic problem's evaluation event. A public registration lifetime with a release hook was then drafted to make that cleanup explicit.

Neither survived. The only analyzer that ever acquired anything was dynamic analysis, and only because the problem published its evaluations through an event. Once the problem owned its batch boundary and the event went away, no analyzer needed releasing, so the concept was removed rather than reshaped. Reopen only for an analyzer that acquires an external resource.

### Namespaces, folders or assemblies by layer

Discussed after the layers overhaul in September 2026: whether the six layers should become top-level namespaces, grouping folders or separate assemblies, for example one home for the domain vocabulary now spread over `SearchSpaces`, `Objectives`, `Random` and `Problems`.

Layers and namespaces answer different questions. Layers direct dependencies for contributors; namespaces are where users find a concept. The layers cut through the concept namespaces instead of grouping them: `Algorithms` and `Operators` hold L1 contracts beside L3 implementations, `Execution` spans four layers, and each encoding is a vertical slice of one representation. A layer-first namespace would make users import two namespaces for one concept, and a grouping folder above a namespace folder contradicts the rule that grouping folders sit beneath it. Assemblies fail harder: configuration creation names `ResolutionScope` and resolution names configurations, so L1 and L2 would reference each other. A hosting assembly would have to take `AlgorithmExtensions` and `RunTraceExtensions` with it, because both reference `AlgorithmRun` from the main assembly's `Algorithms` and `Analysis`. The Contracts assembly had just been folded into the main one for similar reasons. The architecture test enforces the dependency rules that matter, including a floor rule for domain vocabulary. Unlike an assembly boundary it checks only those rules, so each rule needs a probe that shows it fails.

Reopen only if a layer gains an audience of its own, such as a separately distributed package with a consumer that must not see the layers above it.

### A marker interface naming what a configuration is written for

Tried and reverted on 2026-10-25, in the Package 4 review of the [operator role parameterization plan](operator-role-parameterization-rework.md). Once algorithm resolution checked `Fits` first, the bound bases' own mismatch message, which names the types a configuration is written for, was no longer reached. An internal `IDeclaresSupportedExecution` on the operator and algorithm bases brought that wording back into the fit report.

The developer rejected it: an interface added to every authoring base to improve one message. The fit report names each configuration at fault, with its path and the run it cannot serve, which is the information the check owes. Reopen only if users cannot tell from that report what to change.

### Detaching the bound operator bases from the agnostic one

Proposed and rejected on 2026-10-25, against Sonar S4019 after the typed scopes began deriving from `ResolutionScope`. The bound bases would implement `IOperator<TOperation, TCandidate>` themselves, so they would inherit no run-generic hook for the leaf hook to hide. The hierarchy of authoring bases is deliberate, and removing or bypassing a base is not a fix for a warning about it. The two bound bases suppress S4019 instead; author code never triggers it.

### A library advice base keyed on the executor type

Tried and replaced on 2026-10-26, in the Package 4 review of the [operator role parameterization plan](operator-role-parameterization-rework.md). Resolution of a consumer-defined role knows only the executor type, while the author's advice names the configuration type, so resolution needs an entry it can call with the executor type alone. First an internal `IConfigurationNodeAdvice<TExecutor>`, then a public base `ConfigurationNodeAdvice<TExecutor>` between `Advice<TConfiguration>` and `Advice<TConfiguration, TExecutor>`, provided it.

The developer rejected both: a type in the hierarchy that exists only so resolution can find the advice. The base also made role advice an `Advice<IConfigurationNode<TExecutor>>`, so `Aspect.Create` accepted a pointcut for another role with the same executor type. An internal untyped `WrapNode` on the root `Advice` replaces it. Reopen only if resolution comes to know a consumer role's configuration type.

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
