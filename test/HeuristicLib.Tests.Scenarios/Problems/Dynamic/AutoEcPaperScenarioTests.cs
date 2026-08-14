using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators.PermutationCreators;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators.IntegerVectorMutators;
using HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.Dynamic.Analysis;
using HEAL.HeuristicLib.Problems.Dynamic.Operators;
using HEAL.HeuristicLib.Problems.TravelingSalesman.InstanceLoading;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Tests.Scenarios.Problems.Dynamic;

public class AutoEcPaperScenarioTests
{
    [Fact]
    public async Task DynamicRacingGa_OnActivatedTsp_UsesPaperLikeScenario()
    {
        var tspFile = FindLocalFile("tsp", "eil51.tsp");
        var concordePath = FindLocalFile("for_Agent", "Concorde", "executablescygwin", "concorde.exe");
        if (tspFile is null || concordePath is null)
        {
            return;
        }

        const int targetEpochChanges = 50;
        var tspData = TsplibTspInstanceProvider.LoadData(tspFile).ToDistanceMatrixData();
        var problem = new ActivatedTravelingSalesmanProblem(
            tspData,
            RandomNumberGenerator.Create(2024),
            activationProb: 0.7,
            switchProbability: 0.25,
            UpdatePolicy.AfterEvaluation,
            epochLength: 120);
        var metaSpace = CreateTspHyperParameterSearchSpace();
        var metaCreator = metaSpace.CombineCreators(
            new UniformDistributedCreator(),
            new HEAL.HeuristicLib.Operators.Creators.IntegerVectorCreators.UniformDistributedCreator());
        var metaMutator = metaSpace.CombineMutator(
            new GaussianMutator(mutationRate: 1.0, mutationStrength: 0.1),
            new UniformOnePositionMutator());
        var evaluator = DirectEvaluator.For(problem).WithDynamicRelativeQuality(
            problem,
            new ActivatedTravelingSalesmanExactBestKnownProvider(
                new ConcordeTravelingSalesmanExactSolver(concordePath)));

        var racing = new DynamicRacingAlgorithm<Permutation, PermutationSearchSpace, ActivatedTravelingSalesmanProblem,
            PopulationState<Permutation>,
            GeneticAlgorithm<Permutation, PermutationSearchSpace, ActivatedTravelingSalesmanProblem>>(
            metaSpace,
            metaCreator,
            metaMutator,
            new BestPopulationStateMerger<Permutation>(),
            candidate => CreatePermutationGa(problem, evaluator, candidate),
            algorithm => algorithm.Evaluator)
        {
            NoRacers = 2,
            BurnInEpochs = 1,
            EarlyTerminationStrength = 0.0,
            HallOfFameStrength = 0.25,
            ModelObservationInterval = 10
        };
        var qualityCurve =
            new QualityCurvePerEpochAnalysis<Permutation, PermutationSearchSpace, ActivatedTravelingSalesmanProblem>(
                problem,
                evaluator);
        var bbcp =
            new BestBeforeChangePerformanceAnalysis<Permutation, PermutationSearchSpace,
                ActivatedTravelingSalesmanProblem>(
                problem,
                [evaluator]);

        var run = racing.CreateRun(problem, RandomNumberGenerator.Create(123))
                        .WithAnalyzers(qualityCurve, bbcp);

        var finalState = await RunUntilEpochChanges(
            run.Stream(cancellationToken: TestContext.Current.CancellationToken),
            problem, targetEpochChanges, TestContext.Current.CancellationToken);
        var qualityResult = run.GetResult(qualityCurve);
        var bbcpResult = run.GetResult(bbcp);

        finalState.Population.EvaluatedCandidates.Count.ShouldBeGreaterThan(0);
        finalState.Population.EvaluatedCandidates.ShouldAllBe(candidate =>
            problem.SearchSpace.Contains(candidate.Candidate));
        qualityResult.BestPerEpoch.Count.ShouldBeGreaterThanOrEqualTo(2);
        bbcpResult.BestBeforeChange.Count.ShouldBeGreaterThanOrEqualTo(1);
        double.IsFinite(bbcpResult.Performance).ShouldBeTrue();
    }

    [Fact]
    public async Task DynamicRacingGa_OnMovingPeaks_ProducesPaperExperimentSignals()
    {
        const int targetEpochChanges = 4;
        var problem = CreateMovingPeaksProblem();
        var metaSpace = CreateHyperParameterSearchSpace();
        var metaCreator = metaSpace.CombineCreators(
            new UniformDistributedCreator(),
            new HEAL.HeuristicLib.Operators.Creators.IntegerVectorCreators.UniformDistributedCreator());
        var metaMutator = metaSpace.CombineMutator(
            new GaussianMutator(mutationRate: 1.0, mutationStrength: 0.15),
            new UniformOnePositionMutator());
        var evaluator = DirectEvaluator.For(problem);

        var racing = new DynamicRacingAlgorithm<RealVector, RealVectorSearchSpace, MovingPeaksProblem,
            PopulationState<RealVector>, GeneticAlgorithm<RealVector, RealVectorSearchSpace, MovingPeaksProblem>>(
            metaSpace,
            metaCreator,
            metaMutator,
            new BestPopulationStateMerger<RealVector>(),
            candidate => CreateGa(problem, evaluator, candidate),
            algorithm => algorithm.Evaluator)
        {
            NoRacers = 2,
            BurnInEpochs = 1,
            EarlyTerminationStrength = 0.0,
            HallOfFameStrength = 0.25,
            ModelObservationInterval = 10
        };
        var qualityCurve =
            new QualityCurvePerEpochAnalysis<RealVector, RealVectorSearchSpace, MovingPeaksProblem>(
                problem,
                evaluator);
        var bbcp =
            new BestBeforeChangePerformanceAnalysis<RealVector, RealVectorSearchSpace, MovingPeaksProblem>(
                problem,
                [evaluator]);

        var run = racing.CreateRun(problem, RandomNumberGenerator.Create(123))
                        .WithAnalyzers(qualityCurve, bbcp);

        var finalState = await RunUntilEpochChanges(
            run.Stream(cancellationToken: TestContext.Current.CancellationToken),
            problem, targetEpochChanges, TestContext.Current.CancellationToken);
        var qualityResult = run.GetResult(qualityCurve);
        var bbcpResult = run.GetResult(bbcp);

        finalState.Population.EvaluatedCandidates.Count.ShouldBeGreaterThan(0);
        finalState.Population.EvaluatedCandidates.ShouldAllBe(candidate =>
            problem.SearchSpace.Contains(candidate.Candidate));
        qualityResult.BestPerEpoch.Count.ShouldBeGreaterThanOrEqualTo(3);
        qualityResult.BestPerEpoch.Select(entry => entry.timing.Epoch).Distinct().Count()
                     .ShouldBeGreaterThanOrEqualTo(3);
        bbcpResult.BestBeforeChange.Count.ShouldBeGreaterThanOrEqualTo(2);
        double.IsFinite(bbcpResult.Performance).ShouldBeTrue();
    }

