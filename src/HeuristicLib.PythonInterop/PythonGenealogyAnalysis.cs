using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Analysis.GenealogyAnalysis;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Encodings.SymbolicExpressions;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.MachineLearning;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.PythonInterop;

public class PythonGenealogyAnalysis
{
    public delegate void GenerationCallback(object current);

    #region public methods
    #region BatchRuns
    private static ExperimentResult<TCandidate>[]
        RunConfigurableRepeated<TCandidate>(int repetitions, Func<int, ExperimentResult<TCandidate>> experiment,
                                            int seed)
    {
        return BatchExecution.Parallel<ExperimentResult<TCandidate>>(repetitions, r => experiment(r.NextInt()), RandomNumberGenerator.Create(seed))
                             .ToArray();
    }

    public static ExperimentResult<ExpressionTree>[] RunSymbolicRegressionConfigurable(
        string file, SymRegExperimentParameters parameters, int repetitions) =>
        RunConfigurableRepeated(
            repetitions,
            experiment: seed =>
                RunSymbolicRegressionConfigurable(file, new SymRegExperimentParameters(parameters) { Seed = seed }),
            parameters.Seed);

    public static ExperimentResult<Permutation>[] RunTravelingSalesmanConfigurable(
        string file, TravelingSalesmanExperimentParameters parameters, int repetitions) =>
        RunConfigurableRepeated(
            repetitions,
            experiment: seed =>
                RunTravelingSalesmanConfigurable(file,
                    new TravelingSalesmanExperimentParameters(parameters) { Seed = seed }),
            parameters.Seed);

    public static ExperimentResult<RealVector>[] RunTestFunctionConfigurable(
        string file, TestFunctionExperimentParameters parameters, int repetitions) =>
        RunConfigurableRepeated(
            repetitions,
            experiment: seed =>
                RunTestFunctionConfigurable(file, new TestFunctionExperimentParameters(parameters) { Seed = seed }),
            parameters.Seed);
    #endregion

    public static ExperimentResult<ExpressionTree> RunSymbolicRegressionConfigurable(
        string file,
        SymRegExperimentParameters parameters,
        GenerationCallback? callback = null)
    {
        parameters = new SymRegExperimentParameters(parameters)
        {
            Creator = parameters.Creator ?? new ProbabilisticTreeCreator(),
            Crossover = parameters.Crossover ?? new SubtreeCrossover { InternalNodeProbability = 0.9 },
            Mutator = parameters.Mutator ??
                new ChooseOneMutator<ExpressionTree, ExpressionTreeSearchSpace,
                    IProblem<ExpressionTree, ExpressionTreeSearchSpace>>([.. SymbolicExpressionMutators.Default])
        };
        var problem = ProblemGeneration.CreateSymbolicRegressionProblem(file, parameters);
        var actionCallback = callback is null ? null : new Action<PopulationState<ExpressionTree>>(callback);
        var parameterFitting = new NumericParameterFittingRefiner
        {
            MaximumIterations = parameters.ParameterOptimizationIterations
        };

        return RunAlgorithmConfigurable(problem, actionCallback, parameters, parameterFitting);
    }

