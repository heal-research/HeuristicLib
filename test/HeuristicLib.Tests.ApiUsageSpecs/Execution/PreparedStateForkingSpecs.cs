using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Execution;

public sealed class PreparedStateForkingSpecs
{
    [Fact]
    public void AChildScopeCanBranchAnExplicitlyCopyableConfiguration()
    {
        var configuration = new CounterConfiguration();
        var parent = ResolutionScope.Create();
        var incumbentScope = parent.CreateChildScope();
        var incumbent = incumbentScope.Resolve(configuration);
        incumbent.Increment();

        var challengerScope = incumbentScope.CreateForkedScope();
        var challenger = challengerScope.Resolve(configuration);

        incumbent.Increment().ShouldBe(2);
        challenger.Increment().ShouldBe(2);
    }

    private sealed class CounterConfiguration : IConfigurationNode<CounterExecution>
    {
        public ExecutionFactory<CounterExecution> CreateExecutionFactory() =>
            ExecutionFactory.Forkable<CounterState, CounterExecution>(
                new CounterState(),
                static state => new CounterState { Count = state.Count },
                static (state, scope) => new CounterExecution(state));
    }

    private sealed class CounterState
    {
        public int Count { get; set; }
    }

    private sealed class CounterExecution(CounterState state) : IExecutionNode
    {
        public int Increment() => ++state.Count;
    }
}
