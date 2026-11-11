namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public partial class ExecutorResolutionTests
{
    [Fact]
    public void RepeatedResolutionInOneScope_SharesPrivateFieldsByReference()
    {
        var scope = ResolutionScope.Create();
        var source = new CounterConfiguration("equal");
        var equal = new CounterConfiguration("equal");
        source.ShouldBe(equal);
        scope.Resolve(source).Next().ShouldBe(1);
        scope.Resolve(source).Next().ShouldBe(2);
        scope.Resolve(equal).Next().ShouldBe(1); // an equal configuration is another reference
    }

    [Fact]
    public void ChildSelections_DoNotHoistOrChangeWhenParentLaterSelects()
    {
        var parent = ResolutionScope.Create();
        var source = new CounterConfiguration("source");
        var child = parent.CreateChildScope();
        child.Resolve(source).Next().ShouldBe(1);
        parent.CreateChildScope().Resolve(source).Next().ShouldBe(1);
        parent.Resolve(source).Next().ShouldBe(1);
        child.Resolve(source).Next().ShouldBe(2);
        parent.CreateChildScope().Resolve(source).Next().ShouldBe(1);
    }

    // Each scope builds the private fields of what it resolves, while data a configuration holds is shared by every
    // executor built from it.
    [Fact]
    public void NewScope_ConstructsPrivateFieldsAndSharesTheDataOfTheConfiguration()
    {
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source");
        var parentCounts = new List<int>();
        var childCounts = new List<int>();
        IConfigurationNode<ICounterExecutor> observed = new CounterWrapper(source, parentCounts.Add, new Count());
        var parent = ResolutionScope.Create();
        var first = parent.Resolve(observed);
        for (var i = 0; i < 3; i++)
            first.Next();
        var child = parent.CreateChildScope();
        var second = child.Resolve(new CounterWrapper(observed, childCounts.Add));
        second.Next().ShouldBe(1);
        second.Next().ShouldBe(2);
        parentCounts.ShouldBe([1, 2, 3, 4, 5]);
        childCounts.ShouldBe([1, 2]);
        first.Next().ShouldBe(4);
        childCounts.Count.ShouldBe(2);
        parent.CreateChildScope().Resolve(source).Next().ShouldBe(1);
    }

    [Fact]
    public void ChildFirstCollision_TheCompositeInTheChildUsesTheChildsLeafAndItsControl()
    {
        IConfigurationNode<ICounterExecutor> leaf = new CounterConfiguration("leaf");
        var composite = new Composite(leaf);
        var parent = ResolutionScope.Create();
        var child = parent.CreateChildScope();
        var local = child.Resolve(leaf);
        local.Next().ShouldBe(1);
        var ancestor = parent.Resolve(composite);
        ancestor.Next().ShouldBe(1);
        var rebound = child.Resolve(composite);
        rebound.Next().ShouldBe(2);
        rebound.Control.Value.ShouldBe(2);
        ancestor.Control.Value.ShouldBe(1);
        rebound.Invocations.ShouldBe(1);
        ancestor.Invocations.ShouldBe(1);
        local.Next().ShouldBe(3);
        ancestor.Next().ShouldBe(2);
        rebound.Invocations.ShouldBe(1);
        local.Value.ShouldBe(3);
    }

    // A construction failure ends the scope tree: none of its scopes, child scopes included, resolves anything more, so
    // no executor built before the failure is reachable. A new scope retries from scratch.
    [Fact]
    public void ConstructionFault_EndsTheScopeTreeAndIsRetriedInANewScope()
    {
        var fault = new InvalidOperationException("prepare failure");
        var attempts = 0;
        var source = new CounterConfiguration("fault", () => { attempts++; throw fault; });
        var other = new CounterConfiguration("other");
        var parent = ResolutionScope.Create();
        Should.Throw<InvalidOperationException>(() => parent.Resolve(source)).ShouldBeSameAs(fault);
        var child = parent.CreateChildScope();
        Should.Throw<InvalidOperationException>(() => child.Resolve(other)).Message.ShouldContain("new scope");
        Should.Throw<InvalidOperationException>(() => parent.Resolve(other)).Message.ShouldContain("new scope");
        Should.Throw<InvalidOperationException>(() => child.TryResolve(other, out _, out _)).Message.ShouldContain("new scope");
        attempts.ShouldBe(1);
        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().TryResolve(source, out _, out _)).ShouldBeSameAs(fault);
        attempts.ShouldBe(2);
    }

    [Fact]
    public void FailedDependency_LeavesNoBuiltChildReachable()
    {
        var fault = new InvalidOperationException("child failure");
        var preparations = 0;
        var good = new CounterConfiguration("good");
        var bad = new CounterConfiguration("bad", () => { preparations++; throw fault; });
        var composite = new CounterConfiguration("composite", Bind: scope =>
        {
            scope.Resolve(good).Next();
            return scope.Resolve(bad);
        });
        var parent = ResolutionScope.Create();
        Should.Throw<InvalidOperationException>(() => parent.Resolve(composite)).ShouldBeSameAs(fault);
        Should.Throw<InvalidOperationException>(() => parent.Resolve(good)).Message.ShouldContain("new scope");
        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().Resolve(composite)).ShouldBeSameAs(fault);
        preparations.ShouldBe(2);
    }

    [Fact]
    public void BuildFailure_RetryInANewScopeReconstructsPrivateData()
    {
        var prepared = 0;
        var bound = 0;
        var fault = new InvalidOperationException("binding failure");
        var source = new CounterConfiguration("source", () => prepared++, _ => ++bound == 1 ? throw fault : null);
        var scope = ResolutionScope.Create();
        Should.Throw<InvalidOperationException>(() => scope.Resolve(source)).ShouldBeSameAs(fault);
        Should.Throw<InvalidOperationException>(() => scope.Resolve(source)).Message.ShouldContain("new scope");
        var retry = ResolutionScope.Create();
        retry.Resolve(source).Next().ShouldBe(1);
        retry.CreateChildScope().Resolve(source).Next().ShouldBe(1);
        prepared.ShouldBe(3);
        bound.ShouldBe(3);
    }

    [Fact]
    public void TryResolve_PropagatesConstructionFailuresOfEveryType()
    {
        var fault = new ArgumentException("invalid setting");
        var attempts = 0;
        var source = new CounterConfiguration("source", () => { attempts++; throw fault; });
        var scope = ResolutionScope.Create();
        Should.Throw<ArgumentException>(() => scope.TryResolve(source, out _, out _)).ShouldBeSameAs(fault);
        Should.Throw<InvalidOperationException>(() => scope.TryResolve(source, out _, out _)).Message.ShouldContain("new scope");
        attempts.ShouldBe(1);
    }

    [Fact]
    public void FreshSiblingDomain_CannotEvadeActiveSourceDetection()
    {
        var root = ResolutionScope.Create();
        CounterConfiguration? source = null;
        source = new CounterConfiguration("recursive siblings", Bind: _ => root.CreateChildScope().Resolve(source!));
        Should.Throw<InvalidOperationException>(() => root.CreateChildScope().Resolve(source)).Message.ShouldContain("Recursive");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RecursiveConstruction_FailsAcrossContexts(bool newContext)
    {
        CounterConfiguration? source = null;
        source = new CounterConfiguration("recursive", Bind: scope => (newContext ? scope.CreateChildScope() : scope).Resolve(source!));
        var scope = ResolutionScope.Create();
        var fault = Should.Throw<InvalidOperationException>(() => scope.Resolve(source));
        fault.Message.ShouldContain("Recursive");
        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().Resolve(source)).Message.ShouldContain("Recursive");
    }

    [Fact]
    public void RecursionDuringConstruction_IsDiagnosed()
    {
        var scope = ResolutionScope.Create();
        CounterConfiguration? source = null;
        source = new CounterConfiguration("recursive preparation", () => scope.Resolve(source!));
        Should.Throw<InvalidOperationException>(() => scope.Resolve(source)).Message.ShouldContain("Recursive");
    }

    [Fact]
    public void ObserverFailure_DoesNotRepeatConstructionOrRollbackOperation()
    {
        var prepared = 0;
        var observed = 0;
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source", () => prepared++);
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve(new CounterWrapper(source, _ =>
        {
            if (++observed == 1)
                throw new InvalidOperationException("observer");
        }));
        Should.Throw<InvalidOperationException>(() => executor.Next());
        executor.Value.ShouldBe(1);
        executor.Next().ShouldBe(2);
        prepared.ShouldBe(1);
        observed.ShouldBe(2);
    }

    private abstract record WrappingCounter(IConfigurationNode<ICounterExecutor> Child) : IConfigurationNode<ICounterExecutor>
    {
        public ICounterExecutor CreateExecutor(ResolutionScope scope) => Wrap(scope.Resolve(Child), scope);
        protected abstract ICounterExecutor Wrap(ICounterExecutor child, ResolutionScope scope);
    }

    private interface ICounterExecutor : IExecutor
    {
        int Value { get; }
        int Next();
    }

    private interface ICounterControl { int Value { get; } }

    // Private executor data or data a configuration holds.
    private sealed class Count
    {
        public int Value { get; set; }
    }

    private sealed class Counter(Count count) : ICounterExecutor, ICounterControl
    {
        public int Value => count.Value;
        public int Next() => ++count.Value;
    }

    private sealed record CounterConfiguration(string Name, Action? Prepare = null, Func<ResolutionScope, ICounterExecutor?>? Bind = null) : IConfigurationNode<ICounterExecutor>
    {
        public ICounterExecutor CreateExecutor(ResolutionScope scope)
        {
            Prepare?.Invoke();
            var count = new Count();
            return Bind?.Invoke(scope) ?? new Counter(count);
        }

    }

    private sealed record Composite(IConfigurationNode<ICounterExecutor> Child) : IConfigurationNode<CompositeExecutor>
    {
        public CompositeExecutor CreateExecutor(ResolutionScope scope)
        {
            var calls = new Count();
            var child = scope.Resolve(Child);
            var control = ExecutorControls.Find<ICounterControl>(child) ?? throw new InvalidOperationException("The child offers no counter control.");
            return new CompositeExecutor(child, control, calls);
        }
    }

    private sealed class CompositeExecutor(ICounterExecutor child, ICounterControl control, Count calls) : IExecutor
    {
        public ICounterControl Control { get; } = control;
        public int Invocations => calls.Value;
        public int Next()
        {
            var result = child.Next();
            calls.Value++;
            return result;
        }
    }

    // Counts the calls of the executor it wraps, into the count it holds or else into one of that executor.
    private sealed record CounterWrapper(IConfigurationNode<ICounterExecutor> Inner, Action<int> Observe, Count? Calls = null) : WrappingCounter(Inner)
    {
        protected override ICounterExecutor Wrap(ICounterExecutor child, ResolutionScope scope) =>
            new ObservingExecutor(child, Calls ?? new Count(), Observe);
    }

    // A transparent wrapper: it forwards the controls of the executor it wraps.
    private sealed class ObservingExecutor(ICounterExecutor child, Count calls, Action<int> observe) : ICounterExecutor, IExecutorControlProvider
    {
        public int Value => child.Value;
        public int Next()
        {
            var result = child.Next();
            observe(++calls.Value);
            return result;
        }
        public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(child);
    }
}
