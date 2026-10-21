using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Creators;

/// <summary>
/// Pins the child slot of the creator topology bases: the configured children are publicly inspectable and the
/// child collection compares by ordered structural equality rather than by array reference.
/// </summary>
public class CreatorConfigurationEqualityTests
{
    [Fact]
    public void WrappingCreator_ExposesConfiguredChildCreator()
    {
        var childCreator = new ConstantCreator(1);

        var creator = new IdentityCreator(childCreator);

        creator.ChildOperator.ShouldBeSameAs(childCreator);
    }

    [Fact]
    public void MultiCreator_ExposesConfiguredChildCreators()
    {
        var first = new ConstantCreator(1);
        var second = new ConstantCreator(2);

        var creator = ChoosePerCandidate.Create(first, second);

        creator.ChildOperators.ShouldBe([first, second]);
    }

    [Fact]
    public void MultiCreator_SnapshotsConfiguredChildCreators()
    {
        var first = new ConstantCreator(1);
        var second = new ConstantCreator(2);
        var childCreators = new List<IOperator<Creation, int>> { first, second };
        var creator = new FirstOfCreator(childCreators);

        childCreators.Clear();

        creator.ChildOperators.ShouldBe([first, second]);
    }

    [Fact]
    public void MultiCreator_WithSeparatelyBuiltEqualChildCollections_IsEqual()
    {
        var left = ChoosePerCandidate.Create(new ConstantCreator(1), new ConstantCreator(2));
        var right = ChoosePerCandidate.Create(new ConstantCreator(1), new ConstantCreator(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void MultiCreator_WithDifferentChildCreators_IsNotEqual()
    {
        var left = ChoosePerCandidate.Create(new ConstantCreator(1), new ConstantCreator(2));
        var right = ChoosePerCandidate.Create(new ConstantCreator(1), new ConstantCreator(3));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void MultiCreator_WithReorderedChildCreators_IsNotEqual()
    {
        var left = ChoosePerCandidate.Create(new ConstantCreator(1), new ConstantCreator(2));
        var right = ChoosePerCandidate.Create(new ConstantCreator(2), new ConstantCreator(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void MultiCreator_WithDifferentChildCount_IsNotEqual()
    {
        var left = ChoosePerCandidate.Create(new ConstantCreator(1), new ConstantCreator(2));
        var right = ChoosePerCandidate.Create(new ConstantCreator(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ChoosePerCandidateCreation_WithDifferentWeights_IsNotEqual()
    {
        IOperator<Creation, int>[] childCreators = [new ConstantCreator(1), new ConstantCreator(2)];
        var left = new ChoosePerCandidate<Creation, int>(childCreators) { Weights = [1.0, 2.0] };
        var right = new ChoosePerCandidate<Creation, int>(childCreators) { Weights = [2.0, 1.0] };

        left.ShouldNotBe(right);
    }

    /// <summary>
    /// Omitted weights are retained as an empty collection meaning uniform selection, so an explicitly uniform
    /// configuration stays distinguishable from an unweighted one.
    /// </summary>
    [Fact]
    public void ChoosePerCandidateCreation_WithOmittedWeights_IsNotEqualToExplicitUniformWeights()
    {
        IOperator<Creation, int>[] childCreators = [new ConstantCreator(1), new ConstantCreator(2)];
        var omitted = new ChoosePerCandidate<Creation, int>(childCreators);
        var explicitUniform = new ChoosePerCandidate<Creation, int>(childCreators) { Weights = [0.5, 0.5] };

        omitted.Weights.ShouldBeEmpty();
        omitted.ShouldNotBe(explicitUniform);
    }

    [Fact]
    public void MultiCreatorTopologies_WithEqualChildCreators_AreNotEqualAcrossTypes()
    {
        var chooseOne = ChoosePerCandidate.Create(new ConstantCreator(1), new ConstantCreator(2));
        var firstOf = new FirstOfCreator([new ConstantCreator(1), new ConstantCreator(2)]);

        chooseOne.ShouldNotBe<object>(firstOf);
    }

    /// <summary>
    /// Operators whose children carry a specific role declare those children themselves instead of deriving from a
    /// topology base, so their equality comes from the named properties.
    /// </summary>
    [Fact]
    public void PredefinedCandidatesCreator_ExposesItsFallbackCreatorUnderOneName()
    {
        var fallback = new ConstantCreator(1);

        var creator = fallback.SeededWith([7, 8]);

        creator.CreatorForRemainingCandidates.ShouldBeSameAs(fallback);
        creator.PredefinedCandidates.ShouldBe([7, 8]);
    }

    [Fact]
    public void PredefinedCandidatesCreator_WithEqualParts_IsEqual()
    {
        var left = new ConstantCreator(1).SeededWith([7, 8]);
        var right = new ConstantCreator(1).SeededWith([7, 8]);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void PredefinedCandidatesCreator_WithDifferentFallbackCreator_IsNotEqual()
    {
        var left = new ConstantCreator(1).SeededWith([7, 8]);
        var right = new ConstantCreator(2).SeededWith([7, 8]);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void PredefinedCandidatesCreator_SnapshotsPredefinedCandidates()
    {
        var predefined = new List<int> { 7, 8 };
        var creator = new ConstantCreator(1).SeededWith(predefined);

        predefined.Clear();

        creator.PredefinedCandidates.ShouldBe([7, 8]);
    }

    [Fact]
    public void TransformedCreator_ExposesItsSourceAndTransformation()
    {
        var source = new ConstantCreator(1);
        var transformation = new AddOneMutator();

        var creator = source.TransformWith(transformation);

        creator.SourceCreator.ShouldBeSameAs(source);
        creator.TransformationMutator.ShouldBeSameAs(transformation);
    }











    [Fact]
    public void SingleCandidateCreator_WithDifferentConcurrency_IsNotEqual()
    {
        var left = new ConstantCreator(1);
        var right = left with { Concurrency = ExecutionConcurrency.Concurrent(2) };

        left.ShouldNotBe(right);
    }

    [Fact]
    public void NestedCreatorComposition_WithEqualParts_IsEqual()
    {
        var left = ChoosePerCandidate.Create(new ConstantCreator(1).TransformWith(new AddOneMutator()), new ConstantCreator(2));
        var right = ChoosePerCandidate.Create(new ConstantCreator(1).TransformWith(new AddOneMutator()), new ConstantCreator(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void NestedCreatorComposition_WithDifferentNestedChildCreator_IsNotEqual()
    {
        var left = ChoosePerCandidate.Create(new ConstantCreator(1).TransformWith(new AddOneMutator()), new ConstantCreator(2));
        var right = ChoosePerCandidate.Create(new ConstantCreator(9).TransformWith(new AddOneMutator()), new ConstantCreator(2));

        left.ShouldNotBe(right);
    }

    private sealed record ConstantCreator(int Value) : SingleCandidateCreator<int, DummySearchSpace<int>>
    {
        public override int CreateCandidate(IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => Value;
    }

    private sealed record AddOneMutator : SingleCandidateMutator<int, DummySearchSpace<int>>
    {
        public override int MutateCandidate(int parent, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => parent + 1;
    }

    private sealed record FirstOfCreator
        : MultiOperator<Creation, int>
    {
        public FirstOfCreator(IReadOnlyList<IOperator<Creation, int>> childCreators)
            : base(childCreators)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(this);

        private sealed class Factory(FirstOfCreator source) : OperatorExecutorFactory<Creation, int>
        {
            protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
                ResolutionScope<int, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
            {
                ImmutableArray<ICreationExecutor<int, TRunSearchSpace, TRunProblem>> children = [.. source.ChildOperators.Select(child => scope.Resolve(child))];
                return new Executor<TRunSearchSpace, TRunProblem>(children);
            }
        }

        private sealed class Executor<TSearchSpace, TProblem>(ImmutableArray<ICreationExecutor<int, TSearchSpace, TProblem>> childCreators)
            : ICreationExecutor<int, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public IReadOnlyList<int> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                childCreators[0].Create(count, random, searchSpace, problem);
        }
    }
    private sealed record IdentityCreator : WrappingOperator<Creation, int>
    {
        public IdentityCreator(IOperator<Creation, int> child) : base(child)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory() => OperatorExecutorFactory.Forward(ChildOperator);
    }

}
