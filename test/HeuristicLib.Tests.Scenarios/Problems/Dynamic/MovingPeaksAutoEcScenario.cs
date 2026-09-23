using HEAL.HeuristicLib.Encodings.Composite;
using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using MetaOptimizationGenotype =
    HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<HEAL.HeuristicLib.Encodings.RealVectors.RealVector,
        HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector>;
using MetaOptimizationSearchSpace =
    HEAL.HeuristicLib.Encodings.Composite.CompositeSearchSpace<HEAL.HeuristicLib.Encodings.RealVectors.RealVector,
        HEAL.HeuristicLib.Encodings.RealVectors.BoundedRealVectorSearchSpace,
        HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector,
        HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVectorSearchSpace>;
using RealVectorUniformDistributedCreator =
    HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Scenarios.Problems.Dynamic;

public partial class AutoEcPaperScenarioTests
{
    private sealed class MovingPeaksAutoEcScenario(
        AutoEcChildAlgorithmKind childAlgorithmKind = AutoEcChildAlgorithmKind.GeneticAlgorithm,
        int problemSeed = 321,
        int runSeed = 123,
        int targetEpochChanges = MatrixScaffoldTargetEpochChanges,
        int noRacers = 2,
        int epochLength = MatrixScaffoldEpochLength,
        string instanceKey = "scenario2-canonical",
        MovingPeaksParameters? parameters = null)
        : AutoEcPaperScenario<RealVector, BoundedRealVectorSearchSpace, MovingPeaksProblem>(childAlgorithmKind)
    {
        private readonly MovingPeaksParameters movingPeaksParameters = parameters ?? new MovingPeaksParameters(
            Dimension: 2,
            NumberOfPeaks: 3,
            LowerBound: -5.0,
            UpperBound: 5.0,
            MinHeight: 20.0,
            MaxHeight: 80.0,
            MinWidth: 0.5,
            MaxWidth: 2.0,
            ShiftSeverity: 0.5,
            HeightSeverity: 2.0,
            WidthSeverity: 0.1);

        public override string ScenarioName =>
            $"moving-peaks-{instanceKey}-{FormatAlgorithmKind(ChildAlgorithmKind)}-auto-ec-smoke";
        public override int ProblemSeed => problemSeed;
        public override int RunSeed => runSeed;
        public override int TargetEpochChanges => targetEpochChanges;
        public override int NoRacers => noRacers;
        protected override int EpochLength => epochLength;
        public override IReadOnlyList<AutoEcParameterRange> HyperParameterRanges =>
        [
            new("mutationRate", "real", 0.05, 0.4),
            new("populationSize", "integer", 8, 16)
        ];

        public override MovingPeaksProblem CreateProblem() =>
            new(movingPeaksParameters,
                RandomNumberGenerator.Create(ProblemSeed),
                epochSchedule: new EvaluationCountSchedule(EpochLength), updatePolicy: UpdatePolicy.AfterEachBatchEvaluation);

        public override IEvaluator<RealVector> CreateEvaluator(
            MovingPeaksProblem problem) =>
            new ProblemEvaluator<RealVector, BoundedRealVectorSearchSpace, MovingPeaksProblem>().ScaledToDynamicBestKnown(
                problem,
                new FuncBestKnownObjectiveProvider<RealVector, BoundedRealVectorSearchSpace, MovingPeaksProblem>(static currentProblem => new ObjectiveVector(currentProblem.Peaks().Max(peak => peak.Height))));


        public override AutoEcProblemDescription CreateProblemDescription() =>
            new("MovingPeaks", instanceKey,
                new Dictionary<string, string>
                {
                    [nameof(MovingPeaksParameters.Dimension)] = movingPeaksParameters.Dimension.ToString(),
                    [nameof(MovingPeaksParameters.NumberOfPeaks)] = movingPeaksParameters.NumberOfPeaks.ToString(),
                    [nameof(MovingPeaksParameters.LowerBound)] = movingPeaksParameters.LowerBound.ToString("R"),
                    [nameof(MovingPeaksParameters.UpperBound)] = movingPeaksParameters.UpperBound.ToString("R"),
                    [nameof(MovingPeaksParameters.MinHeight)] = movingPeaksParameters.MinHeight.ToString("R"),
                    [nameof(MovingPeaksParameters.MaxHeight)] = movingPeaksParameters.MaxHeight.ToString("R"),
                    [nameof(MovingPeaksParameters.MinWidth)] = movingPeaksParameters.MinWidth.ToString("R"),
                    [nameof(MovingPeaksParameters.MaxWidth)] = movingPeaksParameters.MaxWidth.ToString("R"),
                    [nameof(MovingPeaksParameters.ShiftSeverity)] =
                        movingPeaksParameters.ShiftSeverity.ToString("R"),
                    [nameof(MovingPeaksParameters.HeightSeverity)] =
                        movingPeaksParameters.HeightSeverity.ToString("R"),
                    [nameof(MovingPeaksParameters.WidthSeverity)] =
                        movingPeaksParameters.WidthSeverity.ToString("R"),
                    ["UpdatePolicy"] = UpdatePolicy.AfterEachBatchEvaluation.ToString(),
                    ["EpochLength"] = EpochLength.ToString(),
                    ["QualityScale"] = "RelativeToCurrentHighestPeak",
                    ["BestKnownProvider"] = "CurrentHighestPeakHeight"
                });

        public override AutoEcBaseAlgorithmDescription CreateBaseAlgorithmDescription() =>
            new(FormatAlgorithmKind(ChildAlgorithmKind),
                new Dictionary<string, string>
                {
                    ["Encoding"] = "RealVector",
                    ["Creator"] = nameof(RealVectorUniformDistributedCreator),
                    ["Crossover"] = nameof(SimulatedBinaryCrossover),
                    ["Mutator"] = nameof(GaussianMutator),
                    ["MutationStrength"] = "0.5",
                    ["Selector"] = "TournamentSelector",
                    ["TournamentSize"] = "2",
                    ["Elites"] = "1",
                    ["HypermutationRate"] = HypermutationRate.ToString("R"),
                    ["HypermutationDuration"] = HypermutationDuration.ToString(),
                    ["ChangeTriggeredImmigrantRate"] = ChangeTriggeredImmigrantRate.ToString("R")
                });

        protected override MetaOptimizationSearchSpace CreateHyperParameterSearchSpace() =>
            new BoundedRealVectorSearchSpace(1, new RealVector(0.05), new RealVector(0.4))
                .CombinedWith<RealVector, BoundedRealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>(
                    new IntegerVectorSearchSpace(1, new IntegerVector(8), new IntegerVector(16)));

        protected override IAlgorithm<RealVector, PopulationState<RealVector>>
            CreateAlgorithm(
                MovingPeaksProblem problem,
                IEvaluator<RealVector> evaluator,
                MetaOptimizationGenotype hyperParameters)
        {
            var creator = new RealVectorUniformDistributedCreator(problem.SearchSpace);
            var crossover = new SimulatedBinaryCrossover();
            var mutator = new GaussianMutator(mutationRate: 1.0, mutationStrength: 0.5);
            var selector = TournamentSelector.For(problem, tournamentSize: 2);
            var populationSize = hyperParameters.Part2[0];
            var mutationRate = hyperParameters.Part1[0];
            IInterceptor<RealVector>? interceptor = null;

            return ChildAlgorithmKind switch
            {
                AutoEcChildAlgorithmKind.HypermutationGeneticAlgorithm => HypermutationGeneticAlgorithm.Create<RealVector, BoundedRealVectorSearchSpace, MovingPeaksProblem>(
                    creator, crossover, mutator, mutationRate, HypermutationRate, HypermutationDuration,
                    selector, populationSize, evaluator, interceptor: interceptor),
                AutoEcChildAlgorithmKind.ChangeTriggeredRandomImmigrantGeneticAlgorithm =>
                    ChangeTriggeredRandomImmigrantGeneticAlgorithm.Create<RealVector, BoundedRealVectorSearchSpace, MovingPeaksProblem>(
                        creator, crossover, mutator, mutationRate, ChangeTriggeredImmigrantRate,
                        selector, populationSize, evaluator, interceptor: interceptor),
                _ => GeneticAlgorithm.Create(creator, crossover, mutator, selector, evaluator,
                    populationSize: populationSize, mutationRate: mutationRate, interceptor: interceptor)
            };
        }
    }
}
