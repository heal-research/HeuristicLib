namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public partial class ExecutorFactoryResolutionTests
{
    [Fact]
    public void Selection_UsesReferenceIdentityAndReusesPreparedLeafState()
    {
        var scope = ResolutionScope.Create();
        var source = new CounterConfiguration("equal");
        var equal = new CounterConfiguration("equal");
        source.ShouldBe(equal);
        var executor = scope.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice);
        scope.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).ShouldBeSameAs(executor);
        scope.Resolve(equal, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).ShouldNotBeSameAs(executor);
        executor.Next().ShouldBe(1);
        scope.Resolve(equal, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
    }

    [Fact]
    public void ChildSelections_DoNotHoistOrChangeWhenParentLaterSelects()
    {
        var parent = ResolutionScope.Create();
        var source = new CounterConfiguration("source");
        var child = parent.CreateChildScope();
        var first = child.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice);
        var sibling = parent.CreateChildScope().Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice);
        var ancestor = parent.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice);
        first.ShouldNotBeSameAs(sibling);
        first.ShouldNotBeSameAs(ancestor);
        child.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).ShouldBeSameAs(first);
        parent.CreateChildScope().Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).ShouldBeSameAs(ancestor);
    }

    [Fact]
    public void RebindingChangesPredecessorsButRetainsLeafAndAdviceState()
    {
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source");
        var parentCounts = new List<int>();
        var childCounts = new List<int>();
        var callbacks = 0;
        var preparations = 0;
        var parent = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new CounterAdvice(original =>
        {
            original.ShouldBeSameAs(source);
            callbacks++;
            return new CounterObservationAdvice(parentCounts.Add, () => preparations++).CreateWrapperFactory(original);
        })));
        var first = parent.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice);
        for (var i = 0; i < 3; i++)
            first.Next();
        var child = parent.CreateChildScope(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(childCounts.Add)));
        var second = child.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice);
        second.ShouldNotBeSameAs(first);
        second.Next().ShouldBe(4);
        second.Next().ShouldBe(5);
        parentCounts.ShouldBe([1, 2, 3, 4, 5]);
        childCounts.ShouldBe([1, 2]);
        callbacks.ShouldBe(1);
        preparations.ShouldBe(1);
        first.Next().ShouldBe(6);
        childCounts.Count.ShouldBe(2);
        parent.CreateChildScope().Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).ShouldNotBeSameAs(first);
    }

    [Fact]
    public void ChildFirstCollision_PreservesTheCompositeDependencyAndItsControl()
    {
        IConfigurationNode<ICounterExecutor> leaf = new CounterConfiguration("leaf");
        var composite = new Composite(leaf);
        var parent = ResolutionScope.Create();
        var observed = 0;
        var child = parent.CreateChildScope(Aspect.Create(Pointcut.Reference(leaf), new CounterObservationAdvice(_ => observed++)));
        var local = child.Resolve(leaf, static source => source.CreateExecutorFactory(), PrepareCounterAdvice);
        local.Next().ShouldBe(1);
        var ancestor = parent.Resolve(composite);
        ancestor.Next().ShouldBe(1);
        var rebound = child.Resolve(composite);
        rebound.ShouldNotBeSameAs(ancestor);
        rebound.Next().ShouldBe(2);
        rebound.Control.ShouldBeSameAs(ancestor.Control);
        rebound.Control.Value.ShouldBe(2);
        rebound.Calls.ShouldBe(2);
        ancestor.Calls.ShouldBe(2);
        local.Next().ShouldBe(2);
        observed.ShouldBe(3);
        ancestor.Next().ShouldBe(3);
        rebound.Calls.ShouldBe(3);
        local.Value.ShouldBe(2);
    }

    [Fact]
    public void Controls_AreProjectedFromRawBindingOnlyAfterCompleteSuccess()
    {
        IConfigurationNode<ICounterExecutor> leaf = new CounterConfiguration("leaf");
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(leaf), new CounterObservationAdvice(_ => { })));
        var observed = scope.Resolve(leaf, static source => source.CreateExecutorFactory(), PrepareCounterAdvice, static executor => executor as ICounterControl, out var control);
        observed.ShouldNotBeAssignableTo<ICounterControl>();
        control.ShouldNotBeNull();
        observed.Next();
        control.Value.ShouldBe(1);

        IConfigurationNode<ICounterExecutor> explicitWrapper = new CounterWrapper(leaf, _ => { });
        scope.Resolve(explicitWrapper, static source => source.CreateExecutorFactory(), PrepareCounterAdvice, static executor => executor as ICounterControl, out var absent);
        absent.ShouldBeNull();
        IConfigurationNode<ICounterExecutor> forwarding = new ForwardingWrapper(leaf);
        scope.Resolve(forwarding, static source => source.CreateExecutorFactory(), PrepareCounterAdvice, static executor => executor as ICounterControl, out var forwarded);
        forwarded.ShouldNotBeNull().Value.ShouldBe(1);
    }

    [Fact]
    public void Ordering_UsesScopeLayersThenInstallationSequence_WithReverseEntryOrder()
    {
        var events = new List<string>();
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source");
        var module = Aspect.Create(Pointcut.Reference(source), new TraceAdvice("PM", events));
        var parent = ResolutionScope.Create(module,
            module,
            Aspect.Create(Pointcut.Reference(source), new TraceAdvice("PC", events)));
        var child = parent.CreateChildScope(Aspect.Create(Pointcut.Reference(source), new TraceAdvice("C1", events)),
            Aspect.Create(Pointcut.Reference(source), new TraceAdvice("C2", events)),
            Aspect.Create(Pointcut.Reference(source), new TraceAdvice("CM", events)));
        child.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next();
        events.ShouldBe(["PC+", "PM+", "CM+", "C2+", "C1+", "C1-", "C2-", "CM-", "PM-", "PC-"]);
    }

    [Fact]
    public void GeneratedWrappers_AreNotTargets_ButExplicitWrappersAre()
    {
        IConfigurationNode<ICounterExecutor> leaf = new CounterConfiguration("leaf");
        IConfigurationNode<ICounterExecutor> wrapper = new CounterWrapper(leaf, _ => { });
        var observed = 0;
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(leaf), new CounterObservationAdvice(_ => { })),
            Aspect.Create(Pointcut.Reference(wrapper), new CounterObservationAdvice(_ => observed++)));
        scope.Resolve(leaf, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next();
        observed.ShouldBe(0);
        scope.Resolve(wrapper, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next();
        observed.ShouldBe(1);
    }


    [Fact]
    public void UnmatchedReferenceDeclaration_DoesNotObserveOrRepeatPreparation()
    {
        var preparations = 0;
        var observed = 0;
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source", () => preparations++);
        IConfigurationNode<ICounterExecutor> other = new CounterConfiguration("other");
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(other), new CounterObservationAdvice(_ => observed++)));

        var executor = scope.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice);
        executor.Next().ShouldBe(1);
        scope.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).ShouldBeSameAs(executor);
        executor.Next().ShouldBe(2);
        observed.ShouldBe(0);
        preparations.ShouldBe(1);
    }


    [Fact]
    public void PreparationFault_IsRetriedAcrossInheritedContextsAndTryResolve()
    {
        var fault = new InvalidOperationException("prepare failure");
        var attempts = 0;
        var source = new CounterConfiguration("fault", () => { attempts++; throw fault; });
        var parent = ResolutionScope.Create();
        Should.Throw<InvalidOperationException>(() => parent.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice)).ShouldBeSameAs(fault);
        var child = NewContext(parent);
        Should.Throw<InvalidOperationException>(() => child.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice)).ShouldBeSameAs(fault);
        child.TryResolve(source, out var executor, out var reason).ShouldBeFalse();
        executor.ShouldBeNull();
        reason.ShouldBe("prepare failure");
        attempts.ShouldBe(3);
        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice)).ShouldBeSameAs(fault);
        attempts.ShouldBe(4);
    }

    [Fact]
    public void FailedDependency_IsRetriedInItsOwnersState_AndCompletedSiblingSurvives()
    {
        var fault = new InvalidOperationException("child failure");
        var preparations = 0;
        var good = new CounterConfiguration("good");
        var bad = new CounterConfiguration("bad", () => { preparations++; throw fault; });
        var composite = new CounterConfiguration("composite", Bind: scope =>
        {
            scope.Resolve(good, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next();
            return scope.Resolve(bad, static source => source.CreateExecutorFactory(), PrepareCounterAdvice);
        });
        var parent = ResolutionScope.Create();
        Should.Throw<InvalidOperationException>(() => parent.Resolve(composite)).ShouldBeSameAs(fault);
        Should.Throw<InvalidOperationException>(() => parent.Resolve(composite)).ShouldBeSameAs(fault);
        parent.Resolve(good, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Value.ShouldBe(2);
        Should.Throw<InvalidOperationException>(() => NewContext(parent).Resolve(composite)).ShouldBeSameAs(fault);
        parent.Resolve(good, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Value.ShouldBe(3);
        preparations.ShouldBe(3);
    }

    [Fact]
    public void ConstructionFailureIsRetriedWithoutRepeatingSuccessfulPreparation()
    {
        var prepared = 0;
        var bound = 0;
        var fault = new InvalidOperationException("binding failure");
        var source = new CounterConfiguration("source", () => prepared++, _ => ++bound == 1 ? throw fault : new Counter());
        var scope = ResolutionScope.Create();
        Should.Throw<InvalidOperationException>(() => scope.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice)).ShouldBeSameAs(fault);
        scope.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
        NewContext(scope).Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
        prepared.ShouldBe(1);
        bound.ShouldBe(3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AdvicePreparationFailureIsRetried(bool inPreparation)
    {
        var fault = new InvalidOperationException("wrapper failure");
        var callbacks = 0;
        var preparations = 0;
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source");
        var parent = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new CounterAdvice(original =>
        {
            callbacks++;
            if (!inPreparation)
                throw fault;
            return new CounterObservationAdvice(_ => { }, () => { preparations++; throw fault; }).CreateWrapperFactory(original);
        })));
        Should.Throw<InvalidOperationException>(() => parent.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice)).ShouldBeSameAs(fault);
        Should.Throw<InvalidOperationException>(() => NewContext(parent).Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice)).ShouldBeSameAs(fault);
        callbacks.ShouldBe(2);
        preparations.ShouldBe(inPreparation ? 2 : 0);
    }

    [Fact]
    public void AdviceApplicationFailureIsRetriedBeforeControlProjection()
    {
        var fault = new InvalidOperationException("advice application failure");
        var applications = 0;
        var preparations = 0;
        var projections = 0;
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source");
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new CounterAdvice(original => new FaultingAdvice(() => preparations++, inner => ++applications == 1 ? throw fault : inner).CreateWrapperFactory(original))));
        Should.Throw<InvalidOperationException>(() => scope.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice, executor =>
        {
            projections++;
            return executor as ICounterControl;
        }, out _)).ShouldBeSameAs(fault);
        projections.ShouldBe(0);
        scope.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
        preparations.ShouldBe(1);
        applications.ShouldBe(2);
    }

    [Fact]
    public void ProjectionFailure_DoesNotPoisonACompletedBinding()
    {
        var source = new CounterConfiguration("source");
        var scope = ResolutionScope.Create();
        Should.Throw<ArgumentException>(() => scope.Resolve<IConfigurationNode<ICounterExecutor>, ICounterExecutor, ICounterControl>(source,
            static source => source.CreateExecutorFactory(), PrepareCounterAdvice, _ => throw new ArgumentException("projection"), out _));
        scope.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
    }

    [Fact]
    public void TryResolve_OnlyConvertsInvalidOperationExceptions_AndRetries()
    {
        var fault = new ArgumentException("invalid setting");
        var attempts = 0;
        var source = new CounterConfiguration("source", () => { attempts++; throw fault; });
        var scope = ResolutionScope.Create();
        Should.Throw<ArgumentException>(() => scope.TryResolve(source, out _, out _)).ShouldBeSameAs(fault);
        Should.Throw<ArgumentException>(() => scope.TryResolve(source, out _, out _)).ShouldBeSameAs(fault);
        attempts.ShouldBe(2);
    }

    [Fact]
    public void FreshSiblingDomain_CannotEvadeActiveSourceDetection()
    {
        var root = ResolutionScope.Create();
        CounterConfiguration? source = null;
        source = new CounterConfiguration("recursive siblings", Bind: _ => root.CreateChildScope().Resolve(source!, static source => source.CreateExecutorFactory(), PrepareCounterAdvice));
        Should.Throw<InvalidOperationException>(() => root.CreateChildScope().Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice)).Message.ShouldContain("Recursive");
    }

    [Fact]
    public void IndependentAdviceInstancesKeepSeparateState()
    {
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source");
        var counts = new List<int>();
        var scope = ResolutionScope.Create(
            Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(counts.Add)),
            Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(counts.Add)));
        scope.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next();
        scope.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next();
        counts.ShouldBe([1, 1, 2, 2]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RecursiveConstruction_FailsAcrossContexts(bool newContext)
    {
        CounterConfiguration? source = null;
        source = new CounterConfiguration("recursive", Bind: scope => (newContext ? NewContext(scope) : scope).Resolve(source!, static source => source.CreateExecutorFactory(), PrepareCounterAdvice));
        var scope = ResolutionScope.Create();
        var fault = Should.Throw<InvalidOperationException>(() => scope.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice));
        fault.Message.ShouldContain("Recursive");
        Should.Throw<InvalidOperationException>(() => scope.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice)).Message.ShouldContain("Recursive");
    }

    [Fact]
    public void RecursivePreparationIsDiagnosed()
    {
        var scope = ResolutionScope.Create();
        CounterConfiguration? source = null;
        source = new CounterConfiguration("recursive preparation", () => scope.Resolve(source!, static source => source.CreateExecutorFactory(), PrepareCounterAdvice));
        Should.Throw<InvalidOperationException>(() => scope.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice)).Message.ShouldContain("Recursive");

    }

    [Fact]
    public void ObserverFailure_DoesNotRepeatConstructionOrRollbackOperation()
    {
        var prepared = 0;
        var observed = 0;
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source", () => prepared++);
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(_ =>
        {
            if (++observed == 1)
                throw new InvalidOperationException("observer");
        })));
        var executor = scope.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice);
        Should.Throw<InvalidOperationException>(() => executor.Next());
        scope.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).ShouldNotBeSameAs(executor);
        executor.Next().ShouldBe(2);
        prepared.ShouldBe(1);
        observed.ShouldBe(2);
    }

    [Fact]
    public void OneAdviceWithTwoPointcutsUsesItsFirstPairingEvenWhenTheSecondWouldMatch()
    {
        IConfigurationNode<ICounterExecutor> first = new CounterConfiguration("first");
        IConfigurationNode<ICounterExecutor> second = new CounterConfiguration("second");
        var observed = new List<int>();
        var advice = new CounterObservationAdvice(observed.Add);
        var scope = ResolutionScope.Create(
            Aspect.Create(Pointcut.Reference(first), advice),
            Aspect.Create(Pointcut.Reference(second), advice));

        scope.Resolve(second, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
        observed.ShouldBeEmpty();
        scope.Resolve(first, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
        observed.ShouldBe([1]);
    }

    [Theory]
    [InlineData(ChildScopePolicy.Fresh, 1)]
    [InlineData(ChildScopePolicy.Retained, 2)]
    [InlineData(ChildScopePolicy.Shared, 2)]
    public void ChildScopeSelectorKeepsAdviceStateAccordingToItsActivationPolicy(ChildScopePolicy policy, int secondCount)
    {
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("child");
        var observed = new List<int>();
        var currentContext = new List<int>();
        var aspect = Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(observed.Add));
        var parent = ResolutionScope.Create(aspect);
        var selector = new ChildScopeSelector(policy);
        var original = selector.GetScopeFor(source, parent);
        original.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next().ShouldBe(1);

        var contextual = parent.With(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(currentContext.Add)));
        var resumed = selector.GetScopeFor(source, contextual);
        resumed.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next().ShouldBe(secondCount);

        observed.ShouldBe([1, secondCount]);
        currentContext.ShouldBe([1]);
    }

    private static ResolutionScope NewContext(ResolutionScope parent)
    {
        IConfigurationNode<ICounterExecutor> unrelated = new CounterConfiguration("unrelated");
        return parent.CreateChildScope(Aspect.Create(Pointcut.Reference(unrelated), new CounterObservationAdvice(_ => { })));
    }

    private sealed class FaultingAdvice(Action prepare, WrapperExecutorFactory<ICounterExecutor> apply) : CounterAdviceBase
    {
        public override WrapperExecutorFactory<ICounterExecutor> CreateWrapperFactory(IConfigurationNode<ICounterExecutor> configuration)
        {
            prepare();
            return apply;
        }
    }



    private static WrapperExecutorFactory<ICounterExecutor> PrepareCounterAdvice(Advice advice, IConfigurationNode<ICounterExecutor> source) =>
        ((CounterAdviceBase)advice).CreateWrapperFactory(source);

    private abstract record WrappingCounter(IConfigurationNode<ICounterExecutor> Child) : IConfigurationNode<ICounterExecutor>
    {
        public ExecutorFactory<ICounterExecutor> CreateExecutorFactory()
        {
            var advice = CreateWrapperFactory();
            return scope => advice(scope.Resolve(Child, static source => source.CreateExecutorFactory(), PrepareCounterAdvice));
        }
        public abstract WrapperExecutorFactory<ICounterExecutor> CreateWrapperFactory();
    }

    private abstract class CounterAdviceBase : Advice<IConfigurationNode<ICounterExecutor>>
    {
        public abstract WrapperExecutorFactory<ICounterExecutor> CreateWrapperFactory(IConfigurationNode<ICounterExecutor> configuration);
    }

    private sealed class CounterAdvice(Func<IConfigurationNode<ICounterExecutor>, WrapperExecutorFactory<ICounterExecutor>> prepare) : CounterAdviceBase
    {
        public override WrapperExecutorFactory<ICounterExecutor> CreateWrapperFactory(IConfigurationNode<ICounterExecutor> configuration) => prepare(configuration);
    }

    private sealed class CounterObservationAdvice(Action<int> observe, Action? prepare = null) : CounterAdviceBase
    {
        public override WrapperExecutorFactory<ICounterExecutor> CreateWrapperFactory(IConfigurationNode<ICounterExecutor> configuration)
        {
            prepare?.Invoke();
            var state = new Counter();
            return inner => new WrapperExecutor(inner, state, observe);
        }
    }

    private sealed class TraceAdvice(string name, List<string> events) : CounterAdviceBase
    {
        public override WrapperExecutorFactory<ICounterExecutor> CreateWrapperFactory(IConfigurationNode<ICounterExecutor> configuration) =>
            inner => new TraceExecutor(inner, name, events);
    }

    private sealed class AdviceGroup : Aspect
    {
        public AdviceGroup(params IReadOnlyList<Aspect> aspects)
        {
            Advices = [.. aspects.SelectMany(aspect => aspect.Advices)];
        }
    }

    private interface ICounterExecutor : IExecutor
    {
        int Value { get; }
        int Next();
    }

    private interface ICounterControl { int Value { get; } }


    private sealed class Counter : ICounterExecutor, ICounterControl
    {
        public int Value { get; private set; }
        public int Next() => ++Value;
    }

    private sealed record CounterConfiguration(string Name, Action? Prepare = null, Func<ResolutionScope, ICounterExecutor>? Bind = null) : IConfigurationNode<ICounterExecutor>
    {
        public ExecutorFactory<ICounterExecutor> CreateExecutorFactory()
        {
            Prepare?.Invoke();
            var counter = new Counter();
            return scope => Bind?.Invoke(scope) ?? counter;
        }
    }

    private sealed record Composite(IConfigurationNode<ICounterExecutor> Child) : IConfigurationNode<CompositeExecutor>
    {
        public ExecutorFactory<CompositeExecutor> CreateExecutorFactory()
        {
            var state = new Counter();
            return scope =>
            {
                var child = scope.Resolve(Child, static source => source.CreateExecutorFactory(), PrepareCounterAdvice, static raw => raw as ICounterControl, out var control);
                return new CompositeExecutor(child, control!, state);
            };
        }
    }

    private sealed class CompositeExecutor(ICounterExecutor child, ICounterControl control, Counter state) : IExecutor
    {
        public ICounterControl Control { get; } = control;
        public int Calls => state.Value;
        public int Next()
        {
            var result = child.Next();
            state.Next();
            return result;
        }
    }

    private sealed record CounterWrapper(IConfigurationNode<ICounterExecutor> Inner, Action<int> Observe, Action? Prepare = null) : WrappingCounter(Inner)
    {
        public override WrapperExecutorFactory<ICounterExecutor> CreateWrapperFactory()
        {
            Prepare?.Invoke();
            var state = new Counter();
            return inner => new WrapperExecutor(inner, state, Observe);
        }
    }

    private sealed class WrapperExecutor(ICounterExecutor child, Counter state, Action<int> observe) : ICounterExecutor
    {
        public int Value => child.Value;
        public int Next()
        {
            var result = child.Next();
            observe(state.Next());
            return result;
        }
    }

    private sealed record ForwardingWrapper(IConfigurationNode<ICounterExecutor> Inner) : WrappingCounter(Inner)
    {
        public override WrapperExecutorFactory<ICounterExecutor> CreateWrapperFactory() => inner => new ForwardingExecutor(inner);
    }

    private sealed class ForwardingExecutor(ICounterExecutor child) : ICounterExecutor, ICounterControl
    {
        public int Value => child.Value;
        public int Next() => child.Next();
    }

    private sealed record TraceWrapper(IConfigurationNode<ICounterExecutor> Inner, string Name, List<string> Events) : WrappingCounter(Inner)
    {
        public override WrapperExecutorFactory<ICounterExecutor> CreateWrapperFactory() => inner => new TraceExecutor(inner, Name, Events);
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
