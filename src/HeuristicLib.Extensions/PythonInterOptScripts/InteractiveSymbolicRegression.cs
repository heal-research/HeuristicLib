using System.Collections;
using System.Globalization;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionTreeCreators;
using HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionTreeCrossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionTreeMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Formatter;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

/// <summary>
/// Parameters for interactive symbolic regression, configurable from Python.
/// </summary>
public class InteractiveSymRegParameters {
  public int PopulationSize { get; set; } = 200;
  public int Generations { get; set; } = 30;
  public int TreeLength { get; set; } = 40;
  public int TreeDepth { get; set; } = 20;
  public double MutationRate { get; set; } = 0.1;
  public int TournamentSize { get; set; } = 4;
  public int Elites { get; set; } = 1;
  public int ParameterOptimizationIterations { get; set; } = 5;
  public int Seed { get; set; } = -1; // -1 means use random seed
  public string[] AllowedSymbols { get; set; } = ["add", "sub", "mul", "div", "sqrt", "log", "constant", "variable"];
  public bool UseLinearScaling { get; set; } = true;
}

/// <summary>
/// Evaluator that calls a Python callback after each population evaluation for visualization.
/// The callback is invoked as a side-effect; the original R² objectives are returned unchanged.
/// </summary>
public record VisualizationCallbackEvaluator(
  Func<SymbolicExpressionTree[], ObjectiveVector[], double[][]> PopulationCallback)
  : StatelessEvaluator<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, SymbolicRegressionProblem> {

  public override IReadOnlyList<ObjectiveVector> Evaluate(
    IReadOnlyList<SymbolicExpressionTree> genotypes,
    IRandomNumberGenerator random,
    SymbolicExpressionTreeSearchSpace searchSpace,
    SymbolicRegressionProblem problem) {

    var objectives = new DirectEvaluator<SymbolicExpressionTree>()
      .Evaluate(genotypes, random, searchSpace, problem);

    // Call Python callback for visualization side-effects.
    // Callback returns the objectives to use (allows pass-through).
    var result = PopulationCallback(genotypes.ToArray(), objectives.ToArray());
    return result.Select(x => (ObjectiveVector)x).ToArray();
  }
}

/// <summary>
/// Interactive symbolic regression runner designed for use from a Python web application.
/// Provides fully configurable GP parameters and helper methods for tree formatting and prediction.
/// </summary>
public static class InteractiveSymbolicRegression {

  private static readonly Dictionary<string, Func<string[], Symbol>> SymbolMap = new(StringComparer.OrdinalIgnoreCase) {
    ["add"] = _ => new Addition(),
    ["sub"] = _ => new Subtraction(),
    ["mul"] = _ => new Multiplication(),
    ["div"] = _ => new Division(),
    ["sqrt"] = _ => new SquareRoot(),
    ["log"] = _ => new Logarithm(),
    ["exp"] = _ => new Exponential(),
    ["sin"] = _ => new Sine(),
    ["cos"] = _ => new Cosine(),
    ["tan"] = _ => new Tangent(),
    ["tanh"] = _ => new HyperbolicTangent(),
    ["square"] = _ => new Square(),
    ["cube"] = _ => new Cube(),
    ["cbrt"] = _ => new CubeRoot(),
    ["abs"] = _ => new Absolute(),
    ["pow"] = _ => new Power(),
    ["constant"] = _ => new Number(),
    ["variable"] = varNames => new Variable { VariableNames = varNames },
  };

  /// <summary>
  /// Creates RegressionProblemData from raw x and y arrays (single input variable).
  /// </summary>
  public static RegressionProblemData CreateProblemDataFromArrays(double[] xValues, double[] yValues) {
    var xList = xValues.ToList() as IList;
    var yList = yValues.ToList() as IList;
    var dataset = new ModifiableDataset(
      ["x", "y"],
      new[] { xList, yList },
      cloneValues: false
    );
    return new RegressionProblemData(dataset, "y", ["x"],
      new Range(0, xValues.Length));
  }

