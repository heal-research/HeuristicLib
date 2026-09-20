using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Analysis;

public class AnalysisUsabilityTests
{
    [Fact]
    public void SharedRetentionConfiguration_CreatesIndependentTraceState()
    {
        var evaluator = new ManualEvaluator();
        var retention = TraceRetention.EveryNth(2);
        var first = Analyzer.Trace(evaluator, observation => observation.ObjectiveVectors[0][0], retention: retention);
        var second = Analyzer.Trace(evaluator, observation => observation.ObjectiveVectors[0][0], retention: retention);
        var scope = Install(first, second);

        foreach (var candidate in Enumerable.Range(1, 5))
            Evaluate(scope, evaluator, candidate);

        first.Snapshot().Select(entry => entry.Value).ShouldBe([2, 4]);
        second.Snapshot().Select(entry => entry.Value).ShouldBe([2, 4]);
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
    public void Scope_UsesConfigurationIdentityForAggregationAndRetention()
    {
        var scope = ResolutionScope.Create();
        var aggregation = Aggregate.BestSoFar();
        scope.Resolve(aggregation).ShouldBeSameAs(scope.Resolve(aggregation));
        scope.Resolve(aggregation).ShouldNotBeSameAs(scope.Resolve(Aggregate.BestSoFar()));
        var retention = TraceRetention.EveryNth(2);
        scope.Resolve(retention).ShouldBeSameAs(scope.Resolve(retention));
        scope.Resolve(retention).ShouldNotBeSameAs(ResolutionScope.Create().Resolve(retention));
    }

    [Fact]
    public void RetentionOnChange_HandlesNullAndUsesTypedEquality()
    {
        var retention = ResolutionScope.Create().Resolve(TraceRetention.OnChange());
        retention.ShouldRetain<string?>(null).ShouldBeTrue();
        retention.ShouldRetain<string?>(null).ShouldBeFalse();
        retention.ShouldRetain("value").ShouldBeTrue();
        retention.ShouldRetain("value").ShouldBeFalse();
        retention.ShouldRetain<string?>(null).ShouldBeTrue();
        retention.ShouldRetain(0).ShouldBeTrue();
        retention.ShouldRetain(0).ShouldBeFalse();
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

    private sealed record CountAggregation : IAggregation<ObjectiveVector, int>
    {
        public IAggregationInstance<ObjectiveVector, int> CreateExecutionInstance(ResolutionScope scope) => new Instance();

        private sealed class Instance : IAggregationInstance<ObjectiveVector, int>
        {
            private int count;
            public int Aggregate(IReadOnlyList<ObjectiveVector> readings, ObjectiveDirections objective,
                IComparer<ObjectiveVector>? objectiveComparer = null) => count += readings.Count;
        }
    }
}
