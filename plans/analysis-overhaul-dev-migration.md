# Migrating the analysis overhaul onto dev

Type names below predate the resolution-scope rename: `ExecutionInstanceResolver` and `TypedExecutionResolver` are now
`ResolutionScope` at different arities, `ExecutionInstanceResolverBuilder` is now `ResolutionScopeBuilder`,
`IExecutionInstanceResolvable` is now `IExecutionConfiguration`, `IExecutionHook` is now `IExecutionModule`, and
`TypedObservationBuilder` was deleted. The text is left as written.

## Summary

`analysis-overhaul` and `dev` diverged at `9b44006a`. Both reworked the execution-instance resolution contract,
in incompatible directions, so a plain merge yields 169 conflicted paths and does not build.

dev is merged into `analysis-overhaul` so the branch benefits from dev's work, and the overhaul continues from
there. Where both sides changed the same thing, dev's version is kept unless the overhaul made a deliberate
behavioural change.

The result is a single merge commit on `analysis-overhaul` that merges dev into it. That commit is assembled step by
step on a workbench branch, `analysis-overhaul-migration`: each slice carries one part of the overhaul across and is
committed as a checkpoint once it builds and passes its gate. At the end, the merge commit takes the last
checkpoint's tree. The workbench's commits never become part of the history.

The workbench starts from dev's tree. That choice concerns only where the work is assembled, not the direction of the
merge: dev's reworks are kept by default, and every change reviewed on the workbench is an overhaul change being
adapted onto them. Starting from the overhaul's tree instead would first require one checkpoint that brings in all of
dev at once, resolving all 169 conflicts, before any slice could begin.

The per-file detail lived in a companion ledger, one row per path with what the migration does to it and the slice
that finishes it. It was removed once every row was ticked.

## Decisions

| #  | Question | Decision |
| -- | -------- | -------- |
| 1  | `ExecutionInstanceResolver` name collision | The overhaul's resolver keeps the name and its behaviour, including the ordering fixes. dev's typed facade becomes `TypedExecutionResolver` (provisional). |
| 2  | Generic vs non-generic `IExecutionInstanceResolvable` | Keep both. The non-generic interface stays the root and carries dev's `Fits(ExecutionSignature)`; the generic one derives from it. |
| 3  | Observation model | The overhaul's: `Observing*` and execution hooks replace `Observable*` and the `I*Observer` family. |
| 4  | `IAnalyzer` | The overhaul's `Install(ExecutionInstanceResolverBuilder)`. |
| 5  | Dynamic problems and epochs | dev as the base, the overhaul's epoch model applied on top. |
| 6  | Analyzer typing | Analyzers follow dev's reduced arity. Where that leaves an analyzer untyped in the search space or problem, compatibility is decided at resolve time by the check operators already use. |
| 7  | Naming collisions | dev's renames stay unless listed otherwise below. |
| 8  | Building during the migration | Every checkpoint builds on its own, because each overhaul file enters in the slice that makes it compile. |
| 9  | Direction | dev is merged into `analysis-overhaul`. The overhaul branch continues afterwards. |
| 10 | History | One merge commit on `analysis-overhaul` with two parents: the `analysis-overhaul` tip and the dev commit the checkpoints were built on. Checkpoints live on a workbench branch that starts from dev's tree; they are working state, not history. |
| 11 | Git operations | Claude changes the working tree only. Philipp reviews by staging what he approves, commits a checkpoint when he considers a slice done, and runs every git command. |
| 12 | Resolver keying | Decorations, the instance cache and `Decorate` key on the non-generic interface. Resolution takes dev's create-function shape with the run signature; the generic `Resolve<T>` stays as a convenience. |
| 13 | dev's old analyzers | They cross the contract swap through a legacy shim, are compared against their replacements, and are deleted together once every replacement exists. |

## Naming

### Resolved

| Base | dev | Overhaul | Taken |
| ---- | --- | -------- | ----- |
| `WithAnalyzer` / `WithAnalyzers` | `AttachAnalyzer` / `AttachAnalyzers` | `AddAnalyzer` | **`AddAnalyzer`**, a deliberate change of this branch |
| `ExecutionInstanceResolver` | typed facade over the registry | the decoration-tree resolver | **overhaul** keeps the name; dev's facade becomes `TypedExecutionResolver` |

### dev renames that stay

| Base (overhaul kept) | dev |
| -------------------- | --- |
| `WithMax{Role}{Calls,Duration,Candidates}` (24 methods) | `LimitedTo…` |
| `WithMaxAlgorithmDuration` | `LimitedToDuration` |
| `WithMaxIterations` | `TerminatedAfterIterations` |
| `WithTerminator` | `TerminatedBy` |
| `WithCache` | `Cached` |
| `WithElites` | `CombinedWithElites` |
| `WithImprovementCheck` | `CheckedForImprovement` |
| `WithRate` | `AppliedAtRate` |
| `WithRefinement` | `AppliedAfterRefinement` |
| `WithRelativeQuality` / `WithDynamicRelativeQuality` | `ScaledToBestKnown` / `ScaledToDynamicBestKnown` |
| `WithPredefinedCandidates` | `SeededWith` |
| `Count{Role}Calls` / `Count{Role}Candidates` (16) | `CountCalls` / `CountCandidates` |
| `Measure{Role}Duration` (9) | `MeasureDuration` |
| `CreateDefault{Creator,Crossover,Mutator}` | `TryCreateRecommendedOperator` |
| `WithWithWeightedSum` | `WithWeightedSum` |

