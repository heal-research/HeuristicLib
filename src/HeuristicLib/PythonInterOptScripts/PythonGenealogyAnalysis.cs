using System.Collections.Concurrent;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.EvolutionStrategy;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.NSGA2;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Encodings.Trees;
using HEAL.HeuristicLib.Encodings.Trees.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.Encodings.Trees.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.Encodings.Vectors;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Operators.Analyzer.Genealogy;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Creator.Permutations;
using HEAL.HeuristicLib.Operators.Creator.SymbolicExpressionTrees;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Crossover.Permutations;
using HEAL.HeuristicLib.Operators.Crossover.SymbolicExpressionTrees;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Mutator.Permutations;
using HEAL.HeuristicLib.Operators.Mutator.SymbolicExpressionTrees;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Operators.Terminator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Problems.TravelingSalesman.InstanceLoading;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

public class PythonGenealogyAnalysis {
  public delegate void GenerationCallback(PopulationIterationResult<SymbolicExpressionTree> current);

  public delegate void PermutationGenerationCallback(PopulationIterationResult<Permutation> current);

  private static MultiMutator<SymbolicExpressionTree, SymbolicExpressionTreeEncoding> CreateSymRegAllMutator() {
    var symRegAllMutator = MultiMutator.Create(
      new ChangeNodeTypeManipulation(),
      new FullTreeShaker(),
      new OnePointShaker(),
      new RemoveBranchManipulation(),
      new ReplaceBranchManipulation());
    return symRegAllMutator;
  }

  #region public methods
  public static (string graph, List<List<double>> childRanks, List<BestMedianWorstEntry<SymbolicExpressionTree>>)[]
    RunSymbolicRegressionConfigurable(string file, SymRegExperimentParameters parameters, int repetitions, double trainingSplit = 0.66) {
    return Enumerable.Range(0, repetitions).ParallelSelect(new SystemRandomNumberGenerator(parameters.Seed),
      (i, _, _) => RunSymbolicRegressionConfigurable(
        file,
        parameters with { Seed = parameters.Seed + i },
        trainingSplit)
    ).ToArray();
  }

  public static (string graph, List<List<double>> childRanks, List<BestMedianWorstEntry<SymbolicExpressionTree>>) RunSymbolicRegressionConfigurable(
    string file,
    SymRegExperimentParameters parameters,
    double trainingSplit = 0.66,
    GenerationCallback? callback = null) {
    parameters = parameters with {
      Creator = parameters.Creator ?? new ProbabilisticTreeCreator(),
      Crossover = parameters.Crossover ?? new SubtreeCrossover(),
      Mutator = parameters.Mutator ?? CreateSymRegAllMutator()
    };
    var problem = CreateTestSymbolicRegressionProblem(file, trainingSplit);
    var actionCallback = callback is null ? null : new Action<PopulationIterationResult<SymbolicExpressionTree>>(callback);
    return RunAlgorithmConfigurable(problem, actionCallback, parameters);
  }

  public static (string graph, List<List<double>> childRanks, List<BestMedianWorstEntry<Permutation>>)[]
    RunTravelingSalesmanConfigurable(string file, TravelingSalesmanExperimentParameters parameters, int repetitions) {
    return Enumerable.Range(0, repetitions).ParallelSelect(new SystemRandomNumberGenerator(parameters.Seed),
      (i, _, _) => RunTravelingSalesmanConfigurable(
        file,
        parameters with { Seed = parameters.Seed + i })
    ).ToArray();
  }

  public static (string graph, List<List<double>> childRanks, List<BestMedianWorstEntry<Permutation>>) RunTravelingSalesmanConfigurable(
    string file,
    TravelingSalesmanExperimentParameters parameters,
    PermutationGenerationCallback? callback = null) {
    var problem = CreateTestTravellingSalesmanProblem(file);

    parameters = parameters with {
      Creator = parameters.Creator ?? new RandomPermutationCreator(),
      Crossover = parameters.Crossover ?? new EdgeRecombinationCrossover(),
      Mutator = parameters.Mutator ?? new InversionMutator()
    };
    var actionCallback = callback is null ? null : new Action<PopulationIterationResult<Permutation>>(callback);
    return RunAlgorithmConfigurable(problem, actionCallback, parameters);
  }
  #endregion

