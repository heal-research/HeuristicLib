namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public partial class ExecutionFactoryResolutionTests
{
    [Fact]
    public void Selection_UsesReferenceIdentityAndCachesTheBinding()
    {
        var scope = ResolutionScope.Create();
        var source = new CounterConfiguration("equal");
        var equal = new CounterConfiguration("equal");
        source.ShouldBe(equal);
        var execution = scope.Resolve(source);
        scope.Resolve(source).ShouldBeSameAs(execution);
        scope.Resolve(equal).ShouldNotBeSameAs(execution);
        execution.Next().ShouldBe(1);
        scope.Resolve(equal).Next().ShouldBe(1);
    }

    [Fact]
    public void ChildSelections_DoNotHoistOrChangeWhenParentLaterSelects()
    {
        var parent = ResolutionScope.Create();
        var source = new CounterConfiguration("source");
        var child = parent.CreateChildScope();
        var first = child.Resolve(source);
        var sibling = parent.CreateChildScope().Resolve(source);
        var ancestor = parent.Resolve(source);
        first.ShouldNotBeSameAs(sibling);
        first.ShouldNotBeSameAs(ancestor);
        child.Resolve(source).ShouldBeSameAs(first);
        parent.CreateChildScope().Resolve(source).ShouldBeSameAs(ancestor);
    }

    [Fact]
    public void RebindingChangesPredecessorsButRetainsLeafAndAdviceState()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var parentCounts = new List<int>();
        var childCounts = new List<int>();
        var callbacks = 0;
        var preparations = 0;
        var parent = ResolutionScope.Create(builder => builder.Wrap(source, original =>
        {
            original.ShouldBeSameAs(source);
            callbacks++;
            return new CounterWrapper(original, parentCounts.Add, () => preparations++);
        }));
        var first = parent.Resolve(source);
        for (var i = 0; i < 3; i++)
            first.Next();
        var child = parent.CreateChildScope(builder => builder.Wrap(source, original => new CounterWrapper(original, childCounts.Add)));
        var second = child.Resolve(source);
        second.ShouldNotBeSameAs(first);
        second.Next().ShouldBe(4);
        second.Next().ShouldBe(5);
        parentCounts.ShouldBe([1, 2, 3, 4, 5]);
        childCounts.ShouldBe([1, 2]);
        callbacks.ShouldBe(1);
        preparations.ShouldBe(1);
        first.Next().ShouldBe(6);
        childCounts.Count.ShouldBe(2);
        parent.CreateChildScope().Resolve(source).ShouldBeSameAs(first);
    }

    [Fact]
    public void ChildFirstCollision_PreservesTheCompositeDependencyAndItsControl()
    {
        IConfigurationNode<ICounterExecution> leaf = new CounterConfiguration("leaf");
        var composite = new Composite(leaf);
        var parent = ResolutionScope.Create();
        var observed = 0;
        var child = parent.CreateChildScope(builder => builder.Wrap(leaf, original => new CounterWrapper(original, _ => observed++)));
        var local = child.Resolve(leaf);
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
        var scope = ResolutionScope.Create(builder => builder.Wrap(leaf, original => new CounterWrapper(original, _ => { })));
        var observed = scope.Resolve(leaf, static source => source.CreateExecutionFactory(), static execution => execution as ICounterControl, out var control);
        observed.ShouldNotBeAssignableTo<ICounterControl>();
        control.ShouldNotBeNull();
        observed.Next();
        control.Value.ShouldBe(1);

        IConfigurationNode<ICounterExecution> explicitWrapper = new CounterWrapper(leaf, _ => { });
        scope.Resolve(explicitWrapper, static source => source.CreateExecutionFactory(), static execution => execution as ICounterControl, out var absent);
        absent.ShouldBeNull();
        IConfigurationNode<ICounterExecution> forwarding = new ForwardingWrapper(leaf);
        scope.Resolve(forwarding, static source => source.CreateExecutionFactory(), static execution => execution as ICounterControl, out var forwarded);
        forwarded.ShouldNotBeNull().Value.ShouldBe(1);
    }

    [Fact]
    public void Ordering_UsesOriginThenDepthThenSequence_WithReverseEntryOrder()
    {
        var events = new List<string>();
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var module = new Module(b => b.Wrap(source, original => new TraceWrapper(original, "PM", events)));
        var parent = ResolutionScope.Create(builder =>
        {
            builder.Install(module);
            builder.Install(module);
            builder.Wrap(source, original => new TraceWrapper(original, "PC", events));
        });
        var child = parent.CreateChildScope(builder =>
        {
            builder.Wrap(source, original => new TraceWrapper(original, "C1", events));
            builder.Wrap(source, original => new TraceWrapper(original, "C2", events));
            builder.Install(new Module(b => b.Wrap(source, original => new TraceWrapper(original, "CM", events))));
        });
        child.Resolve(source).Next();
        events.ShouldBe(["PM+", "CM+", "PC+", "C2+", "C1+", "C1-", "C2-", "PC-", "CM-", "PM-"]);
    }

    [Fact]
    public void GeneratedWrappers_AreNotTargets_ButExplicitWrappersAre()
    {
        IConfigurationNode<ICounterExecution> leaf = new CounterConfiguration("leaf");
        IConfigurationNode<ICounterExecution> wrapper = new CounterWrapper(leaf, _ => { });
        var observed = 0;
        var scope = ResolutionScope.Create(builder =>
        {
            builder.Wrap(leaf, _ => wrapper);
            builder.Wrap(wrapper, original => new CounterWrapper(original, _ => observed++));
        });
        scope.Resolve(leaf).Next();
        observed.ShouldBe(0);
        scope.Resolve(wrapper).Next();
        observed.ShouldBe(1);
    }

    [Fact]
    public void AdviceAdditionalChild_HasPrivatePinnedIdentity()
    {
        IConfigurationNode<ICounterExecution> leaf = new CounterConfiguration("leaf");
        IConfigurationNode<ICounterExecution> extra = new CounterConfiguration("extra");
        var extras = new List<ICounterExecution>();
        var parent = ResolutionScope.Create(builder => builder.Wrap(leaf, original => new ExtraChildWrapper(original, extra, extras.Add)));
        parent.Resolve(leaf);
        var privateExtra = extras.Single();
        privateExtra.Next();
        parent.Resolve(extra).Next().ShouldBe(1);
        parent.CreateChildScope(builder => builder.Wrap(extra, original => new CounterWrapper(original, _ => { }))).Resolve(leaf);
        extras.Count.ShouldBe(2);
        extras[1].Next().ShouldBe(2);
    }

    [Fact]
    public void PredecessorOverride_DoesNotLeakIntoAnAdditionalChildFrame()
    {
        IConfigurationNode<ICounterExecution> leaf = new CounterConfiguration("leaf");
        IConfigurationNode<ICounterExecution> extra = new ForwardingWrapper(leaf);
        var scope = ResolutionScope.Create(builder => builder.Wrap(leaf, original => new ExtraChildWrapper(original, extra, _ => { })));
        Should.Throw<InvalidOperationException>(() => scope.Resolve(leaf)).Message.ShouldContain("Recursive");
    }

    [Fact]
    public void PreparationFault_IsStickyAcrossInheritedContextsAndTryResolve()
    {
        var fault = new InvalidOperationException("prepare failure");
        var attempts = 0;
        var source = new CounterConfiguration("fault", () => { attempts++; throw fault; });
        var parent = ResolutionScope.Create();
        Should.Throw<InvalidOperationException>(() => parent.Resolve(source)).ShouldBeSameAs(fault);
        var child = NewContext(parent);
        Should.Throw<InvalidOperationException>(() => child.Resolve(source)).ShouldBeSameAs(fault);
        child.TryResolve(source, out var execution, out var reason).ShouldBeFalse();
        execution.ShouldBeNull();
        reason.ShouldBe("prepare failure");
        attempts.ShouldBe(1);
        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().Resolve(source)).ShouldBeSameAs(fault);
        attempts.ShouldBe(2);
    }

    [Fact]
    public void FailedDependency_RemainsPinned_AndCompletedSiblingSurvives()
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
        Should.Throw<InvalidOperationException>(() => parent.Resolve(composite)).ShouldBeSameAs(fault);
        parent.Resolve(good).Value.ShouldBe(1);
        Should.Throw<InvalidOperationException>(() => NewContext(parent).Resolve(composite)).ShouldBeSameAs(fault);
        parent.Resolve(good).Value.ShouldBe(2);
        preparations.ShouldBe(1);
    }

    [Fact]
    public void BinderFault_IsStickyPerContext_WithSharedPreparedState()
    {
        var prepared = 0;
        var bound = 0;
        var fault = new InvalidOperationException("binding failure");
        var source = new CounterConfiguration("source", () => prepared++, _ => ++bound == 1 ? throw fault : new Counter());
        var parent = ResolutionScope.Create();
        Should.Throw<InvalidOperationException>(() => parent.Resolve(source)).ShouldBeSameAs(fault);
        Should.Throw<InvalidOperationException>(() => parent.Resolve(source)).ShouldBeSameAs(fault);
        NewContext(parent).Resolve(source).Next().ShouldBe(1);
        prepared.ShouldBe(1);
        bound.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WrapperPreparationFault_IsStickyPerOccurrence(bool inPreparation)
    {
        var fault = new InvalidOperationException("wrapper failure");
        var callbacks = 0;
        var preparations = 0;
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var parent = ResolutionScope.Create(builder => builder.Wrap(source, original =>
        {
            callbacks++;
            if (!inPreparation)
                throw fault;
            return new CounterWrapper(original, _ => { }, () => { preparations++; throw fault; });
        }));
        Should.Throw<InvalidOperationException>(() => parent.Resolve(source)).ShouldBeSameAs(fault);
        Should.Throw<InvalidOperationException>(() => NewContext(parent).Resolve(source)).ShouldBeSameAs(fault);
        callbacks.ShouldBe(1);
        preparations.ShouldBe(inPreparation ? 1 : 0);
    }

    [Fact]
    public void WrapperBindingFault_DoesNotPublishRawAsCompletedOrRunControlProjection()
    {
        var fault = new InvalidOperationException("wrapper binding failure");
        var bindings = 0;
        var projections = 0;
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var parent = ResolutionScope.Create(builder => builder.Wrap(source, original =>
            new CounterConfiguration("wrapper", Bind: scope => ++bindings == 1 ? throw fault : scope.Resolve(original))));
        Should.Throw<InvalidOperationException>(() => parent.Resolve(source, s => s.CreateExecutionFactory(), execution =>
        {
            projections++;
            return execution as ICounterControl;
        }, out _)).ShouldBeSameAs(fault);
        Should.Throw<InvalidOperationException>(() => parent.Resolve(source)).ShouldBeSameAs(fault);
        projections.ShouldBe(0);
        NewContext(parent).Resolve(source).Next().ShouldBe(1);
        bindings.ShouldBe(2);
    }

    [Fact]
    public void ProjectionFailure_DoesNotPoisonACompletedBinding()
    {
        var source = new CounterConfiguration("source");
        var scope = ResolutionScope.Create();
        Should.Throw<ArgumentException>(() => scope.Resolve<CounterConfiguration, ICounterExecution, ICounterControl>(source,
            s => s.CreateExecutionFactory(), _ => throw new ArgumentException("projection"), out _));
        scope.Resolve(source).Next().ShouldBe(1);
    }

    [Fact]
    public void TryResolve_OnlyConvertsInvalidOperationExceptions_AndDoesNotRetry()
    {
        var fault = new ArgumentException("invalid setting");
        var attempts = 0;
        var source = new CounterConfiguration("source", () => { attempts++; throw fault; });
        var scope = ResolutionScope.Create();
        Should.Throw<ArgumentException>(() => scope.TryResolve(source, out _, out _)).ShouldBeSameAs(fault);
        Should.Throw<ArgumentException>(() => scope.TryResolve(source, out _, out _)).ShouldBeSameAs(fault);
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
    public void IndependentDeclarations_KeepSeparateAdviceStateEvenWithTheSameRecipe()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var counts = new List<int>();
        Func<IConfigurationNode<ICounterExecution>, IConfigurationNode<ICounterExecution>> recipe = original => new CounterWrapper(original, counts.Add);
        var scope = ResolutionScope.Create(builder => builder.Wrap(source, recipe).Wrap(source, recipe));
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
        Should.Throw<InvalidOperationException>(() => scope.Resolve(source)).ShouldBeSameAs(fault);
    }

    [Fact]
    public void RecursivePreparation_AndUnchangedWrapper_AreDiagnosed()
    {
        var scope = ResolutionScope.Create();
        CounterConfiguration? source = null;
        source = new CounterConfiguration("recursive preparation", () => scope.Resolve(source!));
        Should.Throw<InvalidOperationException>(() => scope.Resolve(source)).Message.ShouldContain("Recursive");
        IConfigurationNode<ICounterExecution> other = new CounterConfiguration("unchanged");
        var wrapped = ResolutionScope.Create(builder => builder.Wrap(other, original => original));
        Should.Throw<InvalidOperationException>(() => wrapped.Resolve(other)).Message.ShouldContain("unchanged");
    }

    [Fact]
    public void ObserverFailure_DoesNotRepeatConstructionOrRollbackOperation()
    {
        var prepared = 0;
        var observed = 0;
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source", () => prepared++);
        var scope = ResolutionScope.Create(builder => builder.Wrap(source, original => new CounterWrapper(original, _ =>
        {
            if (++observed == 1)
                throw new InvalidOperationException("observer");
        })));
        var execution = scope.Resolve(source);
        Should.Throw<InvalidOperationException>(() => execution.Next());
        scope.Resolve(source).ShouldBeSameAs(execution);
        execution.Next().ShouldBe(2);
        prepared.ShouldBe(1);
        observed.ShouldBe(2);
    }

    private static ResolutionScope NewContext(ResolutionScope parent)
    {
        IConfigurationNode<ICounterExecution> unrelated = new CounterConfiguration("unrelated");
        return parent.CreateChildScope(builder => builder.Wrap(unrelated, original => new CounterWrapper(original, _ => { })));
    }

    private interface ICounterExecution : IExecutionNode
    {
        int Value { get; }
        int Next();
    }

    private interface ICounterControl { int Value { get; } }

    private sealed record Module(Action<ResolutionScopeBuilder> Declare) : IExecutionModule
    {
        public void Install(ResolutionScopeBuilder builder) => Declare(builder);
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
                var child = scope.Resolve(Child, static source => source.CreateExecutionFactory(), static raw => raw as ICounterControl, out var control);
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

    private sealed record CounterWrapper(IConfigurationNode<ICounterExecution> Child, Action<int> Observe, Action? Prepare = null) : IConfigurationNode<ICounterExecution>
    {
        public ExecutionFactory<ICounterExecution> CreateExecutionFactory()
        {
            Prepare?.Invoke();
            var state = new Counter();
            return scope => new WrapperExecution(scope.Resolve(Child), state, Observe);
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

    private sealed record ForwardingWrapper(IConfigurationNode<ICounterExecution> Child) : IConfigurationNode<ICounterExecution>
    {
        public ExecutionFactory<ICounterExecution> CreateExecutionFactory() => scope => new ForwardingExecution(scope.Resolve(Child));
    }

    private sealed class ForwardingExecution(ICounterExecution child) : ICounterExecution, ICounterControl
    {
        public int Value => child.Value;
        public int Next() => child.Next();
    }

    private sealed record TraceWrapper(IConfigurationNode<ICounterExecution> Child, string Name, List<string> Events) : IConfigurationNode<ICounterExecution>
    {
        public ExecutionFactory<ICounterExecution> CreateExecutionFactory() => scope => new TraceExecution(scope.Resolve(Child), Name, Events);
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
            var child = scope.Resolve(Child);
            BoundExtra(scope.Resolve(Extra));
            return new WrapperExecution(child, new Counter(), _ => { });
        };
    }
}