    public static ExperimentResult<Permutation> RunTravelingSalesmanConfigurable(
        string file,
        TravelingSalesmanExperimentParameters parameters,
        GenerationCallback? callback = null)
    {
        var problem = ProblemGeneration.CreateTravellingSalesmanProblem(file);

        parameters = new TravelingSalesmanExperimentParameters(parameters)
        {
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
        parameters = new TestFunctionExperimentParameters(parameters)
        {
            Creator = parameters.Creator ?? new UniformDistributedCreator(),
            Crossover = parameters.Crossover ?? new SimulatedBinaryCrossover(),
            Mutator = parameters.Mutator ?? new GaussianMutator(1.0 / parameters.Dimension, 0.01)
        };
        var problem =
            ProblemGeneration.CreateTestFunctionProblem(parameters.Problem, parameters.Dimension, parameters.Instance);
        var actionCallback = callback is null ? null : new Action<PopulationState<RealVector>>(callback);

        return RunAlgorithmConfigurable(problem, actionCallback, parameters);
    }
    #endregion

    #region generic helpers
    public static ExperimentResult<TCandidate> RunAlgorithmConfigurable<TCandidate, TSearchSpace, TProblem>(
        TProblem problem,
        Action<PopulationState<TCandidate>>? callback,
        ExperimentParameters<TCandidate, TSearchSpace> parameters,
        IRefiner<TCandidate, TSearchSpace, TProblem>? refiner = null) where TCandidate : notnull
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        //var terminator = new AfterIterationsTerminator<TCandidate>(parameters.Iterations);
        if (parameters.NoChildren < 0)
        {
            parameters.NoChildren = parameters.PopulationSize;
        }

        switch (parameters.AlgorithmName.ToLower())
        {
            case "ga":
                {
                    var gaAlgorithm = GeneticAlgorithm.Create(
                        RequireCreator(parameters),
                        RequireCrossover(parameters),
                        RequireMutator(parameters),
                        selector: parameters.Selector,
                        refiner: refiner,
                        populationSize: parameters.PopulationSize,
                        mutationRate: parameters.MutationRate,
                        elites: parameters.Elites);
                    var analyzers = CreateAnalyzers(parameters, gaAlgorithm, gaAlgorithm.Evaluator, gaAlgorithm.Crossover, gaAlgorithm.Mutator, callback);
                    var gaRun = gaAlgorithm.WithMaxIterations(parameters.Iterations)
                                           .CreateRun(problem, RandomNumberGenerator.Create(parameters.Seed), analyzers.GetAll());
                    gaRun.Complete();
                    return analyzers.ToExperimentResult(gaRun);
                }
            case "es":
                {
                    var esAlgorithm = EvolutionStrategy.Create(
                        RequireCreator(parameters),
                        RequireMutator(parameters),
                        crossover: parameters.WithCrossover ? parameters.Crossover : null,
                        selector: parameters.Selector,
                        refiner: refiner,
                        populationSize: parameters.PopulationSize,
                        numberOfChildren: parameters.NoChildren,
                        strategy: parameters.Strategy);
                    var analyzers = CreateAnalyzers(parameters, esAlgorithm, esAlgorithm.Evaluator, esAlgorithm.Crossover, esAlgorithm.Mutator, callback);

                    var esRun = esAlgorithm.WithMaxIterations(parameters.Iterations)
                                           .CreateRun(problem, RandomNumberGenerator.Create(parameters.Seed), analyzers.GetAll());
                    esRun.Complete();
                    return analyzers.ToExperimentResult(esRun);
                }
            case "ls":
                var lsAlgorithm = HillClimber.Create(
                    RequireCreator(parameters),
                    RequireMutator(parameters),
                    refiner: refiner,
                    maxNeighbors: parameters.NoChildren,
                    batchSize: parameters.NoChildren);

                var lsRun = lsAlgorithm.WithMaxIterations(parameters.Iterations).CreateRun(problem, RandomNumberGenerator.Create(parameters.Seed));
                lsRun.Complete();
                throw new NotSupportedException(
                    "Configured experiment result extraction is not implemented for local search in this analyzer pipeline.");
            case "nsga2":
                {
                    var nsga2Algorithm = NSGA2.Create(
                        RequireCreator(parameters),
                        RequireCrossover(parameters),
                        RequireMutator(parameters),
                        selector: parameters.Selector,
                        refiner: refiner,
                        populationSize: parameters.PopulationSize,
                        mutationRate: parameters.MutationRate);
                    var analyzers = CreateAnalyzers(parameters, nsga2Algorithm, nsga2Algorithm.Evaluator, nsga2Algorithm.Crossover, nsga2Algorithm.Mutator, callback);
                    var nsga2Run = nsga2Algorithm.WithMaxIterations(parameters.Iterations)
                                                 .CreateRun(problem, RandomNumberGenerator.Create(parameters.Seed), analyzers.GetAll());
                    _ = nsga2Run.Complete();
                    return analyzers.ToExperimentResult(nsga2Run);
                }
            default:
                throw new ArgumentException($"Algorithm '{parameters.AlgorithmName}' is not supported.");
        }
    }

    private interface IAnalyzerSet<TCandidate>
        where TCandidate : notnull
    {
        ExperimentResult<TCandidate> ToExperimentResult(AlgorithmRun run);
        IReadOnlyList<IAnalyzer> GetAll();
    }

