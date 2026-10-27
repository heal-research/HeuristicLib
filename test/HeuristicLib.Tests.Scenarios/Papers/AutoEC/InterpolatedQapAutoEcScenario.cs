using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.QuadraticAssignment;
using MetaOptimizationGenotype =
    HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<HEAL.HeuristicLib.Encodings.RealVectors.RealVector,
        HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector>;

namespace HEAL.HeuristicLib.Tests.Scenarios.Papers.AutoEC;

public partial class AutoEcPaperScenarioTests
{
    private sealed class InterpolatedQapAutoEcScenario(
        AutoEcChildAlgorithmKind childAlgorithmKind = AutoEcChildAlgorithmKind.GeneticAlgorithm,
        int problemSeed = 321,
        int runSeed = 123,
        int targetEpochChanges = MatrixScaffoldTargetEpochChanges,
        int noRacers = 2,
        int epochLength = MatrixScaffoldEpochLength,
        string instanceKey = "synthetic-4-a",
        string? qapFile = null)
        : AutoEcPaperScenario<Permutation, PermutationSearchSpace, InterpolatedQuadraticAssignmentProblem>(
            childAlgorithmKind)
    {
        public override string ScenarioName =>
            $"interpolated-qap-{instanceKey}-{FormatAlgorithmKind(ChildAlgorithmKind)}-auto-ec-smoke";
        public override int ProblemSeed => problemSeed;
        public override int RunSeed => runSeed;
        public override int TargetEpochChanges => targetEpochChanges;
        public override int NoRacers => noRacers;
        protected override int EpochLength => epochLength;
        protected override AutoEcParameterRange MutationRateRange => new("mutationRate", "real", 0.01, 0.2);
        protected override AutoEcParameterRange PopulationSizeRange => new("populationSize", "integer", 8, 20);
        protected override IReadOnlyList<AutoEcOperatorChoice<ICrossover<Permutation>>> CrossoverChoices { get; } = CreatePermutationCrossoverChoices();
        protected override IReadOnlyList<AutoEcOperatorChoice<IMutator<Permutation>>> MutatorChoices { get; } = CreatePermutationMutatorChoices();

        public override InterpolatedQuadraticAssignmentProblem CreateProblem()
        {
            var problemDataA = CreateProblemDataA();
            var problemDataB = CreateProblemDataB(problemDataA);

            return new InterpolatedQuadraticAssignmentProblem(
                problemDataA,
                problemDataB,
                RandomNumberGenerator.Create(ProblemSeed),
                alphaStart: 0.0,
                alphaStep: 0.1,
                interpolateDistances: true,
                pingPong: true,
                epochSchedule: new EvaluationCountSchedule(EpochLength), updatePolicy: UpdatePolicy.AfterEachIteration);
        }

        public override AutoEcProblemDescription CreateProblemDescription() =>
            new("InterpolatedQuadraticAssignment", instanceKey,
                new Dictionary<string, string>
                {
                    ["SourceFile"] = qapFile ?? "synthetic",
                    ["Size"] = CreateProblemDataA().Size.ToString(),
                    ["AlphaStart"] = "0",
                    ["AlphaStep"] = "0.1",
                    ["InterpolateDistances"] = "true",
                    ["PingPong"] = "true",
                    ["UpdatePolicy"] = nameof(UpdatePolicy.AfterEachIteration),
                    ["EpochLength"] = EpochLength.ToString(),
                    ["QualityScale"] = "RelativeToCurrentRearrangementLowerBound",
                    ["BestKnownProvider"] = "CurrentRearrangementLowerBound"
                });

        public override IEvaluator<Permutation>
            CreateEvaluator(InterpolatedQuadraticAssignmentProblem problem) =>
            new ProblemEvaluator<Permutation, PermutationSearchSpace, InterpolatedQuadraticAssignmentProblem>()
                .ScaledToDynamicBestKnown(
                problem,
                new FuncBestKnownObjectiveProvider<Permutation, PermutationSearchSpace, InterpolatedQuadraticAssignmentProblem>(static currentProblem => currentProblem.CurrentRearrangementLowerBound()),
                RelativeQualityZeroBestKnownPolicy.Difference);


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

        protected override IAlgorithm<Permutation, PopulationState<Permutation>>
            CreateAlgorithm(
                InterpolatedQuadraticAssignmentProblem problem,
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
                AutoEcChildAlgorithmKind.HypermutationGeneticAlgorithm => HypermutationGeneticAlgorithm.Create<Permutation, PermutationSearchSpace, InterpolatedQuadraticAssignmentProblem>(
                    creator, crossover, mutator, mutationRate, HypermutationRate, HypermutationDuration,
                    selector, populationSize, evaluator, interceptor: interceptor),
                AutoEcChildAlgorithmKind.ChangeTriggeredRandomImmigrantGeneticAlgorithm =>
                    ChangeTriggeredRandomImmigrantGeneticAlgorithm.Create<Permutation, PermutationSearchSpace, InterpolatedQuadraticAssignmentProblem>(
                        creator, crossover, mutator, mutationRate, ChangeTriggeredImmigrantRate,
                        selector, populationSize, evaluator, interceptor: interceptor),
                _ => GeneticAlgorithm.Create(creator, crossover, mutator, selector, evaluator,
                    populationSize: populationSize, mutationRate: mutationRate, interceptor: interceptor)
            };
        }

