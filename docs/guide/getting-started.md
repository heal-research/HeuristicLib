# Build your first optimizer

This guide creates a console application that minimizes the Rastrigin benchmark function with a genetic algorithm. The example is small enough to copy but uses the same components as a real optimization task.

::: warning Prerelease package
HeuristicLib is under active development. Pin the package version in applications where repeatable builds matter.
:::

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A terminal and editor

## Create the project

```console
dotnet new console --framework net10.0 --name FirstOptimizer
cd FirstOptimizer
dotnet add package HEAL.HeuristicLib --prerelease
```

Replace `Program.cs` with this program:

```csharp
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));

var algorithm = new GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>
{
    PopulationSize = 200,
    MaximumGenerations = 500,
    Creator = new UniformDistributedCreator(),
    Crossover = new AlphaBetaBlendCrossover { Alpha = 0.7 },
    Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
    Selector = TournamentSelector.For(problem, tournamentSize: 2),
    MutationRate = 0.2,
    Elites = 1
};

var finalState = await algorithm.CompleteAsync(
    problem,
    RandomNumberGenerator.Create(seed: 123));

var best = finalState.Population.EvaluatedCandidates
    .MinBy(candidate => candidate.ObjectiveVector, problem.Objective.TotalOrderComparer)!;

Console.WriteLine($"Best objective: {best.ObjectiveVector[0]:F8}");
Console.WriteLine($"Candidate: {string.Join(", ", best.Candidate.Select(x => x.ToString("F6")))}");
```

Run it:

```console
dotnet run
```

```
Best objective: 0.00000004
Candidate: -0.000003, 0.000010, -0.000004, 0.000008
```

The Rastrigin function has its global minimum at the zero vector with objective value `0`, so this run finished on the optimum to eight decimal places. It takes well under a second.

Seed `123` reproduces exactly these numbers. A heuristic search does not guarantee the optimum on every run: Rastrigin has local minima at every integer coordinate, so some seeds finish at `0.995` or `1.99` instead. Reducing the population or the generation count makes that much more likely.

## What the program assembled

| Part                    | Role in this example                              |
| ----------------------- | ------------------------------------------------- |
| `TestFunctionProblem`   | Evaluates each vector with the Rastrigin function |
| `RealVectorSearchSpace` | Defines the dimension and valid numeric bounds    |
| `GeneticAlgorithm`      | Controls the population and generation loop       |
| Creator                 | Produces the initial candidate vectors            |
| Crossover and mutator   | Produce variation from selected candidates        |
| Selector                | Chooses candidates that can reproduce             |
| Random number generator | Makes the stochastic decisions reproducible       |

The generic type parameters connect compatible components at compile time. A real vector algorithm cannot accidentally receive a permutation search space.

### Two settings are both called a mutation rate

`GaussianMutator(mutationRate: 0.2, ...)` and the algorithm's own `MutationRate = 0.2` are different settings that happen to share a name:

- The algorithm's `MutationRate` is the probability that a given child is handed to the mutator at all.
- The mutator's `mutationRate` is the probability that each individual position within that child is perturbed.

With both at `0.2`, roughly one child in five is mutated, and in those children roughly one position in five moves. Set them independently.

## Watch the search progress

`CompleteAsync` returns only the final state. Use `Stream` when you want progress from every generation:

```csharp
var generation = 0;

await foreach (var state in algorithm.Stream(
    problem,
    RandomNumberGenerator.Create(seed: 123)))
{
    generation++;
    if (generation % 100 != 0) continue;

    var currentBest = state.Population.EvaluatedCandidates
        .MinBy(candidate => candidate.ObjectiveVector, problem.Objective.TotalOrderComparer)!;

    Console.WriteLine($"Generation {generation,3}: {currentBest.ObjectiveVector[0]:F8}");
}
```

```
Generation 100: 0.00034423
Generation 200: 0.00001563
Generation 300: 0.00000121
Generation 400: 0.00000014
Generation 500: 0.00000004
```

The stream yields one state per generation. This example reports every hundredth so the output stays readable; drop the `continue` to see all 500.

Starting the stream creates a fresh run. Reusing seed `123` reproduces the same stochastic decisions for the same configuration, which is why the last line matches the `CompleteAsync` result above.

## Use your own objective

For a simple function, create a problem without defining a new class:

```csharp
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

var space = new RealVectorSearchSpace(
    length: 2,
    minimum: [-5.0],
    maximum: [5.0]);

var sphereProblem = FuncProblem.Create(
    evaluateFunc: (RealVector candidate) => candidate.Sum(x => x * x),
    encoding: space,
    objective: SingleObjective.Minimize);
```

You can pass `sphereProblem` to a compatible algorithm just like the built-in benchmark problem.

## Next steps

- [Understand how the components fit together](/guide/fundamentals/core-concepts)
- [Model a custom problem](/guide/fundamentals/problems)
- [Choose search spaces](/guide/fundamentals/search-spaces)
- [Run repeated experiments](/guide/execution/experiments)
