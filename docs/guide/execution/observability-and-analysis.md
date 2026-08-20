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

An interceptor exposes an observation point in an iterative algorithm. An analyzer can then collect typed results from that point:

```csharp
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.States;

var interceptor = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();
var observedAlgorithm = algorithm with { Interceptor = interceptor };
var analysis = Analyzer.BestMedianWorst(interceptor);

var run = observedAlgorithm
    .CreateRun(problem, RandomNumberGenerator.Create(seed: 777))
    .WithAnalyzer(analysis);

await run.CompleteAsync();
var series = run.GetResult(analysis);
```

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
