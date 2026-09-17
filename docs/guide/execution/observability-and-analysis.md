# Observability and analysis

Start with streamed states when you need simple progress. Add an analyzer when a run should collect a reusable time series or metric without mixing that concern into the algorithm.

## Read progress directly

```csharp
await foreach (var state in algorithm.Stream(problem, random))
{
    var best = state.Population.EvaluatedCandidates
        .MinBy(candidate => candidate.ObjectiveVector, problem.Objective.TotalOrderComparer)!;

    Console.WriteLine(best.ObjectiveVector);
}
```

This is a good fit for logging and user interface updates. The consumer decides how much data to retain.

## Attach an analyzer

An analyzer collects typed results from observation points in a run. The end of an iteration is an observation point on the algorithm itself, so recording a quality curve needs nothing but the run:

```csharp
using HEAL.HeuristicLib.Analysis;

var run = algorithm
    .CreateRun(problem, RandomNumberGenerator.Create(seed: 777))
    .TrackBestMedianWorst(out var analysis);

await run.CompleteAsync();
var series = run.GetResult(analysis);
```

Each entry is captured from a state the algorithm yields, after any interceptor has transformed it. Sub-iterations an algorithm does not yield are not observed.

### Anchor on an algorithm by name

`TrackBestMedianWorst` takes the anchor from the run. Name the algorithm instead when the anchor is not the algorithm the run was created from, such as the inner algorithm of a meta-algorithm:

```csharp
var innerQuality = Analyzer.BestMedianWorst(innerAlgorithm);
var run = cycleAlgorithm.CreateRun(problem, random).AttachAnalyzer(innerQuality);
```

An algorithm is an anchor by reference. `algorithm with { PopulationSize = 200 }` is a different object and therefore a different anchor, so an analyzer created for the original silently observes nothing when the copy is run. `TrackBestMedianWorst` resolves the anchor at attach time and cannot get this wrong.

### Anchor on an operator

Some observations are about what an operator did rather than about the resulting state — selection pressure, evaluation counts, crossover statistics — and cannot be derived from search states. Those anchor on the operator:

```csharp
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;

var interceptor = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();
var observedAlgorithm = algorithm with { Interceptor = interceptor };
var analysis = Analyzer.BestMedianWorst(interceptor);

var run = observedAlgorithm
    .CreateRun(problem, RandomNumberGenerator.Create(seed: 777))
    .AttachAnalyzer(analysis);
```

An interceptor anchor observes the state at that point in the iteration, which is what you want when several interceptors run and the distinction matters. For a plain quality curve, prefer the algorithm anchor: it needs no placeholder operator in the configuration.

`series` is a `List<BestMedianWorstEntry<RealVector>>` with one entry per observed generation. Each entry holds three evaluated candidates, so both the objective values and the candidates behind them stay available:

```csharp
Console.WriteLine("generation      best    median     worst");

foreach (var (entry, generation) in series.Select((e, index) => (e, index + 1)))
{
    if (generation % 10 != 0) continue;

    Console.WriteLine(
        $"{generation,10}{entry.Best.ObjectiveVector[0],10:F3}" +
        $"{entry.Median.ObjectiveVector[0],10:F3}{entry.Worst.ObjectiveVector[0],10:F3}");
}
```

Running that against a 50 generation genetic algorithm on the Rastrigin function prints:

```
generation      best    median     worst
        10     3.100    23.707    53.849
        20     1.860     3.280     8.158
        30     1.201     1.490     3.127
        40     1.035     1.068     2.281
        50     1.007     1.014     2.398
```

Read the three columns together rather than watching the best value alone. Best and median start far apart and close by generation 40, which means the improvement reached the whole population instead of one elite. From there both sit near `1.0`, a local minimum of the Rastrigin function, and stop moving: the run has converged and the remaining budget is buying nothing.

The analyzer result belongs to this run, which prevents results from different executions from being mixed accidentally.

## Choose what to retain

Optimization can produce a large amount of data. Prefer compact metrics unless full populations are needed for a stated analysis.

- Keep best and median objective values for convergence plots.
- Sample large populations instead of serializing every state by default.
- Store evaluation counts when operator costs differ.
- Attach units and objective labels at the reporting boundary.
- Record package version, configuration, problem identity and seed with every series.

## Experiments

Experiment runs can bind an analyzer to each concrete algorithm in a grid. This preserves typed trial keys and independent result ownership. See [Experiments](/guide/execution/experiments) for repetition and grid construction.

Instrumentation is an advanced extension point. Prefer existing states and analyzers before adding a custom interceptor.
