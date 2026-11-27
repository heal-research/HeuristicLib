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

<<< ../../test/HeuristicLib.Tests.Docs/Examples/TravelingSalespersonDocumentationSpecs.cs#traveling-salesperson-imports{csharp}

<<< ../../test/HeuristicLib.Tests.Docs/Examples/TravelingSalespersonDocumentationSpecs.cs#traveling-salesperson-program{csharp}

Run it with `dotnet run`. The search takes a few seconds:

```
Execution: berlin52
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

<script setup>
import tours from "../.vitepress/figures/traveling-salesperson-tours";
import curve from "../.vitepress/figures/traveling-salesperson-curve";
</script>

<TourPanels v-bind="tours">

The best tour of the population at four generations of this run. The known optimum is 7542. Point at a city to see its coordinates and where each tour visits it.

</TourPanels>

## Recommendations and the explicit crossover

A valid tour contains each city once. Permutation creators and variation operators preserve that invariant. A real or integer vector operator could introduce duplicate cities and omit others.

`GeneticAlgorithm.For(problem, ...)` gets recommended random creation and inversion mutation from `PermutationSearchSpace`. `TravelingSalesmanProblem` recommends order crossover. This example overrides only that role with edge recombination so the remaining recommendations stay visible.

See [Use operator recommendations](/guide/fundamentals/algorithms#use-operator-recommendations) for the lookup order and the complete list of current built in recommendations.

Edge recombination tries to retain city adjacencies from both parents. Inversion mutation reverses a segment of the route and often produces a meaningful local change. Neither is a tuned choice for every instance. Keep the operators explicit when comparing search policies.

## Read the quality curve

`TracePopulationCandidates` ranks the population of each generation, so the three columns describe the current population, not the whole run. The best column is the shortest tour in that generation's population. It can differ from the shortest tour evaluated so far, because an evaluated offspring that replacement discards never appears in a population. Read the best found so far with `Analyzer.BestSoFar<Permutation>()`. The median shows whether improvement reaches the population rather than one elite.

<LineChart v-bind="curve">

Every generation of the same run, as distance above the known optimum on a log scale. Best and median meet and stay together while the worst tour keeps varying.

</LineChart>

The known optimum for `berlin52` is `7542`, and seed `42` finishes at `7797`, about 3.4 percent above it. The best value is `7797` at generation 300 and in every remaining row shown. The median also equals the best in those rows, so at least half of each reported population has that tour length. The worst value stays near `10000`, so those populations still contain much longer tours.

Equal lengths suggest, but do not prove, that the population has converged onto one tour. Different tours can have the same length. To check, trace the number of distinct tours in each generation:

```csharp
var distinctTours = algorithm.Iterations.Trace(observation =>
    observation.State.Population.EvaluatedCandidates
        .Select(evaluated => evaluated.Candidate)
        .Distinct()
        .Count());
```

Pass `distinctTours` to `CreateRun` next to `qualityAnalyzer` and read it with `run.Result(distinctTours)`. A rotation or reversal of a tour is a different permutation, so the count can overstate the number of different routes. A low count combined with a stalled best value suggests premature convergence. Compare several seeds with a larger population, a smaller tournament size, or a higher mutation rate to see whether each change helps.

Repeat the run across seeds before drawing conclusions. At this configuration some seeds do reach `7542`, and one trajectory tells you very little either way.

Continue with [search spaces](/guide/fundamentals/search-spaces), [operators](/guide/fundamentals/operators) and [analysis of algorithm runs](/guide/execution/observability-and-analysis).
