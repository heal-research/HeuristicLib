# Analysis system rework

## Summary

HeuristicLib has a sound central idea for analysis: an analyzer is reusable configuration, each run creates fresh analysis data and observations attach to algorithm or operator boundaries through the execution registry. The current implementation proves that model with algorithm quality curves, operator traces, nested meta-algorithms and experiment trials.

The surrounding API is not consistent enough to preserve as it stands. Observation merging depends on the caller's closed generic types, analyzer cleanup has lifecycle gaps, live results have no general concurrency contract and several public analyzers are incomplete or untested. Result naming, collection ownership and factory coverage also vary by type.

This plan keeps the run-owned model and redesigns its weak parts. It does not treat every current analyzer as something that must survive. Mature general analysis should remain available from the main package. Specialized or unsettled analysis should remain experimental until its semantics and tests justify promotion.

The backlog phrase "desired-state analysis API" means an executable API usage spec for the intended replacement beside the current API specs. It does not mean analysis of a desired search state.

## Goals

1. Preserve one fresh analysis result per analyzer and run.
2. Keep observations read only and separate from algorithm behavior.
3. Make analyzer setup, execution lifetime and result availability explicit.
4. Merge observations by anchor identity and observation role without depending on the caller's static generic view.
5. Detect observation anchors that never participate in the run instead of silently returning misleading empty results.
6. Give analyzer results a consistent ownership and immutability model.
7. Make custom analyzer authoring smaller than the current `IAnalyzerRunState` plus `ObservationPlan` model.
8. Preserve analysis across nested algorithms and recreated child execution registries.
9. Keep experiment trial results independent and strongly typed.
10. Retain only analyzers whose current contract is honest, observable and tested.

## Non-goals

1. Do not redesign algorithms, operator role contracts or the execution registry as a whole.
2. Do not combine run-owned analyzers with direct callbacks or caller-owned instrumentation.
3. Do not settle the complete `AlgorithmRun` and `ExperimentRun` lifecycle redesign unless analysis needs a bounded part of it.
4. Do not redesign multiobjective comparison inside this work. Pareto and hypervolume analysis should follow the separate objective-system decisions.
5. Do not perform namespace or folder moves in the same branch.
6. Do not preserve incomplete public types only for compatibility. HeuristicLib is still in early alpha.

## Terminology and scope

This plan uses the glossary meanings of analyzer, analyzer result, observation, observation anchor and run.

It covers:

- run-owned analyzer configurations and results
- observation registration and installation
- observable algorithm and operator wrappers as analyzer infrastructure
- analyzer attachment and lookup on `AlgorithmRun`
- analyzer fan-out across `ExperimentRun` trials
- concrete quality, population, diversity, genealogy and dynamic analyzers
- private analysis adapters in Python interop

It does not cover:

- Roslyn analyzers
- pure calculators and metrics that do not observe a run
- direct search-state streaming
- counting and duration wrappers except where their boundary with analyzers needs clarification

## Current architecture

The current execution path is:

```text
Analyzer configuration
        ↓ creates
Analyzer run state and result
        ↓ registers
ObservationPlan
        ↓ installs replacements into
ExecutionInstanceRegistry
        ↓ resolves
Observable algorithm and operator wrappers
        ↓ invoke callbacks that update
Analyzer result
        ↓ retrieved through
AlgorithmRun.GetResult(analyzer)
```

`ExperimentRun` creates one concrete analyzer per trial through `TrialAnalyzer`. Each trial keeps its own `AlgorithmRun`, observation registrations and result object.

### Systems in the current implementation