        private QuadraticAssignmentProblemData CreateProblemDataA()
        {
            if (qapFile is not null)
            {
                return QaplibQapInstanceProvider.LoadData(qapFile).ToProblemData();
            }

            return CreateSyntheticProblemData(instanceKey);
        }

        private QuadraticAssignmentProblemData CreateProblemDataB(QuadraticAssignmentProblemData problemDataA)
        {
            if (qapFile is not null)
            {
                return CreatePermutedVariant(problemDataA, StableHash(instanceKey));
            }

            return instanceKey switch
            {
                "synthetic-4-b" => CreateSyntheticProblemData("synthetic-4-a"),
                _ => CreateSyntheticProblemData("synthetic-4-b")
            };
        }

        private static QuadraticAssignmentProblemData CreateSyntheticProblemData(string instance)
        {
            return instance switch
            {
                "synthetic-4-b" => CreateSyntheticProblemDataB(),
                _ => CreateSyntheticProblemDataA()
            };
        }

        private static QuadraticAssignmentProblemData CreateSyntheticProblemDataA()
        {
            var flowMatrix = new double[,] { { 0, 2, 0, 1 }, { 2, 0, 3, 0 }, { 0, 3, 0, 4 }, { 1, 0, 4, 0 } };
            var distanceMatrix = new double[,] { { 0, 1, 2, 3 }, { 1, 0, 1, 2 }, { 2, 1, 0, 1 }, { 3, 2, 1, 0 } };
            return new QuadraticAssignmentProblemData(flowMatrix, distanceMatrix);
        }

        private static QuadraticAssignmentProblemData CreateSyntheticProblemDataB()
        {
            var flowMatrix = new double[,] { { 0, 0, 0, 0 }, { 1, 0, 0, 0 }, { 1, 1, 0, 0 }, { 1, 1, 1, 0 } };
            var distanceMatrix = new double[,] { { 0, 1, 2, 3 }, { 1, 0, 1, 2 }, { 2, 1, 0, 1 }, { 3, 2, 1, 0 } };
            return new QuadraticAssignmentProblemData(flowMatrix, distanceMatrix);
        }

        private static QuadraticAssignmentProblemData CreatePermutedVariant(
            IQuadraticAssignmentProblemData source,
            int offsetSeed)
        {
            var size = source.Size;
            var offset = offsetSeed % Math.Max(1, size - 1) + 1;
            var flows = new double[size, size];
            var distances = new double[size, size];
            for (var i = 0; i < size; i++)
            {
                var pi = (i + offset) % size;
                for (var j = 0; j < size; j++)
                {
                    var pj = (j + offset) % size;
                    flows[i, j] = source.GetFlow(pi, pj);
                    distances[i, j] = source.GetDistance(pi, pj);
                }
            }

            return new QuadraticAssignmentProblemData(flows, distances);
        }

        private static int StableHash(string value)
        {
            var hash = 17;
            foreach (var c in value)
            {
                hash = unchecked(hash * 31 + c);
            }

            return hash & int.MaxValue;
        }
    }
}
