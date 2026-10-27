using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using MetaOptimizationGenotype =
    HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<HEAL.HeuristicLib.Encodings.RealVectors.RealVector,
        HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector>;

namespace HEAL.HeuristicLib.Tests.Scenarios.Papers.AutoEC;

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
        private static string ConcordeWorkingDirectoryRoot => Path.Combine(FindRepositoryRoot(), "artifacts", "concorde");

        public override string ScenarioName =>
            scenarioName ?? $"activated-tsp-{FormatAlgorithmKind(ChildAlgorithmKind)}-auto-ec-smoke";
        public override int ProblemSeed => problemSeed;
        public override int RunSeed => runSeed;
        public override int TargetEpochChanges => targetEpochChanges;
        public override int NoRacers => noRacers;
        public override double MetaMutationStrength => 0.1;
        protected override AutoEcParameterRange MutationRateRange => new("mutationRate", "real", 0.01, 0.2);
        protected override AutoEcParameterRange PopulationSizeRange => new("populationSize", "integer", 20, 60);
        protected override IReadOnlyList<AutoEcOperatorChoice<ICrossover<Permutation>>> CrossoverChoices { get; } = CreatePermutationCrossoverChoices();
        protected override IReadOnlyList<AutoEcOperatorChoice<IMutator<Permutation>>> MutatorChoices { get; } = CreatePermutationMutatorChoices();

        public override ActivatedTravelingSalesmanProblem CreateProblem()
        {
            var tspData = TsplibTspInstanceProvider.LoadData(tspFile).ToDistanceMatrixData();
            return new ActivatedTravelingSalesmanProblem(
                tspData,
                RandomNumberGenerator.Create(ProblemSeed),
                activationProb: ActivationProbability,
                switchProbability: SwitchProbability,
                epochSchedule: new EvaluationCountSchedule(epochLength), updatePolicy: UpdatePolicy.AfterEachIteration);
        }

        public override AutoEcProblemDescription CreateProblemDescription() =>
            new("ActivatedTravelingSalesman", Path.GetFileName(tspFile),
                new Dictionary<string, string>
                {
                    ["SourceFile"] = tspFile,
                    ["ActivationProbability"] = ActivationProbability.ToString("R"),
                    ["SwitchProbability"] = SwitchProbability.ToString("R"),
                    ["UpdatePolicy"] = UpdatePolicy.AfterEachIteration.ToString(),
                    ["EpochLength"] = epochLength.ToString(),
                    ["QualityScale"] = concordePath is null ? "AbsoluteObjectiveValue" : "RelativeToExactEpochOptimum",
                    ["ExactSolver"] = concordePath is null ? "None" : "Concorde",
                    ["ExactSolverPath"] = concordePath ?? string.Empty,
                    ["ExactSolverWorkingDirectoryRoot"] = concordePath is null ? string.Empty : ConcordeWorkingDirectoryRoot,
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
                    ["Crossover"] = "Raced",
                    ["Mutator"] = "Raced",
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
                    new ConcordeTravelingSalesmanExactSolver(concordePath)
                    {
                        WorkingDirectoryRoot = ConcordeWorkingDirectoryRoot
                    }));
        }

        protected override IAlgorithm<Permutation, PopulationState<Permutation>>
            CreateAlgorithm(
                ActivatedTravelingSalesmanProblem problem,
                IEvaluator<Permutation> evaluator,
                MetaOptimizationGenotype hyperParameters)
        {
            var creator = new RandomPermutationCreator();
            var crossover = SelectCrossover(hyperParameters);
            var mutator = SelectMutator(hyperParameters);
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
