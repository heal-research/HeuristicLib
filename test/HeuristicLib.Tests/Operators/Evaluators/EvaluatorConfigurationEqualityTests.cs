using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Evaluators;

public class EvaluatorConfigurationEqualityTests
{
    [Fact]
    public void WrappingEvaluator_ExposesConfiguredChildEvaluator()
    {
        var child = new OffsetEvaluator(1);

        new IdentityEvaluator(child).ChildOperator.ShouldBeSameAs(child);
    }

    [Fact]
    public void MultiEvaluator_ExposesAndSnapshotsConfiguredChildEvaluators()
    {
        var first = new OffsetEvaluator(1);
        var second = new OffsetEvaluator(2);
        var children = new List<IOperator<Evaluation, int>> { first, second };
        var evaluator = new FirstOfEvaluator(children);

        children.Clear();

        evaluator.ChildOperators.ShouldBe([first, second]);
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
    public void ConcreteEvaluatorSettings_ArePubliclyReconfigurable()
    {
        var keySelector = new RemainderCacheKeySelector(3);
        var cache = new CachingEvaluator<int, int>(new OffsetEvaluator(1), keySelector) { SizeLimit = 10 };
        var limit = new LimitEvaluator<int>(new OffsetEvaluator(1), 5) { EnforceLimitWithinBatch = true };
        var repeating = new RepeatingEvaluator<int>(new OffsetEvaluator(1), 2);

        cache.KeySelector.ShouldBe(keySelector);
        cache.ShouldBe(new CachingEvaluator<int, int>(new OffsetEvaluator(1), new RemainderCacheKeySelector(3)) { SizeLimit = 10 });
        new CachingEvaluator<int>(new OffsetEvaluator(1)).KeySelector
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
        : MultiOperator<Evaluation, int>
    {
        public FirstOfEvaluator(IReadOnlyList<IOperator<Evaluation, int>> childEvaluators)
            : base(childEvaluators)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory()
        {
            return new Factory(this);
        }

        private sealed class Factory(FirstOfEvaluator source) : OperatorExecutorFactory<Evaluation, int>
        {
            protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
                ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope)
            {
                System.Collections.Immutable.ImmutableArray<IEvaluationExecutor<int, TSearchSpace, TProblem>> children = [.. source.ChildOperators.Select(child => scope.Resolve(child))];
                return new Executor<TSearchSpace, TProblem>(children);
            }
        }

        private sealed class Executor<TSearchSpace, TProblem>(ImmutableArray<IEvaluationExecutor<int, TSearchSpace, TProblem>> childEvaluators)
            : MultiEvaluationExecutor<int, TSearchSpace, TProblem>(childEvaluators)
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                ChildEvaluators[0].Evaluate(candidates, random, searchSpace, problem);
        }
    }
    private sealed record IdentityEvaluator : WrappingOperator<Evaluation, int>
    {
        public IdentityEvaluator(IOperator<Evaluation, int> child) : base(child)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory() => OperatorExecutorFactory.Forward(ChildOperator);
    }

}
