using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.Problems.Dynamic;

public class DynamicAnalysisTests
{
    [Fact]
    public void RelativeQuality_RefreshesAfterDeferredBatchUpdatesWithoutSubscriptions()
    {
        var problem = new IntegerDynamicProblem(epochLength: 2);
        var provider = new EpochBestKnown();
        var evaluator = new ProblemEvaluator().ScaledToDynamicBestKnown(problem, provider);
        var execution = ResolutionScope.Create().Resolve<int, IntegerSearchSpace, IntegerDynamicProblem>(evaluator);

        execution.ShouldNotBeAssignableTo<IDisposable>();
        execution.Evaluate([2, 2], RandomNumberGenerator.Create(0), problem.SearchSpace, problem)
            .Select(value => value[0]).ShouldBe([1d, 1d]);
        execution.Evaluate([2], RandomNumberGenerator.Create(0), problem.SearchSpace, problem)
            .Single()[0].ShouldBe(0);
        execution.Evaluate([2], RandomNumberGenerator.Create(0), problem.SearchSpace, problem)
            .Single()[0].ShouldBe(0);
        provider.Epochs.ShouldBe([0, 1]);
    }

    private sealed class EpochBestKnown : IBestKnownObjectiveProvider<int, IntegerSearchSpace, IntegerDynamicProblem>
    {
        public List<int> Epochs { get; } = [];
        public ObjectiveVector GetBestKnown(IntegerDynamicProblem problem)
        {
            Epochs.Add(problem.CurrentEpoch);
            return new ObjectiveVector(problem.CurrentEpoch + 1);
        }
    }

