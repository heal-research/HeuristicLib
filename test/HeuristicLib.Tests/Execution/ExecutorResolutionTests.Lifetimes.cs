using System.Runtime.CompilerServices;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public partial class ExecutorResolutionTests
{
    [Fact]
    public void ChildScopesBuildTheirOwnPrivateStateAndShareItWithinThemselves()
    {
        var root = ResolutionScope.Create();
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source");
        var observed = 0;
        var aspect = Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(_ => observed++));
        var first = root.CreateChildScope();
        first.AddAspect(aspect);
        first.Resolve(source).Next().ShouldBe(1);
        var scope = root.CreateChildScope();
        scope.AddAspect(aspect);
        scope.Resolve(source).Next().ShouldBe(1);
        root.Resolve(source).Next().ShouldBe(1);
        first.Resolve(source).Next().ShouldBe(2);
        observed.ShouldBe(3);
    }

    [Fact]
    public void InstallingAnAspectAcrossOverlappingPathsAppliesItsAdviceOnce()
    {
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source");
        var wraps = 0;
        var observed = 0;
        var shared = Aspect.Create(Pointcut.Reference(source), new CounterAdvice((executor, _) =>
        {
            wraps++;
            return new ObservingExecutor(executor, new Count(), _ => observed++);
        }));
        var first = new AdviceGroup(shared);
        var second = new AdviceGroup(shared);
        var scope = ResolutionScope.Create();
        scope.AddAspect(first);
        scope.AddAspect(second);
        scope.AddAspect(shared);
        var advisedScope = scope.CreateChildScope();
        advisedScope.AddAspect(shared);
        advisedScope.Resolve(source).Next();
        var advisedScope2 = scope.CreateChildScope();
        advisedScope2.AddAspect(shared);
        advisedScope2.Resolve(source).Next();
        wraps.ShouldBe(2);
        observed.ShouldBe(2);
    }

    [Fact]
    public void DeferredDependency_ResolvesInTheScopeThatBuiltItsOwner()
    {
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source");
        var root = ResolutionScope.Create();
        var owner = new FrameConfiguration();
        root.Resolve(owner);
        var observed = 0;
        var child = root.CreateChildScope();
        child.AddAspect(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(_ => observed++)));
        var direct = child.Resolve(source);
        direct.Next().ShouldBe(1);
        var frame = child.Resolve(owner).Scope;
        frame.Resolve(source).Next().ShouldBe(2); // the child built its own owner, so its frame resolves in the child
        root.Resolve(source).Next().ShouldBe(1);
        direct.Next().ShouldBe(3);
        observed.ShouldBe(3);
        var freshSource = new CounterConfiguration("fresh");
        var first = frame.CreateChildScope().Resolve(freshSource);
        first.Next().ShouldBe(1);
        frame.CreateChildScope().Resolve(freshSource).Next().ShouldBe(1);
        root.Resolve(freshSource).Next().ShouldBe(1);
        first.Next().ShouldBe(2);
    }

    [Fact]
    public void LaterChildAdvice_DoesNotReachAnEarlierExecutor()
    {
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source");
        var outer = 0;
        var inner = 0;
        var root = ResolutionScope.Create();
        root.AddAspect(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(_ => outer++)));
        var original = root.Resolve(source);
        original.Next().ShouldBe(1);
        var child = root.CreateChildScope();
        child.AddAspect(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(_ => inner++)));
        var rebound = child.Resolve(source);
        rebound.Next().ShouldBe(1);
        original.Next().ShouldBe(2);
        rebound.Next().ShouldBe(2);
        outer.ShouldBe(4);
        inner.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongLivedAspect_DoesNotRetainDiscardedExecutorData(bool directAspect)
    {
        var weak = new List<WeakReference>();
        IConfigurationNode<ICounterExecutor> source = new CollectingCounter(weak);
        var aspect = directAspect
            ? Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(_ => { }))
            : new AdviceGroup(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(_ => { })));
        var root = ResolutionScope.Create();
        root.AddAspect(aspect);
        CreateFreshBindings(root, source, weak);
        weak.Count.ShouldBe(64);
        Collect();
        weak.ShouldAllBe(reference => !reference.IsAlive);
        GC.KeepAlive(root);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongLivedState_DoesNotRetainDiscardedAspectsOrAdvice(bool directAspect)
    {
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source");
        var root = ResolutionScope.Create();
        var original = root.Resolve(source);
        var weak = CreateDiscardedContexts(root, source, directAspect);
        Collect();
        weak.ShouldAllBe(reference => !reference.IsAlive);
        original.Next().ShouldBe(1);
        GC.KeepAlive(root);
    }

    [Fact]
    public void DeferredExecutionRetainsItsScopeButNotTheDataOfDiscardedChildScopes()
    {
        var root = ResolutionScope.Create();
        var observed = new List<int>();
        var (binding, references) = CreateDeferredBinding(root, observed);
        Collect();
        references[0].IsAlive.ShouldBeFalse();
        references[1].IsAlive.ShouldBeTrue();
        binding.Scope.Resolve(binding.Source!).Next().ShouldBe(1);
        binding.Scope.Resolve(binding.Source!).Next().ShouldBe(2);
        observed.ShouldBe([1, 2]);
        GC.KeepAlive(binding);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateFreshBindings(ResolutionScope root, IConfigurationNode<ICounterExecutor> source, List<WeakReference> weak)
    {
        for (var i = 0; i < 32; i++)
        {
            var binding = root.CreateChildScope().Resolve(source);
            binding.Next().ShouldBe(1);
            weak.Add(new WeakReference(binding));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<WeakReference> CreateDiscardedContexts(ResolutionScope root, IConfigurationNode<ICounterExecutor> source, bool directAspect)
    {
        var weak = new List<WeakReference>();
        for (var i = 0; i < 32; i++)
        {
            var observed = new List<int>();
            var aspect = directAspect
                ? Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(observed.Add))
                : new AdviceGroup(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(observed.Add)));
            var child = root.CreateChildScope();
            child.AddAspect(aspect);
            var binding = child.Resolve(source);
            binding.Next();
            weak.AddRange([new(child), new(aspect), new(binding), new(observed)]);
        }
        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (FrameExecutor Binding, List<WeakReference> References) CreateDeferredBinding(ResolutionScope root, List<int> observed)
    {
        var references = new List<WeakReference>();
        IConfigurationNode<ICounterExecutor> source = new CollectingCounter(references);
        var aspect = Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(observed.Add));
        var child = root.CreateChildScope();
        child.AddAspect(aspect);
        var binding = child.Resolve(new FrameConfiguration(source));
        child.CreateChildScope().Resolve(source);
        references.Add(new(aspect));
        return (binding, references);
    }

    private static void Collect()
    {
        for (var pass = 0; pass < 3; pass++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        GC.Collect();
    }

    private sealed class FrameConfiguration(IConfigurationNode<ICounterExecutor>? source = null) : IConfigurationNode<FrameExecutor>
    {
        public FrameExecutor CreateExecutor(ResolutionScope scope) => new(scope, source);
    }

    private sealed class FrameExecutor(ResolutionScope scope, IConfigurationNode<ICounterExecutor>? source) : IExecutor
    {
        public ResolutionScope Scope { get; } = scope;
        public IConfigurationNode<ICounterExecutor>? Source { get; } = source;
    }

    // Records private executor data weakly to test its lifetime.
    private sealed record CollectingCounter(List<WeakReference> Weak) : IConfigurationNode<ICounterExecutor>
    {

        public ICounterExecutor CreateExecutor(ResolutionScope scope)
        {
            var count = new Count();
            Weak.Add(new WeakReference(count));
            return new Counter(count);
        }
    }
}
