using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Crossovers;

/// <summary>
/// Pins the child slot of the crossover topology bases: the configured children are publicly inspectable and the
/// child collection compares by ordered structural equality rather than by array reference.
/// </summary>
public class CrossoverConfigurationEqualityTests
{
    [Fact]
    public void WrappingCrossover_ExposesConfiguredChildCrossover()
    {
        var childCrossover = new OffsetCrossover(1);

        var crossover = new IdentityCrossover(childCrossover);

        crossover.ChildOperator.ShouldBeSameAs(childCrossover);
    }

    [Fact]
    public void MultiCrossover_ExposesConfiguredChildCrossovers()
    {
        var first = new OffsetCrossover(1);
        var second = new OffsetCrossover(2);

        var crossover = ChoosePerItem.Create(first, second);

        crossover.ChildOperators.ShouldBe([first, second]);
    }

    [Fact]
    public void MultiCrossover_SnapshotsConfiguredChildCrossovers()
    {
        var first = new OffsetCrossover(1);
        var second = new OffsetCrossover(2);
        var childCrossovers = new List<IOperator<Crossover, int>> { first, second };
        var crossover = new FirstOfCrossover(childCrossovers);

        childCrossovers.Clear();

        crossover.ChildOperators.ShouldBe([first, second]);
    }

