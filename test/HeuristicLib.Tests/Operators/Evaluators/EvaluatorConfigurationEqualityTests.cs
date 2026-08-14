using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Evaluators;

public class EvaluatorConfigurationEqualityTests
{
    [Fact]
    public void WrappingEvaluator_ExposesConfiguredChildEvaluator()
    {
        var child = new OffsetEvaluator(1);

        child.CountEvaluatorCalls(new ObservationCounter()).ChildEvaluator.ShouldBeSameAs(child);
    }

    [Fact]
    public void MultiEvaluator_ExposesAndSnapshotsConfiguredChildEvaluators()
    {
        var first = new OffsetEvaluator(1);
        var second = new OffsetEvaluator(2);
        var children = new List<IEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> { first, second };
        var evaluator = new FirstOfEvaluator(children);

        children.Clear();

        evaluator.ChildEvaluators.ShouldBe([first, second]);
    }

    [Fact]
    public void MultiEvaluator_UsesOrderedStructuralEquality()
    {
        var left = new FirstOfEvaluator([new OffsetEvaluator(1), new OffsetEvaluator(2)]);
        var equal = new FirstOfEvaluator([new OffsetEvaluator(1), new OffsetEvaluator(2)]);
        var reordered = new FirstOfEvaluator([new OffsetEvaluator(2), new OffsetEvaluator(1)]);

        left.ShouldBe(equal);
        left.GetHashCode().ShouldBe(equal.GetHashCode());
        left.ShouldNotBe(reordered);
    }

    [Fact]
    public void WrappingConcerns_IncludeChildAndSettingsInEquality()
    {
        var counter = new ObservationCounter();
        var duration = new ObservationDuration();
        var counted = new OffsetEvaluator(1).CountEvaluatorCalls(counter);
        var countedEqual = new OffsetEvaluator(1).CountEvaluatorCalls(counter);
        var differentMetric = new OffsetEvaluator(1).CountEvaluatedCandidates(counter);
        var measured = new OffsetEvaluator(1).MeasureEvaluatorDuration(duration, TimeProvider.System);
        var measuredEqual = new OffsetEvaluator(1).MeasureEvaluatorDuration(duration, TimeProvider.System);

        counted.ShouldBe(countedEqual);
        counted.ShouldNotBe(differentMetric);
        measured.ShouldBe(measuredEqual);
    }

    [Fact]
    public void ObservableEvaluator_SnapshotsObserversAndUsesTheirIdentityInEquality()
    {
        var observer = new ActionEvaluatorObserver<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>((_, _, _, _) => { });
        var observers = new List<IEvaluatorObserver<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> { observer };
        var left = new ObservableEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(new OffsetEvaluator(1), observers);
        var equal = new ObservableEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(new OffsetEvaluator(1), observer);

        observers.Clear();

        left.Observers.ShouldBe([observer]);
        left.ShouldBe(equal);
    }

    [Fact]
    public void ConcreteEvaluatorSettings_ArePubliclyReconfigurable()
    {
        var keySelector = new RemainderCacheKeySelector(3);
        var cache = new CachingEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, int>(new OffsetEvaluator(1), keySelector) { SizeLimit = 10 };
        var limit = new LimitEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(new OffsetEvaluator(1), 5) { EnforceLimitWithinBatch = true };
        var repeating = new RepeatingEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(new OffsetEvaluator(1), 2);

        cache.KeySelector.ShouldBe(keySelector);
        cache.ShouldBe(new CachingEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, int>(new OffsetEvaluator(1), new RemainderCacheKeySelector(3)) { SizeLimit = 10 });
        new CachingEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(new OffsetEvaluator(1)).KeySelector
            .ShouldBeSameAs(CacheKeySelection<int>.Identity);
        cache.SizeLimit.ShouldBe(10);
        (limit with { MaxEvaluations = 7 }).ShouldNotBe(limit);
        repeating.Repetitions.ShouldBe(2);
        repeating.Aggregator.ShouldBe(ObjectiveVectorAggregation.Mean);
    }

    [Fact]
    public void SingleCandidateEvaluator_WithDifferentConcurrency_IsNotEqual()
    {
        var left = new OffsetEvaluator(1);
        var right = left with { Concurrency = ExecutionConcurrency.Concurrent(2) };

        left.ShouldNotBe(right);
    }

    private sealed record OffsetEvaluator(int Offset) : SingleCandidateEvaluator<int, DummySearchSpace<int>>
    {
        public override ObjectiveVector EvaluateCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) =>
            new(candidate + Offset);
    }

    private sealed record RemainderCacheKeySelector(int Divisor) : ICacheKeySelector<int, int>
    {
        public int SelectKey(int candidate) => candidate % Divisor;
    }

    private sealed record FirstOfEvaluator
        : MultiEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        public FirstOfEvaluator(IReadOnlyList<IEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> childEvaluators)
            : base(childEvaluators)
        {
        }

        protected override MultiEvaluatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> CreateExecutionInstance(ImmutableArray<IEvaluatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> childEvaluators) =>
            new Instance(childEvaluators);

        private sealed class Instance(ImmutableArray<IEvaluatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> childEvaluators)
            : MultiEvaluatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(childEvaluators)
        {
            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) =>
                ChildEvaluators[0].Evaluate(candidates, random, searchSpace, problem);
        }
    }
}
