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

        new IdentityInterceptor(child).ChildOperator.ShouldBeSameAs(child);
    }

    [Fact]
    public void PipelineInterceptor_SnapshotsChildrenAndUsesOrderedStructuralEquality()
    {
        var first = new OffsetInterceptor(1);
        var second = new OffsetInterceptor(2);
        var children = new List<IOperator<Interception<TestState>, int>> { first, second };
        var left = new PipelineInterceptor<int, TestState>(children);

        children.Clear();
        var equal = PipelineInterceptor.Create(new OffsetInterceptor(1), new OffsetInterceptor(2));
        var reordered = PipelineInterceptor.Create(new OffsetInterceptor(2), new OffsetInterceptor(1));

        left.ChildOperators.ShouldBe([first, second]);
        left.ShouldBe(equal);
        left.GetHashCode().ShouldBe(equal.GetHashCode());
        left.ShouldNotBe(reordered);
    }

    [Fact]
    public void NestedComposition_WithEqualParts_IsEqual()
    {
        var left = PipelineInterceptor.Create(new OffsetInterceptor(1), new OffsetInterceptor(2));
        var equal = PipelineInterceptor.Create(new OffsetInterceptor(1), new OffsetInterceptor(2));
        var different = PipelineInterceptor.Create(new OffsetInterceptor(1), new OffsetInterceptor(3));

        left.ShouldBe(equal);
        left.ShouldNotBe(different);
    }


    private sealed record TestState(int Value) : SearchState;

    private sealed record OffsetInterceptor(int Offset)
        : StatelessInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestState>
    {
        public override TestState Transform(TestState currentState, TestState? previousState, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) =>
            currentState with { Value = currentState.Value + Offset };
    }
    private sealed record IdentityInterceptor : WrappingOperator<Interception<TestState>, int>
    {
        public IdentityInterceptor(IOperator<Interception<TestState>, int> child) : base(child)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory() => OperatorExecutorFactory.Forward(ChildOperator);
    }

}
