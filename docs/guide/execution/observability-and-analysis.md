# Observability and analysis

An analyzer collects data from the execution boundaries you select. Create it, attach it to a prepared run, then read its typed properties or take a snapshot.

```csharp
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;

var run = algorithm.CreateRun(problem, random)
    .TracePopulationCandidates(out var quality);
await run.CompleteAsync();

var best = quality.RequireLatestValue().Best;
```

An algorithm observation captures each search state it yields, after its interceptors have transformed that state. No placeholder interceptor is needed. For simple progress logging, consuming `algorithm.Stream(problem, random)` directly is also sufficient.

## Choose the right tool

An analyzer is one of several ways to see what a run does. Pick by what you need:

| Need                                                                                        | Use                                                                                            |
| ------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| Look at every state the algorithm yields                                                    | `algorithm.Stream(problem, random)`                                                            |
| Only the final state                                                                        | `algorithm.CompleteAsync(problem, random)`                                                     |
| Typed data from chosen boundaries, such as a quality curve or an operator's offspring       | An analyzer attached with `AddAspect`                                                          |
| Behavior at a boundary that is not analysis, such as logging or bridging to another runtime | An aspect added with `AddAspect`                                                               |
| A count or duration that stops or limits the run                                            | Inspection such as `CountCandidates` or `LimitedToCandidates`, read by terminators and budgets |
| The same analysis for every experiment trial                                                | A trial analyzer, see [Experiments](/guide/execution/experiments)                              |

Inspection preserves the observed computation while recording its own observations, counts, durations and clocks. Analyzers derive results from those observations. Observation callbacks must preserve the operation's inputs and outputs; they receive live references rather than isolated deep copies. General aspects may change behavior, and explicit algorithm policies such as budgets may read inspection data to decide when to stop.

Inspection of creation, crossover, mutation, evaluation, selection, refinement and replacement passes on the selected executor's controls. Generic naming and retry wrappers do too, so inspection through those wrappers keeps mutation-strength adaptation. Composites expose their own controls.

## Choose a boundary and a clock

Each operator offers its successful calls as `Calls`, and each algorithm offers its yielded states as `Iterations`. These observation sources describe a boundary; creating one installs nothing. An evaluator's `Calls` observes one completed evaluator call, including the whole batch. An algorithm's `Iterations` observes a yielded state. A nested algorithm is a separate observation boundary.

Ready-made traces are extensions on sources, such as `evaluator.Calls` or `algorithm.Iterations`:

```csharp
var evaluations = Clock.FromEvaluations(algorithm.Evaluator);
var best = algorithm.Evaluator.Calls.TraceBestSoFar(clocks: [evaluations]);
var population = algorithm.Iterations.TracePopulationQuality(clocks: [evaluations]);
var run = algorithm.CreateRun(problem, random)
    .AddAspect(best)
    .AddAspect(population);
await run.CompleteAsync();
```

`Clock.FromElapsedTime(timeProvider)` measures time since the clock was constructed. Create it when that interval should begin.

Several traces may share the same clock in one run. Its advice applies once. Use that same clock object to project the trace; constructing another clock does not identify the original axis.

Evaluation counts describe the selected evaluator boundary. A caching evaluator's outer boundary can count requests while its inner evaluator counts cache misses. Choose the boundary whose work you intend to compare.

Observation sources match configurations by reference. A copied configuration is observed separately, even if its settings compare equal. A configuration that never participates produces no observations. Create analyzers for the actual configured objects that will execute. An operator implementing several operations offers each operation's source through that operation's typed view.

### Sources and `IObservable<T>`

A source resembles a cold `IObservable<T>`: it describes observations without producing any until something subscribes. Here, `Analyzer.Trace` and `Aspect.Observe` subscribe by creating an aspect, and adding that aspect to a run or scope attaches it. The library implements neither `IObservable<T>` nor Rx-style operators. A callback can feed observations into Rx, for example into a subject:

