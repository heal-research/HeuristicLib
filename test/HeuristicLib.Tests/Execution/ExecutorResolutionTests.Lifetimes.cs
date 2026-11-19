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
        IConfigurationNode<ICounterExecutor> observedSource = new CounterWrapper(source, _ => observed++);
        var first = root.CreateChildScope();
        first.Resolve(observedSource).Next().ShouldBe(1);
        var scope = root.CreateChildScope();
        scope.Resolve(observedSource).Next().ShouldBe(1);
        root.Resolve(source).Next().ShouldBe(1);
        first.Resolve(observedSource).Next().ShouldBe(2);
        observed.ShouldBe(3);
    }

    [Fact]
    public void DeferredDependency_ResolvesInTheScopeThatBuiltItsOwner()
    {
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source");
        var root = ResolutionScope.Create();
        var owner = new FrameConfiguration();
        root.Resolve(owner);
        var observed = 0;
        IConfigurationNode<ICounterExecutor> observedSource = new CounterWrapper(source, _ => observed++);
        var child = root.CreateChildScope();
        var direct = child.Resolve(observedSource);
        direct.Next().ShouldBe(1);
        var frame = child.Resolve(owner).Scope;
        frame.Resolve(observedSource).Next().ShouldBe(2); // the child built its own owner, so its frame resolves in the child
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
    public void LongLivedWrapper_DoesNotRetainDiscardedExecutorData()
    {
        var weak = new List<WeakReference>();
        IConfigurationNode<ICounterExecutor> source = new CollectingCounter(weak.Add);
        IConfigurationNode<ICounterExecutor> observed = new CounterWrapper(source, _ => { });
        var root = ResolutionScope.Create();
        CreateFreshBindings(root, observed, weak);
        weak.Count.ShouldBe(64);
        Collect();
        weak.ShouldAllBe(reference => !reference.IsAlive);
        GC.KeepAlive(observed);
        GC.KeepAlive(root);
    }

    [Fact]
    public void LongLivedState_DoesNotRetainDiscardedChildScopesOrWrappers()
    {
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source");
        var root = ResolutionScope.Create();
        var original = root.Resolve(source);
        var weak = CreateDiscardedContexts(root, source);
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
    private static List<WeakReference> CreateDiscardedContexts(ResolutionScope root, IConfigurationNode<ICounterExecutor> source)
    {
        var weak = new List<WeakReference>();
        for (var i = 0; i < 32; i++)
        {
            var observed = new List<int>();
            IConfigurationNode<ICounterExecutor> wrapper = new CounterWrapper(source, observed.Add);
            var child = root.CreateChildScope();
            var binding = child.Resolve(wrapper);
            binding.Next();
            weak.AddRange([new(child), new(wrapper), new(binding), new(observed)]);
        }
        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (FrameExecutor Binding, List<WeakReference> References) CreateDeferredBinding(ResolutionScope root, List<int> observed)
    {
        var references = new List<WeakReference>();
        IConfigurationNode<ICounterExecutor> source = new CollectingCounter(references.Add);
        IConfigurationNode<ICounterExecutor> observedSource = new CounterWrapper(source, observed.Add);
        var child = root.CreateChildScope();
        var binding = child.Resolve(new FrameConfiguration(observedSource));
        child.CreateChildScope().Resolve(observedSource);
        references.Add(new(observedSource));
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
        public FrameExecutor CreateExecutionNode(ResolutionScope scope) => new(scope, source);
    }

    private sealed class FrameExecutor(ResolutionScope scope, IConfigurationNode<ICounterExecutor>? source) : IExecutor
    {
        public ResolutionScope Scope { get; } = scope;
        public IConfigurationNode<ICounterExecutor>? Source { get; } = source;
    }

    // Records private executor data weakly to test its lifetime.
    private sealed record CollectingCounter(Action<WeakReference> CollectReference) : IConfigurationNode<ICounterExecutor>
    {

        public ICounterExecutor CreateExecutionNode(ResolutionScope scope)
        {
            var count = new Count();
            CollectReference(new WeakReference(count));
            return new Counter(count);
        }
    }
}
