# Migrating `dynamic-Racing` to substitution-based observation

`core-simplification` replaces aspects and advice for observation with wrappers placed in the configuration graph before it is resolved. Phase 3 step 4 of `plans/core-simplification-plan.md` ports `DynamicRacingAlgorithm` as it stands on that branch and deletes `Aspect.Observe`, `DynamicProblem.CreateIterationUpdateAspect` and racing's contender advice. This note lists what `dynamic-Racing` (last commit `c35ebf82`) needs when it merges. The branch merges these changes itself.

## Working examples

- `DynamicRacingAlgorithm.Entry` in `src/HeuristicLib.Experimental/Algorithms/DynamicRacingAlgorithm.cs` builds a contender's tracked graph once and resolves it in a fresh child scope at every restart.
- `test/HeuristicLib.Tests.Experimental/Algorithms/DynamicRacingResolutionTests.cs`:
  - `AContendersTracker_SpansItsRestarts`: the tracker survives iterator restarts.
  - `Contenders_EndTheRacesEpochWhetherOrNotTheBuilderAppliesIterationUpdates`: racing applies iteration updates to each contender, so the race's epoch ends under `AfterEachIteration`.
  - `TheBuilder_ReceivesTheAlgorithmItReturnedWithoutTheTracker`: challengers are built from the untracked algorithm.
  - `ATraceOfContenders_IsPlacedInTheBuilder` and `NewExecutorStartsIndependentBurnInWhilePausedExecutorKeepsProgress`: observing contenders from outside racing.
- `test/HeuristicLib.Tests.Experimental/Problems/Dynamic/Analysis/DynamicAnalysisTests.cs`: `ApplyingIterationUpdates()` with pausing, disposal and `Supports`.

## Replacements

| `dynamic-Racing` uses                                                    | Replacement                                                                                                                                 |
| ------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------- |
| `Aspect.Observe<TCandidate, TSearchSpace, TProblem>(evaluator, observe)` | `algorithm.Substitute(evaluator.Invocations.Observing(observe))`, a new graph that racing keeps to itself                                   |
| `problem.CreateIterationUpdateAspect(algorithm)`                         | `algorithm.ApplyingIterationUpdates()` from `HEAL.HeuristicLib.Problems.Dynamic`                                                            |
| `Scope.With(aspects)` before `Resolve`                                   | Nothing: the graph holds the wrappers before it is resolved                                                                                 |
| `.AddAspect(qualityCurve).AddAspect(bbcp)` on a shared evaluator         | `var analyzed = evaluator.AnalyzedBy(qualityCurve, bbcp)`, with `evaluator` typed by its interface, before `AlgBuilder` captures `analyzed` |
| `Aspect.Observe(...)` inside the branch's own analyzers                  | `source.Observing(callback)` for a callback, or a trace from `Analyzer.Trace`, placed with `AnalyzedBy`                                     |
| `CreateForkedScope`, `WithContext`, `ExecutionFactory.Forkable`          | The branch's own feature; the merge moves it onto the reduced scope of phase 4                                                              |

`RacingEntry.CreateEnumerator` then becomes:

```csharp
// Built once per performance object: a new ContenderPerformance needs a new graph.
tracked = ((IAlgorithm<TCandidate, TSearchState>)Algorithm)
    .ApplyingIterationUpdates()
    .Substitute(evaluator.Invocations.Observing(performance.Observe));

// Each start or restart resolves that graph in a fresh child scope, so it gets fresh executors.
private IEnumerator<TSearchState> CreateEnumerator(TProblem problem, IRandomNumberGenerator random,
                                                   TSearchState? initialState, CancellationToken ct) =>
    Scope.CreateChildScope().Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(tracked)
        .Stream(problem, random, initialState, ct).GetEnumerator();
```

`ApplyingIterationUpdates()` only acts under `UpdatePolicy.AfterEachIteration`, so wrapping every contender is harmless under the other policies. Racing wraps inside, as above, and `AlgBuilder` stays unaware of it. A builder that wraps what it returns as well changes nothing, because the second wrapper finds no update pending.

## Points to keep in mind

- **Lifetimes differ.** HEAD keeps one tracker per entry across iterator restarts. The branch creates a new `ContenderPerformance` in `Restart`, and a new performance object needs a new graph built from it. A restart that keeps its performance reuses the graph.
- **`AlgBuilder` keeps receiving the untracked algorithm.** Keep `Entry.Algorithm` as what the builder returned and the tracked graph private, because challengers are built from `entries[0].Algorithm`. Otherwise the incumbent's tracking would leak into them.
- **Use `Substitute` in racing, not `AnalyzedBy`.** Racing is algorithm content, which must not reference `HEAL.HeuristicLib.Analysis` (`LayerDependencyTests` checks this). `Observing` moved to `HEAL.HeuristicLib.Inspection` for this reason. `AnalyzedBy` stays in `Analysis` and is only `Substitute` typed by the receiver's interface.
- **The selected evaluator must be one the contender holds.** The substitution matches it by reference, and a target it does not find is an error instead of an observation that never fires. It still finds the evaluator through the `ApplyingIterationUpdates()` wrapper.
- **Observations read the problem at its interface.** `EvaluationObservation<TCandidate>.Problem` is an `IProblem<TCandidate, ISearchSpace<TCandidate>>`; read the objective from `evaluation.Problem.Objective`.
- **An outer analyzer does not reach contenders.** A substitution stops at delegates such as `AlgBuilder`. A target named by reference reports `AlgBuilder` by name, and a selection by type or predicate passes over contenders without an error. Substitute inside the builder, or analyze a shared operator before the builder captures it.
- **Racing applies iteration updates itself.** Under `AfterEachIteration`, the epoch loop ends only when a contender applies pending updates, so racing wraps every tracked contender. Keep that wrapper when the branch rebuilds the tracked graph on a restart.
