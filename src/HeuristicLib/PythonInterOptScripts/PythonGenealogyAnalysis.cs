using System.Collections.Concurrent;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.EvolutionStrategy;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.NSGA2;
using HEAL.HeuristicLib.Encodings.Permutation;
using HEAL.HeuristicLib.Encodings.Permutation.Creators;
using HEAL.HeuristicLib.Encodings.Permutation.Crossovers;
using HEAL.HeuristicLib.Encodings.Permutation.Mutators;
using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Encodings.RealVector.Creators;
using HEAL.HeuristicLib.Encodings.RealVector.Crossovers;
using HEAL.HeuristicLib.Encodings.RealVector.Mutators;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Creators;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Crossovers;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Mutators;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Operators.Analyzer.Genealogy;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Operators.Terminator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.BBoB;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Problems.TravelingSalesman.InstanceLoading;
using HEAL.HeuristicLib.Random;

#pragma warning disable S1104
#pragma warning disable S1104

namespace HEAL.HeuristicLib.PythonInterOptScripts;

public class PythonGenealogyAnalysis {
  public delegate void GenerationCallback(object current);

  public record ExperimentResult<T>(
    string Graph,
    List<List<double>> ChildRanks,
    List<BestMedianWorstEntry<T>> BestMedianWorst,
    List<ISolution<T>[]> AllPopulations)
    where T : class;

  #region public methods
  public static ExperimentResult<SymbolicExpressionTree>[]
    RunSymbolicRegressionConfigurable(string file, SymRegExperimentParameters parameters, int repetitions, double trainingSplit = 0.66) {
    return Enumerable.Range(0, repetitions).ParallelSelect(new SystemRandomNumberGenerator(parameters.Seed),
      (i, _, _) => RunSymbolicRegressionConfigurable(
        file,
        new SymRegExperimentParameters(parameters) { Seed = parameters.Seed + i },
        trainingSplit)
    ).ToArray();
  }

  public static ExperimentResult<SymbolicExpressionTree> RunSymbolicRegressionConfigurable(
    string file,
    SymRegExperimentParameters parameters,
    double trainingSplit = 0.66,
    GenerationCallback? callback = null) {
    parameters = new SymRegExperimentParameters(parameters) {
      Creator = parameters.Creator ?? new ProbabilisticTreeCreator(),
      Crossover = parameters.Crossover ?? new SubtreeCrossover(),
      Mutator = parameters.Mutator ?? CreateSymRegAllMutator()
    };
    var problem = CreateTestSymbolicRegressionProblem(file, trainingSplit);
    var actionCallback = callback is null ? null : new Action<PopulationIterationResult<SymbolicExpressionTree>>(callback);
    return RunAlgorithmConfigurable(problem, actionCallback, parameters);
  }

  public static ExperimentResult<Permutation>[]
    RunTravelingSalesmanConfigurable(string file, TravelingSalesmanExperimentParameters parameters, int repetitions) {
    return Enumerable.Range(0, repetitions).ParallelSelect(new SystemRandomNumberGenerator(parameters.Seed),
      (i, _, _) => RunTravelingSalesmanConfigurable(
        file, new TravelingSalesmanExperimentParameters(parameters) { Seed = parameters.Seed + i })
    ).ToArray();
  }

  public static ExperimentResult<Permutation> RunTravelingSalesmanConfigurable(
    string file,
    TravelingSalesmanExperimentParameters parameters,
    GenerationCallback? callback = null) {
    var problem = CreateTestTravellingSalesmanProblem(file);

    parameters = new TravelingSalesmanExperimentParameters(parameters) {
      Creator = parameters.Creator ?? new RandomPermutationCreator(),
      Crossover = parameters.Crossover ?? new EdgeRecombinationCrossover(),
      Mutator = parameters.Mutator ?? new InversionMutator()
    };
    var actionCallback = callback is null ? null : new Action<PopulationIterationResult<Permutation>>(callback);
    return RunAlgorithmConfigurable(problem, actionCallback, parameters);
  }

