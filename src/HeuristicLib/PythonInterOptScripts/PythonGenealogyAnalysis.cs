using System.Collections.Concurrent;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Analyzers;
using HEAL.HeuristicLib.Analyzers.Genealogy;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Creators.PermutationCreators;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionTreeCreators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionTreeCrossovers;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;
using HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionTreeMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Problems.TravelingSalesman.InstanceLoading;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.SearchSpaces.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

public class PythonGenealogyAnalysis {
  public delegate void GenerationCallback(PopulationIterationState<SymbolicExpressionTree> current);

  public delegate void PermutationGenerationCallback(PopulationIterationState<Permutation> current);

  private static MultiMutator<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace> CreateSymRegAllMutator() {
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
    var actionCallback = callback is null ? null : new Action<PopulationIterationState<SymbolicExpressionTree>>(callback);
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
    var actionCallback = callback is null ? null : new Action<PopulationIterationState<Permutation>>(callback);
    return RunAlgorithmConfigurable(problem, actionCallback, parameters);
  }
  #endregion

  #region Parameters
  public record ExperimentParameters<T, TS>(
    int Seed = 0,
    int Elites = 1,
    int PopulationSize = 10,
    int Iterations = 30,
    double MutationRate = 0.05,
    int NoChildren = -1,
    bool WithCrossover = false,
    EvolutionStrategyType Strategy = EvolutionStrategyType.Plus,
    ICreator<T, TS>? Creator = null,
    ICrossover<T, TS>? Crossover = null,
    IMutator<T, TS>? Mutator = null,
    ISelector<T>? Selector = null,
    string AlgorithmName = "ga") where TS : class, ISearchSpace<T>;

  public record SymRegExperimentParameters(
    int Seed = 0,
    int Elites = 1,
    int PopulationSize = 10,
    int Iterations = 30,
    double MutationRate = 0.05,
    int NoChildren = -1,
    bool WithCrossover = false,
    EvolutionStrategyType Strategy = EvolutionStrategyType.Plus,
    ICreator<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>? Creator = null,
    ICrossover<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>? Crossover = null,
    IMutator<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>? Mutator = null,
    ISelector<SymbolicExpressionTree>? Selector = null,
    string AlgorithmName = "ga") :
    ExperimentParameters<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>(Seed, Elites, PopulationSize,
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
    ICreator<Permutation, PermutationSearchSpace>? Creator = null,
    ICrossover<Permutation, PermutationSearchSpace>? Crossover = null,
    IMutator<Permutation, PermutationSearchSpace>? Mutator = null,
    ISelector<Permutation>? Selector = null,
    string AlgorithmName = "ga") : ExperimentParameters<Permutation, PermutationSearchSpace>(Seed, Elites, PopulationSize,
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
    BestMedianWorstQualityAttachedAnalysis<T> Qualities,
    RankAnalysis<T> RankAnalysis,
    QualityCurveAnalysis<T> QualityCurve) where T : class { }

  private static MyAnalyzers<T>
    AddAnalyzers<T, TS, TP, TRes>(Action<TRes>? callback, IAlgorithmBuilder<T, TS, TP, TRes> builder)
    where TRes : PopulationIterationState<T>
    where T : class
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS> {
    var qualities = BestMedianWorstAnalysis.Analyze(builder);
    if (callback != null)
      FuncAnalysis.Create(builder, (_, y) => callback(y));
    var rankAnalysis = new RankAnalysis<T>();
    rankAnalysis.AttachTo(builder);
    var qc = QualityCurveAnalysis.Create(builder);
    return new MyAnalyzers<T>(qualities, rankAnalysis, qc);
  }

  private static (string graph, List<List<double>> childRanks, List<BestMedianWorstEntry<T>>) RunAlgorithmConfigurable<T, TS>(
    IProblem<T, TS> problem,
    Action<PopulationIterationState<T>>? callback,
    ExperimentParameters<T, TS> parameters) where TS : class, ISearchSpace<T> where T : class {
    var terminator = new AfterIterationsTerminator<T>(parameters.Iterations);
    if (parameters.NoChildren < 0)
      parameters = parameters with { NoChildren = parameters.PopulationSize };

    IAlgorithm<T, TS, IProblem<T, TS>, PopulationIterationState<T>> algorithm;

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
        algorithm = ga.Build();
        break;
      case "es":
        var es = EvolutionStrategy.GetBuilder(parameters.Creator!, parameters.Mutator!);
        es.PopulationSize = parameters.PopulationSize;
        es.NumberOfChildren = parameters.NoChildren;
        es.Strategy = parameters.Strategy;
        es.Terminator = terminator;
        es.RandomSeed = parameters.Seed;
        if (parameters.Selector != null)
          es.Selector = parameters.Selector;
        if (parameters.WithCrossover)
          es.Crossover = parameters.Crossover;

        analyzers = AddAnalyzers(callback, es);
        algorithm = es.Build();
        break;
      case "ls":
        var ls = HillClimber.GetBuilder(parameters.Creator!, parameters.Mutator!);
        ls.BatchSize = ls.MaxNeighbors = parameters.NoChildren;
        ls.Terminator = terminator;
        ls.RandomSeed = parameters.Seed;
        
        analyzers = AddAnalyzers(callback, ls);
        algorithm = ls.Build();
        break;
      case "nsga2":
        var nsga2 = NSGA2.GetBuilder(parameters.Creator!, parameters.Crossover!, parameters.Mutator!);
        nsga2.PopulationSize = parameters.PopulationSize;
        nsga2.MutationRate = parameters.MutationRate;
        if (parameters.Selector != null)
          nsga2.Selector = parameters.Selector;
        nsga2.RandomSeed = parameters.Seed;
        nsga2.Terminator = terminator;
        //nsga2.DominateOnEquals = true; //todo make configurable
        analyzers = AddAnalyzers(callback, nsga2);
        algorithm = nsga2.Build();
        break;
      default:
        throw new ArgumentException($"Algorithm '{parameters.AlgorithmName}' is not supported.");
    }

    _ = algorithm.Execute(problem, random: new SystemRandomNumberGenerator(parameters.Seed));
    return (analyzers.RankAnalysis.Graph.ToGraphViz(), analyzers.RankAnalysis.Ranks, analyzers.Qualities.BestISolutions);
  }
  #endregion
}