| System                   | Responsibility                                                | Current assessment                                  |
| ------------------------ | ------------------------------------------------------------- | --------------------------------------------------- |
| Search-state streaming   | Direct progress inspection through `Stream()`                 | Strong and simple                                   |
| Observable wrappers      | Callback hooks for algorithms and all nine operator roles     | Strong mechanism with repetitive authoring API      |
| Run-owned analyzers      | Reusable configuration with one result per run                | Sound ownership model with weak lifecycle contracts |
| `ObservationPlan`        | Combines analyzer requests and installs registry replacements | Useful design with brittle generic merging          |
| Experiment analysis      | Creates independent analyzers for every trial                 | Useful and reasonably well tested                   |
| Instrumentation wrappers | Caller-owned counters and duration measurements               | Useful but distinct from run-owned analysis         |
| Experimental analysis    | Genealogy, rank and dynamic-problem analysis                  | Useful research code with unresolved ownership      |
| Concrete core analyzers  | Quality, population, Pareto and diversity analysis            | Uneven quality and coverage                         |

### What should be preserved

#### Run ownership

`AlgorithmRun` creates fresh analyzer data when execution starts. Reusing an analyzer configuration therefore does not mix results from separate runs.

#### Stable scope across nested execution

Observation replacements propagate through child execution registries. Analyzer results can span short-lived algorithm and operator execution instances inside a meta-algorithm.

#### Read-only observation

Algorithms and all nine operator roles expose post-operation observer contracts. Observation records data without changing an operation's inputs, outputs or computation.

#### Typed result lookup

`run.GetResult(analyzer)` connects the analyzer configuration to its result type without an untyped public key.

#### Experiment isolation

`TrialAnalyzer` creates a separate analyzer and result for every materialized trial. Typed trial keys remain attached to the collected results.

## Concrete analyzer inventory

### Main package

| Analyzer                               | Observes                                   | Result                                                            | Assessment                                                                           |
| -------------------------------------- | ------------------------------------------ | ----------------------------------------------------------------- | ------------------------------------------------------------------------------------ |
| `BestMedianWorstAnalysis`              | Algorithms and interceptors                | Best, median and worst candidate per observation                  | Best current reference implementation                                                |
| `BestMedianWorstPerEvaluationAnalysis` | Evaluators and interceptors                | Best, median and worst candidate with cumulative evaluation count | Useful but retains old collection storage and lacks algorithm anchors                |
| `BestPerEvaluationAnalysis`            | Evaluators                                 | Best candidate improvements by evaluation count                   | Coherent but exposed mainly through `ExperimentalAnalyzers`                          |
| `BestQualityAlgorithmAnalysis`         | Evaluators                                 | Best objective vector                                             | Misnamed and inconsistent with its documentation                                     |
| `AllPopulationsAnalysis`               | One interceptor                            | Full population history                                           | Honest but potentially very memory intensive                                         |
| `AllObjectiveVectorsAnalysis`          | Evaluators and optional reset interceptors | Objective-vector collection                                       | The optional clearing behavior conflicts with the name "All"                         |
| `PopulationSimilarityAnalyzer`         | Interceptors                               | Similarity matrices and aggregate history                         | Has correctness gaps and no direct tests                                             |
| `AlleleFrequencyAnalyzer`              | Interceptors                               | Declared allele-frequency state                                   | Public stub that records nothing and returns the wrong result type                   |
| `ParetoFrontAnalysis`                  | Evaluators                                 | Intended Pareto front                                             | Result front is not publicly readable and initial result has the wrong concrete type |
| `HyperVolumeAnalysis`                  | Evaluators                                 | Pareto state plus hypervolume                                     | Coupled to unsettled multiobjective semantics                                        |

### Experimental package

| Analyzer                              | Observes                              | Result                                | Assessment                                               |
| ------------------------------------- | ------------------------------------- | ------------------------------------- | -------------------------------------------------------- |
| `GenealogyAnalysis`                   | Crossovers, mutators and interceptors | Genealogy graph                       | Useful experimental analyzer and a good composition case |
| `RankAnalysis`                        | Genealogy hooks and interceptors      | Genealogy graph plus rank history     | Useful composition over `GenealogyAnalysis`              |
| `QualityCurvePerEpochAnalysis`        | Dynamic-problem evaluation events     | Best candidate per epoch              | Useful but depends on custom event lifetime              |
| `BestBeforeChangePerformanceAnalysis` | Dynamic-problem evaluation events     | Per-change performance and prediction | Useful research analysis with custom lifetime            |
| `InvalidPerEpochAnalysis`             | Dynamic-problem evaluation events     | Invalid evaluation counts per epoch   | Small and coherent but tied to the same event model      |