### Overhaul renames dev does not contest

`GenealogyAnalysis` → `GenealogyAnalyzer`, `RankAnalysis` → `RankAnalyzer`,
`ParetoFrontAnalysis` → `ParetoFrontAnalyzer`, `HyperVolumeAnalysis` → `HyperVolume`,
`PopulationSimilarityAnalyzer` → `PopulationSimilarity`,
`BestBeforeChangePerformanceAnalysis` → `BestBeforeChangePerformanceAnalyzer`, `Observable*` → `Observing*`.

## Concept moves to carry

dev moved several concepts rather than deleting them. An overhaul edit to the old name is re-expressed against the
successor, never dropped because the file is gone.

| Overhaul edited | dev moved it to | Nature |
| --------------- | --------------- | ------ |
| `IEncodingDefaults`, `IEncodingDefault{Creator,Crossover,Mutator}`, `IProblemDefaults`, `IProblemDefault{Creator,Crossover,Mutator}` | `IRecommends<TOperator>` + `OperatorRecommendationResolution` | eight static-abstract interfaces collapsed into one instance-level interface and a runtime resolver |
| `IStatefulProblem` | `IDynamicProblem` | both branches renamed it to `IDynamicProblem`, with different content |
| `RealVectorSearchSpace` | `BoundedRealVectorSearchSpace` | rename; reaches the overhaul's `GaussianMutator` edit |
| `IVariableStrengthMutator(Instance)` | `IAdaptableMutationStrengthInstance` | rename and reshape |
| `RealVectorSearchSpaceRandomExtensions` | `BoundedRealVectorSearchSpaceRandomExtensions` | rename |
| `EncodingAndProblemDefaultSpecs` | `OperatorRecommendationSpecs` | rename and rewrite |
| `ExecutionInstanceResolvableExtensions` | folded into the registry and `For<>()` | removal |

The overhaul's only edit to `IEncodingDefaults` is a terminology change, from "Operators are matched by reference
where they anchor an observation" to "Observation sources are matched by reference". It belongs on
`IRecommends<TOperator>`, which still carries the old wording.

Two overlaps need no decision. Both branches introduced the same non-generic `IProblem` with
`ObjectiveDirections Objective`; only the overhaul's doc summary differs. And git's rename detection already follows
file-level renames, such as the overhaul's edits to `VariableStrengthMutatorTests` belonging in dev's
`AdaptableMutationStrengthTests`. It does not follow concept-level moves like the one to `IRecommends`.

## What the investigation found

These facts decide the procedure. Each was checked against the branches or in a throwaway repository, not inferred.

- **A single merge commit keeps attribution.** In a merge commit, a line identical to either parent is blamed on
  that parent's history; only lines that differ from both are blamed on the merge. A line dropped in one checkpoint
  and re-applied verbatim in a later one is still attributed to the original overhaul commit. Checked in a
  throwaway repository, conflict included.
- **The final merge can be built from a checkpoint.** `git merge --no-ff --no-commit <dev>` followed by
  `git restore --source=<checkpoint> --staged --worktree -- :/` resolves every conflict, applies deletions, and leaves
  a tree identical to the checkpoint; the commit gets exactly the two intended parents.
- **Missed carries are detectable.** Merging the overhaul into a result with `git merge-tree` (merge base
  `9b44006a`) leaves the overhaul hunks the result lacks as clean changes, and the places where the result
  deliberately differs as conflicts. dev's own changes never appear. In a throwaway repository this caught a
  forgotten line inside a file that also held a deliberate adaptation, and a forgotten file. One blind spot: a
  deletion the overhaul made in a file dev modified shows only in git's conflict list, not in the tree; 33 of
  today's 36 modify/delete conflicts are invisible in the diff. The check therefore reads both.
- **Conflicts are substantive.** 169 conflicted paths: 131 content, 33 deleted by the overhaul and modified by dev,
  3 the reverse, 2 added on both sides. Ignoring whitespace removes one. dev's 156-file formatting commit `e7f5271c`
  is mostly re-wrapping, not whitespace.
- **Nineteen paths merge silently.** Both sides changed them and git raises no conflict, so they are carried like
  conflicts.
- **The overhaul edited 35 files dev never touched.** Among them are all 18 `Stateful*`/`Stateless*` operator bases,
  `TrialAnalyzer` and `ExecutionStream`. They reference the overhaul's contracts, so they enter in the slice that
  provides those contracts, not earlier.
- **Some overhaul deletions would break dev.** The overhaul deleted `EvaluationTiming`, `EvaluationClock` and
  `IEpochClock`; 19 dev files use them. They retire in S6.
- **dev made doc-comment errors fatal.** CS1570, 1571, 1572, 1574, 1580, 1584, 1587, 1710 and 1711 are errors on
  dev. Stale `cref`s and `<typeparam>` tags from the arity reduction surface as build errors, which makes the
  compiler the to-do list.
- **dev's role interfaces are not generic resolvables.** `IMutator<TCandidate>` has a generic method
  `CreateExecutionInstance<TRunSearchSpace, TRunProblem>`; its instance type exists only once a run's types are
  known. The overhaul keys decorations on `IExecutionInstanceResolvable<IExecutionInstance>` and constrains
  `Decorate` to it, so it cannot key a dev operator. Hence decision 12.