```csharp
var subject = new Subject<CrossoverObservation<RealVector>>();
var feed = Aspect.Observe(crossover.Calls, subject.OnNext);
var run = algorithm.CreateRun(problem, random).AddAspect(feed);
```

The callback runs on the thread that called the operator, inside the operation's wrapped call.

## Select configurations by properties or names

`Pointcut.Predicate<TConfiguration>(predicate)` matches configuration data without a custom subclass. Combine pointcuts through `And`/`Or` or `&`/`|`, and negate them through `Not()` or `!`. Negation stays within the declared configuration type. Predicates are runtime-only delegates, rather than persistable configuration strategies.

Name a configured operator through `.Named(name)` and select that boundary explicitly:

```csharp
var exploration = mutator.Named("exploration");
var selected = Pointcut.Named<IOperator<Mutation, RealVector>>("exploration");
var excluded = !Pointcut.Reference<IOperator<Mutation, RealVector>>(exploration);
var otherExploration = selected & excluded;
```

The receiver here is an `IOperator<Mutation, RealVector>`. Named wrappers exist for creators, crossovers, evaluators, mutators, refiners, replacers, selectors, interceptors, terminators and algorithms. Mutation and evaluation use `Named.Create(child, name)`; other roles retain their role-specific construction companions. A consumer-defined configuration can implement `INamedConfiguration` directly.

Names compare exactly and case-sensitively. Several nodes may share a name, and all match. The wrapper and its child are separate configuration nodes: name matching selects the wrapper, while a broad role pointcut can select both boundaries. Names do not change execution sharing or create caller paths. The child's concrete properties remain on the child configuration.

## Ownership and reads

A run accepts analyzers and other aspects through `AddAspect` while its lifecycle is `Preparing`. It collects them in the supplied order; resolution weaves their advice into the execution graph when the run starts. Adding the same aspect again does not apply its advice twice. Create fresh analyzers and clocks for independent results. You may reuse them across runs when combined history or cumulative counts are intentional. Adding an aspect does not reset its collected data.

`LifecycleState` reports whether a run is `Preparing`, `Running`, `Paused`, `Completed`, `Canceled`, `Failed` or `Stopped`. Calling an execution entry point freezes its attachments. Each returned execution stream has one consumer. The run itself can continue through a later `Stream()` call after the consumer stops at a yielded root-algorithm state.

`SampleCount` counts recorded entries. `Latest` is the latest recorded entry, or null before any entry is recorded. These properties do not allocate. `Snapshot()` and `By(clock)` allocate stable copies. Taking a snapshot does not stop collection.

Results remain available after completion, cancellation or pausing. An analyzer has no completion flag because another run may still be using it. Disposing a stream enumerator pauses the run without disposing its underlying algorithm iterator.

## Retention and accumulation

`TracePopulationQuality` records the current best, median, and worst of each observation's objective vectors, such as those of a yielded population. `TraceBestSoFar` accumulates the best objective vector across observations, such as evaluator calls, and records only improvements by default. Use `TracePopulationCandidates` or `TraceBestCandidateSoFar` when you need to retain the candidates behind those values.

```csharp
var quality = algorithm.Iterations.TracePopulationQuality(clocks: [iterations], retention: TraceRetention.EveryNth(10));
var bestAtEveryEvaluation = algorithm.Evaluator.Calls.TraceBestSoFar(clocks: [evaluations], retention: TraceRetention.EveryObservation());
```

`TraceRetention.EveryNth(10)` records observations 10, 20, 30, and so on. It adds no extra initial or final entry. `TraceRetention.OnChange()` records the first value and subsequent changes by value equality, and `TraceRetention.OnChange(comparer)` decides sameness with a comparer of your own. `TraceRetention.LatestOnly()` stores each value over the one before it, so the trace holds a single entry carrying the moment of the observation that produced it. Use it when you want what a trace says now rather than how it moved, and read it with `Latest` or `RequireLatestValue()`. A retention counts for the object it is, so give each trace its own, which the factories do.

Retention runs after a trace's function reads an observation. It controls stored entries and never skips expensive computations, so best-so-far still sees an improvement between retained observations. `EveryNth` and `OnChange` leave stored entries alone; `LatestOnly` is the one policy that discards what the trace already held.

