using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators;

public class TerminatorConfigurationEqualityTests
{
    [Fact]
    public void WrappingTerminator_ExposesConfiguredChildTerminator()
    {
        var child = new ThresholdTerminator(1);

        child.CountTerminatorCalls(new ObservationCounter()).ChildTerminator.ShouldBeSameAs(child);
    }

    [Fact]
    public void LogicalTerminator_SnapshotsChildrenAndUsesOrderedStructuralEquality()
    {
        var first = new ThresholdTerminator(1);
        var second = new ThresholdTerminator(2);
        var children = new List<ITerminator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestState>> { first, second };
        var left = new AnyTerminator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestState>(children);

        children.Clear();
        var equal = AnyTerminator.Create(new ThresholdTerminator(1), new ThresholdTerminator(2));
        var reordered = AnyTerminator.Create(new ThresholdTerminator(2), new ThresholdTerminator(1));

        left.ChildTerminators.ShouldBe([first, second]);
        left.ShouldBe(equal);
        left.GetHashCode().ShouldBe(equal.GetHashCode());
        left.ShouldNotBe(reordered);
    }

    [Fact]
    public void NestedComposition_WithEqualParts_IsEqual()
    {
        var counter = new ObservationCounter();
        var left = AnyTerminator.Create(new ThresholdTerminator(1), new ThresholdTerminator(2)).CountTerminatorCalls(counter);
        var equal = AnyTerminator.Create(new ThresholdTerminator(1), new ThresholdTerminator(2)).CountTerminatorCalls(counter);
        var different = AnyTerminator.Create(new ThresholdTerminator(1), new ThresholdTerminator(3)).CountTerminatorCalls(counter);

        left.ShouldBe(equal);
        left.ShouldNotBe(different);
    }

    [Fact]
    public void WrappingConcerns_IncludeChildAndSettingsInEquality()
    {
        var counter = new ObservationCounter();
        var duration = new ObservationDuration();
        var observer = new ActionTerminatorObserver<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestState>((_, _, _, _) => { });

        new ThresholdTerminator(1).CountTerminatorCalls(counter).ShouldBe(new ThresholdTerminator(1).CountTerminatorCalls(counter));
        new ThresholdTerminator(1).MeasureTerminatorDuration(duration, TimeProvider.System).ShouldBe(new ThresholdTerminator(1).MeasureTerminatorDuration(duration, TimeProvider.System));
        new ThresholdTerminator(1).ObserveWith(observer).ShouldBe(new ThresholdTerminator(1).ObserveWith(observer));
        new ThresholdTerminator(1).CountTerminatorCalls(counter).ShouldNotBe(new ThresholdTerminator(2).CountTerminatorCalls(counter));
    }

    [Fact]
    public void StagnationTerminator_SupportsWithReconfiguration()
    {
        var original = new StagnationTerminator<int>();
        var reconfigured = original with { StagnationThreshold = 5 };

        original.StagnationThreshold.ShouldBe(20);
        reconfigured.StagnationThreshold.ShouldBe(5);
        original.ShouldNotBe(reconfigured);
    }

    private sealed record TestState(int Value) : SearchState;

    private sealed record ThresholdTerminator(int Threshold)
        : StatelessTerminator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestState>
    {
        public override bool IsTerminalState(TestState state, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) =>
            state.Value >= Threshold;
    }
}
