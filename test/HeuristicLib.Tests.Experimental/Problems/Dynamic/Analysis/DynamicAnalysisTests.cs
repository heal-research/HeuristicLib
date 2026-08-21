using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.Problems.Dynamic;

public class DynamicAnalysisTests
{
    [Fact]
    public void QualityCurvePerEpochAnalysis_ObservesEvaluatorThroughAlgorithmRun()
    {
        var problem = new IntegerDynamicProblem(epochLength: 2);
        var algorithm = new BatchEvaluationAlgorithm([
            [5, 3],
            [10, 1],
            [4]
        ]);
        var analysis =
            new QualityCurvePerEpochAnalysis<int, IntegerSearchSpace, IntegerDynamicProblem>(
                problem,
                algorithm.Evaluator);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(0)).WithAnalyzer(analysis);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        var result = run.GetResult(analysis);
        result.BestPerEpoch.Select(x => (x.candidate, objective: x.objectiveVector[0], x.timing.Epoch))
              .ShouldBe([(5, 5.0, 0), (3, 3.0, 1), (10, 10.0, 1), (1, 1.0, 2), (4, 4.0, 2)]);
    }

    [Fact]
    public void BestBeforeChangePerformanceAnalysis_RecordsBestCompletedEpochsThroughAlgorithmRun()
    {
        var problem = new IntegerDynamicProblem(epochLength: 2);
        var algorithm = new BatchEvaluationAlgorithm([
            [5, 3],
            [10, 1],
            [4]
        ]);
        var analysis =
            new BestBeforeChangePerformanceAnalysis<int, IntegerSearchSpace, IntegerDynamicProblem>(
                problem,
                [algorithm.Evaluator],
                predictionEpochMultiplier: 2);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(0)).WithAnalyzer(analysis);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        var result = run.GetResult(analysis);
        result.BestBeforeChange.Select(x => (x.Candidate, x.ObjectiveValue, x.Timing.Epoch))
              .ShouldBe([(5, 5.0, 0), (3, 3.0, 1)]);
        result.Performance.ShouldBe(4.0);
    }

    private sealed class IntegerSearchSpace : ISearchSpace<int>
    {
        public bool Contains(int candidate) => true;
    }

    private sealed class IntegerDynamicProblem : DynamicProblem<int, IntegerSearchSpace>
    {
        public IntegerDynamicProblem(int epochLength)
            : base(SingleObjective.Minimize, new IntegerSearchSpace(), RandomNumberGenerator.Create(0),
                UpdatePolicy.AfterEvaluation, epochLength)
        { }

        public override ObjectiveVector Evaluate(int candidate, IRandomNumberGenerator random,
                                                 EvaluationTiming timing) => candidate;

        protected override void Update()
        { }
    }

    private sealed record ProblemEvaluator : StatelessEvaluator<int, IntegerSearchSpace, IntegerDynamicProblem>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates,
                                                                IRandomNumberGenerator random,
                                                                IntegerSearchSpace searchSpace,
                                                                IntegerDynamicProblem problem) =>
            candidates.Select(candidate => problem.Evaluate(candidate, random)).ToArray();
    }

    private sealed record BatchEvaluationAlgorithm(IReadOnlyList<IReadOnlyList<int>> Batches)
        : Algorithm<BatchEvaluationAlgorithm, int, IntegerSearchSpace, IntegerDynamicProblem, PopulationState<int>>
    {
        public IEvaluator<int, IntegerSearchSpace, IntegerDynamicProblem> Evaluator { get; } = new ProblemEvaluator();

        public override AlgorithmInstance<int, IntegerSearchSpace, IntegerDynamicProblem, PopulationState<int>>
            CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
            new Instance(instanceRegistry.Resolve(Evaluator), Batches);

        private sealed class Instance(
            IEvaluatorInstance<int, IntegerSearchSpace, IntegerDynamicProblem> evaluator,
            IReadOnlyList<IReadOnlyList<int>> batches)
            : AlgorithmInstance<int, IntegerSearchSpace, IntegerDynamicProblem, PopulationState<int>>
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(
                IntegerDynamicProblem problem,
                IRandomNumberGenerator random,
                PopulationState<int>? initialState = null,
                [EnumeratorCancellation] CancellationToken ct = default)
            {
                foreach (var batch in batches)
                {
                    ct.ThrowIfCancellationRequested();
                    var objectiveVectors = evaluator.Evaluate(batch, random, problem.SearchSpace, problem);

                    yield return Population.From(batch.ToEvaluated(objectiveVectors)).ToPopulationState();
                    await Task.CompletedTask;
                }
            }
        }
    }
}
