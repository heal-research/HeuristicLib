# Analysis API sketch

## Purpose

This sketch fixes the user-facing shape of the replacement analysis system. The corresponding executable prototype is
`test/HeuristicLib.Tests.ApiUsageSpecs/Analysis/DesiredAnalysisSpecs.cs`.

The algorithm configuration stays pure. A caller creates stateful analysis objects, names their observation anchors and
clocks, and passes them to `CreateRun`. Run creation binds all analyses before the execution graph is materialized.
The returned `AlgorithmRun` can execute but cannot accept more analyses.

## The common case

```csharp
var iterations = Clock.FromIterations(algorithm);
var evaluations = Clock.FromEvaluations(algorithm.Evaluator);

var quality = Analyzer.TraceBestMedianWorst(
    at: algorithm,
    clocks: [iterations, evaluations]);

var run = algorithm.CreateRun(
    problem,
    RandomNumberGenerator.Create(seed: 42),
    quality);

await foreach (var state in run.Stream(cancellationToken))
{
    if (quality.Latest is { } latest)
        Console.WriteLine($"{latest.At(iterations)}: {latest.Value.Best}");
}

var final = quality.By(evaluations);
```

`TraceBestMedianWorst` is a convenience method over `Analyzer.Trace`. The analyzer exists before the run so the
caller can retain its strongly typed reading API. It becomes run-bound when `CreateRun` accepts it.

## Run creation is the composition boundary

`CreateRun` has an ordinary overload and an overload accepting analyses:

```csharp
algorithm.CreateRun(problem, random);
algorithm.CreateRun(problem, random, quality, lineage);
```

Conceptually the overload is:

```csharp
AlgorithmRun CreateRun(
    TProblem problem,
    IRandomNumberGenerator random,
    params IAnalyzer[] analyzers);
```

The parameter is not optional. The overload without analyses keeps ordinary execution concise, while the `params`
overload makes analysis visible without adding an `AlgorithmRunSetup` or another builder type.

Run creation validates analyses, installs clock and analysis observers, and then resolves the execution graph. This
lets the registry install replacements before their anchors materialize. It also makes late attachment impossible through
the type system because `AlgorithmRun` has no `Track`, `Attach` or `WithAnalysis` method.

An analyzer is authored for one run. The first implementation treats this as an API convention rather than maintaining
global ownership state or checking reuse. Shared accumulation across runs has different clock, completion and
ordering semantics, and remains deferred.

## Composing a series

`Analyzer.Trace` receives the independent choices directly and returns the stateful analyzer that will collect the
run's data.

```csharp
var bloat = Analyzer.Trace(
    observation => observation.State.Population.EvaluatedCandidates
        .Select(candidate => candidate.Candidate.Length)
        .ToArray(),
    Aggregate.MinMeanMax(),
    at: algorithm,
    clocks: [iterations]);

var diversity = Analyzer.Trace(
    new PairwiseSimilarityMeasurement(SimilarityCalculator.Levenshtein()),
    Aggregate.MinMeanMax(),
    at: algorithm,
    clocks: [iterations, elapsed],
    retention: Retain.EveryNth(10));

var bestSoFar = Analyzer.Trace(
    new ObjectiveVectorsMeasurement<...>(),
    Aggregate.Best(),
    at: algorithm,
    clocks: [evaluations],
    across: Across.RunningBest(),
    retention: Retain.OnChange());

var run = algorithm.CreateRun(problem, random, bloat, diversity, bestSoFar);
```

There is no separate universal analysis configuration and result lookup. Calling `Trace` twice creates two
independent stateful analyses. Each can be bound once.

## Naming anchors explicitly

Every analysis names its anchor before run creation. The root algorithm is explicit too. This keeps all run-specific
choices together and avoids a split where some analysis settings are universal while anchors appear later.

```csharp
var wholeRun = Analyzer.Trace(
    observation => observation.State.Population.EvaluatedCandidates
        .Select(candidate => candidate.Candidate.Length)
        .ToArray(),
    Aggregate.MinMeanMax(),
    at: algorithm);

var explore = Analyzer.Trace(
    observation => observation.State.Population.EvaluatedCandidates
        .Select(candidate => candidate.Candidate.Length)
        .ToArray(),
    Aggregate.MinMeanMax(),
    at: exploreStage);

var offspringSize = Analyzer.Trace(
    observation => observation.Offspring
        .Select(candidate => candidate.Length)
        .ToArray(),
    Aggregate.MinMeanMax(),
    at: crossover);
```

An anchor that does not belong to the execution graph is a run-creation error rather than an empty analysis.

```csharp
var detached = algorithm with { PopulationSize = 200 };
var analyzer = Analyzer.Trace(
    observation => observation.State.Population.EvaluatedCandidates
        .Select(candidate => candidate.Candidate.Length)
        .ToArray(),
    Aggregate.MinMeanMax(),
    at: detached);

Should.Throw<InvalidOperationException>(() =>
    algorithm.CreateRun(problem, random, analyzer));
```

## Reading while a run executes

Live reads are supported. The analysis object never exposes its mutable collection. Properties may return safe data
without allocation:

```csharp
quality.SampleCount;
quality.Latest;
quality.IsCompleted;
```

These reads are individually safe but are not a transaction. The run may publish another sample between two property
reads. A caller that needs a consistent collection asks for a snapshot.

```csharp
var samples = quality.Snapshot();
var byIteration = quality.By(iterations);
var byEvaluations = quality.By(evaluations);
```

