# Traveling salesperson with a genetic algorithm

This example loads the `berlin52` TSPLIB instance and searches for a short tour through all 52 cities. A candidate is a permutation, so every city appears exactly once.

## Prepare the project

```console
dotnet new console --framework net10.0 --name TspOptimization
cd TspOptimization
dotnet add package HEAL.HeuristicLib --prerelease
```

Download [`berlin52.tsp`](https://github.com/heal-research/HeuristicLib/blob/main/test/HeuristicLib.Tests.Scenarios/TestData/berlin52.tsp) into the project directory.

## Configure the search

```csharp
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Problems.TravelingSalesman.InstanceLoading;
using HEAL.HeuristicLib.Random;

var instance = TsplibTspInstanceProvider.LoadData(
    "berlin52.tsp",
    bestQuality: 7542);
var problem = new TravelingSalesmanProblem(instance.ToCoordinatesData());

var algorithm = GeneticAlgorithm.For(
    problem,
    crossover: new EdgeRecombinationCrossover(),
    selector: TournamentSelector.For(problem, tournamentSize: 3),
    populationSize: 300,
    maximumGenerations: 1000,
    mutationRate: 0.25);

var run = algorithm
    .CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
    .TrackBestMedianWorst(out var qualityAnalyzer);

await run.CompleteAsync();

var qualityCurve = run.GetResult(qualityAnalyzer);
var best = qualityCurve[^1].Best;

Console.WriteLine($"Instance: {instance.Name}");
Console.WriteLine($"Best tour length: {best.ObjectiveVector[0]:F0}");

foreach (var (entry, generation) in
         qualityCurve.Select((entry, index) => (entry, index + 1)))
{
    if (generation % 100 != 0) continue;

    Console.WriteLine(
        $"{generation,4}: " +
        $"best {entry.Best.ObjectiveVector[0],8:F0}  " +
        $"median {entry.Median.ObjectiveVector[0],8:F0}  " +
        $"worst {entry.Worst.ObjectiveVector[0],8:F0}");
}
```

Run it with `dotnet run`. The search takes a few seconds:

```
Instance: berlin52
Best tour length: 7797
 100: best     8283  median     9686  worst    14004
 200: best     7798  median     7926  worst    10318
 300: best     7797  median     7797  worst    10502
 400: best     7797  median     7797  worst     9944
 500: best     7797  median     7797  worst    10108
 600: best     7797  median     7797  worst    10156
 700: best     7797  median     7797  worst    10232
 800: best     7797  median     7797  worst    10310
 900: best     7797  median     7797  worst     9953
1000: best     7797  median     7797  worst    10181
```

## Defaults and the explicit crossover

A valid tour contains each city once. Permutation creators and variation operators preserve that invariant. A real or integer vector operator could introduce duplicate cities and omit others.

`GeneticAlgorithm.For(problem, ...)` gets random creation and inversion mutation from `PermutationSearchSpace`. `TravelingSalesmanProblem` normally suggests order crossover. This example overrides only that role with edge recombination so the remaining defaults stay visible.

Edge recombination tries to retain city adjacencies from both parents. Inversion mutation reverses a segment of the route and often produces a meaningful local change. Neither is a tuned choice for every instance. Keep the operators explicit when comparing search policies.

## Read the quality curve

The best value shows the strongest tour found so far. The median shows whether improvement reaches the population rather than one elite.

The run above is worth reading closely, because it does not end at the optimum. The known optimum for `berlin52` is `7542`, and seed `42` finishes at `7797`, about 3.4 percent above it. Best and median are equal from generation 300 onward, which says the selected part of the population has collapsed onto one tour and crossover has stopped producing anything new. The worst value stays near `10000` because mutation keeps generating variants, but none of them survive selection.

That pattern is premature convergence, and it is the normal reason a permutation search stalls. The usual responses are a larger population, weaker selection pressure through a smaller tournament size, or a higher mutation rate. Everything after generation 300 in this run is wasted budget.

Repeat the run across seeds before drawing conclusions. At this configuration some seeds do reach `7542`, and one trajectory tells you very little either way.

Continue with [search spaces](/guide/fundamentals/search-spaces), [operators](/guide/fundamentals/operators) and [observability and analysis](/guide/execution/observability-and-analysis).