- **dev's observation already respects the overhaul's ordering rule.** `AlgorithmRun.StartExecution` fills an
  `ObservationPlan` and installs it into a fresh registry before anything is resolved. The four users of
  `RegisterReplacement`/`RegisterInstance` are that plan, the two budget algorithms (which the overhaul already
  ported to `CreateChildResolver(b => b.Decorate(...))`) and `DynamicRacingAlgorithm`. The registry can therefore
  be swapped for the resolver behind a small adapter.
- **Line endings are pinned.** `.gitattributes` forces LF for code and Markdown and CRLF for `.ps1` and `.bat`,
  with `core.autocrlf` on. Carried files are written through the checkout filters so they match what git expects.
- **The overhaul made dynamic evaluation batch-only.** `DynamicProblem.Evaluate` takes a batch and the
  single-candidate overload is private, so an epoch cannot advance inside one evaluation. Tests encode it as
  `Evaluate([x])[0][0]`.
- **The slice order is consistent.** A type-level dependency graph over the overhaul's new compiled files shows no
  file entering before something it depends on.
- **`analysis-overhaul` exists only locally.** It has never been pushed; the tag `analysis-overhaul-backup` marks
  `ee39bfa0`.

## Invariants

1. Every checkpoint builds and passes its gate.
2. The final merge commit's tree is the last checkpoint's tree. Its parents are the `analysis-overhaul` tip and the
   dev commit the checkpoints contain.
3. Transitional code exists only in checkpoints: the legacy shim, the `ObservationPlan` adapter and the differential
   tests are gone by S7, so the merge commit carries none of it.
4. Before the merge commit, the carry check finds nothing left to carry: no clean leftover hunk, no unexplained
   conflict, and every test method the overhaul introduced is present or its absence is documented.
5. Overhaul lines are carried verbatim wherever dev's API allows. A verbatim line keeps its original attribution.

## Roles

Claude changes files in the working tree and nothing else, then runs the gates and reports. New files come from
the overhaul with `git cat-file --filters analysis-overhaul-backup:<path> > <path>`, which applies the same
line-ending and encoding rules as a checkout; edited ones are re-applied with `git merge-file`; moves and deletions
use plain `mv` and `rm`.

Claude never runs a command that changes the index, refs, the checkout, the stash or the remote: no `git add`,
`git rm`, `git mv`, `git restore --staged`, `git reset`, `git stash`, `git apply --index`, `git switch`,
`git checkout`, `git merge`, `git commit` or `git push`. Temporary worktrees only when Philipp asks for one; dry
runs use `git merge-tree`, which touches no worktree or index.

Philipp reviews by staging what he approves. Once part of a slice is staged, Claude keeps working on top of it, so
`git diff` shows exactly what is new since the last approval. Philipp commits a checkpoint when he considers a slice
done, and runs every git command in the procedure below.

## Procedure

### Step 0: safety net (done)

The tag `analysis-overhaul-backup` marks the overhaul at `ee39bfa0`. It is also the source Claude reads overhaul
content from, so that source cannot move.

`analysis-overhaul` stays frozen until the merge commit. The merge commit takes its tree from the checkpoints, so any
commit made on `analysis-overhaul` in the meantime would be undone by it unless it is carried into the checkpoints
first.

Optional, repository-local:

```bash
git config merge.conflictStyle zdiff3
```

### Step 1: workbench branch

```bash
git switch -c analysis-overhaul-migration dev
```

Claude copies the plan and the ledger into the working tree, and Philipp commits them as checkpoint 0. The ledger is
ticked and the behaviour-differences log is filled on the workbench, so the final tree carries their last state.

### Step 2: slices

Each slice follows the same routine.