### Integration adapters

Python interop has a private `CallbackAnalysis` that derives from `Analyzer<object>` only to invoke a callback at an interceptor. It still requires a placeholder interceptor. This should become a direct observation adapter or use an algorithm anchor after the replacement API is settled.

## Findings

### Observation registration depends on static generic types

`ObservationPlan` keys entries by anchor reference, which is correct. It merges entries only when their complete closed generic entry types match.

Operator roles are contravariant in problem and search-space types. The same operator may therefore be observed legally through two different static interface views. The current plan treats those registrations as incompatible and throws `InvalidOperationException: Observation conflict` even though both refer to the same object and operation boundary.

The public authoring facade contains twenty overloads, two for every observable role. Every new role or anchor kind adds another hand-written pair.

### Unresolved anchors fail silently

An observation anchor is matched by reference. A configuration copied with `with` is a different anchor. An analyzer attached to the old object records nothing when the copy runs.

`TrackBestMedianWorst` avoids this for the root algorithm because it reads the anchor from the run. Named inner algorithms and operators still rely on caller-managed identity. The framework does not distinguish an anchor that resolved but had no calls from an anchor that never existed in the execution graph.

### Analyzer run state is mostly mechanical

For ordinary analyzers, `Analyzer<TResult>` creates a private `IAnalyzerRunState<TResult>` wrapper that holds only the analyzer and result references. Mutable analysis data lives in the result object.

Dynamic analysis is the only current implementation that needs custom run-state behavior. It subscribes to `DynamicProblem.OnEvaluation` and implements `IDisposable` so the run can unsubscribe later.

The public abstraction is therefore heavy for normal authoring while its exceptional cleanup behavior remains implicit.

`src/HeuristicLib/Analysis/AnalyzerRunState.cs` contains only a commented abandoned base and should be deleted.

### Cleanup has lifecycle gaps

`AlgorithmRun.Stream()` creates analyzer states and registers observations before the returned stream is enumerated. Analyzer states are disposed only from the asynchronous iterator's `finally` block.

This leaves several failure paths:

1. A caller obtains a stream but never enumerates it.
2. One analyzer subscribes successfully and a later analyzer fails during registration.
3. Observation installation fails after an analyzer acquired a resource.
4. Cleanup depends on an optional `IDisposable` check that the analyzer contracts do not describe.

The run needs one explicit ownership mechanism for subscriptions and other cleanup work acquired during analysis setup.

### Result lifecycle is unclear

The current API allows `GetResult` as soon as execution starts. API usage specs deliberately query a result during streaming.

Most analyzer results are mutable lists or mutable classes. The framework has no general synchronization rule for a callback writing while user code reads. There is also no finalization step that turns an accumulator into an immutable completed result.

Result shapes vary between raw `List<T>`, classes named `State`, classes named `Result`, `QualityCurve` and mutable graphs. Some expose raw mutable collections while others expose read-only interfaces backed by mutable collections.

The redesign must decide whether live inspection remains supported. If it does, it needs an explicitly named snapshot or view API and a concurrency contract. Completed results should not expose mutable implementation collections.

### Attachment behavior is inconsistent

`AlgorithmRun` accepts the same analyzer object more than once and fails later when its reference-keyed state dictionary is built. `ExperimentRun` rejects the same trial analyzer at attachment time.

Both run types should apply one rule and report configuration errors before execution side effects begin.

### Configuration equality and collection ownership drifted

`BestMedianWorstAnalysis` uses `ValueArray` for retained anchor collections and has structural equality tests.

