namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public partial class ExecutorFactoryResolutionTests
{
    private sealed class ExceptExcluded : Pointcut<IConfigurationNode<ICounterExecutor>>
    {
        public override bool Matches(IConfigurationNode<ICounterExecutor> configuration) => configuration is CounterConfiguration { Name: not "excluded" };
    }

    private sealed class CounterPointcut(bool result, Action<IConfigurationNode<ICounterExecutor>>? offered = null) : Pointcut<IConfigurationNode<ICounterExecutor>>
    {
        public override bool Matches(IConfigurationNode<ICounterExecutor> configuration)
        {
            offered?.Invoke(configuration);
            return result;
        }
    }

    private static readonly Pointcut<IConfigurationNode<ICounterExecutor>> AllCounters =
        Pointcut.OfType<IConfigurationNode<ICounterExecutor>>();

    [Fact]
    public void SelectorRegistration_WrapsEachMatchingSource_WithItsOwnAdviceState()
    {
        IConfigurationNode<ICounterExecutor> first = new CounterConfiguration("first");
        IConfigurationNode<ICounterExecutor> second = new CounterConfiguration("second");
        IConfigurationNode<ICounterExecutor> excluded = new CounterConfiguration("excluded");
        var wrapped = new List<IConfigurationNode<ICounterExecutor>>();
        var observed = new List<string>();
        var scope = ResolutionScope.Create(Aspect.Create(AllCounters.And(new ExceptExcluded()), new CounterAdvice(original =>
        {
            wrapped.Add(original);
            var name = ((CounterConfiguration)original).Name;
            return new CounterObservationAdvice(count => observed.Add($"{name}:{count}")).CreateWrapperFactory(original);
        })));

        scope.Resolve(first, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next();
        scope.Resolve(second, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next();
        scope.Resolve(first, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next();
        scope.Resolve(excluded, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).ShouldBeOfType<Counter>().Next().ShouldBe(1);

        wrapped.ShouldBe([first, second]);
        observed.ShouldBe(["first:1", "second:1", "first:2"]);
    }

    [Fact]
    public void OverlappingUnion_WrapsOnce_WhileSeparateRegistrationsBothApply()
    {
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source");
        var observed = new List<string>();
        var union = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source) | AllCounters, new CounterObservationAdvice(_ => observed.Add("union"))));
        union.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next();
        observed.ShouldBe(["union"]);

        observed.Clear();
        var separate = ResolutionScope.Create(
            Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(_ => observed.Add("reference"))),
            Aspect.Create(AllCounters, new CounterObservationAdvice(_ => observed.Add("role"))));
        separate.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next();
        observed.ShouldBe(["reference", "role"]);
    }

    [Fact]
    public void EmptySelection_IsSilent_AndNeverRunsItsRecipe()
    {
        var preparations = 0;
        var recipes = 0;
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source", () => preparations++);
        var scope = ResolutionScope.Create(Aspect.Create(AllCounters.And(new CounterPointcut(false)), new CounterAdvice(original =>
        {
            recipes++;
            return new CounterObservationAdvice(_ => { }).CreateWrapperFactory(original);
        })));

        var executor = scope.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice);
        executor.ShouldBeOfType<Counter>().Next().ShouldBe(1);
        scope.CreateChildScope().Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).ShouldBeSameAs(executor);
        recipes.ShouldBe(0);
        preparations.ShouldBe(1);
    }

    [Fact]
    public void ModulesSharingABroadSelector_AreOfferedOnlySourceNodes_AndEachApplyOnce()
    {
        IConfigurationNode<ICounterExecutor> leaf = new CounterConfiguration("leaf");
        IConfigurationNode<ICounterExecutor> named = new CounterWrapper(leaf, _ => { });
        var offered = new List<IConfigurationNode<ICounterExecutor>>();
        var observed = new List<string>();
        var sources = AllCounters.And(new CounterPointcut(true, offered.Add));
        Aspect Observe(string module) => Aspect.Create(sources, new CounterAdvice(original => new CounterObservationAdvice(_ => observed.Add($"{module}:{(ReferenceEquals(original, leaf) ? "leaf" : "named")}")).CreateWrapperFactory(original)));
        var scope = ResolutionScope.Create(Observe("A"),
            Observe("B"));

        scope.Resolve(leaf, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next();
        observed.ShouldBe(["A:leaf", "B:leaf"]);
        offered.ShouldNotBeEmpty();
        offered.ShouldAllBe(configuration => ReferenceEquals(configuration, leaf));

        observed.Clear();
        scope.Resolve(named, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next();
        observed.ShouldBe(["A:leaf", "B:leaf", "A:named", "B:named"]);
        offered.ShouldContain(named);
        offered.ShouldAllBe(configuration => ReferenceEquals(configuration, leaf) || ReferenceEquals(configuration, named));
    }

    [Fact]
    public void ChildSelector_ReachesTheDependencyOfAnAncestorComposite_WithoutResettingState()
    {
        IConfigurationNode<ICounterExecutor> leaf = new CounterConfiguration("leaf");
        var composite = new Composite(leaf);
        var parent = ResolutionScope.Create();
        var ancestor = parent.Resolve(composite);
        ancestor.Next().ShouldBe(1);

        var observed = new List<int>();
        var child = parent.CreateChildScope(Aspect.Create(AllCounters, new CounterObservationAdvice(observed.Add)));
        var rebound = child.Resolve(composite);
        rebound.ShouldNotBeSameAs(ancestor);
        rebound.Next().ShouldBe(2);
        rebound.Next().ShouldBe(3);
        ancestor.Next().ShouldBe(4);

        observed.ShouldBe([1, 2]);
        rebound.Calls.ShouldBe(4);
        rebound.Control.ShouldBeSameAs(ancestor.Control);
        parent.CreateChildScope().Resolve(composite).ShouldNotBeSameAs(ancestor);
    }

    [Fact]
    public void DeferredResolution_MatchesSourcesCreatedAfterTheDeclaration()
    {
        ResolutionScope? retained = null;
        var host = new CounterConfiguration("host", Bind: scope =>
        {
            retained = scope;
            return new Counter();
        });
        var observed = new List<string>();
        var root = ResolutionScope.Create(Aspect.Create(AllCounters, new CounterAdvice(original => new CounterObservationAdvice(count => observed.Add($"{((CounterConfiguration)original).Name}:{count}")).CreateWrapperFactory(original))));
        root.Resolve(host, static source => source.CreateExecutorFactory(), PrepareCounterAdvice);

        IConfigurationNode<ICounterExecutor> late = new CounterConfiguration("late");
        IConfigurationNode<ICounterExecutor> later = new CounterConfiguration("later");
        retained.ShouldNotBeNull().Resolve(late, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
        retained.CreateChildScope().Resolve(later, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next().ShouldBe(1);

        observed.ShouldBe(["late:1", "later:1"]);
    }

    [Fact]
    public void PredicateFault_FailsTheBindingInItsContext_WithoutRepeatingPreparation()
    {
        var fault = new InvalidOperationException("predicate failure");
        var preparations = 0;
        var predicateCalls = 0;
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source", () => preparations++);
        var parent = ResolutionScope.Create(Aspect.Create(AllCounters.And(new CounterPointcut(true, _ => { if (++predicateCalls == 1) throw fault; })), new CounterObservationAdvice(_ => { })));

        Should.Throw<InvalidOperationException>(() => parent.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice)).ShouldBeSameAs(fault);
        parent.Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
        var retried = NewContext(parent).Resolve(source, static source => source.CreateExecutorFactory(), PrepareCounterAdvice);
        retried.ShouldNotBeOfType<Counter>();
        retried.Next().ShouldBe(2);
        preparations.ShouldBe(1);
    }


}
