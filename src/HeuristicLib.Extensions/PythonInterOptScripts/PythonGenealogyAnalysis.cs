using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.GenealogyAnalysis;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators.PermutationCreators;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionTreeCreators;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionTreeCrossovers;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionTreeMutators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.SearchSpaces.Trees;
using HEAL.HeuristicLib.States;

#pragma warning disable S1104
#pragma warning disable S1104

namespace HEAL.HeuristicLib.PythonInterOptScripts;

public class PythonGenealogyAnalysis
{
  public delegate void GenerationCallback(object current);

  #region public methods
  #region BatchRuns
  private static ExperimentResult<T>[]
    RunConfigurableRepeated<T>(int repetitions, Func<int, ExperimentResult<T>> experiment, int seed)
  {
    return BatchExecution.Parallel<ExperimentResult<T>>(repetitions, r => experiment(r.NextInt()), RandomNumberGenerator.Create(seed), maxDegreeOfParallelism: -1)
                         .ToArray();
  }

  public static ExperimentResult<SymbolicExpressionTree>[] RunSymbolicRegressionConfigurable(string file, SymRegExperimentParameters parameters, int repetitions) =>
    RunConfigurableRepeated(
      repetitions,
      experiment: seed => RunSymbolicRegressionConfigurable(file, new SymRegExperimentParameters(parameters) { Seed = seed }),
      parameters.Seed);

  public static ExperimentResult<Permutation>[] RunTravelingSalesmanConfigurable(string file, TravelingSalesmanExperimentParameters parameters, int repetitions) =>
    RunConfigurableRepeated(
      repetitions,
      experiment: seed => RunTravelingSalesmanConfigurable(file, new TravelingSalesmanExperimentParameters(parameters) { Seed = seed }),
      parameters.Seed);

  public static ExperimentResult<RealVector>[] RunTestFunctionConfigurable(string file, TestFunctionExperimentParameters parameters, int repetitions) =>
    RunConfigurableRepeated(
      repetitions,
      experiment: seed => RunTestFunctionConfigurable(file, new TestFunctionExperimentParameters(parameters) { Seed = seed }),
      parameters.Seed);
  #endregion

  public static ExperimentResult<SymbolicExpressionTree> RunSymbolicRegressionConfigurable(
    string file,
    SymRegExperimentParameters parameters,
    GenerationCallback? callback = null)
  {
    parameters = new SymRegExperimentParameters(parameters) {
      Creator = parameters.Creator ?? new ProbabilisticTreeCreator(),
      Crossover = parameters.Crossover ?? new SubtreeCrossover(),
      Mutator = parameters.Mutator ?? CreateSymRegAllMutator()
    };
    var problem = ProblemGeneration.CreateSymbolicRegressionProblem(file, parameters);
    var actionCallback = callback is null ? null : new Action<PopulationState<SymbolicExpressionTree>>(callback);

    return RunAlgorithmConfigurable(problem, actionCallback, parameters);
  }

  public static ExperimentResult<Permutation> RunTravelingSalesmanConfigurable(
    string file,
    TravelingSalesmanExperimentParameters parameters,
    GenerationCallback? callback = null)
  {
    var problem = ProblemGeneration.CreateTravellingSalesmanProblem(file);

    parameters = new TravelingSalesmanExperimentParameters(parameters) {
      Creator = parameters.Creator ?? new RandomPermutationCreator(),
      Crossover = parameters.Crossover ?? new EdgeRecombinationCrossover(),
      Mutator = parameters.Mutator ?? new InversionMutator()
    };
    var actionCallback = callback is null ? null : new Action<PopulationState<Permutation>>(callback);

    return RunAlgorithmConfigurable(problem, actionCallback, parameters);
  }

  public static ExperimentResult<RealVector> RunTestFunctionConfigurable(
    string file,
    TestFunctionExperimentParameters parameters,
    GenerationCallback? callback = null)
  {
    parameters = new TestFunctionExperimentParameters(parameters) {
      Creator = parameters.Creator ?? new UniformDistributedCreator(),
      Crossover = parameters.Crossover ?? new SimulatedBinaryCrossover(),
      Mutator = parameters.Mutator ?? new GaussianMutator(1.0 / parameters.Dimension, 0.01)
    };
    var problem = ProblemGeneration.CreateTestFunctionProblem(parameters.Problem, parameters.Dimension, parameters.Instance);
    var actionCallback = callback is null ? null : new Action<PopulationState<RealVector>>(callback);

    return RunAlgorithmConfigurable(problem, actionCallback, parameters);
  }
  #endregion

