using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Analysis;

public class AnalysisUsabilityTests
{
    [Fact]
    public void SeparateRetentionObjects_CountIndependently()
    {
        var evaluator = new ManualEvaluator();
        var first = Analyzer.Trace(evaluator.Calls, observation => observation.ObjectiveVectors[0][0], retention: TraceRetention.EveryNth(2));
        var second = Analyzer.Trace(evaluator.Calls, observation => observation.ObjectiveVectors[0][0], retention: TraceRetention.EveryNth(2));
        var scope = Install(first, second);

        foreach (var candidate in Enumerable.Range(1, 5))
            Evaluate(scope, evaluator, candidate);

        first.Snapshot().Select(entry => entry.Value).ShouldBe([2, 4]);
        second.Snapshot().Select(entry => entry.Value).ShouldBe([2, 4]);
    }

    /// <summary>
    /// A retention holds its own counting, so handing one object to two traces couples them. The factories return a
    /// fresh retention per call, which is why the ordinary inline use is independent.
    /// </summary>
    [Fact]
    public void OneRetentionObjectSharedByTwoTraces_SharesItsCounting()
    {
        var evaluator = new ManualEvaluator();
        var shared = TraceRetention.EveryNth(2);
        var first = Analyzer.Trace(evaluator.Calls, observation => observation.ObjectiveVectors[0][0], retention: shared);
        var second = Analyzer.Trace(evaluator.Calls, observation => observation.ObjectiveVectors[0][0], retention: shared);
        var scope = Install(first, second);

        foreach (var candidate in Enumerable.Range(1, 5))
            Evaluate(scope, evaluator, candidate);

        // Every firing asks the one counter twice, so the second trace takes every entry and the first takes none.
        first.Snapshot().ShouldBeEmpty();
        second.Snapshot().Select(entry => entry.Value).ShouldBe([1, 2, 3, 4, 5]);
    }

    [Fact]
    public void OneTrace_CanObserveSeveralEvaluators()
    {
        var firstEvaluator = new ManualEvaluator();
        var secondEvaluator = new ManualEvaluator();
        var count = 0;
        var trace = Analyzer.Trace([firstEvaluator.Calls, secondEvaluator.Calls], observation => count += observation.ObjectiveVectors.Count);
        var scope = Install(trace);

        Evaluate(scope, firstEvaluator, 1, 2);
        Evaluate(scope, secondEvaluator, 3, 4);

        trace.Snapshot().Select(entry => entry.Value).ShouldBe([2, 4]);
    }

    [Fact]
    public void OneTrace_ObservesASourceOnceWhenGivenItTwice()
    {
        var evaluator = new ManualEvaluator();
        var count = 0;
        var trace = Analyzer.Trace([evaluator.Calls, evaluator.Calls], observation => count += observation.ObjectiveVectors.Count);
        var scope = Install(trace);

        Evaluate(scope, evaluator, 1, 2);

        trace.Snapshot().Select(entry => entry.Value).ShouldBe([2]);
    }

