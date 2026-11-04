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
        var first = evaluator.Invocations.Trace(observation => observation.ObjectiveVectors[0][0], retention: TraceRetention.EveryNth(2));
        var second = evaluator.Invocations.Trace(observation => observation.ObjectiveVectors[0][0], retention: TraceRetention.EveryNth(2));
        var scope = Install(first, second);

        foreach (var candidate in Enumerable.Range(1, 5))
            Evaluate(scope, evaluator, candidate);

        scope.Results.Result(first).Snapshot().Select(entry => entry.Value).ShouldBe([2, 4]);
        scope.Results.Result(second).Snapshot().Select(entry => entry.Value).ShouldBe([2, 4]);
    }

    /// <summary>
    /// A retention is an immutable policy: each trace keeps its own progress, so handing one retention to two traces
    /// does not couple them.
    /// </summary>
    [Fact]
    public void OneRetentionObjectSharedByTwoTraces_CountsIndependently()
    {
        var evaluator = new ManualEvaluator();
        var shared = TraceRetention.EveryNth(2);
        var first = evaluator.Invocations.Trace(observation => observation.ObjectiveVectors[0][0], retention: shared);
        var second = evaluator.Invocations.Trace(observation => observation.ObjectiveVectors[0][0], retention: shared);
        var scope = Install(first, second);

        foreach (var candidate in Enumerable.Range(1, 5))
            Evaluate(scope, evaluator, candidate);

        scope.Results.Result(first).Snapshot().Select(entry => entry.Value).ShouldBe([2, 4]);
        scope.Results.Result(second).Snapshot().Select(entry => entry.Value).ShouldBe([2, 4]);
    }

    [Fact]
    public void OneTrace_CanObserveSeveralEvaluators()
    {
        var firstEvaluator = new ManualEvaluator();
        var secondEvaluator = new ManualEvaluator();
        var count = 0;
        var trace = Analyzer.Trace([firstEvaluator.Invocations, secondEvaluator.Invocations], observation => count += observation.ObjectiveVectors.Count);
        var scope = Install(trace);

        Evaluate(scope, firstEvaluator, 1, 2);
        Evaluate(scope, secondEvaluator, 3, 4);

        scope.Results.Result(trace).Snapshot().Select(entry => entry.Value).ShouldBe([2, 4]);
    }

    [Fact]
    public void OneTrace_ObservesASourceOnceWhenGivenItTwice()
    {
        var evaluator = new ManualEvaluator();
        var count = 0;
        var trace = Analyzer.Trace([evaluator.Invocations, evaluator.Invocations], observation => count += observation.ObjectiveVectors.Count);
        var scope = Install(trace);

        Evaluate(scope, evaluator, 1, 2);

        scope.Results.Result(trace).Snapshot().Select(entry => entry.Value).ShouldBe([2]);
    }

    [Fact]
    public void TracesOfEachOperationOfOneConfiguration_ObserveTheirOwnCalls()
    {
        var source = new SharedOperations(new ProbeLog());
        IOperator<Mutation, RealVector> mutator = source;
        IOperator<Evaluation, RealVector> evaluator = source;
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var mutations = mutator.Invocations.Trace(observation => observation.Offspring.Count);
        var evaluations = evaluator.Invocations.Trace(observation => observation.Candidates.Count);
        var root = Install(mutations, evaluations);
        var scope = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();

        scope.Resolve(mutator).Mutate([new RealVector(2.0)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        scope.Resolve(evaluator).Evaluate([new RealVector(2.0), new RealVector(3.0)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        root.Results.Result(mutations).Snapshot().Select(entry => entry.Value).ShouldBe([1]);
        root.Results.Result(evaluations).Snapshot().Select(entry => entry.Value).ShouldBe([2]);
    }

    // The scopes of one run share its trace, so calls from concurrent child scopes, such as parallel contenders, record
    // into one trace whose function sees one observation at a time.
    [Fact]
    public async Task OneRunsTrace_SerializesItsFunctionAcrossConcurrentScopes()
    {
        var evaluator = new ManualEvaluator();
        var activeCalls = 0;
        var overlappingCalls = 0;
        var count = 0;
        var trace = evaluator.Invocations.Trace(observation =>
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
        var root = Install(trace);
        using var start = new Barrier(4);
        var workers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            var scope = root.CreateChildScope();
            start.SignalAndWait(TestContext.Current.CancellationToken);
            for (var i = 0; i < 8; i++)
                Evaluate(scope, evaluator, i);
        }, TestContext.Current.CancellationToken)).ToArray();

        await Task.WhenAll(workers);

        overlappingCalls.ShouldBe(0);
        root.Results.Result(trace).Snapshot().Select(entry => entry.Value).ShouldBe(Enumerable.Range(1, 32));
    }

    [Fact]
    public void Analyzer_CanUseDifferentCallbacksForTheSameOperatorRole()
    {
        var firstEvaluator = new ManualEvaluator();
        var secondEvaluator = new ManualEvaluator();
        var analyzer = new SeparateEvaluatorAnalyzer(firstEvaluator, secondEvaluator);
        var scope = ResolutionScope.Create();
        scope.AddAspect(analyzer);

        Evaluate(scope, firstEvaluator, 1, 2);
        Evaluate(scope, secondEvaluator, 3, 4, 5);

        analyzer.FirstCandidates.ShouldBe(2);
        analyzer.SecondCandidates.ShouldBe(3);
    }

    [Fact]
    public void SharedClock_IsInstalledOncePerScope()
    {
        var evaluator = new ManualEvaluator();
        var clock = evaluator.Invocations.CountCandidates();
        var first = evaluator.Invocations.Trace(observation => observation.ObjectiveVectors.Count, clocks: [clock, clock]);
        var second = evaluator.Invocations.Trace(observation => observation.ObjectiveVectors.Count, clocks: [clock]);
        var scope = Install(first, second);

        Evaluate(scope, evaluator, 1, 2);
        Evaluate(scope, evaluator, 3, 4, 5);

        scope.Results.Result(first).By(clock).Select(point => point.Time).ShouldBe([2L, 5L]);
        scope.Results.Result(second).By(clock).Select(point => point.Time).ShouldBe([2L, 5L]);
        scope.Results.Result(clock).CurrentCount.ShouldBe(5);
    }

    [Fact]
    public void RetentionOnChange_HandlesNullAndUsesTypedEquality()
    {
        var retention = TraceRetention.OnChange();
        var strings = retention.CreateDecider<string?>();
        strings.Decide(null).ShouldBe(RetentionDecision.Append);
        strings.Decide(null).ShouldBe(RetentionDecision.Skip);
        strings.Decide("value").ShouldBe(RetentionDecision.Append);
        strings.Decide("value").ShouldBe(RetentionDecision.Skip);
        strings.Decide(null).ShouldBe(RetentionDecision.Append);
        var numbers = retention.CreateDecider<int>();
        numbers.Decide(0).ShouldBe(RetentionDecision.Append);
        numbers.Decide(0).ShouldBe(RetentionDecision.Skip);
    }

    [Fact]
    public void RetentionOnChange_CanBeGivenAComparer()
    {
        var retention = TraceRetention.OnChange(StringComparer.OrdinalIgnoreCase).CreateDecider<string>();
        retention.Decide("value").ShouldBe(RetentionDecision.Append);
        retention.Decide("VALUE").ShouldBe(RetentionDecision.Skip);
        retention.Decide("other").ShouldBe(RetentionDecision.Append);
    }

    [Fact]
    public void ARetentionReturningAnUnknownDecision_FailsWhereItIsUsed()
    {
        var evaluator = new ManualEvaluator();
        var trace = evaluator.Invocations.Trace(observation => observation.ObjectiveVectors[0][0],
            retention: new UnknownDecisionRetention());
        var scope = Install(trace);

        var exception = Should.Throw<InvalidOperationException>(() => Evaluate(scope, evaluator, 1));
        exception.Message.ShouldContain("unknown decision");
    }

    private static ResolutionScope Install(params Instrument[] instruments)
    {
        var scope = ResolutionScope.Create();
        foreach (var instrument in instruments)
            scope.AddAspect(scope.Results.Aspect(instrument));
        return scope;
    }

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
                .. Aspect.Observe(first.Invocations, observation => FirstCandidates += observation.Candidates.Count),
                .. Aspect.Observe(second.Invocations, observation => SecondCandidates += observation.Candidates.Count)
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
        public override RetentionDecider<TValue> CreateDecider<TValue>() => new Decider<TValue>();

        private sealed class Decider<TValue> : RetentionDecider<TValue>
        {
            public override RetentionDecision Decide(TValue value) => (RetentionDecision)99;
        }
    }
}
