using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators;

public class PredefinedCandidatesCreatorTests
{
    [Fact]
    public void Create_ShouldConsumePredefinedCandidatesAcrossCallsAndForwardRandomToFallback()
    {
        var random = RandomNumberGenerator.Create(42);
        var fallback = new ExpectedRandomCreator(random, 99);
        var creator = fallback.SeededWith([10, 20]);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var execution = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(creator);

        var first = execution.Create(1, random, DummySearchSpace<int>.Instance, problem);
        var second = execution.Create(3, random, DummySearchSpace<int>.Instance, problem);

        first.ShouldBe([10]);
        second.ShouldBe([20, 99, 99]);
    }

    [Fact]
    public void Rebinding_PreservesCursorAndUsesTheContextualFallback()
    {
        var random = RandomNumberGenerator.Create(42);
        ICreator<int> fallback = new ExpectedRandomCreator(random, 99);
        var creator = fallback.SeededWith([10, 20, 30]);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var parent = ResolutionScope.Create();
        var outer = parent.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>().Resolve(creator);
        outer.Create(1, random, problem.SearchSpace, problem).ShouldBe([10]);
        var observedFallbackCalls = new CountAccumulator();
        var child = parent.CreateChildScope(builder => builder.Wrap(fallback, original => original.CountCalls(observedFallbackCalls)));
        var inner = child.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>().Resolve(creator);

        inner.ShouldNotBeSameAs(outer);
        inner.Create(3, random, problem.SearchSpace, problem).ShouldBe([20, 30, 99]);
        observedFallbackCalls.CurrentCount.ShouldBe(1);
        outer.Create(1, random, problem.SearchSpace, problem).ShouldBe([99]);
        observedFallbackCalls.CurrentCount.ShouldBe(1);
        inner.Create(1, random, problem.SearchSpace, problem).ShouldBe([99]);
        observedFallbackCalls.CurrentCount.ShouldBe(2);

        var independent = ResolutionScope.Create().For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>().Resolve(creator);
        independent.Create(1, random, problem.SearchSpace, problem).ShouldBe([10]);
        observedFallbackCalls.CurrentCount.ShouldBe(2);
    }

    private sealed record ExpectedRandomCreator(IRandomNumberGenerator ExpectedRandom, int Value)
        : StatelessCreator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<int> Create(int count, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) =>
            Enumerable.Repeat(ReferenceEquals(random, ExpectedRandom) ? Value : -1, count).ToArray();
    }
}