    [Fact]
    public void BestPerEpoch_IsAnOrdinaryTraceReadAgainstTheEpochSchedule()
    {
        var problem = new IntegerDynamicProblem(epochLength: 2);
        var algorithm = new BatchEvaluationAlgorithm([
            [5, 3],
            [10, 1],
            [4]
        ]);
        var epoch = Clock.FromEpoch(problem);
        var bestPerEpoch = Analyzer.Trace(algorithm.Evaluator,
            observation => observation.Candidates.ToEvaluated(observation.ObjectiveVectors),
            Aggregate.Best<int>(),
            [epoch]);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(0)).Attach(bestPerEpoch);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        // One entry per firing, each tagged with the environment version its batch was evaluated against.
        bestPerEpoch.By(epoch).Select(point => (point.Value.Candidate, point.Time))
                    .ShouldBe([(3, 0), (1, 1), (4, 2)]);
    }

    [Fact]
    public void EpochWork_ReportsTheEvaluationsEachEnvironmentGotAndHowManyRanLate()
    {
        var problem = new IntegerDynamicProblem(epochLength: 2);
        var algorithm = new BatchEvaluationAlgorithm([
            [5, 3, 7],
            [10, 1]
        ]);
        var evaluations = Clock.FromEvaluations(algorithm.Evaluator);
        var epoch = Clock.FromEpoch(problem);
        var work = Analyzer.TraceEpochWork(algorithm.Evaluator, evaluations, epoch);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(0)).Attach(work);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        // Under a batch policy an epoch ending mid-batch leaves the rest of that batch scored by the old environment:
        // 7 runs after the boundary at 3, and 1 after the boundary at 10.
        EpochWorkTrace.PerEpoch(work, evaluations, epoch, evaluationsPerEpoch: 2)
                      .ShouldBe([new EpochWork(0, 3, 1), new EpochWork(1, 2, 1)]);
    }

    [Fact]
    public void EnvironmentUpdates_NeedNothingInstalledAndNothingAnalyzing()
    {
        var problem = new IntegerDynamicProblem(epochLength: 2);
        var algorithm = new BatchEvaluationAlgorithm([
            [5, 3],
            [10, 1],
            [4]
        ]);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(0));

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        problem.Updates.ShouldBe(2);
        problem.CurrentEpoch.ShouldBe(2);
    }

    [Fact]
    public void AfterEachIterationPolicy_UpdatesAtTheIterationBoundaryOfTheNamedAlgorithm()
    {
        var problem = new IntegerDynamicProblem(epochLength: 2, UpdatePolicy.AfterEachIteration);
        var algorithm = new BatchEvaluationAlgorithm([
            [5, 3],
            [10, 1],
            [4]
        ]);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(0)).Attach(problem.CreateIterationUpdateModule(algorithm));

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        problem.Updates.ShouldBe(2);
        problem.CurrentEpoch.ShouldBe(2);
    }

    [Fact]
    public void AfterEachIterationPolicy_DoesNotUpdateAtTheBatchBoundary()
    {
        var problem = new IntegerDynamicProblem(epochLength: 2, UpdatePolicy.AfterEachIteration);
        var algorithm = new BatchEvaluationAlgorithm([[5, 3], [10, 1]]);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(0));

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        problem.Updates.ShouldBe(0);
    }

    [Fact]
    public void AfterEachEvaluationPolicy_LetsOnePopulationSpanEpochs()
    {
        var problem = new IntegerDynamicProblem(epochLength: 2, UpdatePolicy.AfterEachEvaluation);
        var algorithm = new BatchEvaluationAlgorithm([[5, 3, 7]]);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(0));

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        // The update falls due mid-batch and is applied before the candidate that follows it, so the batch is scored
        // by two environments. This is the default policy.
        problem.ScoredAgainst.ShouldBe([0, 0, 1]);
    }

    [Fact]
    public void ScheduleOwingSeveralEpochs_AppliesThemAllBeforeTheNextEvaluation()
    {
        // A schedule that ends an epoch on something other than the work itself, such as elapsed time, has nothing to
        // report progress against, and may owe several at once. The problem asks at the boundary anyway, so both
        // updates land before the batch.
        var problem = new IntegerDynamicProblem(new OwedEpochsSchedule(owed: 2));
        var algorithm = new BatchEvaluationAlgorithm([[5, 3]]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(0));

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        problem.Updates.ShouldBe(2);
        problem.ScoredAgainst.ShouldBe([2, 2]);
        // Epochs count applied updates, including environments against which nothing was evaluated.
        problem.CurrentEpoch.ShouldBe(2);
    }

    [Fact]
    public void BestBeforeChangePerformanceAnalyzer_RecordsTheBestOfEveryCompletedEpoch()
    {
        var problem = new IntegerDynamicProblem(epochLength: 2);
        var algorithm = new BatchEvaluationAlgorithm([
            [5, 3],
            [10, 1],
            [4]
        ]);
        var analysis =
            new BestBeforeChangePerformanceAnalyzer<int, IntegerSearchSpace, IntegerDynamicProblem>(
                problem,
                [algorithm.Evaluator],
                predictionEpochMultiplier: 2);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(0)).Attach(analysis);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        // The epoch in progress when the run ends never changed, so its best is not a best before a change.
        analysis.BestBeforeChange.Select(entry => (entry.Candidate, entry.ObjectiveValue, entry.Epoch))
                .ShouldBe([(3, 3.0, 0), (1, 1.0, 1)]);
        analysis.Performance.ShouldBe(2.0);
    }

    /// <summary>
    /// Owes a fixed number of epochs on the first ask and nothing afterwards, the way a schedule watching something
    /// outside the run behaves when several of its epochs elapsed before anything was evaluated.
    /// </summary>
    private sealed class OwedEpochsSchedule(int owed) : IEpochSchedule
    {
        private int remaining = owed;

        public void RecordProgress()
        {
        }

        public int TakeDueEpochs()
        {
            var due = remaining;
            remaining = 0;
            return due;
        }

        public void Restart() => remaining = 0;
    }

    private sealed class IntegerSearchSpace : ISearchSpace<int>
    {
        public bool Contains(int candidate) => true;
    }

    private sealed class IntegerDynamicProblem : DynamicProblem<IntegerDynamicProblem, int, IntegerSearchSpace>
    {
        public IntegerDynamicProblem(int epochLength, UpdatePolicy updatePolicy = UpdatePolicy.AfterEachBatchEvaluation)
            : this(new EvaluationCountSchedule(epochLength), updatePolicy)
        { }

        public IntegerDynamicProblem(IEpochSchedule schedule, UpdatePolicy updatePolicy = UpdatePolicy.AfterEachBatchEvaluation)
            : base(SingleObjective.Minimize, new IntegerSearchSpace(), RandomNumberGenerator.Create(0),
                schedule, updatePolicy)
        { }

        private readonly List<int> scoredAgainst = [];

        public int Updates { get; private set; }

        /// <summary>The environment version each evaluation was scored against, in evaluation order.</summary>
        public IReadOnlyList<int> ScoredAgainst => scoredAgainst;

        protected override ObjectiveVector Evaluate(int candidate, IRandomNumberGenerator random,
                                                 int epoch)
        {
            scoredAgainst.Add(epoch);
            return candidate;
        }

        protected override void Update() => Updates++;
    }

    private sealed record ProblemEvaluator : StatelessEvaluator<int, IntegerSearchSpace, IntegerDynamicProblem>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates,
                                                                IRandomNumberGenerator random,
                                                                IntegerSearchSpace searchSpace,
                                                                IntegerDynamicProblem problem) =>
            problem.Evaluate(candidates, random);
    }

    private sealed record BatchEvaluationAlgorithm(IReadOnlyList<IReadOnlyList<int>> Batches)
        : Algorithm<BatchEvaluationAlgorithm, int, IntegerSearchSpace, IntegerDynamicProblem, PopulationState<int>>
    {
        public IEvaluator<int> Evaluator { get; } = new ProblemEvaluator();

        public override ExecutionFactory<AlgorithmExecution<int, IntegerSearchSpace, IntegerDynamicProblem, PopulationState<int>>> CreateExecutionFactory() =>
            scope => new Execution(scope.Resolve<int, IntegerSearchSpace, IntegerDynamicProblem>(Evaluator), Batches);

        private sealed class Execution(
            IEvaluatorExecution<int, IntegerSearchSpace, IntegerDynamicProblem> evaluator,
            IReadOnlyList<IReadOnlyList<int>> batches)
            : AlgorithmExecution<int, IntegerSearchSpace, IntegerDynamicProblem, PopulationState<int>>
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
