using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.EvolutionStrategy;
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
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Operators.Analyzer.Genealogy;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Crossover;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

public class PythonGenealogyAnalysis {
  public static readonly double[,] Data = new double[,] { { 0, 10 }, { 1, 10 }, { 2, 10 }, { 3, 10 }, { 4, 10 }, { 5, 10 }, { 6, 10 }, { 7, 10 }, { 8, 10 }, { 9, 10 }, { 10, 10 } };

  public delegate void GenerationCallback(PopulationIterationResult<SymbolicExpressionTree> current);

  private static SymbolicRegressionProblem CreateTestSymbolicRegressionProblem(string? file, double trainingSplit = 0.66) {
    var problemData = file is null
      ? new RegressionProblemData(new ModifiableDataset(["x", "y"], Data), ["x"], "y")
      : RegressionCsvInstanceProvider.ImportData(file, trainingSplit);
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

  private static MultiMutator<SymbolicExpressionTree, SymbolicExpressionTreeEncoding> CreateSymRegAllMutator() {
    var symRegAllMutator = MultiMutator.Create(
      new ChangeNodeTypeManipulation(),
      new FullTreeShaker(),
      new OnePointShaker(),
      new RemoveBranchManipulation(),
      new ReplaceBranchManipulation());
    return symRegAllMutator;
  }

  public static (string graph, List<List<double>> childRanks, List<BestMedianWorstEntry<SymbolicExpressionTree>>) RunSymbolicRegressionGeneticAlgorithm(
    string file,
    GenerationCallback callback,
    int seed,
    int populationSize = 10,
    int iterations = 30,
    bool withGenealogy = true,
    double trainingSplit = 0.66
  ) {
    var problem = CreateTestSymbolicRegressionProblem(file, trainingSplit);

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

    FuncAnalysis.Create(ga, (_, y) => callback(y));
    var qualities = BestMedianWorstAnalysis.Create(ga);

    if (withGenealogy) {
      var genealogyAnalysis = GenealogyAnalysis.Create(ga, saveSpace: true);
      var ranks = new List<List<double>>();
      FuncAnalysis.Create(ga, (_, _) => RecordRanks(genealogyAnalysis.Graph, ranks));
      _ = ga.Execute(problem, random: new SystemRandomNumberGenerator(seed));
      var graphViz = genealogyAnalysis.Graph.ToGraphViz();
      return (graphViz, ranks, qualities.BestSolutions);
    }

    _ = ga.Execute(problem, random: new SystemRandomNumberGenerator(seed));
    return (string.Empty, [], qualities.BestSolutions);
  }

  public static (string graph, List<List<double>> childRanks, List<BestMedianWorstEntry<SymbolicExpressionTree>>) RunSymbolicRegressionGeneticAlgorithmConfigurable(
    string file,
    GenerationCallback? callback = null,
    int seed = 0,
    int elites = 1,
    int populationSize = 10,
    int iterations = 30,
    bool withGenealogy = true,
    double trainingSplit = 0.66,
    double mutationRate = 0.05,
    int noChildren = 0,
    bool withCrossover = false,
    EvolutionStrategyType strategy = EvolutionStrategyType.Plus,
    ICreator<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>? creator = null,
    ICrossover<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>? crossover = null,
    IMutator<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>? mutator = null,
    ISelector<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>? selector = null) {
    creator ??= new ProbabilisticTreeCreator();
    crossover ??= new SubtreeCrossover();
    mutator ??= CreateSymRegAllMutator();
    selector ??= new TournamentSelector<SymbolicExpressionTree>(3);

    var problem = CreateTestSymbolicRegressionProblem(file, trainingSplit);
    var evaluator = problem.CreateEvaluator();
    var terminator = new AfterIterationsTerminator<SymbolicExpressionTree>(iterations);

    var ga = GeneticAlgorithm.CreatePrototype(populationSize, creator, crossover, mutator, mutationRate, selector,
      evaluator, elites, seed, terminator);

    var es = EvolutionStrategy.CreatePrototype(populationSize, noChildren, strategy, creator, mutator, mutationRate,
      selector, evaluator, seed, terminator, withCrossover ? crossover : null);

    if (callback != null) {
      FuncAnalysis.Create(es, (_, y) => callback(y));
    }

    var qualities = BestMedianWorstAnalysis.Create(ga);

    if (withGenealogy) {
      var genealogyAnalysis = GenealogyAnalysis.Create(ga, saveSpace: true);
      var ranks = new List<List<double>>();
      FuncAnalysis.Create(ga, (_, _) => RecordRanks(genealogyAnalysis.Graph, ranks));
      _ = ga.Execute(problem, random: new SystemRandomNumberGenerator(seed));
      var graphViz = genealogyAnalysis.Graph.ToGraphViz();
      return (graphViz, ranks, qualities.BestSolutions);
    }

    _ = ga.Execute(problem, random: new SystemRandomNumberGenerator(seed));
    return (string.Empty, [], qualities.BestSolutions);
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
