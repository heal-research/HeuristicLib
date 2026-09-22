# Analysis API simplification

The approved [first-class analyzers and run lifecycle](first-class-analyzers-and-run-lifecycle.md) plan owns the current
rework. This document records the first simplification pass and contains decisions that the newer plan intentionally
revisits, including deleting `IAnalyzer`, introducing anchors and treating analyzers as execution hooks.

The accepted [analysis usability follow-up](analysis-usability-follow-up.md) now owns the next implementation sequence. It supersedes remaining sequencing and conflicting recommendations below, including deleting named measurements, replacing trial results with tuples, and deferring the documentation rewrite. This document records the first simplification and its earlier proposals.

Type names below predate the resolution-scope rename: `ExecutionInstanceResolver` is now `ResolutionScope`,
`ExecutionInstanceResolverBuilder` is now `ResolutionScopeBuilder`, and `IExecutionHook` is now `IExecutionModule`.
The text is left as written.

Aggregation and retention are no longer execution configurations, and accumulating analyzers publish live state rather
than snapshots. See the [analysis system rework](analysis-system-rework.md#status) for the decisions that revised this
document.

## Summary

The analysis rework in [analysis-system-rework.md](analysis-system-rework.md) landed the right model: analysis data belongs to one run, observations attach at declared anchors, and a trace composes from a measurement, an aggregation, a retention and a set of clocks. That model is sound and this plan does not reopen it.

What the implementation added alongside it is concept load. A user who wants "best quality over evaluations" currently meets six stacked layers and about fourteen distinct concepts before the shortcut method they should have called. Some of that is unavoidable — a composable system has parts. The rest is mechanical: overload sets that exist only because "where to observe" has no type, a marker interface with no members, clocks that hand-wire their own installation, and a second observation mechanism living in the `Analysis` namespace that is really control flow.

This plan removes the mechanical part. It does not change what analysis can express, and every user-facing `Analyzer.TraceXxx` shortcut keeps its current signature.

## Current inventory

Six layers, roughly 110 public types.

| Layer | What it is | Types |
| --- | --- | --- |
| L0 Wiring | `ExecutionInstanceResolver`, `ExecutionInstanceResolverBuilder`, `Decoration`, `DecorationOrigin`, `IExecutionHook`, `AlgorithmRun` hook list | ~9 |
| L1 Observation plumbing | `Observation` + 5 typed observations, `IObservationRecorder`, 5 internal `Observing*` wrappers, `Anchors` with 10 `Observe` overloads | ~12 |
| L2 Analyzer shape | `IAnalyzer`, `TraceAnalyzer<T>`, `TraceEntry<T>`, `TracePoint<TTime,T>`, `TraceSnapshot<T>`, `Analyzer` with 20 `Trace` overloads | ~6 |
| L3 Trace pipeline | `IMeasurement` + 6, `IAggregation` + 9, `IRetention` + 4, `Clock`/`Clock<T>`/`Moment` + 3 clocks, and the `Aggregate`/`Retain`/`Clocks` factories | ~30 |
| L4 Ready-made traces | 8 `extension(Analyzer)` classes across two assemblies | 8 |
| L5 Hand-written analyzers | `ParetoFrontAnalysis`, `GenealogyAnalysis`, `RankAnalysis`, `BestBeforeChangePerformanceAnalysis` | ~8 |
| L6 Overlapping systems | Instrumentation (`ObservationCounter`, `ObservationDuration`, 18 wrappers) and dynamic epoch timing (`IEpochSchedule`, `UpdatePolicy`, `EpochClock`, `EpochWork`) | ~35 |

The experiment side adds `TrialAnalyzer` (static), `TrialAnalyzer<A>`, `TrialAnalyzer<A,O,An>`, `TrialAnalysis<T,A>` and `TrialAnalysis` (static) — five types for "one analyzer per trial".

## What stays

These are decisions from the rework plan that this simplification upholds rather than revisits.

- A trace composes from **where**, **what**, **how** and **which clocks**. The four choices stay four choices.
- `Clock<TTime>` stays publicly derivable, and every clock is offered as a `Clock.From...` extension. A lambda-carrying clock is **not** introduced as a way to avoid writing a named clock type; that alternative was considered and rejected because a delegate cannot be compared or serialized.
- Retention stays a configured choice rather than a property of which analyzer type was picked.
- An analyzer is created before the run, bound by `CreateRun`, and holds one run's data with typed live reads. There is no result-lookup service on the run.
- Caller-owned instrumentation stays separate from analyzer-owned data. Item 5 below relocates it; it does not fold it into analysis results.

## The simplifications

Ordered by concepts removed per unit of risk. Items 1, 2 and 6 are in progress; the rest are queued behind them.

### 1. Give the anchor a type — `Anchor<TObservation>` · *done*

`Analyzer.Trace` has 20 overloads and `builder.Observe` has 10 for one reason: "where to observe" is five unrelated interfaces with different arities, so every combination is spelled out. Five anchor kinds × measurement-or-delegate × with-or-without-retention is exactly 20.

```csharp
public abstract class Anchor;                    // non-generic base, so factories can extend it

public abstract class Anchor<TObservation> : Anchor
    where TObservation : Observation
{
    public abstract void Install(ExecutionInstanceResolverBuilder builder, IObservationRecorder<TObservation> recorder);
}
```

An abstract class rather than an interface, mirroring `Clock`/`Clock<TTime>`: the non-generic base is what `extension(Anchor)` hangs the `Anchor.At(...)` factories on, exactly as `extension(Clock)` carries `Clock.From...`. Five public sealed anchors, one per kind, each living in the file already named for it and owning the `Observing*` wrapper that was already there. Then:

- `Analyzer.Trace`: 20 overloads → 4 (measurement or delegate, × with or without retention).
- `Anchors.Observe`: 10 overloads → 2 (recorder or delegate).
- New: 5 `Anchor.At` overloads.

Net 30 → 11, and the arity of every remaining signature drops because `TCandidate`, `TSearchSpace`, `TProblem` and `TSearchState` collapse into the single `TObservation`. `Analyzer.Trace` went from six type parameters to three.

Making the signature generic in `TObservation` exposed a problem that predates it, covered in item 9.

The second win matters more than the count. Observing a boundary the library does not cover is currently "write your own wrapper and call `Decorate` with the right shape" — the least documented and most error-prone corner of the surface. It becomes "implement one interface".

The `Analyzer.TraceXxx` shortcuts keep taking the operator and call `Anchor.At(...)` internally, so no user-facing shortcut signature changes.

### 2. Drop `IAnalyzer` · *done*

```csharp
public interface IAnalyzer : IExecutionHook;
```

No members. Its own documentation concedes the point: *"Analyzers are the common kind of hook, but nothing about this contract is analysis specific."* It is load-bearing only as a generic constraint on `TrialAnalyzer`, `TrialAnalysis` and `ExperimentRun.GetAnalyzers`, and `CreateRun` already takes `IExecutionHook` rather than `IAnalyzer`, so the code has half-moved already.

Constrain those three on `IExecutionHook` and delete the type. One fewer "what is the difference between these two?" for every reader of the analysis surface.

### 6. Clocks stop hand-wiring their own installation · *done*

`Clock.Install` currently means three different things:

- *decorate an anchor and record into myself* — `IterationClock`, `EvaluationClock`, which each publicly implement `IObservationRecorder<...>` **and** override `Install` to call `builder.Observe(anchor, this)`;
- *capture the starting timestamp* — `ElapsedTimeClock`, using install as initialization;
- *nothing* — `EpochClock`, an empty override with a comment explaining why it is empty.

Three changes, in dependency order behind item 1:

1. `Clock.Install` becomes a virtual no-op. `EpochClock`'s empty override disappears; a clock whose source keeps its own time overrides nothing. The base documents that `Install` runs exactly once while the run is being built, which is what makes `ElapsedTimeClock`'s use of it correct rather than incidental.
2. A new `ObservingClock<TTime, TObservation>` base takes an `IAnchor<TObservation>` and handles the wiring. `IterationClock` and `EvaluationClock` derive from it and are left with `Record` and `ReadTime` — nothing else. They stay named types with named settings, so the rejected lambda-clock alternative is not reintroduced.
3. Reading a trace with a clock it does not carry threw `KeyNotFoundException` from `TraceEntry.At` but `InvalidOperationException` from `TraceAnalyzer.By`. Both now throw `InvalidOperationException` with the same message.

A user writing a domain clock at an anchor now writes `Record` and `ReadTime` and nothing about installation. `TraceCompositionTests.CustomObservingClock_InstallsItselfAtItsAnchor` is that clock.

### 9. Ranking reads the run's objective through the problem · *done*

`IAggregation.Aggregate(readings, objective)` and `IRetention.ShouldRecord(value, objective)` need the run's objective, and the question was where it comes from. Four answers were tried and rejected before the right one; they are recorded in the decisions table because each looked plausible.

The structural fact that settles it was there the whole time: **all five observation records already declare `TProblem Problem`**, because every boundary the library observes is reached by calling an operator with the run's problem. Five independent declarations of one shared fact is what a base type is for:

```csharp
public abstract record Observation;

public abstract record Observation<TProblem>(TProblem Problem) : Observation
    where TProblem : class, IProblem;
```

`Objective` also moved onto a new non-generic `IProblem`, which it should always have been on: which direction is better does not depend on the candidate type.

`IAnchor<TObservation, TProblem>` refines `IAnchor<TObservation>` so a trace composed at an anchor infers the problem type, and `Analyzer.Trace` then reads `observation.Problem.Objective` directly. Nothing gains a member, nothing is bound after construction, nothing is type-tested at runtime, and the caller names no objective:

```csharp
Analyzer.TraceBestMedianWorst(algorithm)     // ranks by whatever the run optimizes
Aggregate.BestMedianWorst()
```

`RankingAggregation_TakesItsOrderingFromTheRun` pins this: the same composition ranks the opposite way when handed to a maximizing run, with nothing said at composition time.

### 3. Decide what serializable measurements are for · *queued*

The measurement/delegate split is what doubles the `Trace` overload count, and it exists so measurements can be *"serializable measurement configuration"*. But an analyzer is a run-bound mutable object that is explicitly not configuration, and the only thing in the repository exercising this is `TraceCompositionTests.CommonMeasurements_AreSerializableValueObjects`, which round-trips a measurement through JSON and asserts equality. No persistence infrastructure consumes it.

Either state what serializing a measurement buys, given the analyzer holding it cannot be serialized, or drop the six named measurement records and keep the lambda form. The six are one-line projections, and the `TraceXxx` shortcuts are what users should be calling.

Sequenced after item 1, because item 1 changes the overload arithmetic this decision is weighed against.

### 4. Make cross-firing accumulation a first-class fold · *queued*

There are two contradictory answers to "how do I accumulate across firings":

- `HyperVolumeAggregation` hides a growing `HyperVolumeState` inside an `IAggregation`, whose documentation says it *"reduces the readings taken at one firing"*.
- `ParetoFrontAnalysis` and `BestBeforeChangePerformanceAnalysis` write a whole hook instead, and both carry comments admitting the gap — *"which is the case a trace-based replacement still has to cover"*, *"Once analysis has an accumulator extension point, that fold is what this analyzer supplies to it"*.

The stateful-aggregation route already works, and `IRetention` is already documented as per-trace and per-run stateful. Legitimize it: either document `IAggregation` as run-bound and possibly stateful, or add an explicit `IAccumulation<TValue, TResult>` beside it. `ParetoFrontAnalysis` and `BestBeforeChangePerformanceAnalysis` then become traces.

This is the item that changes how the system reads: one analyzer shape instead of two, with a hand-written hook as the rare exception rather than a co-equal path. Genuinely multi-anchor analyses stay hooks — `GenealogyAnalysis` observes crossovers, mutators and algorithms together, and `RankAnalysis` needs its own recorder.

### 5. Instrumentation is a second observation system · *queued*

`ObservationCounter`, `ObservationDuration` and 18 `Counting*`/`DurationMeasuring*` wrappers do what an anchor and a recorder do, hand-rolled per operator kind and threaded into the budget algorithms through `Func<TOperator, ObservationCounter, TOperator>` factories. It lives in the `Analysis` namespace but it is control flow, and its presence there is part of why the analysis concept count reads as high as it does.

- **Safe:** move it out of `Analysis`. It feeds budgets; name it for that.
- **Bigger:** rebuild the budget algorithms on anchors, so `OperatorBudgetAlgorithm` installs a counting recorder instead of demanding an operator factory. That deletes 18 wrappers, 2 counter types and the factory parameter. `DecorationOrigin` still protects the comparer, provided budgets install at `Configuration` origin.

### 7. `TrialAnalyzer`: five types to two · *queued*

`TrialAnalyzer<TAlgorithm, TOperator, TAnalyzer>` carries `TOperator` only to hold a selector the caller could compose:

```csharp
// now
TrialAnalyzer.Create(a => a.Evaluator, e => Analyzer.TraceBestQuality(e))
// after
TrialAnalyzer.Create(a => Analyzer.TraceBestQuality(a.Evaluator))
```

Keep the abstract `TrialAnalyzer<TAlgorithm>` base for heterogeneous storage in `ExperimentRun`; keep one sealed `TrialAnalyzer<TAlgorithm, TAnalyzer>`. `GetAnalyzers` loses a type parameter, and `TrialAnalysis<T, A>` with its factory can be a tuple.

### 8. Small items · *queued*

- **`DecorationOrigin` should be internal.** It is public, but no caller can supply one: `Install` flips it and `Decorate` reads it. It is documentation wearing an API's clothes.
- **`*Analysis` versus `*Analyzer`.** The glossary defines *analyzer* as the stateful run object and *analysis snapshot* as the immutable value it publishes. `ParetoFrontAnalysis`, `GenealogyAnalysis`, `RankAnalysis` and `BestBeforeChangePerformanceAnalysis` are all analyzers wearing the snapshot's name.
- **`EpochWorkTrace.PerEpoch` reconstructs staleness post hoc**, re-deriving epoch boundaries from evaluation counts, with documented caveats: it breaks under a caching evaluator and mis-reports the final open epoch. The problem knows the exact epoch, evaluation count and staleness at the moment it advances. TraceRetention it there deletes the reconstruction and the caveats.
- **`docs/contributing/architecture/analyzers.md` documents the deleted system** almost end to end — `IAnalyzerRunState`, `ObservationPlan`, `run.GetResult()`, `ObservableAlgorithm`. This branch only renamed `Registry` to `Resolver` in it. It has to be rewritten against whatever shape items 1 through 8 settle on, not before.

## Decisions

| Decision | Rejected alternative | Why |
| --- | --- | --- |
| "Where to observe" becomes one interface, `IAnchor<TObservation>` | Keeping the five operator-typed overload sets; an abstract base class over the operator kinds | The overload count is the symptom; the missing type is the cause. An interface also turns "observe a boundary the library does not cover" from writing a wrapper and calling `Decorate` correctly into implementing one method |
| `Anchor.At(...)` is required at the hook level | Keeping operator-typed `Observe` sugar beside the anchor-based one | Keeping both restores the ten overloads it was meant to remove, and leaves two ways to say one thing. The five `Anchor.At` overloads are the one place the operator kinds are enumerated |
| `Analyzer.TraceXxx` shortcuts keep taking the operator | Making shortcuts take an anchor for consistency | The shortcut layer is where ordinary users live. `Analyzer.TraceBestMedianWorst(algorithm)` staying unchanged is the point of having the layer |
| `IAnalyzer` is deleted and its constraints become `IExecutionHook` | Keeping it as a documentation marker | It has no members, enforces nothing, and `CreateRun` already takes `IExecutionHook`. A marker that appears in constraints reads as a distinction that exists |
| `Clock.Install` becomes a virtual no-op with an `ObservingClock` base for anchor-driven clocks | A required `Install` on every clock; folding installation into the `Clock.From...` factories | Three of the four clocks were writing installation code that says the same thing or nothing at all. The factories are extension methods and cannot carry base behaviour |
| Named clock types stay, no lambda clock | A single clock class taking a read delegate | Upholds the rework plan's decision. A delegate cannot be compared or serialized, and a domain clock is a normal thing to write |
| Ranking reads the objective from `observation.Problem` | An abstract `Objective` on the `Observation` base | An observation is a capture of what happened at a boundary; what the run is optimizing is not part of what happened. Carrying the *problem* is different and was already the case |
| Ranking reads the objective from `observation.Problem` | An `ObjectiveOf(observation)` accessor on the anchor | Moved the same member from a data record to a boundary, where it belongs no better: an anchor names a location, it does not judge results |
| Ranking reads the objective from `observation.Problem` | An `IObjectiveAware` hook the run binds before execution | A marker interface plus mutation after construction, leaving a window where the analyzer is not yet valid, and requiring the run to type-test its hooks. Worse than what it replaced |
| Ranking reads the objective from `observation.Problem` | An `IProblemObservation<TProblem>` interface on the records | Correct mechanically, but a bolt-on interface that exists only to make one signature compile, with no relation to any other concept in the library |
| Ranking reads the objective from `observation.Problem` | The caller supplying it, as `Aggregate.Best(problem.Objective)` | Means a trace cannot simply rank by whatever the run optimizes, and lets a caller name an objective the run does not use |
| Ranking reads the objective from `observation.Problem` | Threading it through `IExecutionHook.Install` | `IExecutionHook` is deliberately not analysis specific, and `ExecutionInstanceResolverTests` installs hooks with no run or problem at all |
| `Objective` moves to a non-generic `IProblem` | Leaving it on `IProblem<TCandidate, TSearchSpace>` | Which direction is better does not depend on what is being searched, so the type parameters were never part of the question |
| `Anchor` is an abstract class with `extension(Anchor)` factories | An `IAnchor<TObservation>` interface | `Clock`/`Clock<TTime>`/`Clocks` already establishes this shape, and C# static extension members need a non-generic type to hang the factories on. Nothing needs an operator to be its own anchor |
| Items 3 to 8 are sequenced behind 1, 2 and 6 | Doing the whole simplification in one pass | Item 1 changes the arithmetic that item 3 is weighed against, and item 4 is the only one that changes what analysis can express. Landing the mechanical items first keeps the risky one readable in isolation |

## Sequencing

1. **Items 1, 2, 6 and 9 are done.** Landed together, since item 6's `ObservingClock` base depends on `Anchor` existing and item 9 surfaced while generalizing item 1. The whole solution builds warning-free, all four test suites pass, and the three formatting gates are clean.
2. Re-measure the concept count and decide whether items 3, 4, 5, 7 and 8 are still worth their churn.
3. Rewrite `docs/contributing/architecture/analyzers.md` once the shape has settled. It is wrong today either way, so it does not gate the code.

### What the first three items actually cost and bought

| | Before | After |
| --- | --- | --- |
| `Analyzer.Trace` overloads | 20 | 4 |
| `builder.Observe` overloads | 10 | 2 |
| `Anchor.At` overloads | — | 5 |
| `Analyzer.Trace` type parameters | 6 | 3 |
| Marker interfaces | `IAnalyzer` | — |
| `Anchor` | abstract class | interface |
| Declarations of `Problem` across the observation records | 5 | 1 (on the shared base) |
| Clock types writing their own installation | 3 of 4 | 1 of 4 (`ElapsedTimeClock`, which starts a stopwatch rather than observing) |

Two new tests cover the extension points this opened: `CustomAnchor_ObservesABoundaryTheLibraryDoesNotCover` observes a creator, which the library ships no anchor for, and `CustomObservingClock_InstallsItselfAtItsAnchor` derives a clock over crossover calls. The second also pins the decoration comparer that makes clocks read correctly, since it asserts the first generation is recorded at zero crossover calls.

Concept count is down from about fourteen to about eleven. Item 4 is what would take it to six.

## Source material

- [Analysis system rework](analysis-system-rework.md) — the model this plan simplifies rather than revisits
- [Analysis API sketch](analysis-api-sketch.md)
- [Analyzer architecture](../docs/contributing/architecture/analyzers.md) — currently describes the removed system
- [Analyzer glossary entries](../docs/guide/glossary.md#analyzer)