    [Fact]
    public void MultiCrossover_WithSeparatelyBuiltEqualChildCollections_IsEqual()
    {
        var left = ChoosePerItem.Create(new OffsetCrossover(1), new OffsetCrossover(2));
        var right = ChoosePerItem.Create(new OffsetCrossover(1), new OffsetCrossover(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void MultiCrossover_WithDifferentChildCrossovers_IsNotEqual()
    {
        var left = ChoosePerItem.Create(new OffsetCrossover(1), new OffsetCrossover(2));
        var right = ChoosePerItem.Create(new OffsetCrossover(1), new OffsetCrossover(3));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void MultiCrossover_WithReorderedChildCrossovers_IsNotEqual()
    {
        var left = ChoosePerItem.Create(new OffsetCrossover(1), new OffsetCrossover(2));
        var right = ChoosePerItem.Create(new OffsetCrossover(2), new OffsetCrossover(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void MultiCrossover_WithDifferentChildCount_IsNotEqual()
    {
        var left = ChoosePerItem.Create(new OffsetCrossover(1), new OffsetCrossover(2));
        var right = ChoosePerItem.Create(new OffsetCrossover(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ChoosePerItemCrossover_WithDifferentWeights_IsNotEqual()
    {
        IOperator<Crossover, int>[] childCrossovers = [new OffsetCrossover(1), new OffsetCrossover(2)];
        var left = new ChoosePerItem<Crossover, int>(childCrossovers) { Weights = [1.0, 2.0] };
        var right = new ChoosePerItem<Crossover, int>(childCrossovers) { Weights = [2.0, 1.0] };

        left.ShouldNotBe(right);
    }

    /// <summary>
    /// Omitted weights are retained as an empty collection meaning uniform selection, so an explicitly uniform
    /// configuration stays distinguishable from an unweighted one.
    /// </summary>
    [Fact]
    public void ChoosePerItemCrossover_WithOmittedWeights_IsNotEqualToExplicitUniformWeights()
    {
        IOperator<Crossover, int>[] childCrossovers = [new OffsetCrossover(1), new OffsetCrossover(2)];
        var omitted = new ChoosePerItem<Crossover, int>(childCrossovers);
        var explicitUniform = new ChoosePerItem<Crossover, int>(childCrossovers) { Weights = [0.5, 0.5] };

        omitted.Weights.ShouldBeEmpty();
        omitted.ShouldNotBe(explicitUniform);
    }

    [Fact]
    public void MultiCrossoverTopologies_WithEqualChildCrossovers_AreNotEqualAcrossTypes()
    {
        var chooseOne = ChoosePerItem.Create(new OffsetCrossover(1), new OffsetCrossover(2));
        var firstOf = new FirstOfCrossover([new OffsetCrossover(1), new OffsetCrossover(2)]);

        chooseOne.ShouldNotBe<object>(firstOf);
    }

    /// <summary>
    /// Operators whose children carry a specific role declare those children themselves instead of deriving from a
    /// topology base, so their equality comes from the named properties.
    /// </summary>
    [Fact]
    public void TransformedCrossover_WithEqualParts_IsEqual()
    {
        var left = new OffsetCrossover(1).TransformWith(new AddOneMutator());
        var right = new OffsetCrossover(1).TransformWith(new AddOneMutator());

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void TransformedCrossover_ExposesItsSourceAndTransformation()
    {
        var source = new OffsetCrossover(1);
        var transformation = new AddOneMutator();

        var crossover = source.TransformWith(transformation);

        crossover.SourceCrossover.ShouldBeSameAs(source);
        crossover.TransformationMutator.ShouldBeSameAs(transformation);
    }











    /// <summary>
    /// Required values are constructor parameters so a configuration cannot be created incomplete, but their
    /// properties are still <c>init</c> so an existing configuration can be reconfigured with a <c>with</c> expression.
    /// </summary>
    [Fact]
    public void RequiredChild_CanBeReplacedWithAWithExpression()
    {
        var original = new IdentityCrossover(new OffsetCrossover(1));
        var replacement = new OffsetCrossover(2);

        var reconfigured = original with { ChildOperator = replacement };

        reconfigured.ChildOperator.ShouldBeSameAs(replacement);
        original.ChildOperator.ShouldNotBeSameAs(replacement);
    }

    /// <summary>
    /// The same holds for a required child collection and for a child that carries a specific role.
    /// </summary>
    [Fact]
    public void RequiredChildCollection_CanBeReplacedWithAWithExpression()
    {
        var original = ChoosePerItem.Create(new OffsetCrossover(1), new OffsetCrossover(2));

        var reconfigured = original with { ChildOperators = [new OffsetCrossover(3)], Weights = [1.0] };

        reconfigured.ChildOperators.Count.ShouldBe(1);
        reconfigured.Weights.ShouldBe([1.0]);
        original.ChildOperators.Count.ShouldBe(2);
    }

    [Fact]
    public void SingleCandidateCrossover_WithDifferentConcurrency_IsNotEqual()
    {
        var left = new OffsetCrossover(1);
        var right = left with { Concurrency = ExecutionConcurrency.Concurrent(2) };

        left.ShouldNotBe(right);
    }

    [Fact]
    public void NestedCrossoverComposition_WithEqualParts_IsEqual()
    {
        var left = ChoosePerItem.Create(new OffsetCrossover(1).TransformWith(new AddOneMutator()), new OffsetCrossover(2));
        var right = ChoosePerItem.Create(new OffsetCrossover(1).TransformWith(new AddOneMutator()), new OffsetCrossover(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void NestedCrossoverComposition_WithDifferentNestedChildCrossover_IsNotEqual()
    {
        var left = ChoosePerItem.Create(new OffsetCrossover(1).TransformWith(new AddOneMutator()), new OffsetCrossover(2));
        var right = ChoosePerItem.Create(new OffsetCrossover(9).TransformWith(new AddOneMutator()), new OffsetCrossover(2));

        left.ShouldNotBe(right);
    }

    private sealed record OffsetCrossover(int Offset) : SingleCandidateCrossover<int, DummySearchSpace<int>>
    {
        public override int CrossParents(Parents<int> parents, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) =>
            parents.Parent1 + Offset;
    }

    private sealed record AddOneMutator : SingleCandidateMutator<int, DummySearchSpace<int>>
    {
        public override int MutateCandidate(int parent, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => parent + 1;
    }

    private sealed record FirstOfCrossover
        : MultiOperator<Crossover, int>
    {
        public FirstOfCrossover(IReadOnlyList<IOperator<Crossover, int>> childCrossovers)
            : base(childCrossovers)
        {
        }

        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<int, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
        {
            ImmutableArray<ICrossoverExecutor<int, TRunSearchSpace, TRunProblem>> children = [.. ChildOperators.Select(child => scope.Resolve(child))];
            return new Executor<TRunSearchSpace, TRunProblem>(children);
        }

        private sealed class Executor<TSearchSpace, TProblem>(ImmutableArray<ICrossoverExecutor<int, TSearchSpace, TProblem>> childCrossovers)
            : ICrossoverExecutor<int, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public IReadOnlyList<int> Cross(IReadOnlyList<Parents<int>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                childCrossovers[0].Cross(parents, random, searchSpace, problem);
        }
    }
    private sealed record IdentityCrossover : WrappingOperator<Crossover, int>
    {
        public IdentityCrossover(IOperator<Crossover, int> child) : base(child)
        {
        }

        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) => scope.ResolveOperator(ChildOperator);
    }

}
