using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Grammars;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.SymbolicExpression.Creators;
using HEAL.HeuristicLib.Operators.SymbolicExpression.Crossovers;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.SymbolicExpression.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib;

public class BernhardPlayground {
  public static readonly double[,] Data = new double[,] { { 0, 10 }, { 1, 10 }, { 2, 10 }, { 3, 10 }, { 4, 10 }, { 5, 10 }, { 6, 10 }, { 7, 10 }, { 8, 10 }, { 9, 10 }, { 10, 10 } };

  public delegate void GenerationCallback(PopulationIterationResult<SymbolicExpressionTree> current);

  private static SymbolicRegressionProblem CreateTestSymbolicRegressionProblem(string? file) {
    var problemData = file is null ? new RegressionProblemData(new ModifiableDataset(["x", "y"], Data), ["x"], "y") : RegressionCsvInstanceProvider.ImportData(file);
    var problem = new SymbolicRegressionProblem(problemData, [new RootMeanSquaredErrorEvaluator()]) {
      SearchSpace = {
        TreeDepth = 40,
        TreeLength = 40
      }
    };
    if (file is null) {
      problem.LowerPredictionBound = 0;
      problem.UpperPredictionBound = 100;
    }

    var aroot = problem.SearchSpace.Grammar.AddLinearScaling();
    problem.SearchSpace.Grammar.AddFullyConnectedSymbols([new Addition(), new Subtraction(), new Multiplication(), new Division(), new Number(), new SquareRoot(), new Logarithm(), new Variable { VariableNames = problemData.InputVariables }], aroot);
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

  public (string graph,
    List<List<double>> childRanks,
    List<(Solution<SymbolicExpressionTree> best, Solution<SymbolicExpressionTree> median, Solution<SymbolicExpressionTree> worst)>
    ) GenealogyGraphGeneticAlgorithm(
      string file,
      GenerationCallback callback,
      int seed,
      int populationSize = 10,
      int iterations = 30
    ) {
    var problem = CreateTestSymbolicRegressionProblem(file);
    var qualities = new BestMedianWorstAnalyzer<SymbolicExpressionTree>();

    var graph = new GenealogyGraph<SymbolicExpressionTree>(ReferenceEqualityComparer.Instance);
    var graphAnalyzer = graph.GetInterceptor();
    graphAnalyzer.SaveSpace = true;
    var graphCrossover = graph.WrapCrossover(new SubtreeCrossover());
    var graphMutator = graph.WrapMutator(CreateSymRegAllMutator());

    var ranks = new List<List<double>>();
    var rankExtractor = FuncAnalyzer.Build<SymbolicExpressionTree, PopulationIterationResult<SymbolicExpressionTree>>(
      (_, _) => RecordRanks(graph, ranks));
    var pythonCallback = FuncAnalyzer.Build<SymbolicExpressionTree, PopulationIterationResult<SymbolicExpressionTree>>(
      (_, y) => callback(y));

    var random = new SystemRandomNumberGenerator(seed);

    var ga = AlgorithmFactory.GeneticAlgorithm(populationSize,
      new ProbabilisticTreeCreator(),
      graphCrossover,
      graphMutator,
      0.05,
      new TournamentSelector<SymbolicExpressionTree>(2),
      1,
      0,
      new AfterIterationsTerminator<SymbolicExpressionTree>(iterations),
      null,
      qualities,
      graphAnalyzer,
      rankExtractor,
      pythonCallback
    );
    _ = ga.Execute(problem, random: random);
    var graphViz = graph.ToGraphViz();

    return (graphViz, ranks, qualities.CurrentState);
  }

  public static void RecordRanks<TGenotype>(GenealogyGraph<TGenotype> graph, List<List<double>> ranks) where TGenotype : notnull {
    if (graph.Nodes.Count < 2)
      return;
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
    if (line.Count > 0) {
      ranks.Add(line);
    }
  }
}
