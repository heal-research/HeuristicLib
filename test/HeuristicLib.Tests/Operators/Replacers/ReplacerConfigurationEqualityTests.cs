using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Replacers;

public class ReplacerConfigurationEqualityTests
{
    [Fact]
    public void WrappingReplacer_ExposesConfiguredChildReplacer()
    {
        var child = new OffsetReplacer(1);

        new IdentityReplacer(child).ChildOperator.ShouldBeSameAs(child);
    }

    [Fact]
    public void MultiReplacer_ExposesAndSnapshotsConfiguredChildReplacers()
    {
        var first = new OffsetReplacer(1);
        var second = new OffsetReplacer(2);
        var children = new List<IOperator<Replacement, int>> { first, second };
        var replacer = new FirstOfReplacer(children);

        children.Clear();

        replacer.ChildOperators.ShouldBe([first, second]);
    }

    [Fact]
    public void MultiReplacer_UsesOrderedStructuralEquality()
    {
        var left = ChoosePerCall.Create(new OffsetReplacer(1), new OffsetReplacer(2));
        var equal = ChoosePerCall.Create(new OffsetReplacer(1), new OffsetReplacer(2));
        var reordered = ChoosePerCall.Create(new OffsetReplacer(2), new OffsetReplacer(1));

        left.ShouldBe(equal);
        left.GetHashCode().ShouldBe(equal.GetHashCode());
        left.ShouldNotBe(reordered);
    }

    [Fact]
    public void ChoosePerCallReplacer_WithDifferentWeights_IsNotEqual()
    {
        IOperator<Replacement, int>[] children = [new OffsetReplacer(1), new OffsetReplacer(2)];
        var left = new ChoosePerCall<Replacement, int>(children) { Weights = [1.0, 2.0] };
        var right = left with { Weights = [2.0, 1.0] };

        left.ShouldNotBe(right);
    }



    [Fact]
    public void ParetoCrowdingReplacer_WithReconfiguredSetting_IsNotEqual()
    {
        var left = new ParetoCrowdingReplacer<int>(false);
        var right = left with { DominateOnEqualities = true };

        left.ShouldNotBe(right);
        right.DominateOnEqualities.ShouldBeTrue();
    }

    private sealed record OffsetReplacer(int Offset) : StatelessReplacer<int, DummySearchSpace<int>>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Replace(IReadOnlyList<EvaluatedCandidate<int>> previousPopulation, IReadOnlyList<EvaluatedCandidate<int>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) =>
            previousPopulation.Concat(offspringPopulation).Skip(Offset).Take(count).ToArray();
    }

    private sealed record FirstOfReplacer
        : MultiOperator<Replacement, int>
    {
        public FirstOfReplacer(IReadOnlyList<IOperator<Replacement, int>> childReplacers)
            : base(childReplacers)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(this);

        private sealed class Factory(FirstOfReplacer source) : OperatorExecutorFactory<Replacement, int>
        {
            protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
                ResolutionScope<int, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
            {
                ImmutableArray<IReplacementExecutor<int, TRunSearchSpace, TRunProblem>> children = [.. source.ChildOperators.Select(child => scope.Resolve(child))];
                return new Executor<TRunSearchSpace, TRunProblem>(children);
            }
        }

        private sealed class Executor<TSearchSpace, TProblem>(ImmutableArray<IReplacementExecutor<int, TSearchSpace, TProblem>> childReplacers)
            : IReplacementExecutor<int, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public IReadOnlyList<EvaluatedCandidate<int>> Replace(IReadOnlyList<EvaluatedCandidate<int>> previousPopulation, IReadOnlyList<EvaluatedCandidate<int>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                childReplacers[0].Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
        }
    }
    private sealed record IdentityReplacer : WrappingOperator<Replacement, int>
    {
        public IdentityReplacer(IOperator<Replacement, int> child) : base(child)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory() => OperatorExecutorFactory.Forward(ChildOperator);
    }

}
