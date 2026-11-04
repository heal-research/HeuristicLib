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

    [Fact]
    public void NewContext_ConstructsPrivateFieldsAndSharesAdviceData()
    {
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source");
        var parentCounts = new List<int>();
        var childCounts = new List<int>();
        var wrapped = new List<IConfigurationNode<ICounterExecutor>>();
        var parent = ResolutionScope.Create();
        parent.AddAspect(Aspect.Create(Pointcut.Reference(source), new CounterAdvice((executor, original) =>
            {
                wrapped.Add(original);
                return executor;
            })));
        parent.AddAspect(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(parentCounts.Add)));
        var first = parent.Resolve(source);
        for (var i = 0; i < 3; i++)
            first.Next();
        var child = parent.CreateChildScope();
        child.AddAspect(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(childCounts.Add)));
        var second = child.Resolve(source);
        second.Next().ShouldBe(1);
        second.Next().ShouldBe(2);
        parentCounts.ShouldBe([1, 2, 3, 4, 5]);
        childCounts.ShouldBe([1, 2]);
        wrapped.ShouldBe([source, source]);
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
        var observed = 0;
        var child = parent.CreateChildScope();
        child.AddAspect(Aspect.Create(Pointcut.Reference(leaf), new CounterObservationAdvice(_ => observed++)));
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
        observed.ShouldBe(3);
        ancestor.Next().ShouldBe(2);
        rebound.Invocations.ShouldBe(1);
        local.Value.ShouldBe(3);
    }

    [Fact]
    public void Ordering_ChildScopesWrapCloser_AndWithinAScopeTheAspectAddedLastWrapsClosest()
    {
        var events = new List<string>();
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source");
        var module = Aspect.Create(Pointcut.Reference(source), new TraceAdvice("PM", events));
        var parent = ResolutionScope.Create();
        parent.AddAspect(module);
        parent.AddAspect(module);
        parent.AddAspect(Aspect.Create(Pointcut.Reference(source), new TraceAdvice("PC", events)));
        var child = parent.CreateChildScope();
        child.AddAspect(Aspect.Create(Pointcut.Reference(source), new TraceAdvice("C1", events)));
        child.AddAspect(Aspect.Create(Pointcut.Reference(source), new TraceAdvice("C2", events)));
        child.AddAspect(Aspect.Create(Pointcut.Reference(source), new TraceAdvice("CM", events)));
        child.Resolve(source).Next();
        events.ShouldBe(["PM+", "PC+", "C1+", "C2+", "CM+", "CM-", "C2-", "C1-", "PC-", "PM-"]);
    }

    [Fact]
    public void GeneratedWrappers_AreNotTargets_ButExplicitWrappersAre()
    {
        IConfigurationNode<ICounterExecutor> leaf = new CounterConfiguration("leaf");
        IConfigurationNode<ICounterExecutor> wrapper = new CounterWrapper(leaf, _ => { });
        var observed = 0;
        var scope = ResolutionScope.Create();
        scope.AddAspect(Aspect.Create(Pointcut.Reference(leaf), new CounterObservationAdvice(_ => { })));
        scope.AddAspect(Aspect.Create(Pointcut.Reference(wrapper), new CounterObservationAdvice(_ => observed++)));
        scope.Resolve(leaf).Next();
        observed.ShouldBe(0);
        scope.Resolve(wrapper).Next();
        observed.ShouldBe(1);
    }

    [Fact]
    public void UnmatchedReferenceDeclaration_DoesNotObserveOrRebuild()
    {
        var preparations = 0;
        var observed = 0;
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source", () => preparations++);
        IConfigurationNode<ICounterExecutor> other = new CounterConfiguration("other");
        var scope = ResolutionScope.Create();
        scope.AddAspect(Aspect.Create(Pointcut.Reference(other), new CounterObservationAdvice(_ => observed++)));

        var executor = scope.Resolve(source);
        executor.ShouldBeOfType<Counter>().Next().ShouldBe(1);
        scope.Resolve(source).Next().ShouldBe(2);
        observed.ShouldBe(0);
        preparations.ShouldBe(1);
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
        var child = NewContext(parent);
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
        NewContext(retry).Resolve(source).Next().ShouldBe(1);
        prepared.ShouldBe(3);
        bound.ShouldBe(3);
    }

    // A failed wrap would leave the executor with part of its advice, so it ends the tree like any construction failure.
    [Fact]
    public void AdviceFailure_EndsTheScopeTreeAndIsRetriedInANewScope()
    {
        var fault = new InvalidOperationException("advice failure");
        var wraps = 0;
        var prepared = 0;
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source", () => prepared++);
        var aspect = Aspect.Create(Pointcut.Reference(source), new CounterAdvice((executor, _) => ++wraps == 1 ? throw fault : executor));
        var scope = ResolutionScope.Create();
        scope.AddAspect(aspect);
        Should.Throw<InvalidOperationException>(() => scope.Resolve(source)).ShouldBeSameAs(fault);
        Should.Throw<InvalidOperationException>(() => scope.Resolve(source)).Message.ShouldContain("new scope");
        var retry = ResolutionScope.Create();
        retry.AddAspect(aspect);
        retry.Resolve(source).Next().ShouldBe(1);
        NewContext(retry).Resolve(source).Next().ShouldBe(1);
        prepared.ShouldBe(3);
        wraps.ShouldBe(3);
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

    [Fact]
    public void IndependentAdviceInstancesKeepSeparateData()
    {
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source");
        var counts = new List<int>();
        var scope = ResolutionScope.Create();
        scope.AddAspect(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(counts.Add)));
        scope.AddAspect(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(counts.Add)));
        scope.Resolve(source).Next();
        scope.Resolve(source).Next();
        counts.ShouldBe([1, 1, 2, 2]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RecursiveConstruction_FailsAcrossContexts(bool newContext)
    {
        CounterConfiguration? source = null;
        source = new CounterConfiguration("recursive", Bind: scope => (newContext ? NewContext(scope) : scope).Resolve(source!));
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
        scope.AddAspect(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(_ =>
        {
            if (++observed == 1)
                throw new InvalidOperationException("observer");
        })));
        var executor = scope.Resolve(source);
        Should.Throw<InvalidOperationException>(() => executor.Next());
        executor.Value.ShouldBe(1);
        executor.Next().ShouldBe(2);
        prepared.ShouldBe(1);
        observed.ShouldBe(2);
    }

    // Advice is matched to the configuration before duplicates are removed, so each pairing applies where it matches.
    [Fact]
    public void OneAdviceWithTwoPointcuts_AppliesThroughThePairingThatMatches()
    {
        IConfigurationNode<ICounterExecutor> first = new CounterConfiguration("first");
        IConfigurationNode<ICounterExecutor> second = new CounterConfiguration("second");
        var observed = new List<int>();
        var advice = new CounterObservationAdvice(observed.Add);
        var scope = ResolutionScope.Create();
        scope.AddAspect(Aspect.Create(Pointcut.Reference(first), advice));
        scope.AddAspect(Aspect.Create(Pointcut.Reference(second), advice));

        scope.Resolve(second).Next().ShouldBe(1);
        observed.ShouldBe([1]);
        scope.Resolve(first).Next().ShouldBe(1);
        observed.ShouldBe([1, 2]); // the count lives on the advice
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 1)]

    public void ConstructionKeepsPrivateStateAndAppliesTheRequestingAspects(bool policy, int secondCount)
    {
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("child");
        var observed = new List<int>();
        var currentContext = new List<int>();
        var parent = ResolutionScope.Create();
        parent.AddAspect(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(observed.Add)));
        parent.Resolve(source).Next().ShouldBe(1);

        var contextual = parent.CreateChildScope();
        contextual.AddAspect(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(currentContext.Add)));
        contextual.Resolve(source).Next().ShouldBe(secondCount);

        observed.ShouldBe([1, 2]);
        currentContext.ShouldBe([1]);
    }

    private static ResolutionScope NewContext(ResolutionScope parent)
    {
        IConfigurationNode<ICounterExecutor> unrelated = new CounterConfiguration("unrelated");
        var scope = parent.CreateChildScope();
        scope.AddAspect(Aspect.Create(Pointcut.Reference(unrelated), new CounterObservationAdvice(_ => { })));
        return scope;
    }

    private abstract record WrappingCounter(IConfigurationNode<ICounterExecutor> Child) : IConfigurationNode<ICounterExecutor>
    {
        public ICounterExecutor CreateExecutor(ResolutionScope scope) => Wrap(scope.Resolve(Child), scope);
        protected abstract ICounterExecutor Wrap(ICounterExecutor child, ResolutionScope scope);
    }

    private sealed class CounterAdvice(Func<ICounterExecutor, IConfigurationNode<ICounterExecutor>, ICounterExecutor> wrap)
        : Advice<IConfigurationNode<ICounterExecutor>, ICounterExecutor>
    {
        public override ICounterExecutor Wrap(ICounterExecutor executor, IConfigurationNode<ICounterExecutor> source) => wrap(executor, source);
    }

    // Counts the calls of every executor it wraps; the count lives on the advice.
    private sealed class CounterObservationAdvice(Action<int> observe) : Advice<IConfigurationNode<ICounterExecutor>, ICounterExecutor>
    {
        private readonly Count calls = new();
        public override ICounterExecutor Wrap(ICounterExecutor executor, IConfigurationNode<ICounterExecutor> source) => new ObservingExecutor(executor, calls, observe);
    }

    private sealed class TraceAdvice(string name, List<string> events) : Advice<IConfigurationNode<ICounterExecutor>, ICounterExecutor>
    {
        public override ICounterExecutor Wrap(ICounterExecutor executor, IConfigurationNode<ICounterExecutor> source) => new TraceExecutor(executor, name, events);
    }

    private sealed class AdviceGroup : Aspect
    {
        public AdviceGroup(params IReadOnlyList<Aspect> aspects)
        {
            Advices = [.. aspects.SelectMany(aspect => aspect)];
        }
    }

    private interface ICounterExecutor : IExecutor
    {
        int Value { get; }
        int Next();
    }

    private interface ICounterControl { int Value { get; } }

    // Private executor data or explicitly supplied advice data.
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

    private sealed record CounterWrapper(IConfigurationNode<ICounterExecutor> Inner, Action<int> Observe) : WrappingCounter(Inner)
    {
        protected override ICounterExecutor Wrap(ICounterExecutor child, ResolutionScope scope) =>
            new ObservingExecutor(child, new Count(), Observe);
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

    private sealed class TraceExecutor(ICounterExecutor child, string name, List<string> events) : ICounterExecutor
    {
        public int Value => child.Value;
        public int Next()
        {
            events.Add(name + "+");
            var result = child.Next();
            events.Add(name + "-");
            return result;
        }
    }
}
