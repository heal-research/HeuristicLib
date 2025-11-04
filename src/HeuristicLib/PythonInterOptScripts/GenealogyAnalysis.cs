using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Creators;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Crossovers;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Mutators;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

public static class GenealogyAnalysis {
  public static readonly double[,] Data = new double[,] { { 0, 10 }, { 1, 10 }, { 2, 10 }, { 3, 10 }, { 4, 10 }, { 5, 10 }, { 6, 10 }, { 7, 10 }, { 8, 10 }, { 9, 10 }, { 10, 10 } };

  public delegate void GenerationCallback(PopulationIterationResult<SymbolicExpressionTree> current);

  public static (string graph, double[][] childRanks, List<BestMedianWorstEntry<SymbolicExpressionTree>>) GenealogyGraphGeneticAlgorithm(
    string file,
    GenerationCallback callback,
    int seed = 0,
    int populationSize = 10,
    int iterations = 30
  ) {
    var problem = CreateTestSymbolicRegressionProblem(file);

    //algorithm components
    var crossover = new SubtreeCrossover();
    var mutator = CreateSymRegAllMutator();
    var creator = new ProbabilisticTreeCreator();
    var selector = new TournamentSelector<SymbolicExpressionTree>(2);
    var terminator = new AfterIterationsTerminator<SymbolicExpressionTree>(iterations);
    var evaluator = problem.CreateEvaluator();

    //analysis
    var bestMedianWorstAnalyzer = new BestMedianWorstAnalyzer<SymbolicExpressionTree>();
    var graph = GenealogyGraph.AddGenealogyAnalysis(crossover, mutator, ReferenceEqualityComparer.Instance, out var graphAnalyzer, out var graphCrossover, out var graphMutator);
    graphAnalyzer.SaveSpace = true;
    var ranks = new List<List<double>>();
    var rankExtractor = FuncAnalyzer.Build<SymbolicExpressionTree, PopulationIterationResult<SymbolicExpressionTree>>(
      (_, _) => RecordRanks(graph, ranks));
    var pythonCallback = FuncAnalyzer.Build<SymbolicExpressionTree, PopulationIterationResult<SymbolicExpressionTree>>(
      (_, y) => callback(y));

    _ = AlgorithmFactory.GeneticAlgorithm(populationSize,
      creator,
      graphCrossover,
      graphMutator,
      0.05,
      selector,
      evaluator,
      1,
      0,
      terminator,
      null,
      bestMedianWorstAnalyzer,
      graphAnalyzer,
      rankExtractor,
      pythonCallback
    ).Execute(problem, random: new SystemRandomNumberGenerator(seed));

    return (graph.ToGraphViz(), ranks.Select(x => x.ToArray()).ToArray(), bestMedianWorstAnalyzer.CurrentState);
  }

  private static void RecordRanks<TGenotype>(GenealogyGraph<TGenotype> graph, List<List<double>> ranks) where TGenotype : notnull {
    if (graph.Nodes.Count < 2) return;
    var line = graph.Nodes[^2].Values
                    .Where(x => x.Layer == 0)
                    .OrderBy(x => x.Rank)
                    .Select(node => node.Children.Count == 0
                      ? double.NaN
                      : node
                        .GetAllDescendants()
                        .Where(x => x.Rank >= 0)
                        .Average(x => x.Rank)
                    ).ToList();
    if (line.Count > 0) ranks.Add(line);
  }

  private static SymbolicRegressionProblem CreateTestSymbolicRegressionProblem(string? file) {
    var problemData = file is null ? new RegressionProblemData(new ModifiableDataset(["x", "y"], Data), ["x"], "y") : RegressionCsvInstanceProvider.ImportData(file);
    var problem = new SymbolicRegressionProblem(problemData, new RootMeanSquaredErrorEvaluator()) {
      SearchSpace = {
        TreeDepth = 40,
        TreeLength = 40
      }
    };
    if (file is null) {
      problem.LowerPredictionBound = 0;
      problem.UpperPredictionBound = 100;
    }

    var linearScalingRoot = problem.SearchSpace.Grammar.AddLinearScaling();
    problem.SearchSpace.Grammar.AddFullyConnectedSymbols(linearScalingRoot, new Addition(), new Subtraction(), new Multiplication(), new Division(), new Number(), new SquareRoot(), new Logarithm(), new Variable { VariableNames = problemData.InputVariables });
    return problem;
  }

  private static MultiMutator<SymbolicExpressionTree, SymbolicExpressionTreeEncoding, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>> CreateSymRegAllMutator() {
    var symRegAllMutator = MultiMutator.Create(
      new ChangeNodeTypeManipulation(),
      new FullTreeShaker(),
      new OnePointShaker(),
      new RemoveBranchManipulation(),
      new ReplaceBranchManipulation());
    return symRegAllMutator;
  }
}