Other analyzer records retain `ImmutableArray` or use arrays in positional record parameters. `ImmutableArray` compares backing storage identity and belongs on execution instances under the repository guidelines. Array-valued record parameters also compare by reference.

Every retained analyzer configuration collection should accept `IReadOnlyList<T>`, snapshot with `ToValueArray()` and expose `ValueArray<T>`.

### Naming and factory coverage have no rule

Configuration types alternate between `Analysis` and `Analyzer`. Results alternate between `State`, `Result`, descriptive names and raw collections. The public construction factory is named `Analyzer` but exposes only part of the catalog.

`ExperimentalAnalyzers` exposes `BestPerEvaluationAnalysis` and `AllPopulationsAnalysis` even though those implementations live in the main assembly. This makes package ownership unclear.

Use these names consistently:

1. `...Analyzer` for reusable analyzer configuration.
2. `...Result` for the user-facing completed or snapshot result.
3. Accumulator or run state only for private mutable execution data.

### Concrete correctness defects

#### `AlleleFrequencyAnalyzer`

The analyzer derives from `Analyzer<PopulationSimilarityAnalyzerState>` instead of its own declared state. It never invokes `IAlleleCalculator`, never records a frequency and leaves its intended history private and unread. Remove it from public API unless this branch implements and tests the missing behavior.

#### `ParetoFrontAnalysis`

`CreateInitialResult` returns `HyperVolumeState<T>` even though the analyzer promises `ParetoState<T>`. `ParetoState.Front` is protected, so callers cannot inspect the Pareto front returned by the analyzer. This violates the basic requirement that a public analyzer expose an observable result.

#### `BestQualityAlgorithmAnalysis`

The type observes evaluators rather than an algorithm. Its result contains only an objective vector, while the architecture document says it stores the best evaluated candidate. It also uses the problem's total-order comparer without the fallback used by the best, median and worst analyzers.

#### `PopulationSimilarityAnalyzer`

Empty populations cause aggregate operations on empty sequences. A one-member population divides by zero while calculating average similarity. `StoreHistory = false` clears similarity matrices but continues appending aggregate history. The type has no direct tests.

#### `AllObjectiveVectorsAnalysis`

When reset mode is enabled, interceptors clear the collected vectors. The resulting object is no longer "all objective vectors" and may be empty after the final interception. Split this into an honest current-window analyzer or remove the reset behavior.

### Test coverage is concentrated on the framework

The main framework has useful coverage for typed result lookup, multiple analyzers on one anchor, algorithm and operator anchors, nested meta-algorithms and experiment trial isolation.

`BestMedianWorstAnalysis` has representative API usage coverage. Genealogy has scenario coverage and dynamic analysis has focused experimental tests.

Most other public analyzers have no direct result tests. This includes Pareto front, hypervolume, population similarity, allele frequency, all objective vectors and best quality. Several current defects would be caught by one basic result assertion.

### Direct instrumentation is a separate model

`ObservationCounter` and `ObservationDuration` are caller-owned mutable sinks passed into counting and duration wrappers. They are useful for local instrumentation and execution budgets, but their data does not belong automatically to one run. Reusing a configuration with the same sink can mix measurements across runs.

Keep this model separate and document its external ownership. Do not force these wrappers through the analyzer result system.

`InvocationTiming` has no consumers and should be removed if a final repository search still finds none.

## Proposed design

### Keep a small analyzer framework in core

The earlier [core and experimental boundary plan](core_experimental_boundary.md) proposes moving the complete run-owned analyzer layer to Experimental. Revisit that decision before implementation.

The current recommendation is to keep a small mature analyzer framework in the main package. Run-wide quality analysis is a normal optimization workflow and now appears in the README. Moving the whole concept would make an ordinary feature look provisional.

Keep in core:

- observable algorithm and operator contracts
- observation installation needed by runs
- analyzer configuration and typed result contracts
- run attachment and result retrieval
- experiment trial integration if its delegate configuration remains acceptable
- a small set of mature general analyzers, starting with best, median and worst quality

