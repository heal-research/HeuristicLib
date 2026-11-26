# Multiobjective optimization with NSGA-II

Many decisions have goals that disagree. A cheaper design holds less. A faster schedule costs more. There is no single best answer, only a set of answers where improving one goal costs another.

This example designs a cylindrical container. It minimizes the wall material, maximizes the volume and returns the whole tradeoff curve.

## Why not just combine the objectives

A score such as `volume - 0.5 * material` turns the problem into a single-objective one. The weight then picks the answer, and you choose it before seeing the tradeoffs. A weighted sum also cannot reach designs on a concave part of the curve at any weight.

Multiobjective search returns the curve first, and you choose from it afterwards. Use a weighted sum when the domain supplies the weights, such as a real price. Use multiobjective search when the weights are a guess.

## Dominance and the Pareto front

One design **dominates** another when it is at least as good on every objective and strictly better on at least one. A design that no other design dominates is **nondominated**. The nondominated designs form the **Pareto front**.

<DominanceRegions>

The designs around A. Designs that cost less and hold less, or cost more and hold more, are not comparable with A. Both can belong to the front.

</DominanceRegions>

`MultiObjective` directions therefore have no total order comparer. The API does not invent a ranking to answer which candidate is best.

## Define the problem

A candidate is a `RealVector` of radius and height. Evaluation returns two values.

<<< ../../test/HeuristicLib.Tests.Docs/Examples/MultiObjectiveDocumentationSpecs.cs#multi-objective-imports{csharp}

<<< ../../test/HeuristicLib.Tests.Docs/Examples/MultiObjectiveDocumentationSpecs.cs#multi-objective-problem{csharp}

`MultiObjective.Create` takes one direction per objective, in the order of the returned array. Directions can differ, so volume needs no rewrite as a minimization.

## Configure NSGA-II

`NSGA2` takes the same creator, crossover and mutator as a genetic algorithm. Selection and replacement rank by dominance, and the replacer keeps the best fronts, so there is no `Elites` setting.

<<< ../../test/HeuristicLib.Tests.Docs/Examples/MultiObjectiveDocumentationSpecs.cs#multi-objective-algorithm{csharp}

Both operators break ties by crowding distance and prefer candidates in sparse regions. This spreads the population along the front. The population size limits the resolution of the answer. A hundred candidates describe at most a hundred points.

## Run and read the front

<<< ../../test/HeuristicLib.Tests.Docs/Examples/MultiObjectiveDocumentationSpecs.cs#multi-objective-front{csharp}

`ParetoFront.ExtractFrom` keeps the nondominated members of a population. Late in a run most of an NSGA-II population qualifies, but nothing guarantees it.

```
Nondominated: 100 of 100

 material    volume   radius   height    h/r
     89.7      63.3     2.48     3.28   1.32
    455.5     745.7     5.03     9.38   1.86
    743.2    1555.5     6.31    12.44   1.97
   1126.7    2903.4     7.67    15.70   2.05
   1524.2    4567.2     8.86    18.53   2.09
   1828.9    6001.6     9.66    20.45   2.12
   2134.8    7567.7    10.41    22.23   2.14
   2343.0    8700.3    10.89    23.37   2.15
   2518.4    9694.2    11.27    24.29   2.15
```

Every row is a valid answer. The table shows nine of the hundred.

<script setup>
import front from "../.vitepress/figures/multi-objective-front";
import ratio from "../.vitepress/figures/multi-objective-ratio";
import hyperVolume from "../.vitepress/figures/multi-objective-hypervolume";
</script>

<ScatterChart v-bind="ratio">

Height over radius for every design on the front. For a fixed volume the least material needs `h = 2r`.

</ScatterChart>

For a fixed volume, the cylinder with the least material has a height of twice its radius. The code does not state this, and the search finds it. Through the middle of the front, `h/r` stays within a few percent of `2`. The ends drift because extreme designs have fewer neighbors to cross with and converge more slowly.

