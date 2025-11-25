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
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Operators.Analyzer.Genealogy;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Terminator;
using HEAL.HeuristicLib.Problems;

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

    var ga = GeneticAlgorithm.GetBuilder(
      new ProbabilisticTreeCreator(),
      new SubtreeCrossover(),
      CreateSymRegAllMutator()
    );
    ga.PopulationSize = populationSize;
    ga.Terminator = new AfterIterationsTerminator<SymbolicExpressionTree>(iterations);
    ga.RandomSeed = seed;

    return AttachAnalysisAndRun(callback, seed, withGenealogy, ga, problem);
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
    ISelector<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>? selector = null,
    string algorithm = "ga") {
    creator ??= new ProbabilisticTreeCreator();
    crossover ??= new SubtreeCrossover();
    mutator ??= CreateSymRegAllMutator();
    selector ??= new TournamentSelector<SymbolicExpressionTree>(3);

    var problem = CreateTestSymbolicRegressionProblem(file, trainingSplit);
    var terminator = new AfterIterationsTerminator<SymbolicExpressionTree>(iterations);

    switch (algorithm.ToLower()) {
      case "ga":
        var ga = GeneticAlgorithm.GetBuilder(creator, crossover, mutator);
        ga.PopulationSize = populationSize;
        ga.MutationRate = mutationRate;
        ga.Elites = elites;
        ga.Selector = selector;
        ga.RandomSeed = seed;
        ga.Terminator = terminator;

        return AttachAnalysisAndRun(callback, seed, withGenealogy, ga, problem);

      case "es":
        var es = EvolutionStrategy.CreatePrototype(creator, mutator);
        es.PopulationSize = populationSize;
        es.NoChildren = noChildren;
        es.Strategy = strategy;
        es.Terminator = terminator;
        es.RandomSeed = seed;
        es.Selector = selector;
        if (withCrossover) es.Crossover = crossover;

        return AttachAnalysisAndRun(callback, seed, withGenealogy, es, problem);
      //case "es": 
      //  proto = EvolutionStrategy.CreatePrototype(populationSize, noChildren, strategy, creator, mutator, mutationRate,
      //    selector, evaluator, seed, terminator, withCrossover ? crossover : null);
      //  break;
      default:
        throw new ArgumentException($"Algorithm '{algorithm}' is not supported.");
    }
  }

  private static (string graphViz, List<List<double>> ranks, List<BestMedianWorstEntry<SymbolicExpressionTree>> BestISolutions)
    AttachAnalysisAndRun<TRes>(GenerationCallback? callback, int seed, bool withGenealogy,
                               IAlgorithmBuilder<SymbolicExpressionTree, SymbolicExpressionTreeEncoding, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>, TRes> ga,
                               IProblem<SymbolicExpressionTree, SymbolicExpressionTreeEncoding> problem)
    where TRes : PopulationIterationResult<SymbolicExpressionTree> {
    var qualities = new BestMedianWorstAnalysis<SymbolicExpressionTree>();
    qualities.AttachTo(ga);
    if (callback != null)
      FuncAnalysis.Create(ga, (_, y) => callback(y));

    var ranks = new List<List<double>>();
    var graphViz = string.Empty;
    if (withGenealogy) {
      var genealogyAnalysis = GenealogyAnalysis.Create(ga, saveSpace: true);
      FuncAnalysis.Create(ga, (_, _) => RecordRanks(genealogyAnalysis.Graph, ranks));
      _ = ga.BuildAlgorithm().Execute(problem, random: new SystemRandomNumberGenerator(seed));
      graphViz = genealogyAnalysis.Graph.ToGraphViz();
    } else {
      _ = ga.BuildAlgorithm().Execute(problem, random: new SystemRandomNumberGenerator(seed));
    }

    return (graphViz, ranks, qualities.BestISolutions);
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
