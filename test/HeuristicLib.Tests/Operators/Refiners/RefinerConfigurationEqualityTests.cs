using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Refiners;

public class RefinerConfigurationEqualityTests
{
    [Fact]
    public void PipelineRefiner_WithEqualChildRefiners_IsEqual()
    {
        var left = PipelineRefiner.Create(new AddOffsetRefiner(1), new AddOffsetRefiner(2));
        var right = PipelineRefiner.Create(new AddOffsetRefiner(1), new AddOffsetRefiner(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void PipelineRefiner_WithDifferentChildRefiners_IsNotEqual()
    {
        var left = PipelineRefiner.Create(new AddOffsetRefiner(1), new AddOffsetRefiner(2));
        var right = PipelineRefiner.Create(new AddOffsetRefiner(1), new AddOffsetRefiner(3));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void PipelineRefiner_WithReorderedChildRefiners_IsNotEqual()
    {
        var left = PipelineRefiner.Create(new AddOffsetRefiner(1), new AddOffsetRefiner(2));
        var right = PipelineRefiner.Create(new AddOffsetRefiner(2), new AddOffsetRefiner(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void PipelineRefiner_WithDifferentChildCount_IsNotEqual()
    {
        var left = PipelineRefiner.Create(new AddOffsetRefiner(1), new AddOffsetRefiner(2));
        var right = PipelineRefiner.Create(new AddOffsetRefiner(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ChoosePerCandidateRefinement_WithDifferentChildRefiners_IsNotEqual()
    {
        var left = ChoosePerCandidate.Create([new AddOffsetRefiner(1)], [1.0]);
        var right = ChoosePerCandidate.Create([new AddOffsetRefiner(2)], [1.0]);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ChoosePerCandidateRefinement_WithDifferentWeights_IsNotEqual()
    {
        var children = new IOperator<Refinement, int>[]
        {
            new AddOffsetRefiner(1),
            new AddOffsetRefiner(2),
        };
        var left = ChoosePerCandidate.Create(children, [1.0, 2.0]);
        var right = ChoosePerCandidate.Create(children, [2.0, 1.0]);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void MultiRefinerTopologies_WithEqualChildRefiners_AreNotEqualAcrossTypes()
    {
        var pipeline = PipelineRefiner.Create(new AddOffsetRefiner(1), new AddOffsetRefiner(2));
        var chooseOne = ChoosePerCandidate.Create(new AddOffsetRefiner(1), new AddOffsetRefiner(2));

        pipeline.ShouldNotBe<object>(chooseOne);
    }










    // A freshly allocated action observer defeats structural equality, which is why concern types carrying only
    // value-like settings must not be expressed as an observer wrapper.

    [Fact]
    public void IteratedRefiner_WithEqualChildRefinerAndIterations_IsEqual()
    {
        var left = new AddOffsetRefiner(1).AsIterated(3);
        var right = new AddOffsetRefiner(1).AsIterated(3);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void IteratedRefiner_WithDifferentIterations_IsNotEqual()
    {
        var left = new AddOffsetRefiner(1).AsIterated(3);
        var right = new AddOffsetRefiner(1).AsIterated(4);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void IteratedRefiner_WithDifferentChildRefiner_IsNotEqual()
    {
        var left = new AddOffsetRefiner(1).AsIterated(3);
        var right = new AddOffsetRefiner(2).AsIterated(3);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void SingleCandidateRefiner_WithDifferentConcurrency_IsNotEqual()
    {
        var left = new AddOffsetRefiner(1);
        var right = left with { Concurrency = ExecutionConcurrency.Concurrent() };

        left.ShouldNotBe(right);
    }

    [Fact]
    public void NestedRefinerComposition_WithEqualParts_IsEqual()
    {
        var left = PipelineRefiner.Create(
            new AddOffsetRefiner(1).AsIterated(2),
            new AddOffsetRefiner(2).AsIterated(2));
        var right = PipelineRefiner.Create(
            new AddOffsetRefiner(1).AsIterated(2),
            new AddOffsetRefiner(2).AsIterated(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void NestedRefinerComposition_WithDifferentNestedChildRefiner_IsNotEqual()
    {
        var left = PipelineRefiner.Create(
            new AddOffsetRefiner(1).AsIterated(2),
            new AddOffsetRefiner(2).AsIterated(2));
        var right = PipelineRefiner.Create(
            new AddOffsetRefiner(9).AsIterated(2),
            new AddOffsetRefiner(2).AsIterated(2));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void HandwrittenMultiRefiner_ComparesChildRefinersWithoutAnyEqualityDeclaration()
    {
        var left = new FirstOfRefiner([new AddOffsetRefiner(1), new AddOffsetRefiner(2)]);
        var right = new FirstOfRefiner([new AddOffsetRefiner(1), new AddOffsetRefiner(2)]);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
        left.ShouldNotBe(new FirstOfRefiner([new AddOffsetRefiner(2), new AddOffsetRefiner(1)]));
        left.ShouldNotBe(new FirstOfRefiner([new AddOffsetRefiner(1)]));
    }

    [Fact]
    public void HandwrittenMultiRefiner_SnapshotsConfiguredChildRefiners()
    {
        var first = new AddOffsetRefiner(1);
        var second = new AddOffsetRefiner(2);
        var childRefiners = new List<IOperator<Refinement, int>> { first, second };
        var refiner = new FirstOfRefiner(childRefiners);

        childRefiners.Clear();

        refiner.ChildOperators.ShouldBe([first, second]);
    }

    private sealed record AddOffsetRefiner(int Offset) : SingleCandidateRefiner<int, DummySearchSpace<int>>
    {
        public override int RefineCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => candidate + Offset;
    }

    // An externally authored topology with no equality attribute, generator or hand-written comparison: its structural
    // equality follows from ChildRefiners being a value array.
    private sealed record FirstOfRefiner
        : MultiOperator<Refinement, int>
    {
        public FirstOfRefiner(IReadOnlyList<IOperator<Refinement, int>> childRefiners)
            : base(childRefiners)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(this);

        private sealed class Factory(FirstOfRefiner source) : OperatorExecutorFactory<Refinement, int>
        {
            protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
                ResolutionScope<int, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
            {
                ImmutableArray<IRefinementExecutor<int, TRunSearchSpace, TRunProblem>> children = [.. source.ChildOperators.Select(child => scope.Resolve(child))];
                return new Executor<TRunSearchSpace, TRunProblem>(children);
            }
        }

        private sealed class Executor<TSearchSpace, TProblem>(ImmutableArray<IRefinementExecutor<int, TSearchSpace, TProblem>> childRefiners)
            : IRefinementExecutor<int, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public IReadOnlyList<int> Refine(IReadOnlyList<int> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                childRefiners[0].Refine(candidates, random, searchSpace, problem);
        }
    }
}
