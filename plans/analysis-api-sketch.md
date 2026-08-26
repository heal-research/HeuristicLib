# Analysis API sketch

## Purpose

This sketch fixes the user-facing shape of the replacement analysis system. The corresponding executable prototype is
`test/HeuristicLib.Tests.ApiUsageSpecs/Analysis/DesiredAnalysisSpecs.cs`.

The main API has one concept: a run-bound analysis. A `Track...` method creates it, attaches it to one or more observation
anchors and returns it. The object collects data while the run executes and provides safe reads during and after execution.

## The common case

```csharp
var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42));
var quality = run.TrackBestMedianWorst();

await foreach (var state in run.Stream(cancellationToken))
{
    if (quality.Latest is { } latest)
        Console.WriteLine($"{latest.Stamp.Iteration}: {latest.Value.Best}");
}

var final = quality.ByEvaluations();
```

`TrackBestMedianWorst` is a convenience method over `TrackSeries`. It takes the root anchor from the run and preconfigures
the measurement and aggregation.

## Composing a series

`TrackSeries` is the main composition entry point. It receives the independent choices directly and returns the attached,
stateful series.

```csharp
var bloat = run.TrackSeries(
    Measure.TreeLength(),
    Aggregate.MinMeanMax());

var variables = run.TrackSeries(
    Measure.VariablesUsed(),
    Aggregate.Frequency());

var diversity = run.TrackSeries(
    Measure.PairwiseSimilarity(SimilarityCalculator.Levenshtein()),
    Aggregate.MinMeanMax(),
    recording: Recording.EveryNth(10));

var bestSoFar = run.TrackSeries(
    Measure.Quality(),
    Aggregate.Best(),
    across: Across.RunningBest(),
    recording: Recording.OnChange());
```

There is no separate universal analysis configuration in the ordinary API. Calling `TrackSeries` twice creates two
independent run-bound analyses. A reusable definition may be added later if repeated composition proves common enough to
justify another public concept.

## Attaching to anchors

Without `at`, `TrackSeries` observes the run's root algorithm. A caller supplies `at` for an inner algorithm or operator.

```csharp
var wholeRun = run.TrackSeries(Measure.TreeLength(), Aggregate.MinMeanMax());

var explore = run.TrackSeries(
    Measure.TreeLength(),
    Aggregate.MinMeanMax(),
    at: exploreStage);

var offspringSize = run.TrackSeries(
    Measure.TreeLength(),
    Aggregate.MinMeanMax(),
    at: crossover);
```

Each call returns a different stateful object. An anchor that does not belong to the execution graph is a configuration
error rather than an empty analysis.

```csharp
var detached = algorithm with { PopulationSize = 200 };

Should.Throw<InvalidOperationException>(() => run.TrackSeries(
    Measure.TreeLength(),
    Aggregate.MinMeanMax(),
    at: detached));
```

## Reading while a run executes

Live reads are supported. The analysis object never exposes its mutable collection.

Properties may return data that is already safe to share without allocation:

```csharp
quality.SampleCount;
quality.Latest;
quality.IsCompleted;
```

These reads are individually safe but are not a transaction. The run may publish another sample between two property
reads. A caller that needs a consistent collection asks for a snapshot.

Methods may allocate an immutable snapshot or construct a projection:

```csharp
var samples = quality.Snapshot();
var byIteration = quality.ByIteration();
var byEvaluations = quality.ByEvaluations();
```

Every object returned by one of these methods remains unchanged when the run publishes later observations. The same rules
apply during and after execution. Completion does not switch the analysis to a different reading API.

The first implementation should use ordinary locking and snapshot copies. A more complex publication store needs measured
evidence that copying is a problem.

## Stamps and coordinate projections

Each sample carries one stamp. The run contributes iteration and evaluation coordinates; a problem may contribute a domain
coordinate such as an epoch.

```csharp
var current = quality.Snapshot();

current[0].Stamp.Iteration.ShouldBe(1);
current[0].Stamp.Evaluations.ShouldBe(100);
current[0].Value.Best.ShouldNotBeNull();

foreach (var point in quality.ByIteration())
    Console.WriteLine($"{point.Coordinate}: {point.Value.Best}");

foreach (var point in quality.ByEvaluations())
    Console.WriteLine($"{point.Coordinate}: {point.Value.Best}");
```

