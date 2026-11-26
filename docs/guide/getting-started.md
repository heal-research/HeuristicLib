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
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;

var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));

var algorithm = GeneticAlgorithm.Create(
    new UniformDistributedCreator(),
    new AlphaBetaBlendCrossover { Alpha = 0.7 },
    new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
    populationSize: 200,
    maximumGenerations: 500,
    mutationRate: 0.2);

var finalState = await algorithm.RunToCompletionAsync(
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

The run found a candidate with an objective value of about `0.00000004`, close to the known minimum of `0` at the zero vector.

`RunToCompletionAsync` creates and completes a run for you. The algorithm owns the search loop and the library constructs its executors automatically. Keep this entry point when changing operators or using your own problem. For progress use `RunStreaming`. For analysis, inspection or pause and resume, retain an [explicit run](/guide/execution/running-algorithms#explicit-runs-for-control-and-analysis).

Seed `123` reproduces exactly these numbers. A heuristic search does not guarantee the optimum on every run: Rastrigin has local minima at every integer coordinate, so some seeds finish at `0.995` or `1.99` instead. Reducing the population or the generation count makes that much more likely.

## Optimizer components {#what-the-program-assembled}

| Part                           | Role in this example                              |
| ------------------------------ | ------------------------------------------------- |
| `TestFunctionProblem`          | Evaluates each vector with the Rastrigin function |
| `BoundedRealVectorSearchSpace` | Defines the dimension and valid numeric bounds    |
| `GeneticAlgorithm`             | Controls the population and generation loop       |
| Creator                        | Produces the initial candidate vectors            |
| Crossover and mutator          | Produce variation from selected candidates        |
| Selector                       | Chooses candidates that can reproduce             |
| Random number generator        | Makes the stochastic decisions reproducible       |

`GeneticAlgorithm.Create(...)` infers its candidate type from the operators. The run supplies the search space and problem types and validates compatibility. A real vector algorithm cannot accidentally receive a permutation search space.

### Two settings are both called a mutation rate

`GaussianMutator(mutationRate: 0.2, ...)` and the algorithm's own `MutationRate = 0.2` are different settings that happen to share a name:

- The algorithm's `MutationRate` is the probability that a given child is handed to the mutator at all.
- The mutator's `mutationRate` is the probability that each individual position within that child is perturbed.

With both at `0.2`, roughly one child in five is mutated, and in those children roughly one position in five moves. Set them independently.

## Search progress {#watch-the-search-progress}

`RunToCompletionAsync` returns only the final state. Use `RunStreaming` when you want progress from every generation:

```csharp
var generation = 0;

await foreach (var state in algorithm.RunStreaming(
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

Starting the stream creates a fresh run. Reusing seed `123` reproduces the same stochastic decisions for the same configuration, which is why the last line matches the `RunToCompletionAsync` result above.

## Quality curves {#record-a-quality-curve}

A stream shows each state as it passes. To keep data about a run, such as how the best objective improved against the number of evaluations, attach an **analyzer**. Add `using HEAL.HeuristicLib.Analysis;` to the top of `Program.cs` and append this code; it reuses `problem` and `algorithm` from the program above:

<<< ../../test/HeuristicLib.Tests.Docs/GettingStarted/GettingStartedDocumentationSpecs.cs#getting-started-best-so-far{csharp}

```
Improvements: 101
Best within   1000 evaluations: 3.37368239
Best within  10000 evaluations: 0.00426511
Best within  50000 evaluations: 0.00000149
Best reached at 88600 evaluations: 0.00000004
```

Three things work together here:

- The **observation source** is where the data comes from. `Analyzer.BestSoFar<RealVector>()` observes the algorithm's evaluator, so it sees every evaluated batch of candidates.
- The **analyzer** `bestSoFar` describes what to collect: each improvement of the best objective, with the number of candidates evaluated up to that point. It holds no data itself, so it can be reused for any number of runs.
- The **run result** holds the data. `CreateRun` attaches the analyzer to one run, and `run.Result(bestSoFar)` returns the curve that run recorded.

The loop reads the curve at three evaluation budgets, which is how you compare runs that use different population sizes or generation counts. The last line shows when the run found its best objective: here, after 88600 evaluations; the remaining generations found nothing better.

In this run the best value matches the earlier `Best objective` line, but that is not guaranteed. The curve covers every evaluated candidate, while the final population holds only the candidates that survived replacement. The genetic algorithm evaluates offspring before replacement, so a discarded offspring can be better than every member of the final population. The best-so-far value is therefore never worse than the final population's best, and sometimes better.

[Analysis of algorithm runs](/guide/execution/observability-and-analysis) covers other analyzers, clocks such as elapsed time, and observations of individual operators.

## Use your own objective

For a simple function, create a problem without defining a new class:

```csharp
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;

var space = new BoundedRealVectorSearchSpace(
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

- [Understand how the components fit together](/guide/fundamentals/core-concepts), the first page of the Fundamentals path
- [Model a custom problem](/guide/fundamentals/problems)
- [Choose search spaces](/guide/fundamentals/search-spaces)
- [Choose completion, progress or explicit run control](/guide/execution/running-algorithms)
- [Compose existing operators](/guide/extending/operator-composition)
- [Run repeated experiments](/guide/execution/experiments)
