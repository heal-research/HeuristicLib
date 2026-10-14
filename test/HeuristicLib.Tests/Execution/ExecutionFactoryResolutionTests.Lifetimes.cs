using System.Runtime.CompilerServices;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public partial class ExecutionFactoryResolutionTests
{
    [Fact]
    public void WithSharesStateAndReusesAdvicePreparation()
    {
        var root = ResolutionScope.Create();
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var observed = 0;
        var aspect = Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(_ => observed++));
        var first = root.With(aspect);
        first.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
        root.With(aspect).Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(2);
        root.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(3);
        first.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(4);
        observed.ShouldBe(3);
    }

    [Fact]
    public void InstallingAnAspectAcrossOverlappingPathsAppliesItsAdviceOnce()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var recipes = 0;
        var observed = 0;
        var shared = Aspect.Create(Pointcut.Reference(source), new CounterAdvice(original =>
        {
            recipes++;
            return new CounterObservationAdvice(_ => observed++).CreateWrapperFactory(original);
        }));
        var first = new AdviceGroup(shared);
        var second = new AdviceGroup(shared);
        var scope = ResolutionScope.Create(first, second, shared);
        scope.With(shared).Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next();
        scope.CreateChildScope(shared).Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next();
        recipes.ShouldBe(1);
        observed.ShouldBe(2);
    }

    [Fact]
    public void DeferredDependency_UsesOriginalOwnershipDespiteAChildDirectSelection()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var root = ResolutionScope.Create();
        var owner = new FrameConfiguration();
        root.Resolve(owner);
        var observed = 0;
        var child = root.CreateChildScope(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(_ => observed++)));
        var direct = child.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
        direct.Next().ShouldBe(1);
        var frame = child.Resolve(owner).Scope;
        frame.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
        root.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(2);
        direct.Next().ShouldBe(2);
        observed.ShouldBe(3);
        var freshSource = new CounterConfiguration("fresh");
        var first = frame.CreateChildScope().Resolve(freshSource, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
        first.Next().ShouldBe(1);
        frame.CreateChildScope().Resolve(freshSource, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
        root.Resolve(freshSource, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
        first.Next().ShouldBe(2);
    }

    [Fact]
    public void DeferredAdvice_KeepsItsOriginalInnerExecution()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var outer = 0;
        var inner = 0;
        var recipes = 0;
        var root = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new CounterAdvice(s =>
        {
            recipes++;
            return new DeferredAdvice(() => outer++).CreateWrapperFactory(s);
        })));
        var original = root.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
        original.Next().ShouldBe(1);
        var child = root.CreateChildScope(Aspect.Create(Pointcut.Reference(source), new DeferredAdvice(() => inner++)));
        var rebound = child.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
        rebound.Next().ShouldBe(2);
        original.Next().ShouldBe(3);
        rebound.Next().ShouldBe(4);
        outer.ShouldBe(4);
        inner.ShouldBe(2);
        recipes.ShouldBe(1);
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongLivedAspect_DoesNotRetainFreshStateOrAdvice(bool directAspect)
    {
        var weak = new List<WeakReference>();
        IConfigurationNode<ICounterExecution> source = new CollectingCounter(weak);
        var aspect = directAspect
            ? Aspect.Create(Pointcut.Reference(source), new CollectingAdvice(weak))
            : new AdviceGroup(Aspect.Create(Pointcut.Reference(source), new CollectingAdvice(weak)));
        var root = ResolutionScope.Create(aspect);
        CreateFreshBindings(root, source, weak);
        weak.Count.ShouldBe(96);
        Collect();
        weak.ShouldAllBe(reference => !reference.IsAlive);
        GC.KeepAlive(root);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongLivedState_DoesNotRetainDiscardedAspectsOrAdvice(bool directAspect)
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var root = ResolutionScope.Create();
        var original = root.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
        var weak = CreateDiscardedContexts(root, source, directAspect);
        Collect();
        weak.ShouldAllBe(reference => !reference.IsAlive);
        original.Next().ShouldBe(33);
        GC.KeepAlive(root);
    }

    [Fact]
    public void LiveDeferredExecutionRetainsItsStateAndOriginalAspect()
    {
        var root = ResolutionScope.Create();
        var observed = new List<int>();
        var (binding, references) = CreateDeferredBinding(root, observed);
        Collect();
        references.ShouldAllBe(reference => reference.IsAlive);
        binding.Scope.Resolve(binding.Source!, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
        binding.Scope.Resolve(binding.Source!, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(2);
        observed.ShouldBe([1, 2]);
        GC.KeepAlive(binding);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateFreshBindings(ResolutionScope root, IConfigurationNode<ICounterExecution> source, List<WeakReference> weak)
    {
        for (var i = 0; i < 32; i++)
        {
            var binding = root.CreateChildScope().Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
            binding.Next().ShouldBe(1);
            weak.Add(new WeakReference(binding));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<WeakReference> CreateDiscardedContexts(ResolutionScope root, IConfigurationNode<ICounterExecution> source, bool directAspect)
    {
        var weak = new List<WeakReference>();
        for (var i = 0; i < 32; i++)
        {
            var observed = new List<int>();
            var aspect = directAspect
                ? Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(observed.Add))
                : new AdviceGroup(Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(observed.Add)));
            var child = root.CreateChildScope(aspect);
            var binding = child.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
            binding.Next();
            weak.AddRange([new(child), new(aspect), new(binding), new(observed)]);
        }
        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (FrameExecution Binding, List<WeakReference> References) CreateDeferredBinding(ResolutionScope root, List<int> observed)
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("local");
        var aspect = Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(observed.Add));
        var child = root.CreateChildScope(aspect);
        var binding = child.Resolve(new FrameConfiguration(source));
        child.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice, inner => inner as ICounterControl, out var raw);
        return (binding, [new(raw!), new(aspect)]);
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

    private sealed class FrameConfiguration(IConfigurationNode<ICounterExecution>? source = null) : IConfigurationNode<FrameExecution>
    {
        public ExecutionFactory<FrameExecution> CreateExecutionFactory() => scope => new FrameExecution(scope, source);
    }

    private sealed class FrameExecution(ResolutionScope scope, IConfigurationNode<ICounterExecution>? source) : IExecutionNode
    {
        public ResolutionScope Scope { get; } = scope;
        public IConfigurationNode<ICounterExecution>? Source { get; } = source;
    }

    private sealed class DeferredAdvice(Action observe) : CounterAdviceBase
    {
        public override WrapperExecutionFactory<ICounterExecution> CreateWrapperFactory(IConfigurationNode<ICounterExecution> configuration) => inner => new Execution(inner, observe);
        private sealed class Execution(ICounterExecution inner, Action observe) : ICounterExecution
        {
            public int Value => inner.Value;
            public int Next()
            {
                var result = inner.Next();
                observe();
                return result;
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

    private sealed class CollectingAdvice(List<WeakReference> weak) : CounterAdviceBase
    {
        public override WrapperExecutionFactory<ICounterExecution> CreateWrapperFactory(IConfigurationNode<ICounterExecution> configuration)
        {
            var state = new Counter();
            weak.Add(new WeakReference(state));
            return inner => new WrapperExecution(inner, state, _ => { });
        }
    }
}