  #region generic helpers
  public static ExperimentResult<T> RunAlgorithmConfigurable<T, TE>(
    IProblem<T, TE> problem,
    Action<PopulationState<T>>? callback,
    ExperimentParameters<T, TE> parameters) where T : notnull where TE : class, ISearchSpace<T>
  {
    //var terminator = new AfterIterationsTerminator<T>(parameters.Iterations);
    if (parameters.NoChildren < 0) {
      parameters.NoChildren = parameters.PopulationSize;
    }

    switch (parameters.AlgorithmName.ToLower()) {
      case "ga": {
        var ga = GeneticAlgorithm.GetBuilder(parameters.Creator!, parameters.Crossover!, parameters.Mutator!);
        ga.PopulationSize = parameters.PopulationSize;
        ga.MutationRate = parameters.MutationRate;
        ga.Elites = parameters.Elites;
        if (parameters.Selector != null) {
          ga.Selector = parameters.Selector;
        }

        var gaAlgorithm = ga.Build();
        if (callback is not null && gaAlgorithm.Interceptor is null) {
          gaAlgorithm = gaAlgorithm with {
            Interceptor = new IdentityInterceptor<T, PopulationState<T>>()
          };
        }

        var analyzers = CreateAnalyzers(parameters, gaAlgorithm, gaAlgorithm.Crossover, gaAlgorithm.Mutator, callback);
        var gaRun = gaAlgorithm.WithMaxIterations(parameters.Iterations).CreateRun(problem, analyzers.GetAll());
        gaRun.RunToCompletion(RandomNumberGenerator.Create(parameters.Seed));
        return analyzers.ToExperimentResult(gaRun);
      }
      case "es": {
        var es = EvolutionStrategy.GetBuilder(parameters.Creator!, parameters.Mutator!);
        es.PopulationSize = parameters.PopulationSize;
        es.NumberOfChildren = parameters.NoChildren;
        es.Strategy = parameters.Strategy;
        //es.Terminator = terminator;
        if (parameters.Selector != null) {
          es.Selector = parameters.Selector;
        }

        if (parameters.WithCrossover) {
          es.Crossover = parameters.Crossover;
        }

        var esAlgorithm = es.Build();
        if (callback is not null && esAlgorithm.Interceptor is null) {
          esAlgorithm = esAlgorithm with {
            Interceptor = new IdentityInterceptor<T, EvolutionStrategyState<T>>()
          };
        }

        var analyzers = CreateAnalyzers(parameters, esAlgorithm, esAlgorithm.Crossover, esAlgorithm.Mutator, callback);

        var esRun = esAlgorithm.WithMaxIterations(parameters.Iterations).CreateRun(problem, analyzers.GetAll());
        esRun.RunToCompletion(RandomNumberGenerator.Create(parameters.Seed));
        return analyzers.ToExperimentResult(esRun);
      }
      case "ls":
        var ls = HillClimber.GetBuilder(parameters.Creator!, parameters.Mutator!);
        ls.BatchSize = ls.MaxNeighbors = parameters.NoChildren;
        //ls.Terminator = terminator;

        var lsRun = ls.Build().WithMaxIterations(parameters.Iterations).CreateRun(problem);
        lsRun.RunToCompletion(RandomNumberGenerator.Create(parameters.Seed));
        throw new NotSupportedException("Configured experiment result extraction is not implemented for local search in this analyzer pipeline.");
      case "nsga2": {
        var nsga2 = NSGA2.GetBuilder(parameters.Creator!, parameters.Crossover!, parameters.Mutator!);
        nsga2.PopulationSize = parameters.PopulationSize;
        nsga2.MutationRate = parameters.MutationRate;
        if (parameters.Selector != null) {
          nsga2.Selector = parameters.Selector;
        }

        //nsga2.Terminator = terminator;
        var nsga2Algorithm = nsga2.Build();
        if (callback is not null && nsga2Algorithm.Interceptor is null) {
          nsga2Algorithm = nsga2Algorithm with {
            Interceptor = new IdentityInterceptor<T, PopulationState<T>>()
          };
        }

        var analyzers = CreateAnalyzers(parameters, nsga2Algorithm, nsga2Algorithm.Crossover, nsga2Algorithm.Mutator, callback);
        var nsga2Run = nsga2Algorithm.WithMaxIterations(parameters.Iterations).CreateRun(problem, analyzers.GetAll());
        _ = nsga2Run.RunToCompletion(RandomNumberGenerator.Create(parameters.Seed));
        return analyzers.ToExperimentResult(nsga2Run);
      }
      default:
        throw new ArgumentException($"Algorithm '{parameters.AlgorithmName}' is not supported.");
    }
  }