  public static ExperimentResult<RealVector> RunTestFunctionConfigurable(
    string file,
    TestFunctionExperimentParameters parameters,
    GenerationCallback? callback = null) {
    parameters = new TestFunctionExperimentParameters(parameters) {
      Creator = parameters.Creator ?? new UniformDistributedCreator(),
      Crossover = parameters.Crossover ?? new SimulatedBinaryCrossover(),
      Mutator = parameters.Mutator ?? new GaussianMutator(1.0 / parameters.dimension, 0.01)
    };
    var problem = CreateTestFunctionProblem(parameters.problem, parameters.dimension, parameters.instance);
    var actionCallback = callback is null ? null : new Action<PopulationIterationResult<RealVector>>(callback);
    return RunAlgorithmConfigurable(problem, actionCallback, parameters);
  }
  #endregion

  #region Parameters
  public class ExperimentParameters<T, TE> where TE : class, IEncoding<T> {
    public int Seed;
    public int Elites = 1;
    public int PopulationSize = 10;
    public int Iterations = 30;
    public double MutationRate = 0.05;
    public int NoChildren = -1;
    public bool WithCrossover;
    public EvolutionStrategyType Strategy = EvolutionStrategyType.Plus;
    public ICreator<T, TE>? Creator;
    public ICrossover<T, TE>? Crossover;
    public IMutator<T, TE>? Mutator;
    public ISelector<T>? Selector;
    public bool TrackGenealogy;
    public bool TrackPopulations;

    public string AlgorithmName = "ga";
    public ExperimentParameters() { }

    public ExperimentParameters(ExperimentParameters<T, TE> parameters) {
      Seed = parameters.Seed;
      Elites = parameters.Elites;
      PopulationSize = parameters.PopulationSize;
      Iterations = parameters.Iterations;
      MutationRate = parameters.MutationRate;
      NoChildren = parameters.NoChildren;
      WithCrossover = parameters.WithCrossover;
      Strategy = parameters.Strategy;
      Selector = parameters.Selector;
      AlgorithmName = parameters.AlgorithmName;
      Creator = parameters.Creator;
      Crossover = parameters.Crossover;
      Mutator = parameters.Mutator;
      TrackGenealogy = parameters.TrackGenealogy;
      TrackPopulations = parameters.TrackPopulations;
    }
  }

  public class SymRegExperimentParameters : ExperimentParameters<SymbolicExpressionTree, SymbolicExpressionTreeEncoding> {
    public double trainingSplit = 0.66;

    public SymRegExperimentParameters() { }

    public SymRegExperimentParameters(SymRegExperimentParameters parameters) : base(parameters) {
      trainingSplit = parameters.trainingSplit;
    }
  }

  public class TravelingSalesmanExperimentParameters : ExperimentParameters<Permutation, PermutationEncoding> {
    public TravelingSalesmanExperimentParameters() { }

    public TravelingSalesmanExperimentParameters(TravelingSalesmanExperimentParameters parameters) : base(parameters) { }
  }

  public class TestFunctionExperimentParameters : ExperimentParameters<RealVector, RealVectorEncoding> {
    public int dimension = 10;
    public int instance = 1;
    public int problem = 1;
    public TestFunctionExperimentParameters() { }

    public TestFunctionExperimentParameters(TestFunctionExperimentParameters parameters) : base(parameters) { }
  }
  #endregion

  #region Problem Generation
  private static readonly ConcurrentDictionary<string, ITravelingSalesmanProblemData> TSPCache = [];

  private static TravelingSalesmanProblem CreateTestTravellingSalesmanProblem(string file) {
    var cdata = TSPCache.GetOrAdd(file, s => {
      var t = TsplibTspInstanceProvider.LoadData(s);
      return t.Distances != null || (t.Coordinates?.Length ?? 0) <= 1000 ? t.ToDistanceMatrixData() : t.ToCoordinatesData();
    });
    return new TravelingSalesmanProblem(cdata);
  }

  private static readonly ConcurrentDictionary<(string, double), RegressionProblemData> SymRegCache = [];