1. Claude carries the slice's ledger rows across. A new file comes from the overhaul and has its compile errors
   fixed in place. An edited file gets a three-way re-application with `git merge-file` (base: the merge-base
   version, ours: the current file, theirs: the overhaul's version) and is then adapted to dev's API. Mechanical
   churn is done with a codemod first and compile errors fixed afterwards.
2. Claude runs the carry check (step 3) for the slice's ledger rows. No clean leftover may remain on those paths.
   Every conflict hunk goes into the slice report with base, overhaul and result side by side and a one-line
   reason, or into the behaviour-differences log.
3. Claude runs the gate and reports the result.
4. Philipp reviews by staging, and commits the checkpoint when the slice is done.

The overhaul's tests come across with their assertions unchanged. Only their API shape follows dev: arity, renames,
moved types. A changed assertion is a behaviour difference and goes into the log, so a passing overhaul test means
the behaviour it pins down survived.

Gates, following AGENTS.md:

| Gate | Commands |
| ---- | -------- |
| core | `dotnet build --configuration Release --no-restore` and `dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore` |
| specs | `dotnet test --project test/HeuristicLib.Tests.ApiUsageSpecs/HeuristicLib.Tests.ApiUsageSpecs.csproj --configuration Release --no-restore` |
| experimental | `dotnet test --project test/HeuristicLib.Tests.Experimental/HeuristicLib.Tests.Experimental.csproj --configuration Release --no-restore` |
| scenarios | `dotnet test --project test/HeuristicLib.Tests.Scenarios/HeuristicLib.Tests.Scenarios.csproj --configuration Release --no-restore` |
| full | `dotnet test --configuration Release --no-restore` |
| format | `dotnet format whitespace`, `style` and `analyzers` on `./HEAL.HeuristicLib.slnx` with `--verify-no-changes`, as in AGENTS.md |

#### S1: resolution contract

Brings in 3 files; 112 ledger rows finish here. The overhaul's resolver becomes the only resolution mechanism, and
everything dev built on the registry runs on it without behavioural change.

Two checkpoints, so each stays reviewable:

**S1a, contract (additive).**
- dev's facade `ExecutionInstanceResolver<…>` and its four-parameter subclass become `TypedExecutionResolver<…>` in
  `TypedExecutionResolver.cs`: 14 files, 18 occurrences, plus `cref`s. `ExecutionInstanceRegistryResolverExtensions.For<>()`
  keeps its shape.
- The non-generic `IExecutionInstanceResolvable` with `Fits` stays the root, and the overhaul's generic
  `IExecutionInstanceResolvable<out TExecutionInstance>` derives from it.
- The overhaul's `ExecutionInstanceResolver`, `ExecutionInstanceResolverBuilder`, `Decoration`, `DecorationOrigin` and
  `IExecutionHook` come in, re-keyed on the non-generic interface. The primary entry point takes dev's shape,
  `Resolve<TResolvable, TInstance>(TResolvable, Func<TResolvable, ExecutionInstanceResolver, TInstance>)`, and
  applies the decoration chain before creating. `Resolve<T>` and `ResolveOptional<T>` become conveniences over it.
- dev's rule that a resolver serves one execution signature, and its `ExecutionSignature.Mismatch` diagnostics,
  carry over.
- The registry still exists; nothing uses the resolver yet.

**S1b, switch-over.**
- `TypedExecutionResolver` wraps the resolver; dev's role `Resolve` extensions forward unchanged.
- Operator bases, including the 18 `Stateful*`/`Stateless*`: dev's `Fits` and run-type bridge stay, and the
  parameter becomes `ExecutionInstanceResolver resolver`.
- Meta-algorithms: `CreateChildRegistry()` becomes `CreateChildResolver()`. The budget algorithms move from
  `RegisterReplacement` to `CreateChildResolver(b => b.Decorate(…))`, as the overhaul already has it.
  `DynamicRacingAlgorithm`'s `RegisterInstance` follows the overhaul's port.
- Transitional adapter: `ObservationPlan` installs through `ExecutionInstanceResolverBuilder.Decorate`, so dev's
  analysis stack keeps running. It is deleted in S7.
- About 56 source files resolve through the registry today; a codemod handles the call sites.
- The Roslyn analyzers and code fix (`CreateExecutionInstanceAnalyzer`, its code fix, `OperatorAuthoringAnalyzer`)
  recognise the resolver.
- `ExecutionInstanceRegistry` and its tests are deleted. Any registry test whose behaviour
  `ExecutionInstanceResolverTests` does not cover is ported first.
- Carries: `GaussianMutator` onto `BoundedRealVectorSearchSpace` and `IAdaptableMutationStrengthInstance`;
  `IProblem`'s doc summary; `AdaptableMutationStrengthTests`.
- To decide in S1b: whether the overhaul's `CreateExecutionInstance(out resolver)` convenience survives, now that dev
  removed `ExecutionInstanceResolvableExtensions`.

Gate: core, specs, experimental. dev's analysis tests still pass at this point, and they now run through the
overhaul's ordering. That makes dev's whole analysis suite the regression test for the swap.

#### S2: observation primitives

Brings in 10 files; additive only.

- `Observation`, `AlgorithmObservation` and `Tracing/Observations/*` at dev's arity: observations wrap role
  interfaces such as `IMutator<TCandidate>`, and their typed members come from the run signature.
- Where an observation needs the search space or problem type, compatibility is checked at resolve time through
  `ExecutionSignature.Fits` and `Mismatch`, not by a parallel mechanism.
- `Observable*` stays, because the legacy stack needs it until S7.
- The overhaul's `OperatorInstrumentationTests` and `EvaluatorInstrumentationTests` come in; dev's originals, from
  which the overhaul renamed them, stay until S7.
- Carry: the terminology line onto `IRecommends<TOperator>`.

Gate: core, specs.

#### S3: analyzer contract, run lifecycle, legacy shim

Brings in 1 file.

- The overhaul's `IAnalyzer.Install(ExecutionInstanceResolverBuilder)` and `AddAnalyzer` on runs and experiments.
- The overhaul's `AlgorithmRun`, `ExperimentRun`, `TrialAnalyzer`, `RunLifecycleState`, and the single-consumption
  guard in `ExecutionStream`.
- Shim: dev's `IAnalyzer` and `IAnalyzer<TResult>` become `ILegacyAnalyzer` and `ILegacyAnalyzer<TResult>`.
  `AddAnalyzer(ILegacyAnalyzer)` creates the run state, fills an `ObservationPlan` and installs it through the S1
  adapter; `GetResult(ILegacyAnalyzer<T>)` reads the stored state. Marking the legacy types `[Obsolete]` lists every
  remaining use as a warning; warnings do not fail this build.
- Carry, moved here from S2: the `RunLifecycleState` assertions in `ExperimentRunTests` and `ExperimentExecutionTests`.

Gate: core, specs, experimental.

#### S4: tracing core

Brings in 20 files.

- The tracing subsystem at dev's arity. `Analyzer.Trace` currently takes five or six type parameters; the search
  space and problem come off, with the resolve-time check from S2 where a measurement needs them.
- dev's legacy `Analyzer` is a plain `static class` in `Analyzer.Factory.cs`, while the overhaul's parts are
  `partial`. The legacy declaration gains `partial`; no member names collide.
- Differential tests, same seed and same run: `BestMedianWorstAnalysis` and `BestMedianWorstPerEvaluationAnalysis`
  against `BestMedianWorstTrace` on the iteration and evaluation clocks; `BestQualityAlgorithmAnalysis` against
  `BestQualityTrace`.
- Core usages in tests, specs and examples move to traces.
- Carry: the overhaul's edits to `EncodingAndProblemDefaultSpecs` into `OperatorRecommendationSpecs`.
- As done: every trace overload keeps the search space and problem as method type parameters, inferred from a
  measurement and checked when the run resolves, so a measurement typed at a concrete problem still works. The
  lambda-based overloads also have a twin observing at `ISearchSpace<TCandidate>` and
  `IProblem<TCandidate, ISearchSpace<TCandidate>>`, which an implicitly typed lambda binds to without type arguments.
  The built-in measurements, clocks and trace extensions are typed at those interfaces, as dev's legacy factories were.

Gate: core, specs.

#### S5: experimental analyzers and Python interop

Brings in 9 files. Genealogy, rank, Pareto front, hypervolume, population similarity and population traces, plus the
Python genealogy and correlation analyses.

Differential tests: each old analyzer against its same-named successor; `AllPopulationsAnalysis` and
`AllObjectiveVectorsAnalysis` against the population traces; `BestPerEvaluationAnalysis` against best-so-far with
on-change retention.

Gate: core, experimental. The Python interop scenario runs only where a Python environment is available.

As done:
- The analyzer classes keep the overhaul's search space and problem type parameters, and their sources take dev's
  role interfaces; `Install` names the observation's types explicitly (the S9 item on explicit generic arguments).
  The `Analyzer.Genealogy`, `Analyzer.Rank` and trace factories drop the search space and problem and type them at
  `ISearchSpace<T>` and `IProblem<T, ISearchSpace<T>>`, as S4 did for the built-in traces.
- `RankState`, `HyperVolumeState` and `ICandidateSimilarityCalculator` are identical on both sides. The overhaul's
  files now declare them, and the legacy `RankAnalysis`, `HyperVolumeAnalysis` and `PopulationSimilarityAnalyzer`
  files lose their copies.
- The Python genealogy analysis observes the algorithm instead of an interceptor, as the overhaul does, and keeps dev's
  explicit `AlgorithmRun` construction.
- `LegacyExperimentalAnalyzerDifferentialTests` pins every old analyzer to its successor on one run. All eight record
  the same values: the genealogy graph, the ranks, the Pareto front, the hypervolume, the similarity matrices and
  averages, all populations, all objective vectors, and the last best candidate. The on-change best-so-far trace keeps
  a subset of the legacy quality curve, because the curve records every improvement inside a batch.

#### S6: dynamic problems and epochs

Brings in 5 files.

- dev's `IDynamicProblem` as the base, with the overhaul's `IEpochSchedule`, `CurrentEpoch`, `OnEpochChange`,
  `EvaluationCountSchedule`, `EpochClock`, `EpochWork` and `BestBeforeChangePerformanceAnalyzer` on top.
- Batch-only evaluation in `DynamicProblem` stays; the one-firing-one-sample rule depends on it.
- `EvaluationTiming`, `EvaluationClock` and `IEpochClock` retire.
- Differential comparison for `DynamicAnalysis`, `QualityCurvePerEpochAnalysis`, `InvalidPerEpochAnalysis` and
  `BestBeforeChangePerformanceAnalysis`. Differences are expected, because the overhaul changed the timing model on
  purpose; they are recorded, not chased.

Gate: core, experimental, scenarios.

As done:
- The four legacy dynamic analyzers retire here rather than in S7, and their rows moved. They are built on dev's
  evaluation log (the problem registered as an evaluator observer, `EvaluationTiming` with its `Valid` flag,
  `OnEvaluation`), which the overhaul's `DynamicProblem` removes, so they cannot run beside their successors. The
  differential tests are therefore a written comparison in the behaviour-differences log below. dev's two tests of
  them go with them; the overhaul's nine successor tests come in.
- `DynamicProblem` is the overhaul's, with dev's `TSelf` base. `CreateIterationUpdateHook` takes dev's
  `IAlgorithm<TCandidate, TSearchState>`. `IDynamicProblem` is the overhaul's; dev's `EpochClock` and `OnEvaluation`
  members retire with the evaluation log.
- `EpochClock`, `TraceEpochWork` and `BestBeforeChangePerformanceAnalyzer` read the problem through
  `IDynamicProblem`, which carries everything they read, because naming `DynamicProblem` would now need `TSelf`.
  `TraceEpochWork` drops its `TProblem`, which could not be inferred at dev's arity.
- dev typed `DynamicCachingEvaluator.SourceProblem` as `IDynamicProblem`, but the overhaul's `RequestUpdate` is
  internal to `DynamicProblem` on purpose. An internal `IUpdateRequestable`, implemented explicitly, gives the caching
  evaluator that call without making it public.
- `DynamicRelativeQualityEvaluator` reads the epoch through its source problem, because dev's run-type bridge types
  the evaluated problem as the run's; the evaluator already checks that both are the same instance.
- The AutoEC scenario keeps `var evaluator = problem.CreateEvaluator()`: the overhaul typed it for the concrete problem
  only because decorations had to agree on the problem type at its arity.

#### S7: legacy teardown

Deletes the shim, `IAnalyzerRunState`, `ObservationPlan` and its extensions, the S1 adapter, the nine `Observable*`
operators and `ObservableAlgorithm` with their extension classes, the `I*Observer` and `Action*Observer` families, all
legacy analyzers with their tests (except the four dynamic ones, retired in S6), and the differential tests. The compiler proves nothing still uses them.

Also carries the overhaul's deletions of Observable-specific tests and assertions, moved here from S2 because the
Observable types they cover live until this slice: `InferenceConstructionSpecs`, `OperatorAuthoringSpecs`, the six
`*ConfigurationEqualityTests`, `RefinerBatchSemanticsTests` and `RefinerCompositionTests`. dev's
`ObservableOperatorCounterTests` and `ObservableEvaluatorTests`, reduced in S2 to their Observable tests, are deleted
whole. From S4: the Observable tests the overhaul deleted in `EvaluatorConfigurationEqualityTests`,
`InterceptorConfigurationEqualityTests` and `ReplacerConfigurationEqualityTests`. The overhaul removed
`WrappingConcerns_IncludeChildAndSettingsInEquality` whole although only one of its assertions uses an observer; keep
the counting and duration assertions. `LegacyAnalyzerShimTests` and `LegacyAnalyzerDifferentialTests` go with the shim.
From S5: `LegacyExperimentalAnalyzerDifferentialTests` goes too, and `AssemblyDependencyTests` drops its namespace check
of `src/HeuristicLib/Analysis/Quality`, as the overhaul did. The check stays until then because the folder holds
`BestQualityAlgorithmAnalysis`, and `Directory.EnumerateFiles` throws once the folder is gone.

Gate: full.

As done:
- `AlgorithmRun` and `ExperimentRun` are the overhaul's files with dev's arity; the shim members, fields,
  `LegacyAnalyzerInstallation` and the lifecycle disposal of legacy states are gone with `ILegacyAnalyzer`,
  `LegacyTrialAnalyzer`, `TrialAnalysisResult` and the legacy `Analyzer<TResult>`.
- `WrappingConcerns_IncludeChildAndSettingsInEquality` keeps its counting and duration assertions in
  `InterceptorConfigurationEqualityTests` and `TerminatorConfigurationEqualityTests`; only the observer assertion goes.
- dev's `AlgorithmInterfaceCapabilityTests`, which the overhaul does not have, probed `held.ObserveWith(...)`. It now
  probes the successor with the same requirement, `held.TracePopulationQuality()`, which likewise compiles only when
  the algorithm names its search state.
- dev's `PrefixingWrappingCreator` doc comment in `OperatorAuthoringSpecs` compared itself to `ObservableCreator`; the
  sentence ends before that reference.
- The emptied folders `Analysis/Quality` and `Analysis/Anchors` are removed from the working tree; git tracks neither.
- The only remaining mention of the removed API is `docs/contributing/architecture/analyzers.md`, an S8 row.

#### S8: documentation, plans and package metadata

The glossary (dev never touched it), `execution-resolver.md` rewritten for the merged contract, `analyzers.md`,
`observability-and-analysis.md`, the authoring guides and examples, the overhaul's plan files, the backlog, and the
overhaul's `PackageProjectUrl` in `Directory.Build.props`.

Gate: format.

As done:
- The overhaul's plan files, `analyzers.md`, `execution-resolver.md` and `observability-and-analysis.md` come across
  verbatim or nearly so. dev's only edits to the last two were the `AttachAnalyzer` rename, which the overhaul's
  rewrite supersedes. The plans record the overhaul's design history and keep its arity.
- `execution-resolver.md` gains a section on what a resolver resolves: keying on the non-generic interface, the
  create-function entry point that role configurations need, `For<>()` and `TypedExecutionResolver`, one signature per
  resolver, and `Fits` and `Mismatch`. Its budget snippet and the one in `execution-instances.md` resolve with the run's
  types, as `OperatorBudgetAlgorithm` does.
- `analyzers.md` shows `Observe` naming the search space and problem, as dev's arity requires, and explains the
  resolve-time check.
- Code snippets follow dev's reduced arity and the merged resolver: parameter `resolver`, local `typed` from
  `For<>()`, `GeneticAlgorithm<RealVector>` in the experiments guide. The prose in the guides, the glossary and the
  developer guidelines says resolver, child resolver and decoration instead of registry and replacement.
- Four files outside the ledger still described removed API and are fixed as well: `README.md` (quality curve via
  `TracePopulationCandidates`), `invariants-and-validation.md`, `operator-composition.md`, and
  `developer-guidelines.md` beyond the overhaul's one-line edit (§ 4.3, § 8.11, § 8.12).
- The changed snippets were compiled in a throwaway probe in `HeuristicLib.Tests.ApiUsageSpecs`, then deleted:
  `README.md` example 1, the TSP example, all seven blocks in the observability guide, the experiments trial analyzer,
  the two authoring examples, and the resolver's create-function call.

#### S9: cleanup

The earlier slices carry the overhaul across as faithfully as possible and defer anything that would add churn. This
slice addresses what they deferred. The backlog below collects it as the slices find it; each item states the problem
and, where one exists, the proposed solution, which is a starting point, not a decision.

- **Explicit generic arguments at observation call sites.** At dev's arity a role configuration names only its
  candidate, so `builder.Observe(evaluator, …)` cannot infer the search space and problem the observation reads; call
  sites name them explicitly, where the overhaul inferred them (first hit in S3's `PipelineAlgorithmTests` and
  `ExperimentAnalysisTests`). Proposed, untested:
  - an overload per role typed at `ISearchSpace<TCandidate>` and `IProblem<TCandidate, ISearchSpace<TCandidate>>`,
    for observers that read only candidates, objective vectors or states. An implicitly typed lambda then resolves to
    it, because the typed overload cannot infer and drops out;
  - for observers that do read a concrete search space or problem, a binding step in the spirit of `For<>()`, such as
    `builder.For<TCandidate, TSearchSpace, TProblem>().Observe(evaluator, Record)`, so the types are named once per
    analyzer rather than per call. A method group typed at concrete types needs this, because the first overload does
    not apply to it.
- **Interceptor traces name their search state.** `IInterceptor<TCandidate>` carries no search state, so
  `Analyzer.Trace(interceptor, …)` with a lambda and `Measurement.Candidates(interceptor)` need it named. An extension
  on the `Interceptor<…>` authoring base infers it for `TracePopulationQuality` and `TracePopulationCandidates` (S4);
  the same could cover the `Analyzer.Trace` overloads.
- **Mismatch messages name generic wrappers by their metadata name**, for example ``ObservingMutator`3``, because
  `ExecutionSignature.Mismatch` prints `GetType().Name`. A readable name for generic types would fix every caller.
- **A genealogy graph cannot hold the same child twice.** `GenealogyGraph.AddConnection` keys nodes by candidate and
  throws when an operator produces a candidate already in the generation, unless it equals one of its parents. A
  deterministic crossover such as `AlphaBetaBlendCrossover` does that whenever selection picks a parent pair twice.
  Both branches have this; S5 found it while writing the differential tests, which use a randomised crossover.
- **Warnings carried verbatim from the overhaul.** Missing `<param>` tags (CS1573): `GenealogyAnalyzer` (`equality`,
  `saveSpace`), `RankAnalyzer` (`crossovers`, `mutators`, `equality`), `RunTraceExtensions` and
  `EpochWorkTrace.PerEpoch` (`trace`, `evaluations`, `epoch`). S1172 on the unused `searchSpace` parameter of
  `DynamicRacingAlgorithm`'s `PerformanceTrackingEvaluatorObserver.AfterEvaluation`, which no longer implements an
  observer interface.
- **Provisional names.** `TypedExecutionResolver`, its extensions `TypedExecutionResolverExtensions`, and the local
  `typed` that S1b introduced where the resolver parameter took the name `resolver`.

Gate: full, format.

As done:
- Observation call sites: both proposals. Each role gains an `Observe` overload typed at `ISearchSpace<TCandidate>` and
  `IProblem<TCandidate, ISearchSpace<TCandidate>>`; the algorithm overload infers its search state, and the interceptor
  overload reads states as `ISearchState`. `builder.For<…>()` returns a `TypedObservationBuilder`, which mirrors
  `TypedExecutionResolver`: three types for most roles, a fourth for interceptors. Where the overhaul's inferred call
  now compiles, it is restored verbatim (`RankAnalyzer`, `AnalysisUsabilityTests`, `TraceCompositionTests`).
  `GenealogyAnalyzer` binds its types once and keeps its resolve-time check. `DynamicProblem`'s iteration hook stays
  explicit, because its check keeps a hook for one problem off a run over another. Calls that pass a single method group
  keep naming their types, which `For<>()` would not shorten.
- Mismatch messages and the resolver's two type messages name generic types with their type arguments, through an
  internal `ExecutionSignature.Name`.
- The carried CS1573 and S1172 warnings are fixed. `InferenceConstructionSpecs`'s S1172 is dev's, where the parameter
  exists for inference, and stays.
- Moved to `developer-backlog.md` rather than done here: the provisional names, now including `TypedObservationBuilder`,
  and interceptor traces inferring their search state. The genealogy item was already there from the overhaul.

### Step 3: completeness check

The merge commit takes its tree from the workbench, so anything the slices never carried would be lost without a
trace. Three checks guard against that. The first two also run at the end of every slice, restricted to its rows.

**Carry check.** Merge the overhaul into the result without touching the worktree or index, and read what is left:

```bash
git -c merge.conflictStyle=zdiff3 merge-tree --write-tree analysis-overhaul-migration analysis-overhaul-backup
```

The first line is a tree. Diffing the workbench against it shows every overhaul hunk the result does not contain:

- a hunk without conflict markers is overhaul work nobody carried, and must be carried;
- a hunk with conflict markers is a place where the result deliberately differs, and needs its reason in a slice
  report or the behaviour-differences log;
- git's conflict messages after the tree list what the diff cannot show, such as a deletion the overhaul made in a
  file dev modified.

`merge-tree` compares commits and cannot see uncommitted work, so a slice in progress is checked by comparing each
working-tree file against the merged tree (`git show "$tree:<path>" | diff - <path>`), and the full check runs once the
checkpoint is committed.

Starting point on the untouched workbench: 703 clean hunks in 172 files, 786 conflict hunks in 164 files, 36
modify/delete conflicts and one rename/delete. At the end: no clean hunk, every conflict explained, and no
modify/delete conflict left for a file the overhaul deleted, because S7 performs those deletions. Three remain by
design, for files dev deleted and the overhaul edited: `IEncodingDefaults`, `ExecutionInstanceResolvableExtensions` and
`EncodingAndProblemDefaultSpecs`. Their edits were carried to dev's successors in S2, S1 and S4 (see "Concept moves to
carry"), and `merge-tree` reports them because the files stay deleted.

**Test inventory.** The overhaul introduced 99 test method names. Each must exist in the result, or its absence must be
documented. Together with the rule that the overhaul's assertions come across unchanged, this checks behaviour rather
than text.

**Ledger.** Every row is ticked. This is bookkeeping for the other two checks, not a check in itself: a row records
that a path was worked on, not that all of the overhaul's change to it arrived.

### Step 4: the merge commit (Philipp)

The second parent is the dev commit the checkpoints contain, not whatever dev points to by then. Using a newer dev
here would record changes the tree does not have.

```bash
D=$(git merge-base dev analysis-overhaul-migration)
git switch analysis-overhaul
git merge --no-ff --no-commit "$D"
git restore --source=analysis-overhaul-migration --staged --worktree -- :/
git diff --cached --stat analysis-overhaul-migration
git commit
```

The `git diff` before the commit prints nothing when the tree matches the last checkpoint. Afterwards,
`git log -1 --format=%p` shows the two parents.

### Step 5: afterwards (Philipp)

The workbench has served its purpose. Deleting it drops the checkpoint commits from the history; tagging it
first keeps them reachable, for example for comparing slices later.

If dev moved during the migration, merging it into `analysis-overhaul` again is an ordinary merge from here on.

## Behaviour differences

Filled in by the differential tests in S4 to S6. A difference the overhaul intended is kept; the old test asserting
dev's behaviour is what changes.

| Old analyzer | Replacement | Difference | Decision |
| ------------ | ----------- | ---------- | -------- |
| `QualityCurvePerEpochAnalysis` | `Analyzer.Trace` of the evaluator with `Aggregate.Best` against `Clock.FromEpoch` | The old one recorded every improvement per candidate, tagged with the nominal epoch counted from evaluations; batches `[5,3] [10,1] [4]` with epochs of two gave `(5,0) (3,1) (10,1) (1,2) (4,2)`. The trace records each batch's best, tagged with the environment that scored it: `(3,0) (1,1) (4,2)`. | Kept: the overhaul's timing model |
| `InvalidPerEpochAnalysis` | `Analyzer.TraceEpochWork` read with `EpochWorkTrace.PerEpoch` | "Invalid" evaluations, scored while an update was pending, become `EpochWork.Stale`, counted per environment in effect alongside the evaluations it received. dev had no test for it. | Kept: the overhaul's timing model |
| `BestBeforeChangePerformanceAnalysis` | `BestBeforeChangePerformanceAnalyzer` | The old one grouped evaluations by nominal epoch and dropped invalid ones: `(5,0) (3,1)`, performance 4. The new one groups each batch under the environment that scored it: `(3,0) (1,1)`, performance 2. Entries carry `int Epoch` instead of `EvaluationTiming`. | Kept: the overhaul's timing model |
| `DynamicAnalysis` (base) | none | The subscription to `OnEvaluation` and its disposal are gone; the successors observe the evaluator and read `CurrentEpoch`. | Kept |
| `UpdatePolicy` `Asynchronous`, `AfterEvaluation`, `AfterInterception` | `AfterEachEvaluation`, `AfterEachBatchEvaluation`, `AfterEachIteration` | The last needs `CreateIterationUpdateHook` installed. The default moves from after the evaluator batch to after each evaluation; tests and scenarios that relied on dev's default name `AfterEachBatchEvaluation`, as the overhaul does. | Kept |

## If dev moves during the migration

Merge dev into the workbench between slices, never in the middle of one, and regenerate the ledger. Step 4
picks up the newer dev commit through `git merge-base`.

## Tooling

- `git merge-tree --write-tree` for dry runs that touch no worktree or index.
- `git merge-file` for per-file re-application in the slices. `mergiraf solve <file>` resolves structural conflicts
  in C# and needs no git configuration; it is optional and a separate install.
- A root `Directory.Build.targets` with `<Compile Remove="$(MSBuildThisFileDirectory)…"/>` can park files that are in
  the tree but not ready to compile. Verified, and not needed by this plan, since files enter the checkpoints only
  when they compile.
- `git-imerge` was considered and rejected. It would merge the six overhaul commits pairwise against dev's twelve,
  several of which touch 150 to 250 files, so the same design conflict would be resolved repeatedly at intermediate
  states that never existed.

## Risks

- **Dropped overhaul work.** The merge commit takes its tree from the checkpoints, so anything they never carried is
  lost silently. The carry check and the test inventory guard against it, per slice and once more in step 3. What
  they cannot judge is intent inside a conflict hunk; that is why every conflict hunk gets a stated reason.
- **Commits on `analysis-overhaul` during the migration** would be undone by the merge commit. Freeze it, or carry
  such commits into the checkpoints first.
- **S1 is large.** 112 rows. The S1a/S1b split keeps each checkpoint reviewable.
- **The shim is throwaway code.** It lives only in checkpoints, and pays for itself by keeping coverage intact and by
  the differential tests.
- **The ledger ages.** It describes one snapshot of both branches.