  #region Parameters
  public record ExperimentParameters<T, TE>(
    int Seed = 0,
    int Elites = 1,
    int PopulationSize = 10,
    int Iterations = 30,
    double MutationRate = 0.05,
    int NoChildren = -1,
    bool WithCrossover = false,
    EvolutionStrategyType Strategy = EvolutionStrategyType.Plus,
    ICreator<T, TE>? Creator = null,
    ICrossover<T, TE>? Crossover = null,
    IMutator<T, TE>? Mutator = null,
    ISelector<T>? Selector = null,
    string AlgorithmName = "ga") where TE : class, IEncoding<T>;

  public record SymRegExperimentParameters(
    int Seed = 0,
    int Elites = 1,
    int PopulationSize = 10,
    int Iterations = 30,
    double MutationRate = 0.05,
    int NoChildren = -1,
    bool WithCrossover = false,
    EvolutionStrategyType Strategy = EvolutionStrategyType.Plus,
    ICreator<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>? Creator = null,
    ICrossover<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>? Crossover = null,
    IMutator<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>? Mutator = null,
    ISelector<SymbolicExpressionTree>? Selector = null,
    string AlgorithmName = "ga") :
    ExperimentParameters<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>(Seed, Elites, PopulationSize,
      Iterations, MutationRate, NoChildren, WithCrossover, Strategy, Creator, Crossover, Mutator, Selector,
      AlgorithmName);

  public record TravelingSalesmanExperimentParameters(
    int Seed = 0,
    int Elites = 1,
    int PopulationSize = 10,
    int Iterations = 30,
    double MutationRate = 0.05,
    int NoChildren = -1,
    bool WithCrossover = false,
    EvolutionStrategyType Strategy = EvolutionStrategyType.Plus,
    ICreator<Permutation, PermutationEncoding>? Creator = null,
    ICrossover<Permutation, PermutationEncoding>? Crossover = null,
    IMutator<Permutation, PermutationEncoding>? Mutator = null,
    ISelector<Permutation>? Selector = null,
    string AlgorithmName = "ga") : ExperimentParameters<Permutation, PermutationEncoding>(Seed, Elites, PopulationSize,
    Iterations, MutationRate, NoChildren, WithCrossover, Strategy, Creator, Crossover, Mutator, Selector,
    AlgorithmName);
  #endregion

  #region Problem Generation
  private static readonly ConcurrentDictionary<string, ITravelingSalesmanProblemData> TSPCache = [];

  private static TravelingSalesmanProblem CreateTestTravellingSalesmanProblem(string file) {
    var cdata = TSPCache.GetOrAdd(file, s => {
      var t = TsplibTspInstanceProvider.LoadData(s);
      return (t.Coordinates?.Length ?? 0) <= 1000 ? t.ToDistanceMatrixData() : t.ToCoordinatesData();
    });
    return new TravelingSalesmanProblem(cdata);
  }

  private static readonly ConcurrentDictionary<(string, double), RegressionProblemData> SymRegCache = [];

  private static SymbolicRegressionProblem CreateTestSymbolicRegressionProblem(string file, double trainingSplit = 0.66) {
    var problemData = SymRegCache.GetOrAdd((file, trainingSplit), key => RegressionCsvInstanceProvider.ImportData(key.Item1, key.Item2));
    var problem = new SymbolicRegressionProblem(problemData, new RootMeanSquaredErrorEvaluator()) {
      SearchSpace = {
        TreeDepth = 40,
        TreeLength = 40
      }
    };
    var root = problem.SearchSpace.Grammar.AddLinearScaling();
    problem.SearchSpace.Grammar.AddFullyConnectedSymbols(root, new Addition(), new Subtraction(), new Multiplication(), new Division(), new Number(), new SquareRoot(), new Logarithm(), new Variable { VariableNames = problemData.InputVariables });
    return problem;
  }
  #endregion

  #region generic helpers
  private record MyAnalyzers<T>(
    BestMedianWorstAnalysis<T> Qualities,
    RankAnalysis<T> RankAnalysis,
    QualityCurveAnalysis<T> QualityCurve) where T : class { }

