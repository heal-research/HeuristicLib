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

        var crossover = childCrossover.CountCrossoverCalls(new ObservationCounter());

        crossover.ChildCrossover.ShouldBeSameAs(childCrossover);
    }

    [Fact]
    public void MultiCrossover_ExposesConfiguredChildCrossovers()
    {
        var first = new OffsetCrossover(1);
        var second = new OffsetCrossover(2);

        var crossover = ChooseOneCrossover.Create(first, second);

        crossover.ChildCrossovers.ShouldBe([first, second]);
    }

    [Fact]
    public void MultiCrossover_SnapshotsConfiguredChildCrossovers()
    {
        var first = new OffsetCrossover(1);
        var second = new OffsetCrossover(2);
        var childCrossovers = new List<ICrossover<int>> { first, second };
        var crossover = new FirstOfCrossover(childCrossovers);

        childCrossovers.Clear();

        crossover.ChildCrossovers.ShouldBe([first, second]);
    }

    [Fact]
    public void MultiCrossover_WithSeparatelyBuiltEqualChildCollections_IsEqual()
    {
        var left = ChooseOneCrossover.Create(new OffsetCrossover(1), new OffsetCrossover(2));
        var right = ChooseOneCrossover.Create(new OffsetCrossover(1), new OffsetCrossover(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void MultiCrossover_WithDifferentChildCrossovers_IsNotEqual()
    {
        var left = ChooseOneCrossover.Create(new OffsetCrossover(1), new OffsetCrossover(2));
        var right = ChooseOneCrossover.Create(new OffsetCrossover(1), new OffsetCrossover(3));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void MultiCrossover_WithReorderedChildCrossovers_IsNotEqual()
    {
        var left = ChooseOneCrossover.Create(new OffsetCrossover(1), new OffsetCrossover(2));
        var right = ChooseOneCrossover.Create(new OffsetCrossover(2), new OffsetCrossover(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void MultiCrossover_WithDifferentChildCount_IsNotEqual()
    {
        var left = ChooseOneCrossover.Create(new OffsetCrossover(1), new OffsetCrossover(2));
        var right = ChooseOneCrossover.Create(new OffsetCrossover(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ChooseOneCrossover_WithDifferentWeights_IsNotEqual()
    {
        ICrossover<int>[] childCrossovers = [new OffsetCrossover(1), new OffsetCrossover(2)];
        var left = new ChooseOneCrossover<int>(childCrossovers) { Weights = [1.0, 2.0] };
        var right = new ChooseOneCrossover<int>(childCrossovers) { Weights = [2.0, 1.0] };

        left.ShouldNotBe(right);
    }

    /// <summary>
    /// Omitted weights are retained as an empty collection meaning uniform selection, so an explicitly uniform
    /// configuration stays distinguishable from an unweighted one.
    /// </summary>
    [Fact]
    public void ChooseOneCrossover_WithOmittedWeights_IsNotEqualToExplicitUniformWeights()
    {
        ICrossover<int>[] childCrossovers = [new OffsetCrossover(1), new OffsetCrossover(2)];
        var omitted = new ChooseOneCrossover<int>(childCrossovers);
        var explicitUniform = new ChooseOneCrossover<int>(childCrossovers) { Weights = [0.5, 0.5] };

        omitted.Weights.ShouldBeEmpty();
        omitted.ShouldNotBe(explicitUniform);
    }

    [Fact]
    public void MultiCrossoverTopologies_WithEqualChildCrossovers_AreNotEqualAcrossTypes()
    {
        var chooseOne = ChooseOneCrossover.Create(new OffsetCrossover(1), new OffsetCrossover(2));
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

    [Fact]
    public void CountingCrossover_WithSameCounterAndMetric_IsEqual()
    {
        var counter = new ObservationCounter();
        var left = new OffsetCrossover(1).CountCrossoverCalls(counter);
        var right = new OffsetCrossover(1).CountCrossoverCalls(counter);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void CountingCrossover_WithDifferentMetric_IsNotEqual()
    {
        var counter = new ObservationCounter();
        var left = new OffsetCrossover(1).CountCrossoverCalls(counter);
        var right = new OffsetCrossover(1).CountCrossedCandidates(counter);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void CountingCrossover_WithDifferentCounter_IsNotEqual()
    {
        var left = new OffsetCrossover(1).CountCrossoverCalls(new ObservationCounter());
        var right = new OffsetCrossover(1).CountCrossoverCalls(new ObservationCounter());

        left.ShouldNotBe(right);
    }

    [Fact]
    public void CountingCrossover_WithDifferentChildCrossover_IsNotEqual()
    {
        var counter = new ObservationCounter();
        var left = new OffsetCrossover(1).CountCrossoverCalls(counter);
        var right = new OffsetCrossover(2).CountCrossoverCalls(counter);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void DurationMeasuringCrossover_WithSameDurationAndTimeProvider_IsEqual()
    {
        var duration = new ObservationDuration();
        var left = new OffsetCrossover(1).MeasureCrossoverDuration(duration, TimeProvider.System);
        var right = new OffsetCrossover(1).MeasureCrossoverDuration(duration, TimeProvider.System);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void DurationMeasuringCrossover_WithDifferentDuration_IsNotEqual()
    {
        var left = new OffsetCrossover(1).MeasureCrossoverDuration(new ObservationDuration(), TimeProvider.System);
        var right = new OffsetCrossover(1).MeasureCrossoverDuration(new ObservationDuration(), TimeProvider.System);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void DurationMeasuringCrossover_WithDifferentChildCrossover_IsNotEqual()
    {
        var duration = new ObservationDuration();
        var left = new OffsetCrossover(1).MeasureCrossoverDuration(duration, TimeProvider.System);
        var right = new OffsetCrossover(2).MeasureCrossoverDuration(duration, TimeProvider.System);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ObservableCrossover_WithSameObserverInstance_IsEqual()
    {
        var observer = new ActionCrossoverObserver<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>((_, _, _, _) => { });
        var left = new OffsetCrossover(1).ObserveWith(observer);
        var right = new OffsetCrossover(1).ObserveWith(observer);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void ObservableCrossover_WithDifferentChildCrossover_IsNotEqual()
    {
        var observer = new ActionCrossoverObserver<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>((_, _, _, _) => { });
        var left = new OffsetCrossover(1).ObserveWith(observer);
        var right = new OffsetCrossover(2).ObserveWith(observer);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ObservableCrossover_WithSeparatelyConstructedActionObservers_IsNotEqual()
    {
        var left = new OffsetCrossover(1).ObserveWith(_ => { });
        var right = new OffsetCrossover(1).ObserveWith(_ => { });

        left.ShouldNotBe(right);
    }

    /// <summary>
    /// Required values are constructor parameters so a configuration cannot be created incomplete, but their
    /// properties are still <c>init</c> so an existing configuration can be reconfigured with a <c>with</c> expression.
    /// </summary>
    [Fact]
    public void RequiredChild_CanBeReplacedWithAWithExpression()
    {
        var original = new OffsetCrossover(1).CountCrossoverCalls(new ObservationCounter());
        var replacement = new OffsetCrossover(2);

        var reconfigured = original with { ChildCrossover = replacement };

        reconfigured.ChildCrossover.ShouldBeSameAs(replacement);
        reconfigured.Counter.ShouldBeSameAs(original.Counter);
        original.ChildCrossover.ShouldNotBeSameAs(replacement);
    }

    /// <summary>
    /// The same holds for a required child collection and for a child that carries a specific role.
    /// </summary>
    [Fact]
    public void RequiredChildCollection_CanBeReplacedWithAWithExpression()
    {
        var original = ChooseOneCrossover.Create(new OffsetCrossover(1), new OffsetCrossover(2));

        var reconfigured = original with { ChildCrossovers = [new OffsetCrossover(3)], Weights = [1.0] };

        reconfigured.ChildCrossovers.Count.ShouldBe(1);
        reconfigured.Weights.ShouldBe([1.0]);
        original.ChildCrossovers.Count.ShouldBe(2);
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
        var left = ChooseOneCrossover.Create(new OffsetCrossover(1).TransformWith(new AddOneMutator()), new OffsetCrossover(2));
        var right = ChooseOneCrossover.Create(new OffsetCrossover(1).TransformWith(new AddOneMutator()), new OffsetCrossover(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void NestedCrossoverComposition_WithDifferentNestedChildCrossover_IsNotEqual()
    {
        var left = ChooseOneCrossover.Create(new OffsetCrossover(1).TransformWith(new AddOneMutator()), new OffsetCrossover(2));
        var right = ChooseOneCrossover.Create(new OffsetCrossover(9).TransformWith(new AddOneMutator()), new OffsetCrossover(2));

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
        : MultiCrossover<int>
    {
        public FirstOfCrossover(IReadOnlyList<ICrossover<int>> childCrossovers)
            : base(childCrossovers)
        {
        }

        protected override ICrossoverInstance<int, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<ICrossoverInstance<int, TRunSearchSpace, TRunProblem>> childCrossovers) =>
            new Instance<TRunSearchSpace, TRunProblem>(childCrossovers);

        private sealed class Instance<TSearchSpace, TProblem>(ImmutableArray<ICrossoverInstance<int, TSearchSpace, TProblem>> childCrossovers)
            : MultiCrossoverInstance<int, TSearchSpace, TProblem>(childCrossovers)
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public override IReadOnlyList<int> Cross(IReadOnlyList<Parents<int>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                ChildCrossovers[0].Cross(parents, random, searchSpace, problem);
        }
    }
}
