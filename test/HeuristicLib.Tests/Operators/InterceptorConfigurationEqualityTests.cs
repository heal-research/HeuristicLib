using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators;

public class InterceptorConfigurationEqualityTests
{
    [Fact]
    public void WrappingInterceptor_ExposesConfiguredChildInterceptor()
    {
        var child = new OffsetInterceptor(1);

        child.CountCalls(new CountAccumulator()).ChildInterceptor.ShouldBeSameAs(child);
    }

    [Fact]
    public void PipelineInterceptor_SnapshotsChildrenAndUsesOrderedStructuralEquality()
    {
        var first = new OffsetInterceptor(1);
        var second = new OffsetInterceptor(2);
        var children = new List<IInterceptor<int>> { first, second };
        var left = new PipelineInterceptor<int>(children);

        children.Clear();
        var equal = PipelineInterceptor.Create(new OffsetInterceptor(1), new OffsetInterceptor(2));
        var reordered = PipelineInterceptor.Create(new OffsetInterceptor(2), new OffsetInterceptor(1));

        left.ChildInterceptors.ShouldBe([first, second]);
        left.ShouldBe(equal);
        left.GetHashCode().ShouldBe(equal.GetHashCode());
        left.ShouldNotBe(reordered);
    }

    [Fact]
    public void NestedComposition_WithEqualParts_IsEqual()
    {
        var counter = new CountAccumulator();
        var left = PipelineInterceptor.Create(new OffsetInterceptor(1), new OffsetInterceptor(2)).CountCalls(counter);
        var equal = PipelineInterceptor.Create(new OffsetInterceptor(1), new OffsetInterceptor(2)).CountCalls(counter);
        var different = PipelineInterceptor.Create(new OffsetInterceptor(1), new OffsetInterceptor(3)).CountCalls(counter);

        left.ShouldBe(equal);
        left.ShouldNotBe(different);
    }

    [Fact]
    public void WrappingConcerns_IncludeChildAndSettingsInEquality()
    {
        var counter = new CountAccumulator();
        var duration = new DurationAccumulator();

        new OffsetInterceptor(1).CountCalls(counter).ShouldBe(new OffsetInterceptor(1).CountCalls(counter));
        new OffsetInterceptor(1).MeasureDuration(duration, TimeProvider.System).ShouldBe(new OffsetInterceptor(1).MeasureDuration(duration, TimeProvider.System));
        new OffsetInterceptor(1).CountCalls(counter).ShouldNotBe(new OffsetInterceptor(2).CountCalls(counter));
    }

    private sealed record TestState(int Value) : SearchState;

    private sealed record OffsetInterceptor(int Offset)
        : StatelessInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestState>
    {
        public override TestState Transform(TestState currentState, TestState? previousState, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) =>
            currentState with { Value = currentState.Value + Offset };
    }
}
