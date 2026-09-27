using System.Runtime.CompilerServices;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public partial class ExecutionFactoryResolutionTests
{
    [Fact]
    public void RetainedChildScopes_UseReferenceKeysAndSnapshotDeclarationsOnce()
    {
        var root = ResolutionScope.Create();
        var key = new ChildKey(1);
        var equalKey = new ChildKey(1);
        key.ShouldBe(equalKey);
        var declarations = 0;
        ResolutionScopeBuilder? captured = null;
        var first = root.GetOrCreateChildScope(key, builder => { declarations++; captured = builder; });
        root.GetOrCreateChildScope(key, _ => throw new InvalidOperationException("must not redeclare")).ShouldBeSameAs(first);
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        first.Resolve(source).Next().ShouldBe(1);
        root.GetOrCreateChildScope(equalKey).Resolve(source).Next().ShouldBe(1);
        root.CreateChildScope().Resolve(source).Next().ShouldBe(1);
        root.Resolve(source).Next().ShouldBe(1);

        // As with ordinary builders, late edits cannot alter the snapshot already used by the retained scope.
        captured!.Wrap(source, _ => throw new InvalidOperationException("late declaration"));
        first.Resolve(source).Next().ShouldBe(2);
        root.CreateChildScope().Resolve(source).Next().ShouldBe(2);
        declarations.ShouldBe(1);
    }

    [Fact]
    public void RetainedChildScopes_ShareStateAcrossBindingsButIsolateOwners()
    {
        var key = new object();
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var owner = new FrameConfiguration();
        var otherOwner = new FrameConfiguration();
        var root = ResolutionScope.Create();
        var original = root.Resolve(owner);
        var local = original.Scope.GetOrCreateChildScope(key).Resolve(source);
        local.Next().ShouldBe(1);
        var observed = 0;
        var child = root.CreateChildScope(builder => builder.Wrap(source, s => new CounterWrapper(s, _ => observed++)));
        var rebound = child.Resolve(owner);
        rebound.ShouldNotBeSameAs(original);
        var current = rebound.Scope.GetOrCreateChildScope(key).Resolve(source);
        current.Next().ShouldBe(2);
        local.Next().ShouldBe(3);
        observed.ShouldBe(1);
        root.Resolve(otherOwner).Scope.GetOrCreateChildScope(key).Resolve(source).Next().ShouldBe(1);
        root.GetOrCreateChildScope(key).Resolve(source).Next().ShouldBe(1);
    }

    [Fact]
    public void RetainedDeclarations_OrderByTheRequestingPathRatherThanOriginalDomainDepth()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var events = new List<string>();
        var root = ResolutionScope.Create(builder =>
        {
            builder.Wrap(source, s => new TraceWrapper(s, "P", events));
            builder.Install(new Module(b => b.Wrap(source, s => new TraceWrapper(s, "PM", events))));
        });
        var owner = new FrameConfiguration();
        var key = new object();
        root.Resolve(owner).Scope.GetOrCreateChildScope(key, builder =>
            builder.Wrap(source, s => new TraceWrapper(s, "D", events))).Resolve(source).Next();
        events.Clear();
        var child = root.CreateChildScope(builder =>
        {
            builder.Wrap(source, s => new TraceWrapper(s, "C", events));
            builder.Install(new Module(b => b.Wrap(source, s => new TraceWrapper(s, "CM", events))));
        });

        child.Resolve(owner).Scope.GetOrCreateChildScope(key).Resolve(source).Next().ShouldBe(2);
        events.ShouldBe(["PM+", "CM+", "P+", "C+", "D+", "D-", "C-", "P-", "CM-", "PM-"]);
    }

    [Fact]
    public void DeferredDependency_UsesOriginalOwnershipDespiteAChildDirectSelection()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var root = ResolutionScope.Create();
        var owner = new FrameConfiguration();
        root.Resolve(owner);
        var observed = 0;
        var child = root.CreateChildScope(builder => builder.Wrap(source, s => new CounterWrapper(s, _ => observed++)));
        var direct = child.Resolve(source);
        direct.Next().ShouldBe(1);
        var frame = child.Resolve(owner).Scope;
        frame.Resolve(source).Next().ShouldBe(1);
        root.Resolve(source).Next().ShouldBe(2);
        direct.Next().ShouldBe(2);
        observed.ShouldBe(3);

        var freshSource = new CounterConfiguration("fresh");
        var first = frame.CreateChildScope().Resolve(freshSource);
        first.Next().ShouldBe(1);
        frame.CreateChildScope().Resolve(freshSource).Next().ShouldBe(1);
        root.Resolve(freshSource).Next().ShouldBe(1);
        first.Next().ShouldBe(2);
    }

    [Fact]
    public void DeferredPredecessor_KeepsItsOriginalObservationContext()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var outer = 0;
        var inner = 0;
        var recipes = 0;
        var root = ResolutionScope.Create(builder => builder.Wrap(source, s =>
        {
            recipes++;
            return new DeferredWrapper(s, () => outer++);
        }));
        var original = root.Resolve(source);
        original.Next().ShouldBe(1);
        var child = root.CreateChildScope(builder => builder.Wrap(source, s => new DeferredWrapper(s, () => inner++)));
        var rebound = child.Resolve(source);
        rebound.Next().ShouldBe(2);
        original.Next().ShouldBe(3);
        rebound.Next().ShouldBe(4);
        outer.ShouldBe(4);
        inner.ShouldBe(2);
        recipes.ShouldBe(1);
    }

    [Fact]
    public void AdviceAdditionalDependency_UsesDeclaringDomainWhenRetainedAndDeclaringDomainsAreIncomparable()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        IConfigurationNode<ICounterExecution> extra = new CounterConfiguration("extra");
        var root = ResolutionScope.Create();
        var owner = new FrameConfiguration();
        var key = new object();
        var retained = root.Resolve(owner).Scope.GetOrCreateChildScope(key);
        retained.Resolve(source).Next();
        var retainedExtra = retained.Resolve(extra);
        retainedExtra.Next();
        ICounterExecution? adviceExtra = null;
        var child = root.CreateChildScope(builder => builder.Wrap(source, s => new ExtraChildWrapper(s, extra, bound => adviceExtra = bound)));
        var childExtra = child.Resolve(extra);
        childExtra.Next();
        childExtra.Next();

        child.Resolve(owner).Scope.GetOrCreateChildScope(key).Resolve(source).Next().ShouldBe(2);
        adviceExtra.ShouldBeSameAs(childExtra);
        adviceExtra!.Next().ShouldBe(3);
        retainedExtra.Next().ShouldBe(2);
        root.Resolve(extra).Next().ShouldBe(1);
    }

    [Fact]
    public void RetainedDeclarationFault_IsStickyAndCannotPublishPartialDeclarations()
    {
        var root = ResolutionScope.Create();
        var owner = new FrameConfiguration();
        var frame = root.Resolve(owner).Scope;
        var key = new object();
        var fault = new InvalidOperationException("declaration failure");
        var attempts = 0;
        var observed = 0;
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        Should.Throw<InvalidOperationException>(() => frame.GetOrCreateChildScope(key, builder =>
        {
            attempts++;
            builder.Wrap(source, s => new CounterWrapper(s, _ => observed++));
            throw fault;
        })).ShouldBeSameAs(fault);
        var rebound = NewContext(root).Resolve(owner).Scope;
        Should.Throw<InvalidOperationException>(() => rebound.GetOrCreateChildScope(key, _ => attempts++)).ShouldBeSameAs(fault);
        root.Resolve(source).Next();
        observed.ShouldBe(0);
        attempts.ShouldBe(1);
        rebound.GetOrCreateChildScope(new object()).Resolve(source).Next().ShouldBe(2);
    }

    [Fact]
    public void RetainedDeclarationRecursion_CannotEscapeThroughAnotherView()
    {
        var root = ResolutionScope.Create();
        var owner = new FrameConfiguration();
        var frame = root.Resolve(owner).Scope;
        var key = new object();
        var fault = Should.Throw<InvalidOperationException>(() => frame.GetOrCreateChildScope(key, _ =>
            NewContext(root).Resolve(owner).Scope.GetOrCreateChildScope(key)));
        fault.Message.ShouldContain("Recursive retained child declaration");
        Should.Throw<InvalidOperationException>(() => frame.GetOrCreateChildScope(key)).ShouldBeSameAs(fault);
    }

    [Fact]
    public void LongLivedDeclaration_DoesNotRetainFreshSourceStateOrAdvice()
    {
        var weak = new List<WeakReference>();
        var source = new CollectingCounter(weak);
        var root = ResolutionScope.Create(builder => builder.Wrap<IConfigurationNode<ICounterExecution>>(source, s => new CollectingWrapper(s, weak)));
        CreateFreshBindings(root, source, weak);
        weak.Count.ShouldBe(96);
        Collect();
        weak.ShouldAllBe(reference => !reference.IsAlive);
        GC.KeepAlive(root);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongLivedSource_DoesNotRetainDiscardedDeclarationDomains(bool extraDependency)
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var root = ResolutionScope.Create();
        var original = root.Resolve(source);
        var weak = CreateDiscardedContexts(root, source, extraDependency);
        Collect();
        weak.ShouldAllBe(reference => !reference.IsAlive);
        original.Next().ShouldBe(33);
        GC.KeepAlive(root);
    }

    [Fact]
    public void LiveDeferredBinding_KeepsItsDeclaringDomainAndExtraDependencyAlive()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var root = ResolutionScope.Create();
        root.Resolve(source);
        var observed = new List<int>();
        var (binding, extra) = CreateDeferredBinding(root, source, observed);
        Collect();
        extra.IsAlive.ShouldBeTrue();
        binding.Next().ShouldBe(1);
        binding.Next().ShouldBe(2);
        observed.ShouldBe([3, 4]);
        GC.KeepAlive(binding);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateFreshBindings(ResolutionScope root, IConfigurationNode<ICounterExecution> source, List<WeakReference> weak)
    {
        for (var i = 0; i < 32; i++)
        {
            var binding = root.CreateChildScope().Resolve(source);
            binding.Next().ShouldBe(1);
            weak.Add(new WeakReference(binding));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<WeakReference> CreateDiscardedContexts(ResolutionScope root, IConfigurationNode<ICounterExecution> source, bool extraDependency)
    {
        var weak = new List<WeakReference>();
        for (var i = 0; i < 32; i++)
        {
            var observed = new List<int>();
            var outer = root.CreateChildScope();
            IConfigurationNode<ICounterExecution> extra = new CounterConfiguration("extra");
            if (extraDependency)
                outer.Resolve(extra).Next();
            var retained = outer.GetOrCreateChildScope(new object(), builder => builder.Wrap(source, s => extraDependency
                ? new DeferredExtraWrapper(s, extra, observed.Add)
                : new CounterWrapper(s, observed.Add)));
            var binding = retained.Resolve(source);
            binding.Next();
            weak.AddRange([new(outer), new(retained), new(binding), new(observed)]);
        }
        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (ICounterExecution Binding, WeakReference Extra) CreateDeferredBinding(
        ResolutionScope root, IConfigurationNode<ICounterExecution> source, List<int> observed)
    {
        IConfigurationNode<ICounterExecution> extra = new CounterConfiguration("extra");
        var child = root.CreateChildScope(builder => builder.Wrap(source, s => new DeferredExtraWrapper(s, extra, observed.Add)));
        var execution = child.Resolve(extra);
        execution.Next();
        execution.Next();
        return (child.Resolve(source), new WeakReference(execution));
    }

    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private sealed record ChildKey(int Value);

    private sealed class FrameConfiguration : IConfigurationNode<FrameExecution>
    {
        public ExecutionFactory<FrameExecution> CreateExecutionFactory() => scope => new FrameExecution(scope);
    }

    private sealed class FrameExecution(ResolutionScope scope) : IExecutionNode
    {
        public ResolutionScope Scope { get; } = scope;
    }

    private sealed record DeferredWrapper(IConfigurationNode<ICounterExecution> Source, Action Observe) : IConfigurationNode<ICounterExecution>
    {
        public ExecutionFactory<ICounterExecution> CreateExecutionFactory() => scope => new Execution(scope, Source, Observe);
        private sealed class Execution(ResolutionScope scope, IConfigurationNode<ICounterExecution> source, Action observe) : ICounterExecution
        {
            public int Value => scope.Resolve(source).Value;
            public int Next()
            {
                var result = scope.Resolve(source).Next();
                observe();
                return result;
            }
        }
    }

    private sealed record DeferredExtraWrapper(
        IConfigurationNode<ICounterExecution> Source, IConfigurationNode<ICounterExecution> Extra, Action<int> Observe) : IConfigurationNode<ICounterExecution>
    {
        public ExecutionFactory<ICounterExecution> CreateExecutionFactory() => scope => new Execution(scope, Source, Extra, Observe);
        private sealed class Execution(ResolutionScope scope, IConfigurationNode<ICounterExecution> source,
            IConfigurationNode<ICounterExecution> extra, Action<int> observe) : ICounterExecution
        {
            public int Value => scope.Resolve(source).Value;
            public int Next()
            {
                observe(scope.Resolve(extra).Next());
                return scope.Resolve(source).Next();
            }
        }
    }

    private sealed record CollectingCounter(List<WeakReference> Weak) : IConfigurationNode<ICounterExecution>
    {
        public ExecutionFactory<ICounterExecution> CreateExecutionFactory()
        {
            var execution = new Counter();
            Weak.Add(new WeakReference(execution));
            return _ => execution;
        }
    }

    private sealed record CollectingWrapper(IConfigurationNode<ICounterExecution> Source, List<WeakReference> Weak) : IConfigurationNode<ICounterExecution>
    {
        public ExecutionFactory<ICounterExecution> CreateExecutionFactory()
        {
            var state = new Counter();
            Weak.Add(new WeakReference(state));
            return scope => new WrapperExecution(scope.Resolve(Source), state, _ => { });
        }
    }
}