Check a front against such a property when you can derive one. A front that violates it has not converged, however smooth it looks.

## Choose a design

The front is the input to a decision. A requirement from the domain usually picks the design.

<<< ../../test/HeuristicLib.Tests.Docs/Examples/MultiObjectiveDocumentationSpecs.cs#multi-objective-choice{csharp}

```
Cheapest design holding at least 2000: radius 6.89, height 13.85, material 898.5
```

<ScatterChart v-bind="front">

The initial population and the final front of seed 5. The requirement picks one design from the front.

</ScatterChart>

The front is sorted by material, so the first design meeting the requirement is the cheapest one. Applying the requirement after the search also shows what relaxing it would save.

## Track progress toward the front

A single-objective run records a best-so-far curve. A multiobjective run has no single best, so it measures the front instead. The **hypervolume** is the area that the front dominates up to a fixed **reference point**. It grows only when a new design dominates area that no earlier design dominated.

<LineChart v-bind="hyperVolume">

The hypervolume of the nondominated designs that the populations of seed 5 held so far, with the reference point `(3000, 0)`. Most of the area is dominated within the first generations.

</LineChart>

The `HEAL.HeuristicLib.Experimental` package provides two analyzers that work by dominance:

- `TraceHyperVolume` records the hypervolume of the front accumulated from every evaluated candidate.
- `ParetoFrontAnalyzer` keeps that front as an archive, including designs the population later lost.

::: warning Experimental
These analyzers are in the Experimental package, whose API may change between prereleases. Add it with `dotnet add package HEAL.HeuristicLib.Experimental --prerelease`.
:::

The reference point must be worse than every relevant design on both objectives. Material stays below about 2790 within the bounds and volume is never negative, so `(3000, 0)` works here. Keep it fixed when you compare runs, because the hypervolume depends on it.

```csharp
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Inspection;

var referencePoint = new ObjectiveVector(3000.0, 0.0);
var evaluations = new Counter();

var hyperVolume = algorithm.Evaluator.Invocations.TraceHyperVolume(
    referencePoint,
    clocks: [evaluations.Clock],
    retention: TraceRetention.OnChange());
var archive = new ParetoFrontAnalyzer<RealVector>(
    problem.Objective,
    referencePoint,
    algorithm.Evaluator);

var run = algorithm.CreateRun(
    problem,
    RandomNumberGenerator.Create(seed: 5),
    [
        hyperVolume,
        archive,
        algorithm.Evaluator.Invocations.CountingCandidates(evaluations)
    ]);
await run.CompleteAsync();

var curve = run.Result(hyperVolume).By(evaluations.Clock);
foreach (var budget in new[] { 1_000, 5_000, 20_000 })
{
    var point = curve.Last(entry => entry.Time <= budget);
    Console.WriteLine($"Hypervolume within {budget,6} evaluations: {point.Value:F0}");
}

Console.WriteLine($"Archived nondominated designs: {run.Result(archive).Count}");
```

For seed `5` the hypervolume is `14594509` within 1000 evaluations, `14642391` within 5000 and `14648361` within 20000.

The counting substitution is listed last, so it counts each evaluation before the trace reads the clock. A hypervolume that stops growing early is the multiobjective counterpart of a flat best-so-far curve. Compare configurations by their hypervolume at a common evaluation budget, with the same reference point, over several seeds.

## Reporting

Report the front, not one point from it. State every objective with its unit and direction, and record the seed. Two seeds give slightly different fronts.

Compare configurations by their fronts. A front that is better everywhere is a clear win. A front that is better in one region and worse in another is a tradeoff that needs a decision, not an average.

Continue with [Objectives and evaluated candidates](/guide/fundamentals/objectives) for dominance semantics, [Custom problem for production planning](/examples/custom-problem) for a problem type with domain data, or [Experiments](/guide/execution/experiments) to repeat this across seeds.
