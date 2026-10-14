namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public partial class ExecutionFactoryResolutionTests
{
    [Fact]
    public void Selection_UsesReferenceIdentityAndReusesPreparedLeafState()
    {
        var scope = ResolutionScope.Create();
        var source = new CounterConfiguration("equal");
        var equal = new CounterConfiguration("equal");
        source.ShouldBe(equal);
        var execution = scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
        scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).ShouldBeSameAs(execution);
        scope.Resolve(equal, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).ShouldNotBeSameAs(execution);
        execution.Next().ShouldBe(1);
        scope.Resolve(equal, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
    }

    [Fact]
    public void ChildSelections_DoNotHoistOrChangeWhenParentLaterSelects()
    {
        var parent = ResolutionScope.Create();
        var source = new CounterConfiguration("source");
        var child = parent.CreateChildScope();
        var first = child.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
        var sibling = parent.CreateChildScope().Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
        var ancestor = parent.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
        first.ShouldNotBeSameAs(sibling);
        first.ShouldNotBeSameAs(ancestor);
        child.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).ShouldBeSameAs(first);
        parent.CreateChildScope().Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).ShouldBeSameAs(ancestor);
    }

    [Fact]
    public void RebindingChangesPredecessorsButRetainsLeafAndAdviceState()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var parentCounts = new List<int>();
        var childCounts = new List<int>();
        var callbacks = 0;
        var preparations = 0;
        var parent = ResolutionScope.Create(builder => builder.Install(source, original =>
        {
            original.ShouldBeSameAs(source);
            callbacks++;
            return new CounterWrapper(original, parentCounts.Add, () => preparations++);
        }));
        var first = parent.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
        for (var i = 0; i < 3; i++)
            first.Next();
        var child = parent.CreateChildScope(builder => builder.Install(source, original => new CounterWrapper(original, childCounts.Add)));
        var second = child.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
        second.ShouldNotBeSameAs(first);
        second.Next().ShouldBe(4);
        second.Next().ShouldBe(5);
        parentCounts.ShouldBe([1, 2, 3, 4, 5]);
        childCounts.ShouldBe([1, 2]);
        callbacks.ShouldBe(1);
        preparations.ShouldBe(1);
        first.Next().ShouldBe(6);
        childCounts.Count.ShouldBe(2);
        parent.CreateChildScope().Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).ShouldNotBeSameAs(first);
    }

    [Fact]
    public void ChildFirstCollision_PreservesTheCompositeDependencyAndItsControl()
    {
        IConfigurationNode<ICounterExecution> leaf = new CounterConfiguration("leaf");
        var composite = new Composite(leaf);
        var parent = ResolutionScope.Create();
        var observed = 0;
        var child = parent.CreateChildScope(builder => builder.Install(leaf, original => new CounterWrapper(original, _ => observed++)));
        var local = child.Resolve(leaf, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
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
        IConfigurationNode<ICounterExecution> leaf = new CounterConfiguration("leaf");
        var scope = ResolutionScope.Create(builder => builder.Install(leaf, original => new CounterWrapper(original, _ => { })));
        var observed = scope.Resolve(leaf, static source => source.CreateExecutionFactory(), PrepareCounterAdvice, static execution => execution as ICounterControl, out var control);
        observed.ShouldNotBeAssignableTo<ICounterControl>();
        control.ShouldNotBeNull();
        observed.Next();
        control.Value.ShouldBe(1);

        IConfigurationNode<ICounterExecution> explicitWrapper = new CounterWrapper(leaf, _ => { });
        scope.Resolve(explicitWrapper, static source => source.CreateExecutionFactory(), PrepareCounterAdvice, static execution => execution as ICounterControl, out var absent);
        absent.ShouldBeNull();
        IConfigurationNode<ICounterExecution> forwarding = new ForwardingWrapper(leaf);
        scope.Resolve(forwarding, static source => source.CreateExecutionFactory(), PrepareCounterAdvice, static execution => execution as ICounterControl, out var forwarded);
        forwarded.ShouldNotBeNull().Value.ShouldBe(1);
    }

    [Fact]
    public void Ordering_UsesScopeLayersThenInstallationSequence_WithReverseEntryOrder()
    {
        var events = new List<string>();
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var module = new Module(b => b.Install(source, original => new TraceWrapper(original, "PM", events)));
        var parent = ResolutionScope.Create(builder =>
        {
            builder.Install(module);
            builder.Install(module);
            builder.Install(source, original => new TraceWrapper(original, "PC", events));
        });
        var child = parent.CreateChildScope(builder =>
        {
            builder.Install(source, original => new TraceWrapper(original, "C1", events));
            builder.Install(source, original => new TraceWrapper(original, "C2", events));
            builder.Install(new Module(b => b.Install(source, original => new TraceWrapper(original, "CM", events))));
        });
        child.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next();
        events.ShouldBe(["PC+", "PM+", "CM+", "C2+", "C1+", "C1-", "C2-", "CM-", "PM-", "PC-"]);
    }

    [Fact]
    public void GeneratedWrappers_AreNotTargets_ButExplicitWrappersAre()
    {
        IConfigurationNode<ICounterExecution> leaf = new CounterConfiguration("leaf");
        IConfigurationNode<ICounterExecution> wrapper = new CounterWrapper(leaf, _ => { });
        var observed = 0;
        var scope = ResolutionScope.Create(builder =>
        {
            builder.Install(leaf, _ => wrapper);
            builder.Install(wrapper, original => new CounterWrapper(original, _ => observed++));
        });
        scope.Resolve(leaf, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next();
        observed.ShouldBe(0);
        scope.Resolve(wrapper, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next();
        observed.ShouldBe(1);
    }

    [Fact]
    public void AdviceRejectsAConfigurationWithIndependentlyResolvedChildren()
    {
        IConfigurationNode<ICounterExecution> leaf = new CounterConfiguration("leaf");
        var constructedExtra = 0;
        var extra = new CounterConfiguration("extra", () => constructedExtra++);
        var scope = ResolutionScope.Create(builder => builder.Install(leaf,
            original => new ExtraChildWrapper(original, extra, _ => { })));
        Should.Throw<InvalidOperationException>(() => scope.Resolve(leaf, static source => source.CreateExecutionFactory(), PrepareCounterAdvice)).Message.ShouldContain("single-child wrapper");
        constructedExtra.ShouldBe(0);
    }

    [Fact]
    public void UnmatchedReferenceDeclaration_DoesNotObserveOrRepeatPreparation()
    {
        var preparations = 0;
        var observed = 0;
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source", () => preparations++);
        IConfigurationNode<ICounterExecution> other = new CounterConfiguration("other");
        var scope = ResolutionScope.Create(builder => builder.Install(other, original => new CounterWrapper(original, _ => observed++)));

        var execution = scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
        execution.Next().ShouldBe(1);
        scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).ShouldBeSameAs(execution);
        execution.Next().ShouldBe(2);
        observed.ShouldBe(0);
        preparations.ShouldBe(1);
    }

    [Fact]
    public void AdviceReceivesTheCurrentExecutionWithoutResolvingItsConfiguredChild()
    {
        IConfigurationNode<ICounterExecution> leaf = new CounterConfiguration("leaf");
        var forbidden = new CounterConfiguration("unused", () => throw new InvalidOperationException("must not resolve"));
        var scope = ResolutionScope.Create(builder => builder.Install(leaf,
            _ => new CounterWrapper(forbidden, _ => { })));
        scope.Resolve(leaf, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
    }

    [Fact]
    public void PreparationFault_IsRetriedAcrossInheritedContextsAndTryResolve()
    {
        var fault = new InvalidOperationException("prepare failure");
        var attempts = 0;
        var source = new CounterConfiguration("fault", () => { attempts++; throw fault; });
        var parent = ResolutionScope.Create();
        Should.Throw<InvalidOperationException>(() => parent.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice)).ShouldBeSameAs(fault);
        var child = NewContext(parent);
        Should.Throw<InvalidOperationException>(() => child.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice)).ShouldBeSameAs(fault);
        child.TryResolve(source, out var execution, out var reason).ShouldBeFalse();
        execution.ShouldBeNull();
        reason.ShouldBe("prepare failure");
        attempts.ShouldBe(3);
        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice)).ShouldBeSameAs(fault);
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
            scope.Resolve(good, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next();
            return scope.Resolve(bad, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
        });
        var parent = ResolutionScope.Create();
        Should.Throw<InvalidOperationException>(() => parent.Resolve(composite)).ShouldBeSameAs(fault);
        Should.Throw<InvalidOperationException>(() => parent.Resolve(composite)).ShouldBeSameAs(fault);
        parent.Resolve(good, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Value.ShouldBe(2);
        Should.Throw<InvalidOperationException>(() => NewContext(parent).Resolve(composite)).ShouldBeSameAs(fault);
        parent.Resolve(good, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Value.ShouldBe(3);
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
        Should.Throw<InvalidOperationException>(() => scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice)).ShouldBeSameAs(fault);
        scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
        NewContext(scope).Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
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
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var parent = ResolutionScope.Create(builder => builder.Install(source, original =>
        {
            callbacks++;
            if (!inPreparation)
                throw fault;
            return new CounterWrapper(original, _ => { }, () => { preparations++; throw fault; });
        }));
        Should.Throw<InvalidOperationException>(() => parent.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice)).ShouldBeSameAs(fault);
        Should.Throw<InvalidOperationException>(() => NewContext(parent).Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice)).ShouldBeSameAs(fault);
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
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var scope = ResolutionScope.Create(builder => builder.Install(source, original =>
            new FaultingWrapper(original, () => preparations++, inner => ++applications == 1 ? throw fault : inner)));
        Should.Throw<InvalidOperationException>(() => scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice, execution =>
        {
            projections++;
            return execution as ICounterControl;
        }, out _)).ShouldBeSameAs(fault);
        projections.ShouldBe(0);
        scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
        preparations.ShouldBe(1);
        applications.ShouldBe(2);
    }

    [Fact]
    public void ProjectionFailure_DoesNotPoisonACompletedBinding()
    {
        var source = new CounterConfiguration("source");
        var scope = ResolutionScope.Create();
        Should.Throw<ArgumentException>(() => scope.Resolve<IConfigurationNode<ICounterExecution>, ICounterExecution, ICounterControl>(source,
            static source => source.CreateExecutionFactory(), PrepareCounterAdvice, _ => throw new ArgumentException("projection"), out _));
        scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
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
        source = new CounterConfiguration("recursive siblings", Bind: _ => root.CreateChildScope().Resolve(source!, static source => source.CreateExecutionFactory(), PrepareCounterAdvice));
        Should.Throw<InvalidOperationException>(() => root.CreateChildScope().Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice)).Message.ShouldContain("Recursive");
    }

    [Fact]
    public void IndependentDeclarations_KeepSeparateAdviceStateEvenWithTheSameRecipe()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var counts = new List<int>();
        Func<IConfigurationNode<ICounterExecution>, IConfigurationNode<ICounterExecution>> recipe = original => new CounterWrapper(original, counts.Add);
        var scope = ResolutionScope.Create(builder => builder.Install(source, recipe).Install(source, recipe));
        scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next();
        scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next();
        counts.ShouldBe([1, 1, 2, 2]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RecursiveConstruction_FailsAcrossContexts(bool newContext)
    {
        CounterConfiguration? source = null;
        source = new CounterConfiguration("recursive", Bind: scope => (newContext ? NewContext(scope) : scope).Resolve(source!, static source => source.CreateExecutionFactory(), PrepareCounterAdvice));
        var scope = ResolutionScope.Create();
        var fault = Should.Throw<InvalidOperationException>(() => scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice));
        fault.Message.ShouldContain("Recursive");
        Should.Throw<InvalidOperationException>(() => scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice)).Message.ShouldContain("Recursive");
    }

    [Fact]
    public void RecursivePreparation_AndUnchangedWrapper_AreDiagnosed()
    {
        var scope = ResolutionScope.Create();
        CounterConfiguration? source = null;
        source = new CounterConfiguration("recursive preparation", () => scope.Resolve(source!, static source => source.CreateExecutionFactory(), PrepareCounterAdvice));
        Should.Throw<InvalidOperationException>(() => scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice)).Message.ShouldContain("Recursive");
        IConfigurationNode<ICounterExecution> other = new CounterConfiguration("unchanged");
        var wrapped = ResolutionScope.Create(builder => builder.Install(other, original => original));
        Should.Throw<InvalidOperationException>(() => wrapped.Resolve(other, static source => source.CreateExecutionFactory(), PrepareCounterAdvice)).Message.ShouldContain("unchanged");
    }

    [Fact]
    public void ObserverFailure_DoesNotRepeatConstructionOrRollbackOperation()
    {
        var prepared = 0;
        var observed = 0;
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source", () => prepared++);
        var scope = ResolutionScope.Create(builder => builder.Install(source, original => new CounterWrapper(original, _ =>
        {
            if (++observed == 1)
                throw new InvalidOperationException("observer");
        })));
        var execution = scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
        Should.Throw<InvalidOperationException>(() => execution.Next());
        scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).ShouldNotBeSameAs(execution);
        execution.Next().ShouldBe(2);
        prepared.ShouldBe(1);
        observed.ShouldBe(2);
    }

    private static ResolutionScope NewContext(ResolutionScope parent)
    {
        IConfigurationNode<ICounterExecution> unrelated = new CounterConfiguration("unrelated");
        return parent.CreateChildScope(builder => builder.Install(unrelated, original => new CounterWrapper(original, _ => { })));
    }

    private sealed record FaultingWrapper(IConfigurationNode<ICounterExecution> Inner, Action Prepare, Advice<ICounterExecution> Apply) : WrappingCounter(Inner)
    {
        public override Advice<ICounterExecution> CreateAdvice()
        {
            Prepare();
            return Apply;
        }
    }

    private static Advice<ICounterExecution> PrepareCounterAdvice(IConfigurationNode wrapper) =>
        wrapper is WrappingCounter typed ? typed.CreateAdvice()
            : throw new InvalidOperationException($"{wrapper.GetType().Name} is not a single-child wrapper for ICounterExecution.");

    private abstract record WrappingCounter(IConfigurationNode<ICounterExecution> Child) : IConfigurationNode<ICounterExecution>
    {
        public ExecutionFactory<ICounterExecution> CreateExecutionFactory()
        {
            var advice = CreateAdvice();
            return scope => advice(scope.Resolve(Child, static source => source.CreateExecutionFactory(), PrepareCounterAdvice));
        }
        public abstract Advice<ICounterExecution> CreateAdvice();
    }

    private interface ICounterExecution : IExecutionNode
    {
        int Value { get; }
        int Next();
    }

    private interface ICounterControl { int Value { get; } }

    private sealed class Module(Action<AspectBuilder> declare) : Aspect
    {
        protected override void Declare(AspectBuilder builder) => declare(builder);
    }

    private sealed class Counter : ICounterExecution, ICounterControl
    {
        public int Value { get; private set; }
        public int Next() => ++Value;
    }

    private sealed record CounterConfiguration(string Name, Action? Prepare = null, Func<ResolutionScope, ICounterExecution>? Bind = null) : IConfigurationNode<ICounterExecution>
    {
        public ExecutionFactory<ICounterExecution> CreateExecutionFactory()
        {
            Prepare?.Invoke();
            var counter = new Counter();
            return scope => Bind?.Invoke(scope) ?? counter;
        }
    }

    private sealed record Composite(IConfigurationNode<ICounterExecution> Child) : IConfigurationNode<CompositeExecution>
    {
        public ExecutionFactory<CompositeExecution> CreateExecutionFactory()
        {
            var state = new Counter();
            return scope =>
            {
                var child = scope.Resolve(Child, static source => source.CreateExecutionFactory(), PrepareCounterAdvice, static raw => raw as ICounterControl, out var control);
                return new CompositeExecution(child, control!, state);
            };
        }
    }

    private sealed class CompositeExecution(ICounterExecution child, ICounterControl control, Counter state) : IExecutionNode
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

    private sealed record CounterWrapper(IConfigurationNode<ICounterExecution> Inner, Action<int> Observe, Action? Prepare = null) : WrappingCounter(Inner)
    {
        public override Advice<ICounterExecution> CreateAdvice()
        {
            Prepare?.Invoke();
            var state = new Counter();
            return inner => new WrapperExecution(inner, state, Observe);
        }
    }

    private sealed class WrapperExecution(ICounterExecution child, Counter state, Action<int> observe) : ICounterExecution
    {
        public int Value => child.Value;
        public int Next()
        {
            var result = child.Next();
            observe(state.Next());
            return result;
        }
    }

    private sealed record ForwardingWrapper(IConfigurationNode<ICounterExecution> Inner) : WrappingCounter(Inner)
    {
        public override Advice<ICounterExecution> CreateAdvice() => inner => new ForwardingExecution(inner);
    }

    private sealed class ForwardingExecution(ICounterExecution child) : ICounterExecution, ICounterControl
    {
        public int Value => child.Value;
        public int Next() => child.Next();
    }

    private sealed record TraceWrapper(IConfigurationNode<ICounterExecution> Inner, string Name, List<string> Events) : WrappingCounter(Inner)
    {
        public override Advice<ICounterExecution> CreateAdvice() => inner => new TraceExecution(inner, Name, Events);
    }

    private sealed class TraceExecution(ICounterExecution child, string name, List<string> events) : ICounterExecution
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

    private sealed record ExtraChildWrapper(IConfigurationNode<ICounterExecution> Child, IConfigurationNode<ICounterExecution> Extra, Action<ICounterExecution> BoundExtra) : IConfigurationNode<ICounterExecution>
    {
        public ExecutionFactory<ICounterExecution> CreateExecutionFactory() => scope =>
        {
            var child = scope.Resolve(Child, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
            BoundExtra(scope.Resolve(Extra, static source => source.CreateExecutionFactory(), PrepareCounterAdvice));
            return new WrapperExecution(child, new Counter(), _ => { });
        };
    }
}
