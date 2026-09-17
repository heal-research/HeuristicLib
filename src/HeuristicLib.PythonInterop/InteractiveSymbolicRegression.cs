using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Data;
using HEAL.HeuristicLib.Encodings.SymbolicExpressions;
using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems.MachineLearning;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.PythonInterop;

/// <summary>
/// Parameters for interactive symbolic regression, configurable from Python.
/// </summary>
public class InteractiveSymRegParameters
{
    public int PopulationSize { get; set; } = 200;
    public int Generations { get; set; } = 30;
    public int TreeLength { get; set; } = 40;
    public int TreeDepth { get; set; } = 20;
    public double MutationRate { get; set; } = 0.1;
    public int TournamentSize { get; set; } = 4;
    public int Elites { get; set; } = 1;
    public int ParameterOptimizationIterations { get; set; } = 5;
    public int Seed { get; set; } = -1;
    public string[] AllowedSymbols { get; set; } = ["add", "sub", "mul", "div", "sqrt", "log", "constant", "variable"];
    public bool UseLinearScaling { get; set; } = true;
}

/// <summary>
/// Evaluator that calls a Python callback after each population evaluation for visualization.
/// </summary>
public sealed record VisualizationCallbackEvaluator(Func<ExpressionTree[], ObjectiveVector[], double[][]> PopulationCallback)
    : StatelessEvaluator<ExpressionTree, ExpressionTreeSearchSpace, SymbolicRegressionProblem>
{
    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<ExpressionTree> candidates, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, SymbolicRegressionProblem problem)
    {
        var objectives = problem.Evaluate(candidates, random);
        var callbackObjectives = PopulationCallback(candidates.ToArray(), objectives.ToArray());

        return callbackObjectives.Select(objective => (ObjectiveVector)objective).ToArray();
    }
}

/// <summary>
/// Interactive symbolic regression runner designed for use from a Python web application.
/// </summary>
public static class InteractiveSymbolicRegression
{
    private static readonly Dictionary<string, OperationSymbol> OperationSymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        ["add"] = Symbols.Addition,
        ["sub"] = Symbols.Subtraction,
        ["mul"] = Symbols.Multiplication,
        ["div"] = Symbols.Division,
        ["sqrt"] = Symbols.SquareRoot,
        ["log"] = Symbols.Logarithm,
        ["exp"] = Symbols.Exponential,
    };

    private static readonly string[] TerminalSymbolNames = ["constant", "variable"];

    public static RegressionData CreateRegressionDataFromArrays(double[] xValues, double[] yValues)
    {
        return new RegressionData(
            new DataFrame([new Series<double>("x", xValues)]),
            new Series<double>("y", yValues));
    }

    public static ExpressionTreeSearchSpace BuildSearchSpace(string[] allowedSymbols, string[] variableNames, bool useLinearScaling, int treeLength, int treeDepth)
    {
        var operations = new List<OperationSymbol>();
        foreach (var symbolName in allowedSymbols)
        {
            var name = symbolName.Trim();
            if (OperationSymbols.TryGetValue(name, out var operation))
            {
                operations.Add(operation);
                continue;
            }

            if (!TerminalSymbolNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(
                    $"Symbol '{symbolName}' is not yet supported by interactive symbolic regression.");
            }
        }

        return new ExpressionTreeSearchSpace(treeLength, treeDepth, operations, variableNames, [new EvolvableConstantSymbol()]);
    }

    public static string FormatTree(ExpressionTree tree)
    {
        return tree.ToInfixString();
    }

    public static double[] PredictValues(ExpressionTree tree, double[] xValues)
    {
        var inputs = new DataFrame([new Series<double>("x", xValues)]);
        return tree.Evaluate(inputs);
    }

    public static string[] GetAvailableSymbols()
    {
        return [.. OperationSymbols.Keys, .. TerminalSymbolNames];
    }

    public static Population<ExpressionTree> Run(double[] xValues, double[] yValues, Func<ExpressionTree[], ObjectiveVector[], double[][]> populationCallback, InteractiveSymRegParameters parameters, CancellationToken ct = default)
    {
        var data = CreateRegressionDataFromArrays(xValues, yValues);
        var searchSpace = BuildSearchSpace(parameters.AllowedSymbols, ["x"], parameters.UseLinearScaling, parameters.TreeLength, parameters.TreeDepth);
        var problem = new SymbolicRegressionProblem(data, Metrics.R2, searchSpace, parameters.UseLinearScaling);
        var mutator = new ChooseOneMutator<ExpressionTree>(
            [
                new NodeReplacementMutator(),
                new LocalPerturbationMutator(),
                new LocalPerturbationMutator(LocalPerturbationTargets.All),
                new ShrinkSubtreeMutator(),
                new SubtreeMutator()
            ]);
        var algorithm = new GeneticAlgorithm<ExpressionTree>
        {
            Creator = new RampedHalfAndHalfTreeCreator(),
            Crossover = new SubtreeCrossover(),
            Mutator = mutator,
            MutationRate = parameters.MutationRate,
            Selector = new TournamentSelector<ExpressionTree>(parameters.TournamentSize),
            PopulationSize = parameters.PopulationSize,
            Elites = parameters.Elites,
            Evaluator = new VisualizationCallbackEvaluator(populationCallback),
            MaximumGenerations = parameters.Generations,
            Refiner = new NumericParameterFittingRefiner { MaximumIterations = parameters.ParameterOptimizationIterations }
        };
        var seed = parameters.Seed >= 0
            ? parameters.Seed
            : System.Random.Shared.Next();

        return algorithm
            .Complete(problem, RandomNumberGenerator.Create(seed), ct: ct)
            .Population;
    }
}