    private static async Task<TSearchState> RunUntilEpochChanges<TCandidate, TSearchSpace, TSearchState>(
        IAsyncEnumerable<TSearchState> stream,
        DynamicProblem<TCandidate, TSearchSpace> problem,
        int epochChanges,
        CancellationToken cancellationToken)
        where TSearchSpace : class, HEAL.HeuristicLib.SearchSpaces.ISearchSpace<TCandidate>
        where TSearchState : class
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(epochChanges);

        var observedEpochChanges = 0;
        TSearchState? finalState = null;

        void OnEpochChange(object? sender, int epoch)
        {
            observedEpochChanges += 1;
        }

        problem.EpochClock.OnEpochChange += OnEpochChange;
        try
        {
            await using var enumerator = stream.GetAsyncEnumerator(cancellationToken);
            while (observedEpochChanges < epochChanges)
            {
                var hasNext = await enumerator.MoveNextAsync();
                hasNext.ShouldBeTrue();
                finalState = enumerator.Current;
            }
        }
        finally
        {
            problem.EpochClock.OnEpochChange -= OnEpochChange;
        }

        return finalState ?? throw new InvalidOperationException("The stream did not produce a state.");
    }

    private static MovingPeaksProblem CreateMovingPeaksProblem() =>
        new(new MovingPeaksParameters
        {
            Dimension = 2,
            NumberOfPeaks = 3,
            LowerBound = -5.0,
            UpperBound = 5.0,
            MinHeight = 20.0,
            MaxHeight = 80.0,
            MinWidth = 0.5,
            MaxWidth = 2.0,
            ShiftSeverity = 0.5,
            HeightSeverity = 2.0,
            WidthSeverity = 0.1
        },
            RandomNumberGenerator.Create(321),
            UpdatePolicy.AfterEvaluation,
            epochLength: 30);

    private static CompositeSearchSpace<RealVector, RealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>
        CreateHyperParameterSearchSpace() =>
        new RealVectorSearchSpace(1, new RealVector(0.05), new RealVector(0.4))
            .WithSearchSpace<RealVector, RealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>(
                new IntegerVectorSearchSpace(1, new IntegerVector(8), new IntegerVector(16)));

    private static CompositeSearchSpace<RealVector, RealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>
        CreateTspHyperParameterSearchSpace() =>
        new RealVectorSearchSpace(1, new RealVector(0.01), new RealVector(0.2))
            .WithSearchSpace<RealVector, RealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>(
                new IntegerVectorSearchSpace(1, new IntegerVector(20), new IntegerVector(60)));

    private static GeneticAlgorithm<Permutation, PermutationSearchSpace, ActivatedTravelingSalesmanProblem>
        CreatePermutationGa(
            ActivatedTravelingSalesmanProblem problem,
            IEvaluator<Permutation, PermutationSearchSpace, ActivatedTravelingSalesmanProblem> evaluator,
            CompositeGenotype<RealVector, IntegerVector> hyperParameters)
    {
        return new GeneticAlgorithm<Permutation, PermutationSearchSpace, ActivatedTravelingSalesmanProblem>
        {
            Creator = new RandomPermutationCreator(),
            Crossover = new EdgeRecombinationCrossover(),
            Mutator = new InversionMutator(),
            MutationRate = hyperParameters.Part1[0],
            Selector = GeneralizedRankSelector.For(problem, pressure: 4.0),
            PopulationSize = hyperParameters.Part2[0],
            Elites = 1,
            Evaluator = evaluator
        };
    }

    private static GeneticAlgorithm<RealVector, RealVectorSearchSpace, MovingPeaksProblem> CreateGa(
        MovingPeaksProblem problem,
        IEvaluator<RealVector, RealVectorSearchSpace, MovingPeaksProblem> evaluator,
        CompositeGenotype<RealVector, IntegerVector> hyperParameters)
    {
        return new GeneticAlgorithm<RealVector, RealVectorSearchSpace, MovingPeaksProblem>
        {
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SimulatedBinaryCrossover(),
            Mutator = new GaussianMutator(mutationRate: 1.0, mutationStrength: 0.5),
            MutationRate = hyperParameters.Part1[0],
            Selector = TournamentSelector.For(problem, tournamentSize: 2),
            PopulationSize = hyperParameters.Part2[0],
            Elites = 1,
            Evaluator = evaluator
        };
    }

    private static string? FindLocalFile(params string[] relativePathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(relativePathParts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
