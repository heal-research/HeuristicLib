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
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Mutators;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Operators.Analyzer.Genealogy;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Operators.Terminator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
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
  #region BatchRuns
  private static ExperimentResult<T>[]
    RunConfigurableRepeated<T>(int repetitions, Func<int, ExperimentResult<T>> experiment, int seed)
    where T : class {
    return Enumerable.Range(0, repetitions).ParallelSelect(
      new SystemRandomNumberGenerator(seed),
      (_, _, r) => experiment(r.Integer())
    ).ToArray();
  }

  public static ExperimentResult<SymbolicExpressionTree>[] RunSymbolicRegressionConfigurable(string file, SymRegExperimentParameters parameters, int repetitions) =>
    RunConfigurableRepeated(
      repetitions,
      seed => RunSymbolicRegressionConfigurable(file, new SymRegExperimentParameters(parameters) { Seed = seed }),
      parameters.Seed);

  public static ExperimentResult<Permutation>[] RunTravelingSalesmanConfigurable(string file, TravelingSalesmanExperimentParameters parameters, int repetitions) =>
    RunConfigurableRepeated(
      repetitions,
      seed => RunTravelingSalesmanConfigurable(file, new TravelingSalesmanExperimentParameters(parameters) { Seed = seed }),
      parameters.Seed);

  public static ExperimentResult<RealVector>[] RunTestFunctionConfigurable(string file, TestFunctionExperimentParameters parameters, int repetitions) =>
    RunConfigurableRepeated(
      repetitions,
      seed => RunTestFunctionConfigurable(file, new TestFunctionExperimentParameters(parameters) { Seed = seed }),
      parameters.Seed);
  #endregion

  public static ExperimentResult<SymbolicExpressionTree> RunSymbolicRegressionConfigurable(
    string file,
    SymRegExperimentParameters parameters,
    GenerationCallback? callback = null) {
    parameters = new SymRegExperimentParameters(parameters) {
      Creator = parameters.Creator ?? new ProbabilisticTreeCreator(),
      Crossover = parameters.Crossover ?? new SubtreeCrossover(),
      Mutator = parameters.Mutator ?? CreateSymRegAllMutator()
    };
    var problem = ProblemGeneration.CreateSymbolicRegressionProblem(file, parameters);
    var actionCallback = callback is null ? null : new Action<PopulationIterationResult<SymbolicExpressionTree>>(callback);
    return RunAlgorithmConfigurable(problem, actionCallback, parameters);
  }

  public static ExperimentResult<Permutation> RunTravelingSalesmanConfigurable(
    string file,
    TravelingSalesmanExperimentParameters parameters,
    GenerationCallback? callback = null) {
    var problem = ProblemGeneration.CreateTravellingSalesmanProblem(file);

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
      Mutator = parameters.Mutator ?? new GaussianMutator(1.0 / parameters.Dimension, 0.01)
    };
    var problem = ProblemGeneration.CreateTestFunctionProblem(parameters.Problem, parameters.Dimension, parameters.Instance);
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
    public double TrainingSplit = 0.66;
    public int TreeDepth = 40;
    public int TreeLength = 40;
    public int ParameterOptimizationIterations = 10;

    public SymRegExperimentParameters() { }

    public SymRegExperimentParameters(SymRegExperimentParameters parameters) : base(parameters) {
      TrainingSplit = parameters.TrainingSplit;
      TreeDepth = parameters.TreeDepth;
      TreeLength = parameters.TreeLength;
      ParameterOptimizationIterations = parameters.ParameterOptimizationIterations;
    }
  }

  public class TravelingSalesmanExperimentParameters : ExperimentParameters<Permutation, PermutationEncoding> {
    public TravelingSalesmanExperimentParameters() { }

    public TravelingSalesmanExperimentParameters(TravelingSalesmanExperimentParameters parameters) : base(parameters) { }
  }

  public class TestFunctionExperimentParameters : ExperimentParameters<RealVector, RealVectorEncoding> {
    public int Dimension = 10;
    public int Instance = 1;
    public int Problem = 1;
    public TestFunctionExperimentParameters() { }

    public TestFunctionExperimentParameters(TestFunctionExperimentParameters parameters) : base(parameters) { }
  }
  #endregion

  #region generic helpers
  public static ExperimentResult<T> RunAlgorithmConfigurable<T, TE>(
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

  private static MyAnalyzers<T> AddAnalyzers<T, TE, TP, TRes>(
    Action<TRes>? callback,
    IAlgorithmBuilder<T, TE, TP, TRes> builder,
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