  private interface IAnalyzerSet<T>
    where T : notnull
  {
    ExperimentResult<T> ToExperimentResult(Run run);
    IReadOnlyList<IAnalyzer> GetAll();
  }

  private sealed record MyAnalyzers<T>(
    Analyzer<List<BestMedianWorstEntry<T>>> Qualities,
    Analyzer<RankState<T>>? RankAnalysis,
    Analyzer<QualityCurve<T>> QualityCurve,
    Analyzer<List<ISolution<T>[]>>? AllPopulations,
    Analyzer<object>? CallbackAnalyzer)
    : IAnalyzerSet<T>
    where T : notnull
  {
    public ExperimentResult<T> ToExperimentResult(Run run)
    {
      var qRes = run.GetAnalyzerResult(Qualities);

      var rankGraph = string.Empty;
      IReadOnlyList<List<double>> rankLines = [];

      if (RankAnalysis is not null) {
        var rankResult = run.GetAnalyzerResult(RankAnalysis).Result;
        rankGraph = rankResult.Graph.ToGraphViz();
        rankLines = rankResult.Ranks.Select(x => x.ToList()).ToArray();
      }

      IReadOnlyList<ISolution<T>[]> apRes = [];
      if (AllPopulations is not null && run.TryGetAnalyzerResult(AllPopulations, out var populations) && populations is not null) {
        apRes = populations;
      }

      return new ExperimentResult<T>(rankGraph, rankLines, qRes, apRes);
    }

    public IReadOnlyList<IAnalyzer> GetAll()
    {
      var analyzers = new List<IAnalyzer> { Qualities, QualityCurve };
      if (RankAnalysis is not null) {
        analyzers.Add(RankAnalysis);
      }

      if (AllPopulations is not null) {
        analyzers.Add(AllPopulations);
      }

      if (CallbackAnalyzer is not null)
        analyzers.Add(CallbackAnalyzer);
      return analyzers;
    }
  }

  private sealed record CallbackAnalysis<T, TS, TP, TR>(
    IAlgorithm<T, TS, TP, TR> Algorithm,
    IInterceptor<T, TS, TP, TR> Interceptor,
    Action<PopulationState<T>> Callback)
    : Analyzer<T, TS, TP, TR, object>(Algorithm)
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
    where TR : PopulationState<T>, ISearchState
  {
    public override object CreateInitialResult() => new();

    public override void RegisterObservations(ObservationPlan observations, object result)
    {
      observations.Observe(Interceptor, (algorithmState, _, _, _, _) => Callback(algorithmState));
    }
  }

  private static MyAnalyzers<T> CreateAnalyzers<T, TE, TP, TR>(
    ExperimentParameters<T, TE> parameters,
    IIterativeAlgorithm<T, TE, TP, TR> algorithm,
    ICrossover<T, TE, TP>? crossover,
    IMutator<T, TE, TP>? mutator,
    Action<PopulationState<T>>? callback)
    where T : notnull
    where TE : class, ISearchSpace<T>
    where TP : class, IProblem<T, TE>
    where TR : PopulationState<T>
  {
    var interceptor = algorithm.Interceptor ?? throw new InvalidOperationException("Population-based analysis requires an interceptor.");
    var qualities = new BestMedianWorstAnalysis<T, TE, TP, TR>(algorithm, interceptor);
    var rankAnalysis = parameters.TrackGenealogy ? new RankAnalysis<T, TE, TP, TR>(algorithm, crossover, mutator, interceptor) : null;
    var qc = new QualityCurveAnalysis<T, TE, TP, TR>(algorithm, algorithm.Evaluator);
    var apt = parameters.TrackPopulations ? new AllPopulationsTracker<T, TE, TP, TR>(algorithm, interceptor) : null;
    var c = callback != null ? new CallbackAnalysis<T, TE, TP, TR>(algorithm, interceptor, callback) : null;
    return new MyAnalyzers<T>(qualities, rankAnalysis, qc, apt, c);
  }

  private static ChooseOneMutator<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>> CreateSymRegAllMutator()
  {
    return ChooseOneMutator.Create(
      new ChangeNodeTypeManipulation(),
      new FullTreeShaker(),
      new OnePointShaker(),
      new RemoveBranchManipulation(),
      new ReplaceBranchManipulation());
  }
  #endregion
}