Keep or move to Experimental until mature:

- genealogy and rank analysis
- dynamic-problem analysis
- population diversity analysis
- Pareto and hypervolume analysis until the objective model is settled
- research-specific or high-memory trace analyzers without a common user story

### Separate four responsibilities

The replacement design should make these responsibilities explicit:

1. Analyzer configuration declares reusable settings and requested anchors.
2. A run-owned accumulator receives observations and holds mutable work data.
3. The run owns observation installation, registration lifetime and cleanup.
4. A result is the typed user-facing view or immutable value produced from the accumulator.

Do not require ordinary analyzer authors to implement a wrapper whose only job is to forward `RegisterObservations` to the analyzer configuration.

### Write the desired API usage spec first

Add a desired-state API usage spec beside the current API specs before changing contracts. It should demonstrate:

1. Attaching a quality analyzer to the run without an interceptor.
2. Observing an explicitly named inner algorithm.
3. Observing an operator for data that search states do not contain.
4. Reading the completed typed result.
5. The chosen behavior for live result inspection.
6. One custom analyzer with more than one anchor role.
7. One experiment that creates an independent analyzer result per trial.
8. A configuration error for an anchor that never resolves, or the explicit diagnostic model chosen instead.

This spec is the target for the redesign. Keep the current API specs until the migration is complete so reviewers can compare the two usage models.

### Replace opportunistic disposal with registration lifetime

Analysis setup may acquire subscriptions or other cleanup work. The run should own those registrations explicitly from the moment setup begins.

Required behavior:

1. Setup is transactional. If one analyzer fails, the run cleans up every registration already acquired.
2. A stream that is created but never enumerated cannot leak subscriptions.
3. Cancellation, early disposal, completion and failure all release registrations exactly once.
4. Ordinary analyzers that only register observation callbacks do not implement disposal.
5. Resource ownership is visible in one framework type rather than discovered through `state is IDisposable`.

Prefer a dedicated internal registration lifetime or cleanup collection over making every analyzer result disposable.

### Redesign observation registration around typed events

The public authoring API should provide typed event data for each role while the internal plan merges registrations independently of their static generic view.

The exact type shape needs a prototype. The accepted design must satisfy these constraints:

1. Analyzer authors do not spell three otherwise inferable type arguments.
2. Adding an observation role does not require two new overloads in one central facade.
3. Multiple analyzers observing one anchor create one replacement rather than a deep wrapper chain.
4. The same contravariant anchor observed through compatible static types does not conflict.
5. One configuration that genuinely exposes different operation roles can distinguish them.
6. Public callbacks remain strongly typed.
7. The internal implementation may erase callback types after public registration if that is the simplest way to merge compatible observers safely.

