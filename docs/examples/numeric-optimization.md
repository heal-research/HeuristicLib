# Numeric optimization with a genetic algorithm

This example minimizes the four dimensional Rastrigin function. The global optimum is the zero vector with objective value `0`.

## Create the project

```console
dotnet new console --framework net10.0 --name NumericOptimization
cd NumericOptimization
dotnet add package HEAL.HeuristicLib --prerelease
```

Replace `Program.cs` with:

```csharp
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;

var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));

var algorithm = GeneticAlgorithm.Create(
    new UniformDistributedCreator(),
    new AlphaBetaBlendCrossover { Alpha = 0.7 },
    new GaussianMutator(
        mutationRate: 0.2,
        mutationStrength: 0.15),
    selector: TournamentSelector.For(problem, tournamentSize: 2),
    populationSize: 200,
    maximumGenerations: 500,
    mutationRate: 0.2);

var random = RandomNumberGenerator.Create(seed: 123);
var generation = 0;

await foreach (var state in algorithm.Stream(problem, random))
{
    generation++;
    if (generation % 50 != 0) continue;

    var best = state.Population.EvaluatedCandidates
        .MinBy(
            candidate => candidate.ObjectiveVector,
            problem.Objective.TotalOrderComparer)!;

    Console.WriteLine(
        $"{generation,3}  {best.ObjectiveVector[0],10:F6}  " +
        $"[{string.Join(", ", best.Candidate.Select(value => $"{value:F3}"))}]");
}
```

Run it with `dotnet run`:

```
 50    0.004265  [-0.003, 0.003, -0.002, 0.002]
100    0.000344  [-0.001, 0.001, -0.000, 0.001]
150    0.000084  [-0.000, 0.000, -0.000, 0.000]
200    0.000016  [-0.000, 0.000, -0.000, 0.000]
250    0.000001  [-0.000, 0.000, -0.000, 0.000]
300    0.000001  [-0.000, -0.000, -0.000, 0.000]
350    0.000000  [-0.000, 0.000, -0.000, 0.000]
400    0.000000  [-0.000, 0.000, -0.000, 0.000]
450    0.000000  [-0.000, 0.000, -0.000, 0.000]
500    0.000000  [-0.000, 0.000, -0.000, 0.000]
```

The stream yields one state per generation; this example reports every fiftieth so the output stays readable. The objective falls quickly at first and then refines slowly, which is the usual shape for a genetic algorithm on a continuous problem.

A heuristic search does not guarantee the optimum in one run. Across twenty seeds at this configuration, eighteen finish below `1e-4` and the rest stop at `0.995`, the nearest local minimum of the Rastrigin function.

## Why these components fit

`BoundedRealVectorSearchSpace` provides the bounds defined by `RastriginFunction`. `UniformDistributedCreator` samples the initial population inside those bounds. Blend crossover combines real valued parents while Gaussian mutation makes local numeric changes.

Tournament selection adds selection pressure without requiring objective values to be transformed into probabilities. One elite preserves the best candidate found so far.

## Compare repeated runs

One trajectory only shows what happened for seed `123`. Use the [experiments API](/guide/execution/experiments) to repeat the configuration with a fixed seed schedule. Compare the final distributions and evaluation counts before changing operators or parameters.

Continue with [problems](/guide/fundamentals/problems), [algorithms](/guide/fundamentals/algorithms) and [reproducible randomness](/guide/execution/randomness).