    private sealed record MyAnalyzers<TCandidate>(
        TraceAnalyzer<BestMedianWorstEntry<TCandidate>> Qualities,
        IRankAnalysis<TCandidate>? RankAnalysis,
        TraceAnalyzer<EvaluatedCandidate<TCandidate>> QualityCurve,
        TraceAnalyzer<IReadOnlyList<EvaluatedCandidate<TCandidate>>>? AllPopulations,
        IAnalyzer? CallbackAnalyzer)
        : IAnalyzerSet<TCandidate>
        where TCandidate : notnull
    {
        public ExperimentResult<TCandidate> ToExperimentResult(AlgorithmRun run)
        {
            var qRes = Qualities.Snapshot().Select(entry => entry.Value).ToList();

            var rankGraph = string.Empty;
            IReadOnlyList<List<double>> rankLines = [];

            if (RankAnalysis is not null)
            {
                var rankResult = RankAnalysis.State.Result();
                rankGraph = rankResult.Graph.ToGraphViz();
                rankLines = rankResult.Ranks.Select(x => x.ToList()).ToArray();
            }

            IReadOnlyList<EvaluatedCandidate<TCandidate>[]> apRes =
                AllPopulations is null ? [] : [.. AllPopulations.Snapshot().Select(entry => entry.Value.ToArray())];

            return new ExperimentResult<TCandidate>(rankGraph, rankLines, qRes, apRes);
        }

        public IReadOnlyList<IAnalyzer> GetAll()
        {
            var analyzers = new List<IAnalyzer>
            {
                Qualities,
                QualityCurve
            };
            if (RankAnalysis is not null)
            {
                analyzers.Add(RankAnalysis);
            }

            if (AllPopulations is not null)
            {
                analyzers.Add(AllPopulations);
            }

            if (CallbackAnalyzer is not null)
                analyzers.Add(CallbackAnalyzer);
            return analyzers;
        }
    }

    /// <summary>
    /// Reads an operator that the requested algorithm cannot run without, failing at the interop boundary with a
    /// message naming the missing setting.
    /// </summary>
    /// <remarks>
    /// The parameters arrive from Python, where every operator is optional and unset, so this is the boundary that has
    /// to establish the contract the rest of the library relies on.
    /// </remarks>
    private static ICreator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> RequireCreator<TCandidate, TSearchSpace>(
        ExperimentParameters<TCandidate, TSearchSpace> parameters)
        where TSearchSpace : class, ISearchSpace<TCandidate> =>
        parameters.Creator ?? throw MissingOperator(parameters.AlgorithmName, nameof(parameters.Creator));

    /// <inheritdoc cref="RequireCreator{TCandidate, TSearchSpace}"/>
    private static ICrossover<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> RequireCrossover<TCandidate, TSearchSpace>(
        ExperimentParameters<TCandidate, TSearchSpace> parameters)
        where TSearchSpace : class, ISearchSpace<TCandidate> =>
        parameters.Crossover ?? throw MissingOperator(parameters.AlgorithmName, nameof(parameters.Crossover));

    /// <inheritdoc cref="RequireCreator{TCandidate, TSearchSpace}"/>
    private static IMutator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> RequireMutator<TCandidate, TSearchSpace>(
        ExperimentParameters<TCandidate, TSearchSpace> parameters)
        where TSearchSpace : class, ISearchSpace<TCandidate> =>
        parameters.Mutator ?? throw MissingOperator(parameters.AlgorithmName, nameof(parameters.Mutator));

    private static ArgumentException MissingOperator(string algorithmName, string operatorName) =>
        new($"Algorithm '{algorithmName}' requires '{operatorName}' to be set on the experiment parameters.");

    /// <summary>
    /// Forwards every intercepted population to a Python callback. It collects nothing of its own.
    /// </summary>
    private sealed class CallbackAnalysis<TCandidate, TSearchSpace, TProblem, TSearchState>(
        IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm,
        Action<PopulationState<TCandidate>> callback)
        : IAnalyzer, IObservationRecorder<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate>, ISearchState
    {
        public void Install(ExecutionInstanceResolverBuilder builder) => builder.Observe(algorithm, this);

        public void Record(AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState> observation) =>
            callback(observation.State);
    }

    private static MyAnalyzers<TCandidate> CreateAnalyzers<TCandidate, TSearchSpace, TProblem, TSearchState>(
        ExperimentParameters<TCandidate, TSearchSpace> parameters,
        IIterativeAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm,
        IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
        ICrossover<TCandidate, TSearchSpace, TProblem>? crossover,
        IMutator<TCandidate, TSearchSpace, TProblem>? mutator,
        Action<PopulationState<TCandidate>>? callback)
        where TCandidate : notnull
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate>
    {
        var qualities = Analyzer.TraceBestMedianWorst(algorithm);
        var rankAnalysis = parameters.TrackGenealogy
            ? Analyzer.Rank(crossover, mutator, algorithm)
            : null;
        var qc = Analyzer.TraceBestQuality(evaluator);
        var apt = parameters.TrackPopulations ? Analyzer.TraceAllPopulations(algorithm) : null;
        var c = callback != null
            ? new CallbackAnalysis<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, callback)
            : null;
        return new MyAnalyzers<TCandidate>(qualities, rankAnalysis, qc, apt, c);
    }

    #endregion
}