  private static SymbolicRegressionProblem CreateTestSymbolicRegressionProblem(string file, double trainingSplit = 0.66) {
    var problemData = SymRegCache.GetOrAdd((file, trainingSplit), key => RegressionCsvInstanceProvider.ImportData(key.Item1, key.Item2));
    var problem = new SymbolicRegressionProblem(problemData, new RootMeanSquaredErrorEvaluator(), new TreeLengthEvaluator()) {
      SearchSpace = {
        TreeDepth = 40,
        TreeLength = 40
      }
    };
    var root = problem.SearchSpace.Grammar.AddLinearScaling();
    problem.SearchSpace.Grammar.AddFullyConnectedSymbols(root, new Addition(), new Subtraction(), new Multiplication(), new Division(), new Number(), new SquareRoot(), new Logarithm(), new Variable { VariableNames = problemData.InputVariables });
    return problem;
  }

  private static TestFunctionProblem CreateTestFunctionProblem(int function, int dimension, int instance)
    => new(BBoBSuite.GetProblem(function, dimension, instance));
  #endregion

  public static bool TrackAllPopulations { get; set; } = true;

  #region generic helpers
  private sealed record MyAnalyzers<T>(
    BestMedianWorstAnalysis<T> Qualities,
    RankAnalysis<T>? RankAnalysis,
    QualityCurveAnalysis<T> QualityCurve,
    AllPopulationsTracker<T>? AllPopulations) where T : class {
    public ExperimentResult<T> ToExperimentResult()
      => new(
        RankAnalysis?.Graph.ToGraphViz() ?? "",
        RankAnalysis?.Ranks ?? [],
        Qualities.BestISolutions,
        AllPopulations?.AllSolutions ?? []
      );
  }

  private static MyAnalyzers<T>
    AddAnalyzers<T, TE, TP, TRes>(Action<TRes>? callback, IAlgorithmBuilder<T, TE, TP, TRes> builder,
                                  ExperimentParameters<T, TE> parameters)
    where TRes : PopulationIterationResult<T>
    where T : class
    where TE : class, IEncoding<T>
    where TP : class, IProblem<T, TE> {
    var qualities = BestMedianWorstAnalysis.Analyze(builder);
    if (callback != null)
      FuncAnalysis.Create(builder, (_, y) => callback(y));

    var rankAnalysis = parameters.TrackGenealogy ? new RankAnalysis<T>() : null;
    rankAnalysis?.AttachTo(builder);

    var qc = QualityCurveAnalysis.Create(builder);

    var apt = parameters.TrackPopulations ? new AllPopulationsTracker<T>() : null;
    apt?.AttachTo(builder);
    return new MyAnalyzers<T>(qualities, rankAnalysis, qc, apt);
  }

  private static ExperimentResult<T> RunAlgorithmConfigurable<T, TE>(
    IProblem<T, TE> problem,
    Action<PopulationIterationResult<T>>? callback,
    ExperimentParameters<T, TE> parameters) where TE : class, IEncoding<T> where T : class {
    var terminator = new AfterIterationsTerminator<T>(parameters.Iterations);
    if (parameters.NoChildren < 0)
      parameters.NoChildren = parameters.PopulationSize;

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

        analyzers = AddAnalyzers(callback, ga, parameters);
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

        analyzers = AddAnalyzers(callback, es, parameters);
        algorithm = es.BuildAlgorithm();
        break;
      case "ls":
        var ls = LocalSearch.GetBuilder(parameters.Creator!, parameters.Mutator!);
        ls.BatchSize = ls.MaxNeighbors = parameters.NoChildren;
        ls.Terminator = terminator;
        ls.RandomSeed = parameters.Seed;

        analyzers = AddAnalyzers(callback, ls, parameters);
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
        analyzers = AddAnalyzers(callback, nsga2, parameters);
        algorithm = nsga2.BuildAlgorithm();
        break;
      default:
        throw new ArgumentException($"Algorithm '{parameters.AlgorithmName}' is not supported.");
    }

    _ = algorithm.Execute(problem, random: new SystemRandomNumberGenerator(parameters.Seed));
    return analyzers.ToExperimentResult();
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
  #endregion
}