    [Fact]
    public void TracesOfEachOperationOfOneConfiguration_ObserveTheirOwnCalls()
    {
        var source = new SharedOperations(new ProbeLog());
        IOperator<Mutation, RealVector> mutator = source;
        IOperator<Evaluation, RealVector> evaluator = source;
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var mutations = Analyzer.Trace(mutator.Calls, observation => observation.Offspring.Count);
        var evaluations = Analyzer.Trace(evaluator.Calls, observation => observation.Candidates.Count);
        var scope = ResolutionScope.Create(mutations, evaluations).For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();

        scope.Resolve(mutator).Mutate([new RealVector(2.0)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        scope.Resolve(evaluator).Evaluate([new RealVector(2.0), new RealVector(3.0)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        mutations.Snapshot().Select(entry => entry.Value).ShouldBe([1]);
        evaluations.Snapshot().Select(entry => entry.Value).ShouldBe([2]);
    }

    [Fact]
    public async Task SharedTrace_SerializesItsFunctionAcrossConcurrentScopes()
    {
        var evaluator = new ManualEvaluator();
        var activeCalls = 0;
        var overlappingCalls = 0;
        var count = 0;
        var trace = Analyzer.Trace(evaluator.Calls, observation =>
        {
            if (Interlocked.Increment(ref activeCalls) > 1)
                Interlocked.Increment(ref overlappingCalls);
            try
            {
                // Keep the function active long enough for another worker to reach the same trace.
                Thread.Sleep(1);
                return count += observation.ObjectiveVectors.Count;
            }
            finally
            {
                Interlocked.Decrement(ref activeCalls);
            }
        });
        using var start = new Barrier(4);
        var workers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            var scope = Install(trace);
            start.SignalAndWait(TestContext.Current.CancellationToken);
            for (var i = 0; i < 8; i++)
                Evaluate(scope, evaluator, i);
        }, TestContext.Current.CancellationToken)).ToArray();

        await Task.WhenAll(workers);

        overlappingCalls.ShouldBe(0);
        trace.Snapshot().Select(entry => entry.Value).ShouldBe(Enumerable.Range(1, 32));
    }

    [Fact]
    public void Analyzer_CanUseDifferentCallbacksForTheSameOperatorRole()
    {
        var firstEvaluator = new ManualEvaluator();
        var secondEvaluator = new ManualEvaluator();
        var analyzer = new SeparateEvaluatorAnalyzer(firstEvaluator, secondEvaluator);
        var scope = Install(analyzer);

        Evaluate(scope, firstEvaluator, 1, 2);
        Evaluate(scope, secondEvaluator, 3, 4, 5);

        analyzer.FirstCandidates.ShouldBe(2);
        analyzer.SecondCandidates.ShouldBe(3);
    }

    [Fact]
    public void SharedClock_IsInstalledOncePerScope()
    {
        var evaluator = new ManualEvaluator();
        var clock = Clock.FromEvaluations(evaluator);
        var first = Analyzer.Trace(evaluator.Calls, observation => observation.ObjectiveVectors.Count, clocks: [clock, clock]);
        var second = Analyzer.Trace(evaluator.Calls, observation => observation.ObjectiveVectors.Count, clocks: [clock]);
        var scope = Install(first, second);

        Evaluate(scope, evaluator, 1, 2);
        Evaluate(scope, evaluator, 3, 4, 5);

        first.By(clock).Select(point => point.Time).ShouldBe([2L, 5L]);
        second.By(clock).Select(point => point.Time).ShouldBe([2L, 5L]);
    }

    [Fact]
    public void RetentionOnChange_HandlesNullAndUsesTypedEquality()
    {
        var retention = TraceRetention.OnChange();
        retention.Decide<string?>(null).ShouldBe(RetentionDecision.Append);
        retention.Decide<string?>(null).ShouldBe(RetentionDecision.Skip);
        retention.Decide("value").ShouldBe(RetentionDecision.Append);
        retention.Decide("value").ShouldBe(RetentionDecision.Skip);
        retention.Decide<string?>(null).ShouldBe(RetentionDecision.Append);
        retention.Decide(0).ShouldBe(RetentionDecision.Append);
        retention.Decide(0).ShouldBe(RetentionDecision.Skip);
    }

    [Fact]
    public void RetentionOnChange_CanBeGivenAComparer()
    {
        var retention = TraceRetention.OnChange(StringComparer.OrdinalIgnoreCase);
        retention.Decide("value").ShouldBe(RetentionDecision.Append);
        retention.Decide("VALUE").ShouldBe(RetentionDecision.Skip);
        retention.Decide("other").ShouldBe(RetentionDecision.Append);
    }

    [Fact]
    public void ARetentionReturningAnUnknownDecision_FailsWhereItIsUsed()
    {
        var evaluator = new ManualEvaluator();
        var trace = Analyzer.Trace(evaluator.Calls, observation => observation.ObjectiveVectors[0][0],
            retention: new UnknownDecisionRetention());
        var scope = Install(trace);

        var exception = Should.Throw<InvalidOperationException>(() => Evaluate(scope, evaluator, 1));
        exception.Message.ShouldContain("unknown decision");
    }

    private static ResolutionScope Install(params Aspect[] analyzers) =>
        ResolutionScope.Create(analyzers);

    private static void Evaluate(ResolutionScope scope, ManualEvaluator evaluator, params int[] candidates) =>
        scope.Resolve<int, DummySearchSpace<int>, TestProblem>(evaluator).Evaluate(candidates, RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, TestProblem.Instance);

    private sealed class SeparateEvaluatorAnalyzer : Aspect
    {
        public int FirstCandidates { get; private set; }
        public int SecondCandidates { get; private set; }

        public SeparateEvaluatorAnalyzer(ManualEvaluator first, ManualEvaluator second)
        {
            Advices =
            [
                .. Aspect.Observe(first.Calls, observation => FirstCandidates += observation.Candidates.Count),
                .. Aspect.Observe(second.Calls, observation => SecondCandidates += observation.Candidates.Count)
            ];
        }
    }

    private sealed record ManualEvaluator : Evaluator<int, DummySearchSpace<int>, TestProblem>
    {
        protected override IEvaluationExecutor<int, DummySearchSpace<int>, TestProblem> CreateExecutor(ResolutionScope scope) => new Executor();

        private sealed class Executor : IEvaluationExecutor<int, DummySearchSpace<int>, TestProblem>
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random,
                DummySearchSpace<int> searchSpace, TestProblem problem) =>
                [.. candidates.Select(candidate => new ObjectiveVector(candidate))];
        }
    }

    private sealed class TestProblem : IProblem<int, DummySearchSpace<int>>
    {
        public static TestProblem Instance { get; } = new();
        public ObjectiveDirections Objective => SingleObjective.Minimize;
        public DummySearchSpace<int> SearchSpace => DummySearchSpace<int>.Instance;
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random) =>
            [.. candidates.Select(candidate => new ObjectiveVector(candidate))];
    }

    private sealed class UnknownDecisionRetention : TraceRetention
    {
        public override RetentionDecision Decide<T>(T value) => (RetentionDecision)99;
    }
}
