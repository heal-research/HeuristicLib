using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Analysis;

public class AnalysisUsabilityTests
{
    [Fact]
    public void SeparateRetentionObjects_CountIndependently()
    {
        var evaluator = new ManualEvaluator();
        var first = Analyzer.Trace(evaluator, observation => observation.ObjectiveVectors[0][0], retention: TraceRetention.EveryNth(2));
        var second = Analyzer.Trace(evaluator, observation => observation.ObjectiveVectors[0][0], retention: TraceRetention.EveryNth(2));
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
        var first = Analyzer.Trace(evaluator, observation => observation.ObjectiveVectors[0][0], retention: shared);
        var second = Analyzer.Trace(evaluator, observation => observation.ObjectiveVectors[0][0], retention: shared);
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
        var trace = Analyzer.Trace(
            [firstEvaluator, secondEvaluator],
            new ObjectiveVectorsFromEvaluationMeasurement<int>(),
            new CountAggregation());
        var scope = Install(trace);

        Evaluate(scope, firstEvaluator, 1, 2);
        Evaluate(scope, secondEvaluator, 3, 4);

        trace.Snapshot().Select(entry => entry.Value).ShouldBe([2, 4]);
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
        var first = Analyzer.Trace(evaluator, observation => observation.ObjectiveVectors.Count, clocks: [clock, clock]);
        var second = Analyzer.Trace(evaluator, observation => observation.ObjectiveVectors.Count, clocks: [clock]);
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
        var trace = Analyzer.Trace(evaluator, observation => observation.ObjectiveVectors[0][0],
            retention: new UnknownDecisionRetention());
        var scope = Install(trace);

        var exception = Should.Throw<InvalidOperationException>(() => Evaluate(scope, evaluator, 1));
        exception.Message.ShouldContain("unknown decision");
    }

    private static ResolutionScope Install(params IAnalyzer[] analyzers) =>
        ResolutionScope.Create(builder =>
        {
            foreach (var analyzer in analyzers)
                analyzer.Install(builder);
        });

    private static void Evaluate(ResolutionScope scope, ManualEvaluator evaluator, params int[] candidates) =>
        scope.Resolve<int, DummySearchSpace<int>, TestProblem>(evaluator).Evaluate(candidates, RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, TestProblem.Instance);

    private sealed class SeparateEvaluatorAnalyzer(ManualEvaluator first, ManualEvaluator second) : IAnalyzer
    {
        public int FirstCandidates { get; private set; }
        public int SecondCandidates { get; private set; }

        public void Install(ResolutionScopeBuilder builder)
        {
            builder.Observe(first, observation => FirstCandidates += observation.Candidates.Count);
            builder.Observe(second, observation => SecondCandidates += observation.Candidates.Count);
        }
    }

    private sealed record ManualEvaluator : Evaluator<int, DummySearchSpace<int>, TestProblem>
    {
        public override IEvaluatorInstance<int, DummySearchSpace<int>, TestProblem> CreateExecutionInstance(ResolutionScope scope) => new Instance();

        private sealed class Instance : IEvaluatorInstance<int, DummySearchSpace<int>, TestProblem>
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

    private sealed class CountAggregation : IAggregation<ObjectiveVector, int>
    {
        private int count;
        public int Aggregate(IReadOnlyList<ObjectiveVector> readings, ObjectiveDirections objective,
            IComparer<ObjectiveVector>? objectiveComparer = null) => count += readings.Count;
    }
}
