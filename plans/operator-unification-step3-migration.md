# Operator unification: step 3 migration

Status on 2026-11-05: the developer accepted finishing step 3 before selecting any simplifications. The scope below implements the accepted [lifetime design](operator-state-lifetimes.md#implementation-sequence-confirmed-on-2026-11-04). The developer accepted decisions A and B, including the dynamic problem dependency, on 2026-11-05. Implementation and validation are complete.

## Problem and boundaries

Step 2 introduced instruments, run-bound results and executor-call observation alongside the old APIs. The checkout still supports run setup, supplied resources, observation points and accumulating analyzers that retain their own data. Finish the migration so that subsequent simplifications can be compared with one coherent implementation.

Keep the accepted scope, sharing, advice ordering, failure and operation contracts. This work approves none of the reports under `simplification/`. In particular it does not select configuration rewriting, problem-bound executors, removal of executor controls, persistence introspection, trial identity changes or a replacement analysis framework.

Defer the broad step 4 architecture and guide rewrite. Update affected examples, public XML contracts and directly contradictory passages together with the removal they describe. Record outstanding prose work here rather than leaving deleted APIs presented as usable.

No compatibility shims, replacement run setup, live-object supply mechanism or temporary parallel APIs are part of this migration. Build and test commands run sequentially in this checkout. Do not commit or stage the implementation until the developer has reviewed it.

## Accepted work to implement

### Run construction and result storage

- Remove `AlgorithmRunSetup`, `ExperimentRunSetup`, `TrialAspect` and `ResolutionScopeBuilder`.
- Remove setup callbacks from the run constructors and the algorithm and experiment `CreateRun` extensions. Preserve existing validation, lazy executor construction, RNG forks, lifecycle behavior and the materialized experiment cases.
- Each algorithm run, including each experiment trial, creates its own root `ResolutionScope`. Advice is configured through `Advised`; direct scope users call `AddAspect` before resolution.
- Remove `ResourceHandle<T>`, `SuppliedResourceHandle<T>`, `CountAccumulatorHandle`, `DurationAccumulatorHandle`, `RunResources`, supply bindings and `Resource` lookups on scopes and runs.
- Make `RunResults` an instrument-only table, keyed by instrument reference. Preserve atomic result/aspect publication, lazy lookup, retry after result creation failure, result access after execution or construction failure, and the elapsed-time clock.
- Keep the internal controllable time provider used by elapsed-time tests without retaining a supplied-resource overload.

### Counting, timing and trace inference

- Remove `Aspect.CountInvocations`, `Aspect.CountCandidates`, `Aspect.MeasureDuration` and the equivalent fluent extensions accepting live accumulators, including `out` forms.
- Migrate callers to source-defined instruments, `Advised`, `Counted` and `Timed`, and read their run-bound accumulators through `run.Result(counter)` or `scope.Results.Result(counter)`.
- Keep the counting and timing advice needed by instruments and private budgets. A runtime advice constructor receiving an accumulator is an implementation dependency, not a restored supply or attachment API.
- Preserve completed-call operator counting, candidate-work validation, timing of failed calls, and private budget counters that restart in fresh child scopes.
- Implement the already accepted `Trace(createState, value, clocks, retention)` overload on a source and on the multi-source factory. Constrain `TState` to classes. Keep the internal trace factory and migrate all callers of the public nested-function overload.
- Preserve the accepted algorithm-call start ordering: a `Started` observation reads the count before that call is counted.

### Observation points

- Remove `ObservationPoint`, its executor contract and factory, and the associated weak-table identity machinery.
- Remove Cycle's `Cycles` and `Turns`, `CycleStart`, `CycleTurn`, `Clock.FromCycles` and `Clock.FromTurns`, and the point resolution/reporting arguments in its executor.
- Use inner algorithms' `Calls` for examples and tests that need turn boundaries. Include a turn yielding no state. Call observations do not promise the deleted point's cycle number, slot index or start-state payload, and no convenience API recreating those payloads is added.
- Keep existing iteration and call observation contracts, including transparent-wrapper control behavior as currently implemented. Do not select the review's proposed removal of controls by implication.

### Analyzers and consumers

- Convert the accumulating analysis definitions into instruments with fresh graphs, fronts, histories and prediction state for each run; their concrete shape is decision B below.
- Migrate PythonInterop's rank result extraction as well as its instrument attachment list. Preserve its runtime callbacks under the accepted temporary delegate exception.
- Audit `src`, analyzers, every test suite, samples, Python samples, README and published snippets. Previously migrated `RunTraceExtensions` callers are checked, not implemented again under another name.
- Remove the HLib0002 checks specific to resource handles and observation-point ownership when those concepts disappear, and remove their obsolete fixtures. Preserve HLib0003 configuration-mutation checking and HLib0001 child-resolution checking. Do not reassign HLib0002 to a new rule.
- Update architecture type-location assertions to the remaining types and new result types. Tests that exist solely to specify a removed API are deleted; composed behavior such as cache concurrency and run independence stays covered through the supported model.

## Decision A: experiment analysis without a new attachment API

The accepted removal of per-trial aspects already gives each trial's run its own results. The remaining issue is concrete algorithm typing: `Advised` returns a wrapper, so it cannot be assigned into a slot constrained to `HillClimber<T>`, for example.

**Accepted on 2026-11-05:** finish configuring each concrete algorithm before attaching analysis. Build grids over the advised cases using the existing `IAlgorithm<TCandidate, TSearchState>` path, as `ExperimentSpecs.TrialTraces_GiveEachConcreteAlgorithmItsOwnResult` already demonstrates. Do not add a grid-mapping operation or a trial callback API in step 3.

Representative usage:

```csharp
var algorithm = CreateHillClimber(problem);
var best = algorithm.Evaluator.Invocations.TraceBestSoFar();
var experiment = algorithm.Advised(best)
    .AsGrid()
    .VaryBy([4, 8], (_, maximumNeighbors) =>
        (algorithm with { MaxNeighbors = maximumNeighbors }).Advised(best))
    .Repeat(3);
var run = experiment.CreateRun(problem, random);
await run.CompleteAsync();
foreach (var trial in run.Trials)
{
    var curve = trial.Run.Result(best).Snapshot();
}
```

This example varies settings that retain the evaluator reference. If a case replaces an observed operator, create an instrument targeting that operator while constructing the advised case and retain its definition for reading the trial result. Reusing an instrument targeting the old evaluator would collect nothing from the new one.

Tradeoff: candidate and search-state typing remain, but direct access to concrete algorithm properties through the grid's algorithm key is lost at the wrapper boundary. Configure through the captured concrete algorithm and retain any reporting metadata explicitly at the caller. Preserving concrete typed keys while adding analysis would require a separately designed experiment transformation API. Live trial-tagged callbacks remain deferred as previously accepted.

Evidence required: repeated sequential and concurrent trials using one definition receive distinct results; a grid changing the observed evaluator targets each case's evaluator; results are readable independently as trials finish; RNG identities and scheduling behavior stay unchanged. No substitute live per-trial callback is introduced.

## Decision B: concrete accumulating instruments and their results

**Accepted on 2026-11-05:** use the existing `Instrument<TResult>` contract directly and remove the non-generic `AccumulatingAnalyzer : Aspect` base. Its only implementation is a shared lock; making it a new generic authoring hierarchy would add no necessary responsibility. Locks and mutable collection state belong to each result or its run-created collector.

| Definition                                                                | Per-run result                                | Migrated read                              |
| ------------------------------------------------------------------------- | --------------------------------------------- | ------------------------------------------ |
| `ParetoFrontAnalyzer<T>`                                                  | Existing `ParetoFront<T>`                     | `run.Result(front).Points`                 |
| `GenealogyAnalyzer<TCandidate, TSearchState>`                             | Existing `GenealogyGraph<TCandidate>`         | `run.Result(genealogy).ToGraphViz()`       |
| `RankAnalyzer<TCandidate, TSearchState>`                                  | Existing `RankState<TCandidate>`              | `run.Result(rank).Ranks`                   |
| `BestBeforeChangePerformanceAnalyzer<TCandidate, TSearchSpace, TProblem>` | New `BestBeforeChangePerformance<TCandidate>` | `run.Result(performance).BestBeforeChange` |

Keep the reduced `RankAnalyzer<TCandidate>` view needed by PythonInterop, deriving it from `Instrument<RankState<TCandidate>>`. It describes the result type without exposing source types; it no longer owns a `State` object.

Each definition snapshots its sources and retains immutable settings. `CreateResult` creates one accumulator; `CreateAspect` closes over that accumulator. A rank result owns one genealogy graph, and genealogy observation runs before rank recording, preserving the current ordering. Reuse the genealogy observation logic through an internal method accepting that run's graph rather than a definition containing another definition's live graph. No general fold framework or collector base is introduced.

`BestBeforeChangePerformance<TCandidate>` owns the epoch history, current best, sum, fitted model, prediction and synchronization. The definition retains its value selector and prediction multiplier, using the already accepted temporary delegate allowance. Read these accumulating results after their run has stopped; do not add snapshots or completion enforcement as a new mechanism.

### Dynamic problem dependency

The dynamic analyzer currently stores the `TProblem` passed to its constructor and reads that object during every callback. Merely moving its history into a result would still let a definition attached to another run read the original problem's epoch and objective.

As part of decision B, remove the constructor's problem instance and read the problem actually carried by each evaluation observation. Keep the existing generic parameters for this migration. An observation exposes its problem at the general candidate interface, so explicitly check `observation.Problem is TProblem problem` before updating the result and report a clear mismatch otherwise. This accepted check is necessary because the existing observation contract cannot express the dynamic requirement statically. No observation-contract redesign or problem binding at resolution is included.

Preserve the current rule that the epoch still in progress is not published as a completed epoch. In scenarios where the same definition is reused with two problem instances of the supported type, each result reads its own observations and starts with fresh prediction state. Concurrent reuse must not share a collector lock or accumulator.

Alternatives considered: retaining the supplied live problem leaves the wrong-problem hazard and requires an additional configuration exception; introducing typed dynamic observations changes another public contract; deferring the dynamic analyzer would leave the accumulating migration incomplete. The observation check is the smallest complete option, at the cost of runtime rejection for a mismatched problem.

## Implementation sequence

1. Implement the trace inference correction and its focused API usage evidence.
2. Migrate accumulating instruments and their consumers under decision B, then verify result independence and composition ordering.
3. Migrate experiment callers under decision A and other setup callers to `Advised`; remove run and experiment setup.
4. Migrate remaining accounting and custom fixture uses, then remove supply, handles, the builder and the obsolete accounting extensions. Retain supported cache/concurrency behavior tests.
5. Remove points, Cycle reporting and their analyzer checks; migrate boundary examples to `Calls`.
6. Fix the root README example, executable documentation regions, affected prose and the temporary developer-guidelines § 4.8 exception. List remaining step 4 prose work.
7. Run the complete validation, review the final diff against this scope, and record results. Return for discussion if implementation requires new scope rules, adapters, flags, casts other than the named dynamic observation check, or another execution mechanism.

## Validation and completion

Use the SDK selected by `global.json` and the commands in `AGENTS.md` and `test/README.md`. Focused checks cover trace inference; accumulating results across independent roots and concurrent runs; genealogy/rank ordering; completed-epoch behavior; experiment analysis isolation; call observations; and private budgets versus run-wide instruments.

Final checks are restore, Release build, whole-solution tests with one parallel test module, whitespace verification, warning-severity style verification, error-severity analyzer verification, the documentation build, Prettier on changed prose/configuration files, and Release packing. Audit deleted API references outside historical plans, including analyzer string and metadata matches. Python files, if changed, receive their separate validation; the solution build alone does not validate them.

Completion requires all in-repository callers migrated, no transitional public path retained, affected examples compiling, validation results recorded and known limitations stated. Any pre-existing correctness concern encountered is assessed on its own merits rather than ignored because a later simplification might remove it. Step 4's broad rewrite and selection of simplifications remain separate work.

## Inspection and baseline

- Inspected commit: `3608e0e0`, with an initially clean working tree.
- SDK selected by `global.json`: `10.0.402`.
- `dotnet restore`: passed after retrying with the access needed for NuGet.
- `dotnet build --configuration Release --no-restore`: passed with 174 warnings and no errors after retrying with access to existing build outputs.
- At plan preparation, no tests had been run and no C# source or public API had been changed. The implementation and validation below supersede that checkpoint.

## Implementation on 2026-11-05

The accepted migrations are implemented. Run construction has one path, the result table accepts instrument definitions only, and setup, supply, handles, observation points, Cycle point payloads, per-trial aspect mappings and live-accumulator accounting factories are removed. Runtime advice constructors remain available for private budgets and direct advice authoring. The accumulating analyzer base is removed; definitions create independent domain results, and PythonInterop reads rank results through the run.

The public trace overload infers its observation, state and value types without explicit arguments. Instrument definitions, counters and retention may be reused across runs with independent data. Dynamic performance reads each observed problem and rejects a mismatched type before updating its result. Advised experiment cases use the accepted existing grid path.

Validation evidence includes result publication across concurrent child lookups, retry after result or aspect creation failure, concurrent Pareto and genealogy/rank analyses, rank collection after genealogy updates, dynamic definitions reused with different problem instances, mismatched dynamic observations, and grids replacing observed evaluators. Existing call tests include empty turns and explicit versus automatic placement; the complete solution also exercises scope failures, budgets, sharing, caches, racing and integrations.

Published examples and directly contradictory contributor/guide/glossary passages were corrected. Broader step 4 organization and teaching work remains deferred, and no simplification proposal was implemented.

### Retained boundaries

- Advised grid cases retain candidate and search-state typing while concrete algorithm-key conveniences remain deferred, as accepted in decision A. No replacement trial callback or mapping API was introduced.
- Delegates in analysis definitions remain the accepted temporary persistence exception. Caller-captured state remains caller owned.
- Cycle call observations do not recreate cycle numbers, slot indexes or the deleted observation-point payloads.
- The previously recorded algorithm-executor control-forwarding gaps remain unchanged: step 3 does not alter those wrappers, and migrated callers do not depend on accessing custom algorithm controls through them. This is a pre-existing limitation to resolve separately, not a conclusion that a future simplification will remove it.
- No Python source was changed. PythonInterop C# consumers were migrated and are included in the solution validation.
- The developer reviewed the implementation and authorized committing it on 2026-11-05.

### Final validation

Using SDK `10.0.402`, build and test commands ran sequentially in this checkout:

- `dotnet restore`: passed.
- `dotnet build --configuration Release --no-restore`: passed with warnings and no errors.
- `dotnet test --solution ./HEAL.HeuristicLib.slnx --configuration Release --no-restore --max-parallel-test-modules 1`: all **3289** tests passed, with no failures or skips. The changed core, Experimental, API usage and documentation suites and the scenario suite are all included. The earlier baseline's higher count included tests specifying removed APIs; supported behavior was migrated and new isolation/publication cases were added.
- `dotnet format whitespace ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore`: passed.
- `dotnet format style ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore --severity warn`: passed.
- `dotnet format analyzers ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore --severity error`: passed. Format commands report workspace-loading warnings but succeed.
- `npm run docs:build`: passed, including imported example regions and link checking.
- Prettier on every changed Markdown file and `git diff --check`: passed.
- `dotnet pack --configuration Release --no-restore`: passed for core, Experimental and PythonInterop.
- The exact changed README and traveling-salesperson guide code blocks were compiled and executed against this checkout in temporary console projects using `berlin52.tsp`; both completed successfully. This caught and corrected a missing `Inspection` import in the README.
- The source/reference audit found no uses of deleted APIs in production, analyzers, tests, samples, Python samples or published docs. Historical plans retain their decision records.

The initial sandbox could not access NuGet or several existing output directories; the authorized restore/build/test/docs commands succeeded with the required access. No dependency, SDK or build configuration changes were necessary.