## Accumulating analyzers

Some analysis produces one mutable summary rather than a history. Derive from `AccumulatingAnalyzer`, assign observation advice in the constructor, and update the summary under `Sync`:

```csharp
public CandidateSummaryAnalyzer(IOperator<Evaluation, TCandidate> evaluator)
{
    Advices = Aspect.Observe(evaluator.Calls, Record).Advices;
}

private void Record(EvaluationObservation<TCandidate> observation)
{
    lock (Sync)
        accumulator.Add(observation.Candidates);
}
```

Read an accumulating analyzer after the run finishes. Clocks and retention apply to trace entries, so they do not apply to a single mutable summary.

## Custom traces

`Analyzer.Trace` reads one value from each observation of a source through a function:

```csharp
var size = Analyzer.Trace(algorithm.Iterations,
    observation => observation.State.Population.EvaluatedCandidates.Count);
var offspring = Analyzer.Trace(crossover.Calls, observation => observation.Offspring.Count);
var quality = Analyzer.Trace(evaluator.Calls,
    observation => observation.ObjectiveVectors.BestMedianWorst(observation.Problem.Objective));
```

Each operation has its own observation record, such as `CrossoverObservation<TCandidate>` with `Parents` and `Offspring`, or `EvaluationObservation<TCandidate>` with `Candidates` and `ObjectiveVectors`. It holds the call's inputs and outputs and reads the search space and problem at the candidate's interfaces, so it fits every run over the candidate.

Counting, collecting and single values are plain functions of the observation. Ranking functions such as `Best` and `BestMedianWorst` receive the objective directions they order by, usually the observed call's own, `observation.Problem.Objective`, so each call is ranked by its own problem. `MinMeanMax` summarizes plain numbers and needs no directions.

A best-so-far object keeps the running best across observations. `BestSoFar` keeps the best objective vector; `BestSoFar<T>` keeps any other value, ranked by the objective vector its selector reads, such as `new BestSoFar<EvaluatedCandidate<RealVector>>(candidate => candidate.ObjectiveVector)`. Create one per trace and update it from the trace's function:

```csharp
var best = new BestSoFar();
var bestSoFar = Analyzer.Trace(evaluator.Calls,
    observation => best.Update(observation.ObjectiveVectors, observation.Problem.Objective));
```

The first directions it receives fix it; other directions or another dimensionality throw. The ready-made traces create their own best-so-far and retention objects, as `TraceRetention.EveryNth(10)` returns a fresh object every call. Hoisting one into a variable and passing it to two traces makes them share its state.

To collect several sources of one observation type into one accumulator and result sink, create one combined trace:

```csharp
var calls = new[] { firstAlgorithm.Evaluator.Calls, secondAlgorithm.Evaluator.Calls };
var combined = calls.TraceBestSoFar();
```

Attach `combined` to both runs to intentionally accumulate shared history. Separate traces have independent state when each receives its own best-so-far and retention objects.

A combined trace observes each boundary once. Giving it the same source twice records each call once. The sources of different operations of one operator observe different boundaries, and each is recorded.

Take one snapshot when you want consistent projections onto several axes:

```csharp
var snapshot = population.Snapshot();
var byEvaluations = snapshot.By(evaluations);
```

The snapshot preserves its selected axes even when empty. Selected clocks are read when an entry is recorded. Clocks on independent concurrent boundaries do not promise an atomic global timestamp.

## Multi-objective ranking

Single-objective traces use the observed problem's objective comparer automatically. A multi-objective problem without a total order cannot supply a unique best, median, or worst. Ranking throws when first required, even for a singleton batch. There is no implicit lexicographic fallback.

Select an objective comparer explicitly when the trace needs a total order:

```csharp
var best = algorithm.Evaluator.Calls.TraceBestSoFar(objectiveComparer: new LexicographicComparer(problem.Objective.Directions));
```

