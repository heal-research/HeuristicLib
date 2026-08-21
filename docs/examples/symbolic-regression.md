# Symbolic regression

Symbolic regression searches for an equation that fits observed data. The search changes the structure of an expression tree while a numeric refiner fits constants inside each candidate.

This example learns the linear function `y = 2.5x + 1.3` from generated samples.

## Create the training data

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
```

`RegressionData` holds named input columns and one target column. Use the same structure when loading data from a file or another .NET data source.

## Define the expression language

```csharp
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
```

The search space limits tree size and specifies which symbols may occur. The problem evaluates an expression with mean squared error. Lower values are better.

## Configure genetic programming

```csharp
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
```

The creator generates trees of different shapes and depths. Crossover exchanges subtrees. Mutation either replaces a node, perturbs a constant or replaces a subtree. The numeric refiner fits constants after those structural changes.

## Run and use the best model

```csharp
var finalState = await algorithm.CompleteAsync(
    problem,
    RandomNumberGenerator.Create(seed: 42));

var best = finalState.Population.EvaluatedCandidates
    .MinBy(candidate => candidate.ObjectiveVector[0])!;
var compiledModel = best.Candidate.Compile(optimize: true);
var predictions = compiledModel.Evaluate(trainingData.Inputs);

Console.WriteLine($"Training MSE: {best.ObjectiveVector[0]:G6}");
Console.WriteLine($"Model: {best.Candidate.ToInfixString()}");
Console.WriteLine();
Console.WriteLine("    x        y  prediction");

foreach (var row in Enumerable.Range(0, 5))
{
    Console.WriteLine($"{x[row],5:F2}  {y[row],7:F3}  {predictions[row],10:F3}");
}
```

Run it with `dotnet run`:

```
Training MSE: 6.86811E-28
Model: (((1.2697749738321393 - -0.030225026167874623) - (x * -1.447240488147826)) + ((-3.0884972563612356 * x) * (0.38585605518288119 * -0.8833984526111659)))

    x        y  prediction
-5.00  -11.200     -11.200
-4.75  -10.575     -10.575
-4.50   -9.950      -9.950
-4.25   -9.325      -9.325
-4.00   -8.700      -8.700
```

The predictions reproduce the targets exactly, and the mean squared error is at the limit of double precision. The search recovered the underlying function.

The printed tree does not look like `2.5x + 1.3`, because nothing simplifies it algebraically. Read it as two terms in `x`: `1.447 * x` from the first half and `3.088 * 0.341 * x` from the second, which sum to `2.5`. The constant folds to `1.3`. Genetic programming finds numerically correct models in structurally redundant forms, and the redundancy is normal rather than a defect.

`Compile(optimize: true)` folds the constant subtrees, which makes the intercept visible:

```csharp
Console.WriteLine(compiledModel.ToInfixString());
```

```
((1.300000000000014 - (x * -1.447240488147826)) + ((-3.0884972563612356 * x) * -0.34086464207920586))
```

The result remains an expression tree. You can inspect it, render it as infix, C#, Python or LaTeX, and evaluate it on new rows. There is no algebraic simplifier in the library today.

Read [Symbolic regression](/guide/domains/symbolic-regression) for train and test partitions, metrics and model export. Read [Symbolic expressions](/guide/domains/symbolic-expressions) to define a different expression language.
