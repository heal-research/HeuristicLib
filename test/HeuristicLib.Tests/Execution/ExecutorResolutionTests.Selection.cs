namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public partial class ExecutorResolutionTests
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

    // Wraps in an executor of its own that counts that executor's calls.
    private static CounterAdvice ObserveEach(Action<IConfigurationNode<ICounterExecutor>, int> observe) =>
        new((executor, source) => new ObservingExecutor(executor, new Count(), count => observe(source, count)));

    [Fact]
    public void SelectorRegistration_WrapsEveryResolutionOfEachMatchingSource()
    {
        IConfigurationNode<ICounterExecutor> first = new CounterConfiguration("first");
        IConfigurationNode<ICounterExecutor> second = new CounterConfiguration("second");
        IConfigurationNode<ICounterExecutor> excluded = new CounterConfiguration("excluded");
        var observed = new List<string>();
        var scope = ResolutionScope.Create(Aspect.Create(AllCounters.And(new ExceptExcluded()),
            ObserveEach((source, count) => observed.Add($"{((CounterConfiguration)source).Name}:{count}"))));

        scope.Resolve(first).Next();
        scope.Resolve(second).Next();
        scope.Resolve(first).Next().ShouldBe(2);
        scope.Resolve(excluded).ShouldBeOfType<Counter>().Next().ShouldBe(1);

        observed.ShouldBe(["first:1", "second:1", "first:1"]);
    }

    [Fact]
    public void OverlappingUnion_WrapsOnce_WhileSeparateRegistrationsBothApply()
    {
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source");
        var observed = new List<string>();
        var union = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source) | AllCounters, new CounterObservationAdvice(_ => observed.Add("union"))));
        union.Resolve(source).Next();
        observed.ShouldBe(["union"]);

        observed.Clear();
        var separate = ResolutionScope.Create(
            Aspect.Create(Pointcut.Reference(source), new CounterObservationAdvice(_ => observed.Add("reference"))),
            Aspect.Create(AllCounters, new CounterObservationAdvice(_ => observed.Add("role"))));
        separate.Resolve(source).Next();
        observed.ShouldBe(["reference", "role"]);
    }

    [Fact]
    public void EmptySelection_IsSilent_AndNeverWraps()
    {
        var preparations = 0;
        var wraps = 0;
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source", () => preparations++);
        var scope = ResolutionScope.Create(Aspect.Create(AllCounters.And(new CounterPointcut(false)), new CounterAdvice((executor, _) =>
        {
            wraps++;
            return executor;
        })));

        scope.Resolve(source).ShouldBeOfType<Counter>().Next().ShouldBe(1);
        scope.CreateChildScope().Resolve(source).Next().ShouldBe(2);
        wraps.ShouldBe(0);
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
        Aspect Observe(string module) => Aspect.Create(sources, ObserveEach((source, _) => observed.Add($"{module}:{(ReferenceEquals(source, leaf) ? "leaf" : "named")}")));
        var scope = ResolutionScope.Create(Observe("A"),
            Observe("B"));

        scope.Resolve(leaf).Next();
        observed.ShouldBe(["A:leaf", "B:leaf"]);
        offered.ShouldNotBeEmpty();
        offered.ShouldAllBe(configuration => ReferenceEquals(configuration, leaf));

        observed.Clear();
        scope.Resolve(named).Next();
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
        rebound.Next().ShouldBe(2);
        rebound.Next().ShouldBe(3);
        ancestor.Next().ShouldBe(4);

        observed.ShouldBe([1, 2]);
        rebound.Calls.ShouldBe(4);
        rebound.Control.Value.ShouldBe(4);
        parent.CreateChildScope().Resolve(composite).Calls.ShouldBe(4);
    }

    [Fact]
    public void DeferredResolution_MatchesSourcesCreatedAfterTheDeclaration()
    {
        ResolutionScope? retained = null;
        var host = new CounterConfiguration("host", Bind: scope =>
        {
            retained = scope;
            return null;
        });
        var observed = new List<string>();
        var root = ResolutionScope.Create(Aspect.Create(AllCounters, ObserveEach((source, count) => observed.Add($"{((CounterConfiguration)source).Name}:{count}"))));
        root.Resolve(host);

        IConfigurationNode<ICounterExecutor> late = new CounterConfiguration("late");
        IConfigurationNode<ICounterExecutor> later = new CounterConfiguration("later");
        retained.ShouldNotBeNull().Resolve(late).Next().ShouldBe(1);
        retained.CreateChildScope().Resolve(later).Next().ShouldBe(1);

        observed.ShouldBe(["late:1", "later:1"]);
    }

    [Fact]
    public void PredicateFault_FailsTheResolutionInItsContext_WithoutRecreatingState()
    {
        var fault = new InvalidOperationException("predicate failure");
        var preparations = 0;
        var predicateCalls = 0;
        IConfigurationNode<ICounterExecutor> source = new CounterConfiguration("source", () => preparations++);
        var parent = ResolutionScope.Create(Aspect.Create(AllCounters.And(new CounterPointcut(true, _ => { if (++predicateCalls == 1) throw fault; })), new CounterObservationAdvice(_ => { })));

        Should.Throw<InvalidOperationException>(() => parent.Resolve(source)).ShouldBeSameAs(fault);
        parent.Resolve(source).Next().ShouldBe(1);
        var retried = NewContext(parent).Resolve(source);
        retried.ShouldNotBeOfType<Counter>();
        retried.Next().ShouldBe(2);
        preparations.ShouldBe(1);
    }
}