Every returned snapshot or projection remains unchanged when the run publishes later observations. The same rules apply
during and after execution. Completion does not switch the analysis to another reading API.

The first implementation uses ordinary locking and snapshot copies. A more complex store needs measured evidence that
copying is a problem.

## Moments and clock projections

Each trace entry belongs to one moment containing exactly the times read from the clocks selected for that analysis. No
clock is automatic. Clocks are typed objects tied to explicit observation sources.

```csharp
var iterations = Clock.FromIterations(algorithm);             // long
var evaluations = Clock.FromEvaluations(algorithm.Evaluator); // long
var elapsed = Clock.FromElapsedTime(TimeProvider.System);            // TimeSpan
var epoch = Clock.FromEpoch(dynamicProblem);                         // int, a domain clock
```

The clock is also the typed read key. This supports several clocks of the same kind without enums or
nullable moment fields. Asking a trace for a clock it does not use is a usage error.

Clocks use three acquisition modes behind the same typed source API. A source reads directly from the current
immutable observation when the analyzer and clock share an anchor. A source on another anchor retains its
latest observed value. A source such as elapsed time reads its value when the analyzer captures the moment. Only selected
cross-boundary sources install observable replacements, so an unused clock has no observation cost. Analyzer results
do not depend on callback registration order.

## Compile-time anchor compatibility

An anchor determines the typed observation a measurement receives. For the crossover spike, the anchor maps directly to
`CrossoverObservation<TCandidate, TSearchSpace, TProblem>`:

```csharp
var offspringSize = Analyzer.Trace(
    measure: observation => [observation.Offspring.Count],
    aggregate: Aggregate.MinMeanMax(),
    at: crossover);
```

The measurement can read offspring, parent pairs, search space or problem from the observation. The binding does not
project an intermediate supply or candidate list. A quality measurement written for an algorithm observation has no
crossover overload and therefore does not compile at a crossover anchor.

```csharp
Analyzer.Trace(new ObjectiveVectorsMeasurement<...>(), Aggregate.BestMedianWorst(), at: crossover);
// must not compile

var offspringSize = Analyzer.Trace(
    observation => observation.Offspring
        .Select(candidate => candidate.Length)
        .ToArray(),
    Aggregate.MinMeanMax(),
    at: crossover);
// compiles
```

Role-specific `Trace` overloads establish the anchor-to-observation mapping. They can target-type a runtime-only
measurement lambda. Common and reusable measurements are immutable classes implementing
`IMeasurement<CrossoverObservation<...>, TValue>`, so analyzer configuration does not depend on serializing a delegate.

## Analyses that are not series

Lineage uses the same binding and lifecycle model and exposes operations that make sense for a graph.

```csharp
var lineage = Analyzer.TrackLineage(
    at: [creator, crossover, mutator],
    comparer: ExpressionTree.StructuralComparer);

var run = algorithm.CreateRun(problem, random, lineage);

await foreach (var state in run.Stream(cancellationToken))
{
    Console.WriteLine($"roots so far: {lineage.RootCount}");
    var currentGraph = lineage.Snapshot();
}
```

One call with several anchors produces one lineage object fed by all anchors. Separate calls produce independent analyses.

## Experiments

Experiment analysis must preserve one independently bound series per trial. The exact pre-run authoring shape remains open
until ordinary algorithm-run binding is implemented. Trial analyses never share mutable state.

## Settled

1. Algorithm configurations do not own analyses.
2. Callers create stateful analysis objects before creating a run.
3. `CreateRun(..., params IAnalyzer[] analyzers)` installs analyzer observations before returning the run.
4. `AlgorithmRun` has execution methods but no late-attachment methods.
5. An analyzer is used with one run by convention. Cross-run accumulation is deferred.
6. `Analyzer.Trace` is the main trace composition method.
7. Convenience methods such as `Analyzer.TraceBestMedianWorst` preconfigure `Trace`.
8. Live reads work during streaming and after completion.
9. Properties return published values without allocation. Methods may allocate immutable snapshots or projections.
10. Measurements receive typed observations; role-specific `Trace` overloads establish each anchor-to-observation mapping.
11. Non-series analyses use the same binding lifecycle and expose analysis-specific reads.
12. Every clock is opt-in and names the observation source that defines it.
13. A typed clock is also the typed key used by `TraceEntry.At` and `TraceAnalyzer.By`.
14. Same-anchor times come from the current observation rather than callback order.
15. Observation registrations are installed before execution instances are resolved.

## Open for implementation

1. Whether unprojected snapshots expose trace entries publicly or keep them internal.
2. Whether `Latest` returns nullable `TraceEntry<T>`, uses `TryGetLatest`, or exposes both.
3. Whether a concrete analysis demonstrates the need for a problem-dependent measurement input.
4. Whether across-firing accumulation remains a separate slot after implementing the real aggregations.
5. Whether generic numeric measurements can infer cleanly without standardizing the first catalog on `double`.
6. The experiment authoring shape under pre-run analysis binding.
7. The future shape of explicitly shared cross-run analysis.
8. The smallest custom-analyzer API that receives typed observations without retaining `ObservableOperator`,
   `I...Observer` and `ObserveWith` as a second public system.
9. The accumulator extension point for custom stateful, non-trace analysis. Custom trace measurements already receive
   typed observations and do not require a separate public observation-installation API.
10. Benchmark class-based observations against readonly structs before applying the crossover representation to every
    execution role.