The projection methods capture stable reads. They do not return deferred queries over mutable storage.

## Transposing keyed series

```csharp
var variables = run.TrackSeries(
    Measure.VariablesUsed(),
    Aggregate.Frequency());

foreach (var (variable, series) in variables.Transpose())
    Console.WriteLine($"{variable}: {string.Join(", ", series)}");
```

`Transpose()` may allocate and returns one stable series per key. There is no second accumulator.

## Domain stamps

```csharp
var perEpoch = run.TrackSeries(
    Measure.Quality(),
    Aggregate.Best(),
    at: dynamicProblem.Evaluator);

await foreach (var state in run.Stream(cancellationToken))
{
    foreach (var point in perEpoch.ByEpoch())
        Console.WriteLine($"epoch {point.Coordinate}: {point.Value}");
}
```

The dynamic problem contributes the epoch to the stamp. Dynamic analyses use the same observation, accumulation and reading
machinery as other series. They do not subscribe to a separate event or implement disposal.

## Compile-time anchor compatibility

An anchor determines what measurements can be attached there. A crossover supplies candidates and derivation inputs but no
objective readings.

```csharp
run.TrackSeries(
    Measure.Quality(),
    Aggregate.BestMedianWorst(),
    at: crossover);
// must not compile

var offspringSize = run.TrackSeries(
    Measure.TreeLength(),
    Aggregate.MinMeanMax(),
    at: crossover);
// compiles
```

Anchors are classified by what they supply, never by operator role. A measurement declares the narrowest supply it needs.
The initial taxonomy should be settled against the measurements in the executable specs rather than expanded in advance.

| Supply | Carries |
| --- | --- |
| Objective readings | Objective vectors |
| Candidate readings | Candidates |
| Context | Search space and problem |
| Evaluated population | Candidates, objective vectors and context |
| Candidate batch | Candidates and context |
| Derivation | Candidates, context and their inputs |
| State transition | The previous and next search state |

## Analyses that are not series

Series composition is the common path, not a requirement for every analysis. Lineage uses the same attachment and lifecycle
model and exposes operations that make sense for a graph.

```csharp
var lineage = run.TrackLineage(
    at: [creator, crossover, mutator],
    comparer: ExpressionTree.StructuralComparer);

await foreach (var state in run.Stream(cancellationToken))
{
    Console.WriteLine($"roots so far: {lineage.RootCount}");
    var currentGraph = lineage.Snapshot();
}

var descendants = lineage.DescendantsOf(lineage.Snapshot().Roots[0]);
```

`RootCount` and similar properties may read safely published values. `Snapshot()` and graph projections may allocate stable
values. The lineage object itself remains run-bound and stateful.

One call with several anchors produces one lineage object fed by all anchors. Separate calls produce independent analyses.

## Experiments

An experiment attachment returns one run-bound series per trial.

```csharp
var quality = experiment.TrackSeries(
    Measure.Quality(),
    Aggregate.Best());

await experiment.CompleteAsync(cancellationToken);

foreach (var trial in quality.Trials)
    Console.WriteLine($"{trial.Key}: {trial.Series.ByEvaluations().Last()}");
```

Trials never share mutable analysis state.

## Settled

1. `TrackSeries` is the main series attachment and composition method.
2. A `Track...` method returns a run-bound, stateful analysis object rather than a reusable configuration and separate result handle.
3. Convenience methods such as `TrackBestMedianWorst` preconfigure `TrackSeries`.
4. Live reads work during streaming and after completion.
5. Properties return already published values without allocation. Methods may allocate snapshots or projections.
6. Snapshot and projection results are immutable and never change after publication.
7. The analysis object never exposes its mutable accumulator as `IReadOnlyList<T>`.
8. Anchors are classified by supply rather than role.
9. Non-series analyses use the same run-bound lifecycle and expose analysis-specific reads.

## Open for implementation

1. The exact public names of the run-bound series and snapshot types.
2. Whether `Latest` returns nullable `Sample<T>`, uses `TryGetLatest`, or exposes both.
3. The exact initial supply taxonomy.
4. Whether across-firing accumulation remains a separate slot after implementing the real aggregations.
5. Whether generic numeric measurements can infer cleanly without standardizing the first catalog on `double`.
