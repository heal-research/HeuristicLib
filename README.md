<div align="center">

# HeuristicLib

Heuristic and evolutionary optimization for modern .NET

[![CI](https://github.com/heal-research/HeuristicLib/actions/workflows/ci.yml/badge.svg)](https://github.com/heal-research/HeuristicLib/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/HEAL.HeuristicLib.svg)](https://www.nuget.org/packages/HEAL.HeuristicLib)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License: MIT](https://img.shields.io/badge/license-MIT-0b7285.svg)](https://github.com/heal-research/HeuristicLib/blob/main/LICENSE)

[Get started](https://heuristiclib.github.io/guide/getting-started.html) · [Read the guide](https://heuristiclib.github.io/guide/) · [Browse examples](https://heuristiclib.github.io/examples/) · [Report an issue](https://github.com/heal-research/HeuristicLib/issues)

</div>

HeuristicLib is a .NET library for building, running and studying heuristic optimization algorithms. Configure a genetic algorithm, hill climber, evolution strategy or NSGA-II in plain C#. Combine typed operators with your problem, stream every search state and repeat runs with deterministic random seeds.

<p align="center">
  <a href="https://heal.heuristiclab.com/">
    <img src="https://raw.githubusercontent.com/heal-research/HeuristicLib/main/docs/public/heal-logo.png" alt="Heuristic and Evolutionary Algorithms Laboratory" width="360">
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

This example loads the named `berlin52` benchmark from a standard TSPLIB file, solves it with a genetic algorithm and records the best, median and worst tour length after every generation. You can use the [`berlin52.tsp`](https://github.com/heal-research/HeuristicLib/blob/main/test/HeuristicLib.Tests.Scenarios/TestData/berlin52.tsp) file in this repository to run it.

```csharp
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TravelingSalesman;

var instance = TsplibTspInstanceProvider.LoadData(
    "berlin52.tsp",
    bestQuality: 7542);
var problem = new TravelingSalesmanProblem(instance.ToCoordinatesData());

var algorithm = GeneticAlgorithm.For(
    problem,
    selector: TournamentSelector.For(problem, tournamentSize: 3),
    populationSize: 100,
    maximumGenerations: 500,
    mutationRate: 0.05);

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

`GeneticAlgorithm.For(problem, ...)` asks the problem and its encoding for suggested operators. Here the traveling salesperson problem supplies order crossover, while the permutation encoding supplies random creation and inversion mutation. Algorithm settings come from `GeneticAlgorithmDefaults` unless the call overrides them. Defaults are starting points rather than tuned choices. Configure an operator explicitly when an experiment depends on that choice.

The algorithm configuration stays reusable. Change the TSPLIB file to run another named instance. Use a `with` expression to change the crossover or mutation operator and test another search policy. Keep the problem, seed schedule and analyzer unchanged when comparing configurations.

See [problems](https://heuristiclib.github.io/guide/fundamentals/problems.html), [operators](https://heuristiclib.github.io/guide/fundamentals/operators.html), [observability and analysis](https://heuristiclib.github.io/guide/execution/observability-and-analysis.html) and [experiments](https://heuristiclib.github.io/guide/execution/experiments.html) for the underlying APIs.

## Example 2: train a symbolic regression model

Symbolic regression searches for a mathematical expression instead of fitting coefficients in a fixed model form. In this example genetic programming evolves the expression structure. A numeric refiner fits constants inside each candidate with nonlinear least squares.

```csharp
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Data;
using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Encodings.SymbolicExpressions;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.MachineLearning;

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

var algorithm = GeneticAlgorithm.Create(
    new RampedHalfAndHalfTreeCreator(),
    new SubtreeCrossover(),
    ChooseOneMutator.Create(
        new NodeReplacementMutator(),
        new LocalPerturbationMutator(),
        new SubtreeMutator()),
    refiner: new NumericParameterFittingRefiner
    {
        MaximumIterations = 10
    },
    selector: TournamentSelector.For(problem, tournamentSize: 3),
    populationSize: 100,
    maximumGenerations: 50,
    mutationRate: 0.25);

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
  <img src="https://raw.githubusercontent.com/heal-research/HeuristicLib/main/examples/PythonInteractiveDemonstrator/documentation/demo.gif" alt="Interactive symbolic regression demonstrator" width="480">
</p>

The complete application is in [`examples/PythonInteractiveDemonstrator`](https://github.com/heal-research/HeuristicLib/tree/main/examples/PythonInteractiveDemonstrator). It uses pythonnet to host HeuristicLib from a FastAPI application.

## What is included

| Area                      | Available components                                                                     |
| ------------------------- | ---------------------------------------------------------------------------------------- |
| Algorithms                | Genetic algorithm, evolution strategy, NSGA-II and hill climbing                         |
| Candidate representations | Real, integer, Boolean and permutation vectors plus symbolic expression trees            |
| Operators                 | Creation, crossover, mutation, selection, replacement, refinement and composition        |
| Execution                 | Streamed states, cancellation, deterministic random streams and configurable concurrency |
| Experiments               | Repetitions, typed parameter grids, concurrent trials and analyzers                      |
| Analysis                  | Quality progress, evaluation counts, duration and run observability                      |
| Problems                  | Numerical test functions, traveling salesperson and symbolic regression                  |

Research algorithms such as ALPS and the open ended relevant alleles preserving genetic algorithm are in `HEAL.HeuristicLib.Experimental`. The Experimental package also owns genealogy workflows and population level or Pareto history analyzers. Static Quadratic Assignment is part of the main package, while its dynamic variants remain Experimental. Add an explicit package reference when an example or application uses an Experimental feature:

```console
dotnet add package HEAL.HeuristicLib.Experimental --prerelease
```

## Documentation

- [Build your first optimizer](https://heuristiclib.github.io/guide/getting-started.html)
- [Understand the core concepts](https://heuristiclib.github.io/guide/fundamentals/core-concepts.html)
- [Choose an algorithm](https://heuristiclib.github.io/guide/fundamentals/algorithms.html)
- [Run repeatable experiments](https://heuristiclib.github.io/guide/execution/experiments.html)
- [Train symbolic regression models](https://heuristiclib.github.io/examples/symbolic-regression.html)
- [Work with symbolic expressions](https://heuristiclib.github.io/guide/domains/symbolic-expressions.html)
- [Use HeuristicLib from Python](https://heuristiclib.github.io/guide/interop/python.html)

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

Contributor rules and validation commands are in [`AGENTS.md`](https://github.com/heal-research/HeuristicLib/blob/main/AGENTS.md). Implementation and public API conventions are in the [developer guidelines](https://heuristiclib.github.io/contributing/developer-guidelines.html).

## License

HeuristicLib is available under the [MIT License](https://github.com/heal-research/HeuristicLib/blob/main/LICENSE).
