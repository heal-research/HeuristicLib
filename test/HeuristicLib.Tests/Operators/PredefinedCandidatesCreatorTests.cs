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
        var creator = fallback.WithPredefinedCandidates([10, 20]);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var instance = new ExecutionInstanceRegistry().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(creator);

        var first = instance.Create(1, random, DummySearchSpace<int>.Instance, problem);
        var second = instance.Create(3, random, DummySearchSpace<int>.Instance, problem);

        first.ShouldBe([10]);
        second.ShouldBe([20, 99, 99]);
    }

    private sealed record ExpectedRandomCreator(IRandomNumberGenerator ExpectedRandom, int Value)
        : StatelessCreator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<int> Create(int count, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) =>
            Enumerable.Repeat(ReferenceEquals(random, ExpectedRandom) ? Value : -1, count).ToArray();
    }
}
