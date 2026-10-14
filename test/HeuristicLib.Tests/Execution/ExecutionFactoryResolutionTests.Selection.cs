namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public partial class ExecutionFactoryResolutionTests
{
    private static readonly Pointcut<IConfigurationNode<ICounterExecution>> AllCounters =
        Pointcut.OfType<IConfigurationNode<ICounterExecution>>();

    [Fact]
    public void SelectorRegistration_WrapsEachMatchingSource_WithItsOwnAdviceState()
    {
        IConfigurationNode<ICounterExecution> first = new CounterConfiguration("first");
        IConfigurationNode<ICounterExecution> second = new CounterConfiguration("second");
        IConfigurationNode<ICounterExecution> excluded = new CounterConfiguration("excluded");
        var wrapped = new List<IConfigurationNode<ICounterExecution>>();
        var observed = new List<string>();
        var scope = ResolutionScope.Create(builder => builder.Install(
            AllCounters.And(configuration => configuration is CounterConfiguration { Name: not "excluded" }),
            original =>
            {
                wrapped.Add(original);
                var name = ((CounterConfiguration)original).Name;
                return new CounterWrapper(original, count => observed.Add($"{name}:{count}"));
            }));

        scope.Resolve(first, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next();
        scope.Resolve(second, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next();
        scope.Resolve(first, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next();
        scope.Resolve(excluded, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).ShouldBeOfType<Counter>().Next().ShouldBe(1);

        wrapped.ShouldBe([first, second]);
        observed.ShouldBe(["first:1", "second:1", "first:2"]);
    }

    [Fact]
    public void OverlappingUnion_WrapsOnce_WhileSeparateRegistrationsBothApply()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var observed = new List<string>();
        var union = ResolutionScope.Create(builder =>
            builder.Install(Pointcut.Reference(source) | AllCounters, original => new CounterWrapper(original, _ => observed.Add("union"))));
        union.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next();
        observed.ShouldBe(["union"]);

        observed.Clear();
        var separate = ResolutionScope.Create(builder => builder
            .Install(Pointcut.Reference(source), original => new CounterWrapper(original, _ => observed.Add("reference")))
            .Install(AllCounters, original => new CounterWrapper(original, _ => observed.Add("role"))));
        separate.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next();
        observed.ShouldBe(["reference", "role"]);
    }

    [Fact]
    public void EmptySelection_IsSilent_AndNeverRunsItsRecipe()
    {
        var preparations = 0;
        var recipes = 0;
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source", () => preparations++);
        var scope = ResolutionScope.Create(builder => builder.Install(AllCounters.And(_ => false), original =>
        {
            recipes++;
            return new CounterWrapper(original, _ => { });
        }));

        var execution = scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
        execution.ShouldBeOfType<Counter>().Next().ShouldBe(1);
        scope.CreateChildScope().Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).ShouldBeSameAs(execution);
        recipes.ShouldBe(0);
        preparations.ShouldBe(1);
    }

    [Fact]
    public void ModulesSharingABroadSelector_AreOfferedOnlySourceNodes_AndEachApplyOnce()
    {
        IConfigurationNode<ICounterExecution> leaf = new CounterConfiguration("leaf");
        IConfigurationNode<ICounterExecution> named = new CounterWrapper(leaf, _ => { });
        var offered = new List<IConfigurationNode<ICounterExecution>>();
        var observed = new List<string>();
        var sources = AllCounters.And(configuration =>
        {
            offered.Add(configuration);
            return true;
        });
        Module Observe(string module) => new(builder => builder.Install(sources, original =>
            new CounterWrapper(original, _ => observed.Add($"{module}:{(ReferenceEquals(original, leaf) ? "leaf" : "named")}"))));
        var scope = ResolutionScope.Create(builder =>
        {
            builder.Install(Observe("A"));
            builder.Install(Observe("B"));
        });

        scope.Resolve(leaf, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next();
        observed.ShouldBe(["A:leaf", "B:leaf"]);
        offered.ShouldNotBeEmpty();
        offered.ShouldAllBe(configuration => ReferenceEquals(configuration, leaf));

        observed.Clear();
        scope.Resolve(named, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next();
        observed.ShouldBe(["A:leaf", "B:leaf", "A:named", "B:named"]);
        offered.ShouldContain(named);
        offered.ShouldAllBe(configuration => ReferenceEquals(configuration, leaf) || ReferenceEquals(configuration, named));
    }

    [Fact]
    public void ChildSelector_ReachesTheDependencyOfAnAncestorComposite_WithoutResettingState()
    {
        IConfigurationNode<ICounterExecution> leaf = new CounterConfiguration("leaf");
        var composite = new Composite(leaf);
        var parent = ResolutionScope.Create();
        var ancestor = parent.Resolve(composite);
        ancestor.Next().ShouldBe(1);

        var observed = new List<int>();
        var child = parent.CreateChildScope(builder => builder.Install(AllCounters, original => new CounterWrapper(original, observed.Add)));
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
        var root = ResolutionScope.Create(builder => builder.Install(AllCounters, original =>
            new CounterWrapper(original, count => observed.Add($"{((CounterConfiguration)original).Name}:{count}"))));
        root.Resolve(host, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);

        IConfigurationNode<ICounterExecution> late = new CounterConfiguration("late");
        IConfigurationNode<ICounterExecution> later = new CounterConfiguration("later");
        retained.ShouldNotBeNull().Resolve(late, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
        retained.CreateChildScope().Resolve(later, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(1);

        observed.ShouldBe(["late:1", "later:1"]);
    }

    [Fact]
    public void PredicateFault_FailsTheBindingInItsContext_WithoutRepeatingPreparation()
    {
        var fault = new InvalidOperationException("predicate failure");
        var preparations = 0;
        var predicateCalls = 0;
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source", () => preparations++);
        var parent = ResolutionScope.Create(builder => builder.Install(
            AllCounters.And(_ => ++predicateCalls == 1 ? throw fault : true),
            original => new CounterWrapper(original, _ => { })));

        Should.Throw<InvalidOperationException>(() => parent.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice)).ShouldBeSameAs(fault);
        parent.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice).Next().ShouldBe(1);
        var retried = NewContext(parent).Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice);
        retried.ShouldNotBeOfType<Counter>();
        retried.Next().ShouldBe(2);
        preparations.ShouldBe(1);
    }

    [Fact]
    public void CompositeRecipeIsRejectedEvenWhenItProducesTheTargetsExecutionRole()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var scope = ResolutionScope.Create(builder => builder.Install(source,
            original => new CounterConfiguration("composite", Bind: frame => frame.Resolve(original, static source => source.CreateExecutionFactory(), PrepareCounterAdvice))));
        Should.Throw<InvalidOperationException>(() => scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice)).Message.ShouldContain("single-child wrapper");
    }

    [Fact]
    public void BroadSelection_RequiresWrappersToStandInForTheResolvedRole()
    {
        IConfigurationNode<ICounterExecution> source = new CounterConfiguration("source");
        var scope = ResolutionScope.Create(builder => builder.Install(Pointcut.OfType<IConfigurationNode>(), _ => new Composite(source)));

        Should.Throw<InvalidOperationException>(() => scope.Resolve(source, static source => source.CreateExecutionFactory(), PrepareCounterAdvice)).Message.ShouldContain("single-child wrapper");
    }
}