  private static MyAnalyzers<T>
    AddAnalyzers<T, TE, TP, TRes>(Action<TRes>? callback, IAlgorithmBuilder<T, TE, TP, TRes> builder)
    where TRes : PopulationIterationResult<T>
    where T : class
    where TE : class, IEncoding<T>
    where TP : class, IProblem<T, TE> {
    var qualities = BestMedianWorstAnalysis.Analyze(builder);
    if (callback != null)
      FuncAnalysis.Create(builder, (_, y) => callback(y));
    var rankAnalysis = new RankAnalysis<T>();
    rankAnalysis.AttachTo(builder);
    var qc = QualityCurveAnalysis.Create(builder);
    return new MyAnalyzers<T>(qualities, rankAnalysis, qc);
  }

  private static (string graph, List<List<double>> childRanks, List<BestMedianWorstEntry<T>>) RunAlgorithmConfigurable<T, TE>(
    IProblem<T, TE> problem,
    Action<PopulationIterationResult<T>>? callback,
    ExperimentParameters<T, TE> parameters) where TE : class, IEncoding<T> where T : class {
    var terminator = new AfterIterationsTerminator<T>(parameters.Iterations);
    if (parameters.NoChildren < 0)
      parameters = parameters with { NoChildren = parameters.PopulationSize };

    IAlgorithm<T, TE, IProblem<T, TE>, PopulationIterationResult<T>> algorithm;

    MyAnalyzers<T> analyzers;

    switch (parameters.AlgorithmName.ToLower()) {
      case "ga":
        var ga = GeneticAlgorithm.GetBuilder(parameters.Creator!, parameters.Crossover!, parameters.Mutator!);
        ga.PopulationSize = parameters.PopulationSize;
        ga.MutationRate = parameters.MutationRate;
        ga.Elites = parameters.Elites;
        if (parameters.Selector != null)
          ga.Selector = parameters.Selector;
        ga.RandomSeed = parameters.Seed;
        ga.Terminator = terminator;

        analyzers = AddAnalyzers(callback, ga);
        algorithm = ga.BuildAlgorithm();
        break;
      case "es":
        var es = EvolutionStrategy.GetBuilder(parameters.Creator!, parameters.Mutator!);
        es.PopulationSize = parameters.PopulationSize;
        es.NoChildren = parameters.NoChildren;
        es.Strategy = parameters.Strategy;
        es.Terminator = terminator;
        es.RandomSeed = parameters.Seed;
        if (parameters.Selector != null)
          es.Selector = parameters.Selector;
        if (parameters.WithCrossover)
          es.Crossover = parameters.Crossover;

        analyzers = AddAnalyzers(callback, es);
        algorithm = es.BuildAlgorithm();
        break;
      case "ls":
        var ls = LocalSearch.GetBuilder(parameters.Creator!, parameters.Mutator!);
        ls.BatchSize = ls.MaxNeighbors = parameters.NoChildren;
        ls.Terminator = terminator;
        ls.RandomSeed = parameters.Seed;

        analyzers = AddAnalyzers(callback, ls);
        algorithm = ls.BuildAlgorithm();
        break;
      case "nsga2":
        var nsga2 = Nsga2.GetBuilder(parameters.Creator!, parameters.Crossover!, parameters.Mutator!);
        nsga2.PopulationSize = parameters.PopulationSize;
        nsga2.MutationRate = parameters.MutationRate;
        if (parameters.Selector != null)
          nsga2.Selector = parameters.Selector;
        nsga2.RandomSeed = parameters.Seed;
        nsga2.Terminator = terminator;
        nsga2.DominateOnEquals = true; //todo make configurable
        analyzers = AddAnalyzers(callback, nsga2);
        algorithm = nsga2.BuildAlgorithm();
        break;
      default:
        throw new ArgumentException($"Algorithm '{parameters.AlgorithmName}' is not supported.");
    }

    _ = algorithm.Execute(problem, random: new SystemRandomNumberGenerator(parameters.Seed));
    return (analyzers.RankAnalysis.Graph.ToGraphViz(), analyzers.RankAnalysis.Ranks, analyzers.Qualities.BestISolutions);
  }
  #endregion
}
