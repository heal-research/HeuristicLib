namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public partial class ExecutionFactoryResolutionTests
{
    [Fact]
    public void Fork_CapturesLocalStateImmediatelyAndCanBeForkedAgain()
    {
        var preparations = 0;
        var bindings = 0;
        var source = new ForkCounterConfiguration(prepare: () => preparations++, bind: () => bindings++);
        var scope = ResolutionScope.Create();
        var original = ResolveForkCounter(scope, source);
        original.Next().ShouldBe(1);

        var fork = scope.CreateForkedScope();
        bindings.ShouldBe(1);
        original.Next().ShouldBe(2);
        var copied = ResolveForkCounter(fork, source);
        copied.Next().ShouldBe(2);
        copied.ShouldNotBeSameAs(original);

        var secondFork = fork.CreateForkedScope();
        copied.Next().ShouldBe(3);
        ResolveForkCounter(secondFork, source).Next().ShouldBe(3);
        original.Next().ShouldBe(3);
        preparations.ShouldBe(1);
        bindings.ShouldBe(3);
    }

    [Fact]
    public void Fork_ReusesAncestorStateAndPreparesOtherLocalFactoriesAfresh()
    {
        var inherited = new ForkCounterConfiguration();
        var local = new ForkCounterConfiguration();
        var preparations = 0;
        var ordinary = new CounterConfiguration("ordinary", () => preparations++);
        var parent = ResolutionScope.Create();
        var ancestor = ResolveForkCounter(parent, inherited);
        ancestor.Next().ShouldBe(1);
        var scope = parent.CreateChildScope();
        ResolveForkCounter(scope, local).Next().ShouldBe(1);
        ResolveForkCounter(scope, ordinary).Next().ShouldBe(1);

        var fork = scope.CreateForkedScope();
        ResolveForkCounter(fork, inherited).Next().ShouldBe(2);
        ancestor.Value.ShouldBe(2);
        ResolveForkCounter(fork, local).Next().ShouldBe(2);
        ResolveForkCounter(scope, local).Next().ShouldBe(2);
        ResolveForkCounter(fork, ordinary).Next().ShouldBe(1);
        preparations.ShouldBe(2);
    }

    [Fact]
    public void Fork_InstallsEverySnapshotBeforeRebuildingAnOrdinaryComposite()
    {
        var observations = new List<int>();
        var middlePreparations = 0;
        var leaf = new ForkCounterConfiguration();
        var middle = new CounterWrapper(leaf, observations.Add, () => middlePreparations++);
        var outer = new ForkCounterConfiguration(middle);
        var scope = ResolutionScope.Create();
        var original = ResolveForkCounter(scope, outer);
        original.Next().ShouldBe(1);
        original.Next().ShouldBe(2);

        var copied = ResolveForkCounter(scope.CreateForkedScope(), outer);
        copied.Next().ShouldBe(3);
        original.Next().ShouldBe(3);
        observations.ShouldBe([1, 2, 1, 3]);
        middlePreparations.ShouldBe(2);
    }

    [Fact]
    public void WithContext_RetainsStateAndForksReprepareAdviceInTheCurrentContext()
    {
        var source = new ForkCounterConfiguration();
        var firstCounts = new List<int>();
        var secondCounts = new List<int>();
        var firstContext = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(firstCounts.Add)));
        var original = ResolveForkCounter(firstContext, source);
        original.Next().ShouldBe(1);

        var secondContext = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(secondCounts.Add)));
        var reboundScope = firstContext.WithContext(secondContext);
        ResolveForkCounter(reboundScope, source).Next().ShouldBe(2);
        var fork = reboundScope.CreateForkedScope();
        original.Next().ShouldBe(3);
        ResolveForkCounter(fork, source).Next().ShouldBe(3);

        firstCounts.ShouldBe([1, 2]);
        secondCounts.ShouldBe([1, 1]);
    }

    [Fact]
    public void FailedFork_DoesNotMutateTheSourceOrPreventRetry()
    {
        var fail = true;
        var source = new ForkCounterConfiguration(copy: () =>
        {
            if (fail)
                throw new InvalidOperationException("copy failed");
        });
        var scope = ResolutionScope.Create();
        var original = ResolveForkCounter(scope, source);
        original.Next().ShouldBe(1);
        Should.Throw<InvalidOperationException>(() => scope.CreateForkedScope());
        original.Next().ShouldBe(2);
        fail = false;
        ResolveForkCounter(scope.CreateForkedScope(), source).Next().ShouldBe(3);
        original.Next().ShouldBe(3);
    }

    [Fact]
    public void Fork_DuringConstructionIsRejected()
    {
        var source = new CounterConfiguration("constructing", Bind: scope =>
        {
            _ = scope.CreateForkedScope();
            return new Counter();
        });
        Should.Throw<InvalidOperationException>(() => ResolveForkCounter(ResolutionScope.Create(), source))
            .Message.ShouldContain("during execution graph construction");
    }

    private static ICounterExecution ResolveForkCounter(ResolutionScope scope, IConfigurationNode<ICounterExecution> source) =>
        scope.Resolve(source, static configuration => configuration.CreateExecutionFactory(), PrepareCounterAdvice);

    private sealed class ForkCounterState
    {
        public int Value { get; set; }
    }

    private sealed class ForkCounterConfiguration(IConfigurationNode<ICounterExecution>? child = null,
        Action? prepare = null, Action? bind = null, Action? copy = null) : IConfigurationNode<ICounterExecution>
    {
        public ExecutionFactory<ICounterExecution> CreateExecutionFactory()
        {
            prepare?.Invoke();
            return ExecutionFactory.Forkable<ForkCounterState, ICounterExecution>(new(),
                state =>
                {
                    copy?.Invoke();
                    return new() { Value = state.Value };
                },
                (state, scope) =>
                {
                    bind?.Invoke();
                    return new ForkCounterExecution(state, child is null ? null : ResolveForkCounter(scope, child));
                });
        }
    }

    private sealed class ForkCounterExecution(ForkCounterState state, ICounterExecution? child) : ICounterExecution
    {
        public int Value => state.Value;

        public int Next()
        {
            state.Value++;
            return child?.Next() ?? state.Value;
        }
    }
}
