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

        new IdentityTerminator(child).ChildOperator.ShouldBeSameAs(child);
    }

    [Fact]
    public void LogicalTerminator_SnapshotsChildrenAndUsesOrderedStructuralEquality()
    {
        var first = new ThresholdTerminator(1);
        var second = new ThresholdTerminator(2);
        var children = new List<IOperator<Termination, int>> { first, second };
        var left = new AnyTerminator<int>(children);

        children.Clear();
        var equal = AnyTerminator.Create(new ThresholdTerminator(1), new ThresholdTerminator(2));
        var reordered = AnyTerminator.Create(new ThresholdTerminator(2), new ThresholdTerminator(1));

        left.ChildOperators.ShouldBe([first, second]);
        left.ShouldBe(equal);
        left.GetHashCode().ShouldBe(equal.GetHashCode());
        left.ShouldNotBe(reordered);
    }

    [Fact]
    public void NestedComposition_WithEqualParts_IsEqual()
    {
        var left = AnyTerminator.Create(new ThresholdTerminator(1), new ThresholdTerminator(2));
        var equal = AnyTerminator.Create(new ThresholdTerminator(1), new ThresholdTerminator(2));
        var different = AnyTerminator.Create(new ThresholdTerminator(1), new ThresholdTerminator(3));

        left.ShouldBe(equal);
        left.ShouldNotBe(different);
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
        public override bool IsTerminalState(TestState state, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) =>
            state.Value >= Threshold;
    }
    private sealed record IdentityTerminator : WrappingOperator<Termination, int>
    {
        public IdentityTerminator(IOperator<Termination, int> child) : base(child)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory() => OperatorExecutorFactory.Forward(ChildOperator);
    }

}
