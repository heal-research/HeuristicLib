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

        child.CountInterceptorCalls(new ObservationCounter()).ChildInterceptor.ShouldBeSameAs(child);
    }

    [Fact]
    public void PipelineInterceptor_SnapshotsChildrenAndUsesOrderedStructuralEquality()
    {
        var first = new OffsetInterceptor(1);
        var second = new OffsetInterceptor(2);
        var children = new List<IInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestState>> { first, second };
        var left = new PipelineInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestState>(children);

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
        var counter = new ObservationCounter();
        var left = PipelineInterceptor.Create(new OffsetInterceptor(1), new OffsetInterceptor(2)).CountInterceptorCalls(counter);
        var equal = PipelineInterceptor.Create(new OffsetInterceptor(1), new OffsetInterceptor(2)).CountInterceptorCalls(counter);
        var different = PipelineInterceptor.Create(new OffsetInterceptor(1), new OffsetInterceptor(3)).CountInterceptorCalls(counter);

        left.ShouldBe(equal);
        left.ShouldNotBe(different);
    }

    [Fact]
    public void WrappingConcerns_IncludeChildAndSettingsInEquality()
    {
        var counter = new ObservationCounter();
        var duration = new ObservationDuration();
        var observer = new ActionInterceptorObserver<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestState>((_, _, _, _, _) => { });

        new OffsetInterceptor(1).CountInterceptorCalls(counter).ShouldBe(new OffsetInterceptor(1).CountInterceptorCalls(counter));
        new OffsetInterceptor(1).MeasureInterceptorDuration(duration, TimeProvider.System).ShouldBe(new OffsetInterceptor(1).MeasureInterceptorDuration(duration, TimeProvider.System));
        new OffsetInterceptor(1).ObserveWith(observer).ShouldBe(new OffsetInterceptor(1).ObserveWith(observer));
        new OffsetInterceptor(1).CountInterceptorCalls(counter).ShouldNotBe(new OffsetInterceptor(2).CountInterceptorCalls(counter));
    }

    [Fact]
    public void ObservableInterceptor_SnapshotsObservers()
    {
        var observer = new ActionInterceptorObserver<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestState>((_, _, _, _, _) => { });
        var observers = new List<IInterceptorObserver<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestState>> { observer };
        var observable = new ObservableInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestState>(new OffsetInterceptor(1), observers);

        observers.Clear();

        observable.Observers.ShouldBe([observer]);
    }

    private sealed record TestState(int Value) : SearchState;

    private sealed record OffsetInterceptor(int Offset)
        : StatelessInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestState>
    {
        public override TestState Transform(TestState currentState, TestState? previousState, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) =>
            currentState with { Value = currentState.Value + Offset };
    }
}