Do not introduce a generic operator invocation path. The rejected typed-invocation design in [developer-backlog.md](developer-backlog.md#typed-operator-invocation) remains rejected. Observation plumbing may use internal type erasure because it does not replace role execution contracts.

### Track anchor resolution separately from invocation count

Each observation registration should know whether its anchor was resolved into an observable execution instance.

This distinguishes:

1. An anchor that was never part of the execution graph.
2. An anchor that resolved but its operation was not invoked.
3. An anchor that produced zero items in a valid invocation.

Decide whether an unresolved anchor throws at completion, records a diagnostic or follows analyzer-specific policy. Silent empty results should not remain the only behavior.

Root run helpers such as `TrackBestMedianWorst` should continue taking their anchor from the run so the common case cannot use a stale copy.

### Define result availability and publication

The branch must choose one of these models before implementation:

#### Completed results only

`GetResult` becomes valid only after execution ends. Direct streaming and caller-owned instrumentation handle live progress. This is the simplest ownership and concurrency model.

#### Explicit live snapshots plus completed results

`GetCurrentResult` or an equivalent API returns a safe snapshot while the run is active. `GetResult` returns the finalized result after execution ends. Accumulators remain private and synchronized.

Do not keep the current accidental middle ground where `GetResult` returns a live mutable object without a concurrency contract.

The plan currently recommends completed results only unless a real operator-level live-analysis use case cannot be handled by streaming or direct instrumentation.

The lifecycle decision must also state what happens after cancellation or failure. Returning a partial result can be useful, but it should be named and typed as partial rather than presented as a normal completed result.

### Make results immutable at the public boundary

Analyzer callbacks may update private mutable accumulators. User-facing completed results should expose immutable values or snapshots.

Apply these rules:

1. Do not return raw `List<T>`.
2. Snapshot historical collections before publication.
3. Keep mutable graphs private while they are being built, then publish an immutable graph view or value.
4. Use descriptive result entry types instead of public tuples when field meaning matters.
5. Include observation count or other metadata only when it has defined semantics.
6. Do not store reporting concerns such as chart labels or units unless the analyzer owns that meaning.

### Normalize analyzer configuration values

All analyzer configuration records should follow the same ownership rules as algorithms and operators.

1. Accept finite ordered anchor collections as `IReadOnlyList<T>`.
2. Snapshot retained collections with `ToValueArray()`.
3. Expose retained collections as `ValueArray<T>`.
4. Keep mutable accumulators and `ImmutableArray` out of configuration records.
5. Use records only where structural equality is the intended contract.
6. Use classes for runtime adapters that hold identity-bearing delegates or external sinks.

### Make composition a supported authoring case

`RankAnalysis` currently calls `GenealogyAnalysis.RegisterObservations` with the outer result's graph. The idea is useful: one analyzer can reuse another analyzer's observation logic while publishing a different result.

The new authoring model should allow reusable observation components without creating a hidden second run-owned result. Add one desired-state spec for this case before migrating genealogy and rank.

### Clarify the boundary with direct callbacks

Keep these choices distinct:

| Need                                                        | Use                                              |
| ----------------------------------------------------------- | ------------------------------------------------ |
| Inspect every yielded search state                          | `Stream()`                                       |
| Run one local callback at an algorithm or operator boundary | Observable wrapper or direct observation adapter |
| Collect one coherent typed result across a run              | Analyzer                                         |
| Supply an externally owned counter or timer                 | Instrumentation wrapper                          |
| Collect independent results for experiment trials           | Trial analyzer integration                       |

The Python callback adapter should use the direct callback path. It should not create `Analyzer<object>` unless Python needs a run-owned result.

## Concrete migration decisions

### Use best, median and worst quality as the reference

Migrate this analyzer first because its common run-level usage is already documented and tested.

The replacement should:

1. Anchor on the run's root algorithm through a convenience method.
2. Support named inner algorithms and exact operator boundaries.
3. Publish an immutable quality history.
4. Use the problem's defined total order and preserve the documented lexicographic fallback when none exists.
5. Reject an empty observed population with a clear error.
6. Preserve structural equality for retained anchor collections if anchors remain part of analyzer configuration.

### Consolidate evaluation-count quality analysis

`BestPerEvaluationAnalysis`, `BestMedianWorstPerEvaluationAnalysis` and `BestQualityAlgorithmAnalysis` overlap.

Before migrating them, define the actual user stories:

1. Best candidate whenever a new best appears.
2. Population summary at iteration boundaries indexed by cumulative evaluations.
3. Best objective vector only for low-retention experiment summaries.

Keep separate analyzers only where those output and retention differences matter. Rename every retained type for what it returns and observes.

### Split full-history and current-window collection

`AllObjectiveVectorsAnalysis` should not switch between full history and a clearing window through one boolean.

Either provide two explicit analyzers or keep only the use case with a demonstrated consumer. Apply the same standard to full population capture. High-memory analyzers must say what they retain and should not be the default recommendation.

### Remove or implement allele frequency analysis

Do not move the current stub unchanged. The implementation branch must choose one:

1. Delete the public analyzer, result and allele contracts if no current consumer needs them.
2. Implement allele extraction, frequency aggregation, impact semantics, history retention and tests before exposing it again.

The default recommendation is deletion until a concrete analysis workflow requires it.

### Repair population similarity before retaining it

If retained, define behavior for populations of size zero and one, clarify whether diagonal similarity participates and make `StoreHistory` apply to every history collection consistently. Add focused unit tests for each case and for invalid similarity values.

Keep it Experimental until those semantics are settled.

### Defer Pareto and hypervolume promotion

Fix the inaccessible Pareto result and wrong initial result immediately if the types remain public during migration. Do not finalize their replacement API until the objective-system rework defines dominance, objective directions and total ordering clearly.

These analyzers should remain Experimental during that work.

### Rework dynamic analysis subscriptions

Dynamic analysis currently combines an evaluator observer implemented by the problem with an event subscription from analyzer state to problem events.

The replacement should make evaluation timing data an explicit observation payload or a dedicated experimental observation source. A problem should not need to be both the evaluated domain object and the observer plumbing for its analyzers.

Until that design exists, keep the dynamic analyzers Experimental and use the new registration lifetime to prevent subscription leaks.

## Implementation sequence

### Phase 1: Lock the desired usage

1. Add desired-state API usage specs beside the current analysis specs.
2. Decide completed-only results versus explicit live snapshots.
3. Decide unresolved-anchor behavior.
4. Reconfirm the minimal core and Experimental package boundary.
5. Record the accepted public naming scheme.

### Phase 2: Replace lifecycle and observation internals

1. Introduce the explicit registration lifetime.
2. Make analyzer setup transactional.
3. Replace static-type-dependent observation merging.
4. Track whether each anchor resolves.
5. Preserve replacement inheritance through child registries.
6. Add tests for stream creation without enumeration, setup failure, cancellation, early disposal and normal completion.

### Phase 3: Replace the analyzer authoring contract

1. Remove the mechanical run-state wrapper from the ordinary authoring path.
2. Keep a deliberate advanced path for custom lifetime behavior if still needed.
3. Add typed observation event data and inference-friendly registration.
4. Add custom analyzer specs for one anchor, multiple roles and composed observation logic.
5. Delete the commented `AnalyzerRunState.cs` source.

### Phase 4: Migrate mature analyzers

1. Migrate best, median and worst quality first.
2. Migrate the distinct evaluation-count stories that survive consolidation.
3. Migrate experiment trial integration.
4. Replace raw mutable result collections with immutable results.
5. Update README and user documentation only after the new path is stable.

### Phase 5: Triage specialized analyzers

1. Delete or implement allele frequency analysis.
2. Repair population similarity and keep it Experimental until tested.
3. Split or remove ambiguous full-history analyzers.
4. Migrate genealogy and rank using the supported composition pattern.
5. Rework dynamic analysis subscriptions.
6. Keep Pareto and hypervolume analysis aligned with the objective-system branch.
7. Replace the Python callback analyzer with direct observation.
8. Remove `InvocationTiming` if it remains unused.

### Phase 6: Documentation and namespace work

1. Rewrite the analyzer architecture page to describe the implemented contracts.
2. Update the glossary only if settled terminology changed.
3. Update observability, experiment and meta-algorithm guides.
4. Remove current-state API specs after equivalent replacement coverage exists.
5. Reconcile any analysis-specific namespace changes required by the accepted replacement API without reopening the broader folder restructuring.

## Validation plan

### Contract and lifecycle tests

Cover:

1. One fresh result per analyzer and run.
2. Duplicate analyzer attachment behavior.
3. Result access before start, during execution, after completion, after cancellation and after failure.
4. Cleanup when a stream is never enumerated.
5. Cleanup after partial analyzer setup failure.
6. Cleanup after observation installation failure.
7. Cleanup on early stream disposal and cancellation.
8. One cleanup action executed exactly once.

### Observation tests

Cover:

1. Multiple analyzers on one anchor.
2. One anchor observed through compatible contravariant static types.
3. Algorithm and operator anchors in one run.
4. An unresolved anchor.
5. A resolved anchor with zero invocations.
6. Root algorithm helpers that cannot hold a stale copy.
7. Named inner algorithm observation.
8. Child registry recreation and reuse.
9. A meta-algorithm that bypasses `Resolve`, if the existing authoring guard remains documentation only.

### Result tests

Every public analyzer retained after the rework needs:

1. One representative result assertion.
2. Empty-input behavior where applicable.
3. Single-item behavior where applicable.
4. Multiobjective behavior where applicable.
5. Proof that published history cannot be mutated through the public API.
6. Structural equality tests for configuration collections.
7. A memory-retention test or documented bound for analyzers that store full populations or evaluation histories.

### Project validation

During implementation, run the narrowest relevant tests first. Before merging the branch, run:

1. `HeuristicLib.Tests`
2. `HeuristicLib.Tests.ApiUsageSpecs`
3. `HeuristicLib.Tests.Experimental`
4. Relevant genealogy, dynamic-problem and meta-algorithm scenarios
5. Release build
6. Whitespace, style and analyzer verification
7. Documentation build
8. Package creation for main, Experimental and Python interop

## Acceptance criteria

The rework is complete when:

1. The desired API usage specs are the normal documented path.
2. Analyzer setup cannot leak registrations on any tested exit path.
3. Observation merging no longer depends on compatible callers choosing identical closed generic types.
4. Unresolved anchors have explicit behavior and cannot masquerade silently as a successful empty analysis.
5. Public completed results do not expose mutable implementation collections.
6. Live result access is either removed or has an explicit snapshot and concurrency contract.
7. Configuration records use structural collection values consistently.
8. Every retained public analyzer has focused tests and an observable result.
9. `AlleleFrequencyAnalyzer` is implemented or removed.
10. `ParetoFrontAnalysis` no longer returns an unreadable front or the type is removed from the public main package.
11. Analyzer, result and factory naming follows one recorded rule.
12. Experiment trial analysis preserves independent typed results.
13. Nested and recreated execution registries preserve run-wide analysis.
14. Python interop no longer uses `Analyzer<object>` solely as a callback carrier.
15. The analyzer architecture document describes the implementation rather than an earlier design.
16. The rework preserves the accepted broad namespace layout and limits any further moves to analysis concepts that genuinely changed.

## Sequencing with other work

Do not start this rework before the planned early main release.

Complete the mechanical namespace and folder restructuring first. Then do this rework on its own branch.

This ordering may move or rename some analysis files a second time if the replacement API changes their concepts. That cost is acceptable because the namespace work is bounded and release-facing, while the analysis redesign needs more time and should not delay the next main release. Keep the namespace branch mechanical. Do not redesign analyzer contracts, results or lifecycle while moving files.

Coordinate with these plans and backlog items:

- [API ergonomics](api-ergonomics.md), especially the implemented algorithm observation work and pending namespace changes
- [Core and Experimental package boundary](core_experimental_boundary.md), especially the unresolved placement of run-owned analysis
- the `AlgorithmRun` and `ExperimentRun` lifecycle backlog item
- the objective-system backlog item before finalizing Pareto and hypervolume analysis

Keep the implementation on its own branch. Avoid mixing it with namespace moves, broad glossary renaming or unrelated algorithm work.

## Source material

- [Analyzer architecture](../docs/contributing/architecture/analyzers.md)
- [Observability and analysis](../docs/guide/execution/observability-and-analysis.md)
- [Configuration and execution ownership](../docs/contributing/developer-guidelines.md#-4-configuration-and-execution-ownership)
- [Collection ownership](../docs/contributing/developer-guidelines.md#-5-immutability-and-collection-ownership)
- [Analyzer glossary entries](../docs/guide/glossary.md#analyzer)
- [Developer backlog](developer-backlog.md)
- [API ergonomics](api-ergonomics.md)
- [Core and Experimental package boundary](core_experimental_boundary.md)
