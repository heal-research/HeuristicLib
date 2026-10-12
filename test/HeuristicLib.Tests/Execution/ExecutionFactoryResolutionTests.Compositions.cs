namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public partial class ExecutionFactoryResolutionTests
{
    [Fact]
    public void InternalComposition_HidesPlumbingAndIsolatesItsStateFromTheUserGraph()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        IConfigurationNode<ICounterExecution> plumbing = new CounterConfiguration("plumbing");
        ICounterExecution? privatePlumbing = null;
        IConfigurationNode<ICounterExecution> composition = new ExtraChildWrapper(source, plumbing, node => privatePlumbing = node);
        var offered = new List<IConfigurationNode>();
        var observed = new List<int>();
        var scope = ResolutionScope.Create(builder => builder.Wrap(AllCounters.And(configuration =>
        {
            offered.Add(configuration);
            return true;
        }), original => new CounterWrapper(original, observed.Add)));
        var external = scope.Resolve(plumbing);
        external.Next().ShouldBe(1);
        offered.Clear();
        observed.Clear();

        var execution = scope.Resolve(source, static configuration => configuration.CreateExecutionFactory(), composedAs: composition);
        offered.ShouldBe([source]);
        privatePlumbing.ShouldNotBeNull().ShouldBeOfType<Counter>();
        privatePlumbing.Value.ShouldBe(0);
        privatePlumbing.Next().ShouldBe(1);
        observed.ShouldBeEmpty();
        execution.Next().ShouldBe(1);
        observed.ShouldBe([1]);
        external.Value.ShouldBe(1);
        scope.Resolve(source, static configuration => configuration.CreateExecutionFactory(), composition).ShouldBeSameAs(execution);
    }

    [Fact]
    public void InternalComposition_RebindsAroundSourceObservers_WithoutResettingEitherState()
    {
        var sourcePreparations = 0;
        var compositionPreparations = 0;
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source", () => sourcePreparations++);
        var compositionCalls = new List<int>();
        var owner = new ComposedCounter(source, original => new CounterWrapper(original, compositionCalls.Add, () => compositionPreparations++));
        var parent = ResolutionScope.Create();
        var first = parent.Resolve(owner);
        first.Next().ShouldBe(1);
        parent.Resolve(source).Next().ShouldBe(2);
        compositionCalls.ShouldBe([1]);

        var observed = new List<int>();
        var child = parent.CreateChildScope(builder => builder.Wrap(source, original => new CounterWrapper(original, observed.Add)));
        var rebound = child.Resolve(owner);
        rebound.ShouldNotBeSameAs(first);
        rebound.Next().ShouldBe(3);
        first.Next().ShouldBe(4);
        compositionCalls.ShouldBe([1, 2, 3]);
        observed.ShouldBe([1]);
        sourcePreparations.ShouldBe(1);
        compositionPreparations.ShouldBe(1);
        child.Resolve(owner).ShouldBeSameAs(rebound);
    }

    [Fact]
    public void InternalComposition_NullUsesTheNormalBinding()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var scope = ResolutionScope.Create();
        scope.Resolve(source, static configuration => configuration.CreateExecutionFactory(), composedAs: null).ShouldBeSameAs(scope.Resolve(source));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InternalComposition_RejectsSourceResolutionBelowItsRoot(bool retainedScope)
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        IConfigurationNode<ICounterExecution> nested = new ForwardingWrapper(source);
        var key = new object();
        IConfigurationNode<ICounterExecution> composition = new CounterConfiguration("composition", Bind: scope =>
            retainedScope ? scope.GetOrCreateChildScope(key).Resolve(source) : scope.Resolve(nested));
        var scope = ResolutionScope.Create();
        Should.Throw<InvalidOperationException>(() => scope.Resolve(source, static configuration => configuration.CreateExecutionFactory(), composition))
            .Message.ShouldContain("directly from its root factory");
        scope.Resolve(source).Next().ShouldBe(1);
    }

    [Fact]
    public void InternalComposition_DeferredDependenciesStayHidden_AndDirectSourceResolutionRetainsItsTarget()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        IConfigurationNode<ICounterExecution> plumbing = new CounterConfiguration("plumbing");
        ResolutionScope? retained = null;
        IConfigurationNode<ICounterExecution> composition = new CounterConfiguration("composition", Bind: frame =>
        {
            retained = frame;
            return frame.Resolve(source);
        });
        var offered = new List<IConfigurationNode>();
        var scope = ResolutionScope.Create(builder => builder.Wrap(AllCounters.And(configuration =>
        {
            offered.Add(configuration);
            return true;
        }), original => new ForwardingWrapper(original)));
        var execution = scope.Resolve(source, static configuration => configuration.CreateExecutionFactory(), composition);
        retained.ShouldNotBeNull().Resolve(source).ShouldBeSameAs(execution);
        retained.GetOrCreateChildScope(new object()).Resolve(plumbing).ShouldBeOfType<Counter>();
        offered.ShouldBe([source]);
    }

    [Fact]
    public void InternalComposition_OwnersKeepIndependentCompositionState_WithSharedSourceState()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var calls = new List<int>();
        var preparations = 0;
        IConfigurationNode<ICounterExecution> composition = new CounterWrapper(source, calls.Add, () => preparations++);
        var firstOwner = new ComposedCounter(source, _ => composition);
        var secondOwner = new ComposedCounter(source, _ => composition);
        var scope = ResolutionScope.Create();
        scope.Resolve(source);
        scope.Resolve(firstOwner).Next().ShouldBe(1);
        scope.Resolve(secondOwner).Next().ShouldBe(2);
        scope.Resolve(firstOwner).Next().ShouldBe(3);
        calls.ShouldBe([1, 1, 2]);
        preparations.ShouldBe(2);
    }

    [Fact]
    public void InternalComposition_InARegistrationWrapper_PreservesTheSuppliedInnerChain()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var innerCalls = new List<int>();
        var compositionCalls = new List<int>();
        var scope = ResolutionScope.Create(builder => builder
            .Wrap(source, original => new CounterWrapper(original, innerCalls.Add))
            .Wrap(source, original => new ComposedCounter(original, child => new CounterWrapper(child, compositionCalls.Add))));
        scope.Resolve(source).Next().ShouldBe(1);
        innerCalls.ShouldBe([1]);
        compositionCalls.ShouldBe([1]);
    }

    [Fact]
    public void InternalComposition_PreparationFaultIsStickyAcrossContexts()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var fault = new InvalidOperationException("composition preparation failed");
        var preparations = 0;
        var owner = new ComposedCounter(source, original => new CounterWrapper(original, _ => { }, () =>
        {
            preparations++;
            throw fault;
        }));
        var scope = ResolutionScope.Create();
        Should.Throw<InvalidOperationException>(() => scope.Resolve(owner)).ShouldBeSameAs(fault);
        Should.Throw<InvalidOperationException>(() => scope.Resolve(owner)).ShouldBeSameAs(fault);
        Should.Throw<InvalidOperationException>(() => NewContext(scope).Resolve(owner)).ShouldBeSameAs(fault);
        preparations.ShouldBe(1);
        scope.Resolve(source).Next().ShouldBe(1);
    }

    [Fact]
    public void InternalComposition_BindingFaultIsLocalToItsContext_AndRetriesWithoutPreparation()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var fault = new InvalidOperationException("composition binding failed");
        var preparations = 0;
        var bindings = 0;
        var owner = new ComposedCounter(source, original => new CounterConfiguration("composition", () => preparations++, frame =>
            ++bindings == 1 ? throw fault : frame.Resolve(original)));
        var scope = ResolutionScope.Create();
        Should.Throw<InvalidOperationException>(() => scope.Resolve(owner)).ShouldBeSameAs(fault);
        Should.Throw<InvalidOperationException>(() => scope.Resolve(owner)).ShouldBeSameAs(fault);
        NewContext(scope).Resolve(owner).Next().ShouldBe(1);
        preparations.ShouldBe(1);
        bindings.ShouldBe(2);
    }

    private sealed record ComposedCounter(IConfigurationNode<ICounterExecution> Source,
        Func<IConfigurationNode<ICounterExecution>, IConfigurationNode<ICounterExecution>> Compose) : IConfigurationNode<ICounterExecution>
    {
        public ExecutionFactory<ICounterExecution> CreateExecutionFactory()
        {
            var composition = Compose(Source);
            return scope => scope.Resolve(Source, static configuration => configuration.CreateExecutionFactory(), composedAs: composition);
        }
    }
}
