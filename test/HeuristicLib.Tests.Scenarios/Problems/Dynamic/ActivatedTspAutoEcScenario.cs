using HEAL.HeuristicLib.Encodings.Composite;
using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using MetaOptimizationGenotype =
    HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<HEAL.HeuristicLib.Encodings.RealVectors.RealVector,
        HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector>;
using MetaOptimizationSearchSpace =
    HEAL.HeuristicLib.Encodings.Composite.CompositeSearchSpace<HEAL.HeuristicLib.Encodings.RealVectors.RealVector,
        HEAL.HeuristicLib.Encodings.RealVectors.BoundedRealVectorSearchSpace,
        HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector,
        HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVectorSearchSpace>;

namespace HEAL.HeuristicLib.Tests.Scenarios.Problems.Dynamic;

public partial class AutoEcPaperScenarioTests
{
    private sealed class ActivatedTspAutoEcScenario(
        string tspFile,
        string? concordePath,
        int problemSeed = 2024,
        int runSeed = 123,
        int targetEpochChanges = 50,
        string? scenarioName = null,
        int noRacers = 2,
        int epochLength = 120,
        AutoEcChildAlgorithmKind childAlgorithmKind = AutoEcChildAlgorithmKind.GeneticAlgorithm)
        : AutoEcPaperScenario<Permutation, PermutationSearchSpace, ActivatedTravelingSalesmanProblem>(
            childAlgorithmKind)
    {
        private const double ActivationProbability = 0.7;
        private const double SwitchProbability = 0.25;

        public override string ScenarioName =>
            scenarioName ?? $"activated-tsp-{FormatAlgorithmKind(ChildAlgorithmKind)}-auto-ec-smoke";
        public override int ProblemSeed => problemSeed;
        public override int RunSeed => runSeed;
        public override int TargetEpochChanges => targetEpochChanges;
        public override int NoRacers => noRacers;
        public override double MetaMutationStrength => 0.1;
        public override IReadOnlyList<AutoEcParameterRange> HyperParameterRanges =>
        [
            new("mutationRate", "real", 0.01, 0.2),
            new("populationSize", "integer", 20, 60)
        ];

        public override ActivatedTravelingSalesmanProblem CreateProblem()
        {
            var tspData = TsplibTspInstanceProvider.LoadData(tspFile).ToDistanceMatrixData();
            return new ActivatedTravelingSalesmanProblem(
                tspData,
                RandomNumberGenerator.Create(ProblemSeed),
                activationProb: ActivationProbability,
                switchProbability: SwitchProbability,
                epochSchedule: new EvaluationCountSchedule(epochLength), updatePolicy: UpdatePolicy.AfterEachBatchEvaluation);
        }

        public override AutoEcProblemDescription CreateProblemDescription() =>
            new("ActivatedTravelingSalesman", Path.GetFileName(tspFile),
                new Dictionary<string, string>
                {
                    ["SourceFile"] = tspFile,
                    ["ActivationProbability"] = ActivationProbability.ToString("R"),
                    ["SwitchProbability"] = SwitchProbability.ToString("R"),
                    ["UpdatePolicy"] = UpdatePolicy.AfterEachBatchEvaluation.ToString(),
                    ["EpochLength"] = epochLength.ToString(),
                    ["QualityScale"] = concordePath is null ? "AbsoluteObjectiveValue" : "RelativeToExactEpochOptimum",
                    ["ExactSolver"] = concordePath is null ? "None" : "Concorde",
                    ["ExactSolverPath"] = concordePath ?? string.Empty,
                    ["EvaluatorCache"] = "DynamicCachingEvaluator",
                    ["EvaluatorCacheKey"] = nameof(PermutationCacheKeySelector),
                    ["EvaluatorCacheGraceCount"] = epochLength.ToString()
                });

        public override AutoEcBaseAlgorithmDescription CreateBaseAlgorithmDescription() =>
            new(FormatAlgorithmKind(ChildAlgorithmKind),
                new Dictionary<string, string>
                {
                    ["Encoding"] = "Permutation",
                    ["Creator"] = nameof(RandomPermutationCreator),
                    ["Crossover"] = nameof(EdgeRecombinationCrossover),
                    ["Mutator"] = nameof(InversionMutator),
                    ["Selector"] = "GeneralizedRankSelector",
                    ["RankPressure"] = "4",
                    ["Elites"] = "1",
                    ["HypermutationRate"] = HypermutationRate.ToString("R"),
                    ["HypermutationDuration"] = HypermutationDuration.ToString(),
                    ["ChangeTriggeredImmigrantRate"] = ChangeTriggeredImmigrantRate.ToString("R")
                });

        public override IEvaluator<Permutation>
            CreateEvaluator(ActivatedTravelingSalesmanProblem problem) =>
            CreateBaseEvaluator(problem).Cached(problem, PermutationCacheKeySelector.Instance) with
            {
                GraceCount = epochLength
            };


        private IEvaluator<Permutation> CreateBaseEvaluator(
            ActivatedTravelingSalesmanProblem problem)
        {
            var evaluator = ProblemEvaluator.For(problem);
            if (concordePath is null)
            {
                return evaluator;
            }

            return evaluator.ScaledToDynamicBestKnown(
                problem,
                new ActivatedTravelingSalesmanExactBestKnownProvider(
                    new ConcordeTravelingSalesmanExactSolver(concordePath)));
        }

        protected override MetaOptimizationSearchSpace CreateHyperParameterSearchSpace() =>
            new BoundedRealVectorSearchSpace(1, new RealVector(0.01), new RealVector(0.2))
                .CombinedWith<RealVector, BoundedRealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>(
                    new IntegerVectorSearchSpace(1, new IntegerVector(20), new IntegerVector(60)));

        protected override IAlgorithm<Permutation, PopulationState<Permutation>>
            CreateAlgorithm(
                ActivatedTravelingSalesmanProblem problem,
                IEvaluator<Permutation> evaluator,
                MetaOptimizationGenotype hyperParameters)
        {
            var creator = new RandomPermutationCreator();
            var crossover = new EdgeRecombinationCrossover();
            var mutator = new InversionMutator();
            var selector = GeneralizedRankSelector.For(problem, pressure: 4.0);
            var populationSize = hyperParameters.Part2[0];
            var mutationRate = hyperParameters.Part1[0];
            IInterceptor<Permutation>? interceptor = null;

            return ChildAlgorithmKind switch
            {
                AutoEcChildAlgorithmKind.HypermutationGeneticAlgorithm => HypermutationGeneticAlgorithm.Create<Permutation, PermutationSearchSpace, ActivatedTravelingSalesmanProblem>(
                    creator, crossover, mutator, mutationRate, HypermutationRate, HypermutationDuration,
                    selector, populationSize, evaluator, interceptor: interceptor),
                AutoEcChildAlgorithmKind.ChangeTriggeredRandomImmigrantGeneticAlgorithm =>
                    ChangeTriggeredRandomImmigrantGeneticAlgorithm.Create<Permutation, PermutationSearchSpace, ActivatedTravelingSalesmanProblem>(
                        creator, crossover, mutator, mutationRate, ChangeTriggeredImmigrantRate,
                        selector, populationSize, evaluator, interceptor: interceptor),
                _ => GeneticAlgorithm.Create(creator, crossover, mutator, selector, evaluator,
                    populationSize: populationSize, mutationRate: mutationRate, interceptor: interceptor)
            };
        }
    }
}
