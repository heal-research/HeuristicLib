<div align="center">

# HeuristicLib

Heuristic and evolutionary optimization for modern .NET

[![CI](https://github.com/heal-research/HeuristicLib/actions/workflows/ci.yml/badge.svg)](https://github.com/heal-research/HeuristicLib/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/HEAL.HeuristicLib.svg)](https://www.nuget.org/packages/HEAL.HeuristicLib)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License: MIT](https://img.shields.io/badge/license-MIT-0b7285.svg)](LICENSE)

[Get started](docs/guide/getting-started.md) · [Read the guide](docs/guide/index.md) · [Browse examples](docs/examples/index.md) · [Report an issue](https://github.com/heal-research/HeuristicLib/issues)

</div>

HeuristicLib is a .NET library for building, running and studying heuristic optimization algorithms. Configure a genetic algorithm, hill climber, evolution strategy or NSGA-II in plain C#. Combine typed operators with your problem, stream every search state and repeat runs with deterministic random seeds.

<p align="center">
  <a href="https://heal.heuristiclab.com/">
    <img src="docs/public/heal-logo.png" alt="Heuristic and Evolutionary Algorithms Laboratory" width="360">
  </a>
</p>

The library is developed by the [Heuristic and Evolutionary Algorithms Laboratory](https://heal.heuristiclab.com/), the research group behind [HeuristicLab](https://github.com/heal-research/HeuristicLab) and [Operon](https://github.com/heal-research/operon). HeuristicLib is a new library with a smaller scope and an API designed for current C#.

> [!WARNING]
> HeuristicLib is in alpha. Public APIs can change while the design settles. Pin the package version when repeatable builds matter.

## Why HeuristicLib

- Configure algorithms with C# object initializers and records. There is no operator graph or separate configuration language.
- Keep candidate, search space, problem and operator types connected through the whole algorithm. The compiler catches incompatible combinations.
- Stream search states or attach analyzers for quality, diversity, genealogy and evaluation counts.
- Reproduce a run from its root seed. Forked random streams keep parallel work independent of scheduling.
- Repeat configurations, build typed parameter grids and process independent trials concurrently.
- Use built in real, integer, Boolean, permutation and symbolic expression representations.

## Install

HeuristicLib targets .NET 10 and is available as a prerelease NuGet package.

```console
dotnet add package HEAL.HeuristicLib --prerelease
```

## Example 1: solve a TSPLIB instance and record its quality curve

This example loads the named `berlin52` benchmark from a standard TSPLIB file, solves it with a genetic algorithm and records the best, median and worst tour length after every generation. You can use the [`berlin52.tsp`](test/HeuristicLib.Tests.Scenarios/TestData/berlin52.tsp) file in this repository to run it.

```csharp
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.PermutationCreators;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Problems.TravelingSalesman.InstanceLoading;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

var instance = TsplibTspInstanceProvider.LoadData(
    "berlin52.tsp",
    bestQuality: 7542);
var problem = new TravelingSalesmanProblem(instance.ToCoordinatesData());

var algorithm =
    new GeneticAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>
    {
        PopulationSize = 100,
        MaximumGenerations = 500,
        Creator = new RandomPermutationCreator(),
        Crossover = new EdgeRecombinationCrossover(),
        Mutator = new InversionMutator(),
        Selector = TournamentSelector.For(problem, tournamentSize: 3),
        MutationRate = 0.05,
        Elites = 1
    };

var run = algorithm
    .CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
    .TrackBestMedianWorst(out var qualityAnalyzer);

await run.CompleteAsync();

var qualityCurve = run.GetResult(qualityAnalyzer);
var best = qualityCurve[^1].Best;

Console.WriteLine($"Instance: {instance.Name}");
Console.WriteLine($"Best tour length: {best.ObjectiveVector[0]:F0}");

foreach (var (entry, generation) in qualityCurve.Select((entry, i) => (entry, i + 1)))
{
    Console.WriteLine(
        $"{generation,3}: " +
        $"best {entry.Best.ObjectiveVector[0],8:F0}  " +
        $"median {entry.Median.ObjectiveVector[0],8:F0}  " +
        $"worst {entry.Worst.ObjectiveVector[0],8:F0}");
}
```

The algorithm configuration stays reusable. Change the TSPLIB file to run another named instance. Change the crossover or mutation operator to test another search policy. Keep the problem, seed schedule and analyzer unchanged when comparing configurations.

See [problems](docs/guide/fundamentals/problems.md), [operators](docs/guide/fundamentals/operators.md), [observability and analysis](docs/guide/execution/observability-and-analysis.md) and [experiments](docs/guide/execution/experiments.md) for the underlying APIs.

## Example 2: train a symbolic regression model

Symbolic regression searches for a mathematical expression instead of fitting coefficients in a fixed model form. In this example genetic programming evolves the expression structure. A numeric refiner fits constants inside each candidate with nonlinear least squares.

```csharp
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.DataAnalysis;
using HEAL.HeuristicLib.DataAnalysis.Regression;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionCreators;
using HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionCrossovers;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionMutators;
using HEAL.HeuristicLib.Operators.Refiners.SymbolicRegressionRefiners;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

var x = Enumerable.Range(0, 40)
    .Select(index => (index - 20) * 0.25)
    .ToArray();
var y = x.Select(value => 2.5 * value + 1.3).ToArray();

var trainingData = new RegressionData(
    new DataFrame([Series<double>.FromOwnedArray("x", x)]),
    Series<double>.FromOwnedArray("y", y));

var searchSpace = new ExpressionTreeSearchSpace(
    maximumLength: 15,
    maximumDepth: 5,
    operations: [Symbols.Addition, Symbols.Subtraction, Symbols.Multiplication],
    variables: ["x"],
    constants: [new EvolvableConstantSymbol()]);

var problem = new SymbolicRegressionProblem(
    trainingData,
    Metrics.MSE,
    searchSpace);

var algorithm =
    new GeneticAlgorithm<ExpressionTree, ExpressionTreeSearchSpace, SymbolicRegressionProblem>
    {
        PopulationSize = 100,
        MaximumGenerations = 50,
        Creator = new RampedHalfAndHalfTreeCreator(),
        Crossover = new SubtreeCrossover(),
        Mutator = ChooseOneMutator.Create(
            new NodeReplacementMutator(),
            new LocalPerturbationMutator(),
            new SubtreeMutator()),
        Refiner = new NumericParameterFittingRefiner
        {
            MaximumIterations = 10
        },
        Selector = TournamentSelector.For(problem, tournamentSize: 3),
        MutationRate = 0.25,
        Elites = 1
    };

var finalState = await algorithm.CompleteAsync(
    problem,
    RandomNumberGenerator.Create(seed: 42));

var best = finalState.Population.EvaluatedCandidates
    .MinBy(candidate => candidate.ObjectiveVector[0])!;
var compiledModel = best.Candidate.Compile(optimize: true);
var predictions = compiledModel.Evaluate(trainingData.Inputs);

Console.WriteLine($"Model: {best.Candidate.ToInfixString()}");
Console.WriteLine($"Training MSE: {best.ObjectiveVector[0]:G6}");
```

The trained candidate is an expression tree. Inspect it, format it as infix, C#, Python or LaTeX and evaluate it on new data.

### Interactive symbolic regression

Draw a target curve in the Python demonstrator and watch HeuristicLib evolve candidate expressions. The browser displays each generation and the final tradeoff between fit and expression complexity.

<p align="center">
  <img src="examples/PythonInteractiveDemonstrator/documentation/demo.gif" alt="Interactive symbolic regression demonstrator" width="480">
</p>

The complete application is in [`examples/PythonInteractiveDemonstrator`](examples/PythonInteractiveDemonstrator). It uses pythonnet to host HeuristicLib from a FastAPI application.

## What is included

| Area                      | Available components                                                                     |
| ------------------------- | ---------------------------------------------------------------------------------------- |
| Algorithms                | Genetic algorithm, evolution strategy, NSGA-II, ALPS and hill climbing                   |
| Candidate representations | Real, integer, Boolean and permutation vectors plus symbolic expression trees            |
| Operators                 | Creation, crossover, mutation, selection, replacement, refinement and composition        |
| Execution                 | Streamed states, cancellation, deterministic random streams and configurable concurrency |
| Experiments               | Repetitions, typed parameter grids, concurrent trials and analyzers                      |
| Analysis                  | Quality progress, evaluation counts, duration, diversity and genealogy                   |
| Problems                  | Numerical test functions, traveling salesperson and symbolic regression                  |

## Documentation

- [Build your first optimizer](docs/guide/getting-started.md)
- [Understand the core concepts](docs/guide/fundamentals/core-concepts.md)
- [Choose an algorithm](docs/guide/fundamentals/algorithms.md)
- [Run repeatable experiments](docs/guide/execution/experiments.md)
- [Train symbolic regression models](docs/examples/symbolic-regression.md)
- [Work with symbolic expressions](docs/guide/domains/symbolic-expressions.md)
- [Use HeuristicLib from Python](docs/guide/interop/python.md)

## Build the repository

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), then run:

```console
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-restore
```

To work on the documentation site:

```console
npm ci
npm run docs:dev
```

Contributor rules and validation commands are in [`AGENTS.md`](AGENTS.md). Implementation and public API conventions are in the [developer guidelines](docs/contributing/developer-guidelines.md).

## License

HeuristicLib is available under the [MIT License](LICENSE).
