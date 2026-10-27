using HEAL.HeuristicLib.Data;
using HEAL.HeuristicLib.Encodings.SymbolicExpressions;
using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.Dynamic.SlidingWindowRegression;
using HEAL.HeuristicLib.Problems.MachineLearning;
using MetaOptimizationGenotype =
    HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<HEAL.HeuristicLib.Encodings.RealVectors.RealVector,
        HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector>;

namespace HEAL.HeuristicLib.Tests.Scenarios.Papers.AutoEC;

public partial class AutoEcPaperScenarioTests
{
    private sealed class SlidingWindowSymRegAutoEcScenario(
        AutoEcChildAlgorithmKind childAlgorithmKind = AutoEcChildAlgorithmKind.GeneticAlgorithm,
        int problemSeed = 321,
        int runSeed = 123,
        int targetEpochChanges = MatrixScaffoldTargetEpochChanges,
        int noRacers = 2,
        int epochLength = MatrixScaffoldEpochLength)
        : AutoEcPaperScenario<ExpressionTree, ExpressionTreeSearchSpace,
            SlidingWindowSymbolicRegressionProblem>(childAlgorithmKind)
    {
        public override string ScenarioName =>
            $"sliding-window-symreg-{FormatAlgorithmKind(ChildAlgorithmKind)}-auto-ec-smoke";
        public override int ProblemSeed => problemSeed;
        public override int RunSeed => runSeed;
        public override int TargetEpochChanges => targetEpochChanges;
        public override int NoRacers => noRacers;
        protected override int EpochLength => epochLength;
        protected override AutoEcParameterRange MutationRateRange => new("mutationRate", "real", 0.01, 0.2);
        protected override AutoEcParameterRange PopulationSizeRange => new("populationSize", "integer", 8, 20);
        protected override IReadOnlyList<AutoEcOperatorChoice<ICrossover<ExpressionTree>>> CrossoverChoices { get; } =
        CreateCrossoverChoices<ExpressionTree>([
            new(nameof(SubtreeCrossover), new SubtreeCrossover())
        ]);
        protected override IReadOnlyList<AutoEcOperatorChoice<IMutator<ExpressionTree>>> MutatorChoices { get; } =
        CreateMutatorChoices<ExpressionTree>([
            new(nameof(NodeReplacementMutator), new NodeReplacementMutator()),
            new(nameof(LocalPerturbationMutator), new LocalPerturbationMutator()),
            new(nameof(ShrinkSubtreeMutator), new ShrinkSubtreeMutator()),
            new(nameof(SubtreeMutator), new SubtreeMutator())
        ]);

        public override SlidingWindowSymbolicRegressionProblem CreateProblem()
        {
            var data = CreateRegressionProblemData();
            var inner = new SymbolicRegressionProblem(
                data,
                Metrics.RMSE,
                CreateSymbolicRegressionSearchSpace());
            return new SlidingWindowSymbolicRegressionProblem(
                inner,
                windowStart: 0,
                windowLength: 12,
                stepSize: 4,
                epochSchedule: new EvaluationCountSchedule(EpochLength), updatePolicy: UpdatePolicy.AfterEachIteration);
        }

        public override AutoEcProblemDescription CreateProblemDescription() =>
            new("SlidingWindowSymbolicRegression", "synthetic-sine-trend",
                new Dictionary<string, string>
                {
                    ["Rows"] = "48",
                    ["WindowStart"] = "0",
                    ["WindowLength"] = "12",
                    ["StepSize"] = "4",
                    ["UpdatePolicy"] = nameof(UpdatePolicy.AfterEachIteration),
                    ["EpochLength"] = EpochLength.ToString()
                });

        public override AutoEcBaseAlgorithmDescription CreateBaseAlgorithmDescription() =>
            new(FormatAlgorithmKind(ChildAlgorithmKind),
                new Dictionary<string, string>
                {
                    ["Encoding"] = "SymbolicExpressionTree",
                    ["Creator"] = nameof(ProbabilisticTreeCreator),
                    ["Crossover"] = "Raced",
                    ["Mutator"] = "Raced",
                    ["Selector"] = "TournamentSelector",
                    ["TournamentSize"] = "3",
                    ["Elites"] = "1",
                    ["HypermutationRate"] = HypermutationRate.ToString("R"),
                    ["HypermutationDuration"] = HypermutationDuration.ToString(),
                    ["ChangeTriggeredImmigrantRate"] = ChangeTriggeredImmigrantRate.ToString("R")
                });

        protected override IAlgorithm<ExpressionTree, PopulationState<ExpressionTree>>
            CreateAlgorithm(
                SlidingWindowSymbolicRegressionProblem problem,
                IEvaluator<ExpressionTree> evaluator,
                MetaOptimizationGenotype hyperParameters)
        {
            var creator = new ProbabilisticTreeCreator();
            var crossover = SelectCrossover(hyperParameters);
            var mutator = SelectMutator(hyperParameters);
            var selector = TournamentSelector.For(problem, tournamentSize: 3);
            var populationSize = hyperParameters.Part2[0];
            var mutationRate = hyperParameters.Part1[0];
            IInterceptor<ExpressionTree>? interceptor = null;

            return ChildAlgorithmKind switch
            {
                AutoEcChildAlgorithmKind.HypermutationGeneticAlgorithm => HypermutationGeneticAlgorithm.Create<ExpressionTree, ExpressionTreeSearchSpace, SlidingWindowSymbolicRegressionProblem>(
                    creator, crossover, mutator, mutationRate, HypermutationRate, HypermutationDuration,
                    selector, populationSize, evaluator, interceptor: interceptor),
                AutoEcChildAlgorithmKind.ChangeTriggeredRandomImmigrantGeneticAlgorithm =>
                    ChangeTriggeredRandomImmigrantGeneticAlgorithm.Create<ExpressionTree, ExpressionTreeSearchSpace, SlidingWindowSymbolicRegressionProblem>(
                        creator, crossover, mutator, mutationRate, ChangeTriggeredImmigrantRate,
                        selector, populationSize, evaluator, interceptor: interceptor),
                _ => GeneticAlgorithm.Create(creator, crossover, mutator, selector, evaluator,
                    populationSize: populationSize, mutationRate: mutationRate, interceptor: interceptor)
            };
        }

        private static RegressionData CreateRegressionProblemData()
        {
            var rows = Enumerable.Range(0, 48)
                                 .Select(i =>
                                 {
                                     var x = i / 8.0;
                                     return new[] { x, Math.Sin(x) + 0.05 * i };
                                 })
                                 .ToArray();
            var values = new double[rows.Length, 2];
            for (var i = 0; i < rows.Length; i++)
            {
                values[i, 0] = rows[i][0];
                values[i, 1] = rows[i][1];
            }

            var frame = DataFrame.FromMatrix(["x", "y"], values);
            return new RegressionData(
                new DataFrame([frame.Get<double>("x")]),
                frame.Get<double>("y"));
        }

        private static ExpressionTreeSearchSpace CreateSymbolicRegressionSearchSpace() =>
            new(
                maximumLength: 31,
                maximumDepth: 6,
                operations: [Symbols.Addition, Symbols.Subtraction, Symbols.Multiplication, Symbols.Sine],
                variables: ["x"]);

    }
}