This makes dimension priority explicit and affects this trace's comparisons, including best-so-far accumulation. It does not change the problem or algorithm's objective. Analysis accepts the objective system's existing `IComparer<ObjectiveVector>` implementations. The `RequireTotalOrder` helper also serves callers outside analysis. Ranking is checked at observation time because installation has no problem context.

## Experiments

An experiment creates fresh analyzers for every trial from a function of the trial's algorithm. `AddAspectPerTrial` returns the typed trial/analyzer pairs; read each analyzer directly. See [Experiments](/guide/execution/experiments).

## Observe an operation of your own

An operation you define becomes observable when it declares an observation record, the advice that builds it, and a `Calls` source, as each built-in operation does. Its source then works with `Analyzer.Trace` and `Aspect.Observe` like any other. An observation that carries the problem of the call it observed lets a trace rank by that problem's objective. See [Make the operation observable](/contributing/architecture/operator-implementation#make-the-operation-observable) for a complete example.

A configuration whose calls receive no problem can still be observed and traced, one value per observation. Construct its source with the aspect that reports each value, for example for a cooling schedule of your own and its recording advice:

```csharp
var temperatures = new ObservationSource<double>(schedule,
    observe => Aspect.Create(Pointcut.Reference(schedule), new RecordedCoolingAdvice(observe)));
var trace = Analyzer.Trace(temperatures, static temperature => temperature);
```

Such an observation carries no objective, so its trace reads plain values and ranks nothing.

## Use an aspect directly

`Aspect.Create` pairs a pointcut with advice. Pass it to `ResolutionScope.Create`, `With` or `CreateChildScope`, or add it to a preparing run with `AddAspect`.

```csharp
var counter = new CountAccumulator();
var counting = Aspect.CountCalls(algorithm.Mutator, counter);
var duration = new DurationAccumulator();
var timing = Aspect.MeasureDuration(algorithm.Mutator, duration, TimeProvider.System);
var observation = Aspect.Observe(algorithm.Mutator.Calls,
    value => Console.WriteLine(value.Offspring.Count));
var run = algorithm.CreateRun(problem, random)
    .AddAspect(counting)
    .AddAspect(timing)
    .AddAspect(observation);
await run.CompleteAsync();
```

Earlier advice wraps closer to the source executor. Here timing finishes before the observation callback, so logging is outside the timed span. Counting updates only after success; timing uses `finally` and records failed calls too.

To select several configurations, pair a pointcut with role advice:

```csharp
var counting = Aspect.Create(Pointcut.OfType<IOperator<Mutation, RealVector>>(),
    new CountingOperatorAdvice<Mutation, RealVector>(counter, OperatorCountMetric.Candidates));
```

Custom selection derives a small class:

```csharp
sealed class SmallMutations : Pointcut<IOperator<Mutation, RealVector>>
{
    public override bool Matches(IOperator<Mutation, RealVector> mutator) =>
        mutator is GaussianMutator { MutationStrength: < 1.0 };
}
```

Reference, type and custom pointcuts of the same configuration type combine with `And`/`&` and `Or`/`|`. Matching short-circuits from left to right. The pointcut sees configurations that resolution reaches, including those created later by a meta-algorithm.

An aspect is the ordered list of its pointcut/advice pairs. An aspect subclass assigns several pairs to `Advices` in its constructor and can spread a clock's pairs before its own observations, as in `Advices = [.. clock, .. Aspect.Observe(evaluator.Calls, Record)]`. Resolution applies each advice instance once, at its first position; sharing a clock across several traces therefore counts each call once. If the same advice is paired with two pointcuts, the first pairing wins. Use separate advice instances for separate selections.

A new scope layer precedes inherited layers. Advice keeps its data on the advice object. Algorithm observation counts iterations there, so the count continues across every execution of the observed algorithm, including a meta-algorithm's fresh activations; reusing the aspect across runs continues it too. Reusing operator advice, or its caller-owned accumulator, intentionally combines its counts or durations.

Continue with [Writing custom advice](/guide/extending/writing-advice) for complete role-specific and general advice examples and run attachment.
