using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Selectors;

/// <summary>
/// Pins the child slot of the selector topology bases: the configured children are publicly inspectable and the
/// child collection compares by ordered structural equality rather than by array reference.
/// </summary>
public class SelectorConfigurationEqualityTests
{
    [Fact]
    public void WrappingSelector_ExposesConfiguredChildSelector()
    {
        var childSelector = new RangeSelector(1);

        var selector = childSelector.AvoidSameMates(maximumAttempts: 3);

        selector.ChildOperator.ShouldBeSameAs(childSelector);
    }

    [Fact]
    public void MultiSelector_ExposesConfiguredChildSelectors()
    {
        var first = new RangeSelector(1);
        var second = new RangeSelector(2);

        var selector = ChoosePerInvocation.Create(first, second);

        selector.ChildOperators.ShouldBe([first, second]);
    }

    [Fact]
    public void MultiSelector_SnapshotsConfiguredChildSelectors()
    {
        var first = new RangeSelector(1);
        var second = new RangeSelector(2);
        var childSelectors = new List<IOperator<Selection, int>> { first, second };
        var selector = new FirstOfSelector(childSelectors);

        childSelectors.Clear();

        selector.ChildOperators.ShouldBe([first, second]);
    }

    [Fact]
    public void MultiSelector_WithSeparatelyBuiltEqualChildCollections_IsEqual()
    {
        var left = ChoosePerInvocation.Create(new RangeSelector(1), new RangeSelector(2));
        var right = ChoosePerInvocation.Create(new RangeSelector(1), new RangeSelector(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void MultiSelector_WithDifferentChildSelectors_IsNotEqual()
    {
        var left = ChoosePerInvocation.Create(new RangeSelector(1), new RangeSelector(2));
        var right = ChoosePerInvocation.Create(new RangeSelector(1), new RangeSelector(3));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void MultiSelector_WithReorderedChildSelectors_IsNotEqual()
    {
        var left = ChoosePerInvocation.Create(new RangeSelector(1), new RangeSelector(2));
        var right = ChoosePerInvocation.Create(new RangeSelector(2), new RangeSelector(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void MultiSelector_WithDifferentChildCount_IsNotEqual()
    {
        var left = ChoosePerInvocation.Create(new RangeSelector(1), new RangeSelector(2));
        var right = ChoosePerInvocation.Create(new RangeSelector(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ChoosePerInvocationSelector_WithDifferentWeights_IsNotEqual()
    {
        IOperator<Selection, int>[] childSelectors = [new RangeSelector(1), new RangeSelector(2)];
        var left = new ChoosePerInvocation<Selection, int>(childSelectors) { Weights = [1.0, 2.0] };
        var right = new ChoosePerInvocation<Selection, int>(childSelectors) { Weights = [2.0, 1.0] };

        left.ShouldNotBe(right);
    }

    [Fact]
    public void MultiSelectorTopologies_WithEqualChildSelectors_AreNotEqualAcrossTypes()
    {
        var chooseOne = ChoosePerInvocation.Create(new RangeSelector(1), new RangeSelector(2));
        var firstOf = new FirstOfSelector([new RangeSelector(1), new RangeSelector(2)]);

        chooseOne.ShouldNotBe<object>(firstOf);
    }

    /// <summary>
    /// Operators whose children carry a specific role declare those children themselves instead of deriving from a
    /// multi base, so their equality comes from the named properties rather than from an ordered child collection.
    /// </summary>
    [Fact]
    public void RoleBearingChildren_WithEqualChildSelectors_IsEqual()
    {
        var left = GenderSpecificSelector.Create(new RangeSelector(1), new RangeSelector(2));
        var right = GenderSpecificSelector.Create(new RangeSelector(1), new RangeSelector(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void RoleBearingChildren_WithSwappedChildSelectors_IsNotEqual()
    {
        var left = GenderSpecificSelector.Create(new RangeSelector(1), new RangeSelector(2));
        var right = GenderSpecificSelector.Create(new RangeSelector(2), new RangeSelector(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void WrappingSelector_WithEqualChildSelector_IsEqual()
    {
        var left = new RangeSelector(1).AvoidSameMates(maximumAttempts: 3);
        var right = new RangeSelector(1).AvoidSameMates(maximumAttempts: 3);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void WrappingSelector_WithDifferentChildSelector_IsNotEqual()
    {
        var left = new RangeSelector(1).AvoidSameMates(maximumAttempts: 3);
        var right = new RangeSelector(2).AvoidSameMates(maximumAttempts: 3);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void EliteSelector_WithDifferentChildSelector_IsNotEqual()
    {
        var left = new RangeSelector(1).CombinedWithElites(2);
        var right = new RangeSelector(2).CombinedWithElites(2);

        left.ShouldNotBe(right);
    }











    [Fact]
    public void NestedSelectorComposition_WithEqualParts_IsEqual()
    {
        var left = ChoosePerInvocation.Create(new RangeSelector(1).CombinedWithElites(2), new RangeSelector(2));
        var right = ChoosePerInvocation.Create(new RangeSelector(1).CombinedWithElites(2), new RangeSelector(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void NestedSelectorComposition_WithDifferentNestedChildSelector_IsNotEqual()
    {
        var left = ChoosePerInvocation.Create(new RangeSelector(1).CombinedWithElites(2), new RangeSelector(2));
        var right = ChoosePerInvocation.Create(new RangeSelector(9).CombinedWithElites(2), new RangeSelector(2));

        left.ShouldNotBe(right);
    }

    private sealed record RangeSelector(int Offset) : StatelessSelector<int, DummySearchSpace<int>>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Select(IReadOnlyList<EvaluatedCandidate<int>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) =>
            population.Skip(Offset).Take(count).ToArray();
    }

    private sealed record FirstOfSelector
        : MultiOperator<Selection, int>
    {
        public FirstOfSelector(IReadOnlyList<IOperator<Selection, int>> childSelectors)
            : base(childSelectors)
        {
        }

        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<int, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
        {
            ImmutableArray<ISelectionExecutor<int, TRunSearchSpace, TRunProblem>> children = [.. ChildOperators.Select(child => scope.Resolve(child))];
            return new Executor<TRunSearchSpace, TRunProblem>(children);
        }

        private sealed class Executor<TSearchSpace, TProblem>(ImmutableArray<ISelectionExecutor<int, TSearchSpace, TProblem>> childSelectors)
            : ISelectionExecutor<int, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public IReadOnlyList<EvaluatedCandidate<int>> Select(IReadOnlyList<EvaluatedCandidate<int>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                childSelectors[0].Select(population, objective, count, random, searchSpace, problem);
        }
    }
}