  /// <summary>
  /// Builds a symbolic expression grammar from allowed symbol name strings.
  /// </summary>
  public static (SimpleSymbolicExpressionGrammar grammar, SymbolicExpressionTreeSearchSpace searchSpace)
    BuildGrammarAndSearchSpace(string[] allowedSymbols, string[] variableNames, bool useLinearScaling, int treeLength, int treeDepth) {

    var grammar = new SimpleSymbolicExpressionGrammar();

    var symbols = new List<Symbol>();
    foreach (var name in allowedSymbols) {
      if (SymbolMap.TryGetValue(name.Trim(), out var factory)) {
        symbols.Add(factory(variableNames));
      }
    }

    // Ensure we always have at least Number and Variable
    if (!symbols.Any(s => s is Number)) symbols.Add(new Number());
    if (!symbols.Any(s => s is Variable)) symbols.Add(new Variable { VariableNames = variableNames });

    if (useLinearScaling) {
      var root = grammar.AddLinearScaling();
      grammar.AddFullyConnectedSymbols(root, symbols.ToArray());
    } else {
      // Without linear scaling, add all symbols fully-connected via Start symbol
      var root = grammar.StartSymbol;
      grammar.AddFullyConnectedSymbols(root, symbols.ToArray());
    }

    var searchSpace = new SymbolicExpressionTreeSearchSpace(grammar, treeLength, treeDepth);
    return (grammar, searchSpace);
  }

  /// <summary>
  /// Formats a symbolic expression tree as a human-readable infix string.
  /// </summary>
  public static string FormatTree(SymbolicExpressionTree tree) {
    return InfixExpressionFormatter.Format(tree, NumberFormatInfo.InvariantInfo, "G4");
  }

  /// <summary>
  /// Evaluates a symbolic expression tree on the given x-values and returns predicted y-values.
  /// Creates a temporary dataset for evaluation.
  /// </summary>
  public static double[] PredictValues(SymbolicExpressionTree tree, double[] xValues) {
    var dummyY = new double[xValues.Length];
    var xList = xValues.ToList() as IList;
    var yList = dummyY.ToList() as IList;
    var dataset = new ModifiableDataset(
      ["x", "y"],
      new[] { xList, yList },
      cloneValues: false
    );
    var interpreter = new SymbolicDataAnalysisExpressionTreeInterpreter();
    var rows = Enumerable.Range(0, xValues.Length).ToList();
    var predictions = interpreter.GetSymbolicExpressionTreeValues(tree, dataset, rows).ToArray();
    return predictions;
  }

  /// <summary>
  /// Returns a list of available symbol names that can be used in AllowedSymbols.
  /// </summary>
  public static string[] GetAvailableSymbols() {
    return SymbolMap.Keys.ToArray();
  }

  /// <summary>
  /// Runs symbolic regression with full parameter control and a population-wide callback.
  /// The callback is invoked after each generation's evaluation, receiving all trees and their R² objectives.
  /// The callback should return a double[][] of the same shape (pass-through objectives).
  /// </summary>
  public static Population<SymbolicExpressionTree> Run(
    double[] xValues,
    double[] yValues,
    Func<SymbolicExpressionTree[], ObjectiveVector[], double[][]> populationCallback,
    InteractiveSymRegParameters parameters,
    CancellationToken ct = default) {

    var data = CreateProblemDataFromArrays(xValues, yValues);

    var (_, searchSpace) = BuildGrammarAndSearchSpace(
      parameters.AllowedSymbols,
      data.InputVariables.ToArray(),
      parameters.UseLinearScaling,
      parameters.TreeLength,
      parameters.TreeDepth
    );

    var problem = new SymbolicRegressionProblem(data, searchSpace,
      new PearsonR2Evaluator()
    ) {
      ParameterOptimizationIterations = parameters.ParameterOptimizationIterations
    };

    var mutator = ChooseOneMutator.Create(
      new ChangeNodeTypeManipulation(),
      new FullTreeShaker(),
      new OnePointShaker(),
      new RemoveBranchManipulation(),
      new ReplaceBranchManipulation()
    );

    var ga = new GeneticAlgorithmBuilder<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, SymbolicRegressionProblem> {
      Creator = new ProbabilisticTreeCreator(),
      Crossover = new SubtreeCrossover(),
      Mutator = mutator,
      MutationRate = parameters.MutationRate,
      Selector = new TournamentSelector<SymbolicExpressionTree>(parameters.TournamentSize),
      PopulationSize = parameters.PopulationSize,
      Elites = parameters.Elites,
      Evaluator = new VisualizationCallbackEvaluator(populationCallback)
    };

    var seed = parameters.Seed >= 0
      ? parameters.Seed
      : System.Random.Shared.Next();

    var res = ga.Build()
      .WithMaxIterations(parameters.Generations)
      .RunToCompletion(problem, RandomNumberGenerator.Create(seed), null, ct);

    return res.Population;
  }
}
