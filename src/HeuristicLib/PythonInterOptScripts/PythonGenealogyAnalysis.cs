using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
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
using HEAL.HeuristicLib.Operators.Analyzer.Genealogy;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

public class PythonGenealogyAnalysis {
  public static readonly double[,] Data = new double[,] { { 0, 10 }, { 1, 10 }, { 2, 10 }, { 3, 10 }, { 4, 10 }, { 5, 10 }, { 6, 10 }, { 7, 10 }, { 8, 10 }, { 9, 10 }, { 10, 10 } };

  public delegate void GenerationCallback(PopulationIterationResult<SymbolicExpressionTree> current);

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

    var aroot = problem.SearchSpace.Grammar.AddLinearScaling();
    problem.SearchSpace.Grammar.AddFullyConnectedSymbols(aroot, new Addition(), new Subtraction(), new Multiplication(), new Division(), new Number(), new SquareRoot(), new Logarithm(), new Variable { VariableNames = problemData.InputVariables });
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

    var ga = GeneticAlgorithm.CreatePrototype(populationSize,
      new ProbabilisticTreeCreator(),
      new SubtreeCrossover(),
      CreateSymRegAllMutator(),
      0.05,
      new TournamentSelector<SymbolicExpressionTree>(3),
      problem.CreateEvaluator(),
      1,
      0,
      new AfterIterationsTerminator<SymbolicExpressionTree>(iterations)
    );

    var genealogyAnalysis = GenealogyAnalysis.Create(ga, saveSpace: true);
    var qualities = BestMedianWorstAnalysis.Create(ga);

    var ranks = new List<List<double>>();
    FuncAnalysis.Create(ga, (_, _) => RecordRanks(genealogyAnalysis.Graph, ranks));

    FuncAnalysis.Create(ga, (_, y) => callback(y));

    _ = ga.CreateAlgorithm().Execute(problem, random: new SystemRandomNumberGenerator(seed));
    var graphViz = genealogyAnalysis.Graph.ToGraphViz();

    return (graphViz, ranks, qualities.CurrentState);
  }

  private static void RecordRanks<TGenotype>(GenealogyGraph<TGenotype> graph, List<List<double>> ranks) where TGenotype : notnull {
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
