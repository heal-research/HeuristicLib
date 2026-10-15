namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

/// <summary>
/// Covers the resolution rules: which wrappers apply, in which order they wrap, and when two resolves share one
/// instance. The cell names refer to the combinations of where wrappers are declared and where resolves happen.
/// </summary>
public class ResolutionScopeTests
{
    // ---------- Sharing across scopes ----------

    [Fact]
    public void UnwrappedChild_ReusesTheExecutionItsParentAlreadyBuilt()
    {
        var parent = ResolutionScope.Create();
        var configuration = new CountingConfiguration("instance");
        var parentExecution = parent.Resolve(configuration, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);

        var childExecution = parent.CreateChildScope().Resolve(configuration, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);

        childExecution.ShouldBeSameAs(parentExecution);
        configuration.CreateCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SiblingScopes_BuildSeparateExecutionsWhenNoAncestorHasResolvedTheConfiguration(bool wrapInParent)
    {
        var configuration = new CountingConfiguration("instance");
        var parent = wrapInParent
            ? ResolutionScope.Create(Wrap(configuration, "parent"))
            : ResolutionScope.Create();

        var first = parent.CreateChildScope().Resolve(configuration, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);
        var second = parent.CreateChildScope().Resolve(configuration, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);

        second.ShouldNotBeSameAs(first);
        configuration.CreateCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SiblingScopes_ReuseAnExecutionAlreadyBuiltInAnAncestor(bool wrapInAncestor)
    {
        var configuration = new CountingConfiguration("instance");
        var ancestor = wrapInAncestor
            ? ResolutionScope.Create(Wrap(configuration, "ancestor"))
            : ResolutionScope.Create();
        var ancestorExecution = ancestor.Resolve(configuration, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);
        var parent = ancestor.CreateChildScope();

        var first = parent.CreateChildScope().Resolve(configuration, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);
        var second = parent.CreateChildScope().Resolve(configuration, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);

        first.Name.ShouldBe(ancestorExecution.Name);
        second.Name.ShouldBe(ancestorExecution.Name);
        if (wrapInAncestor)
        {
            first.ShouldNotBeSameAs(ancestorExecution);
            second.ShouldNotBeSameAs(ancestorExecution);
        }
        else
        {
            first.ShouldBeSameAs(ancestorExecution);
            second.ShouldBeSameAs(ancestorExecution);
        }
        configuration.CreateCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AParent_NeverSeesAnExecutionItsChildBuilt(bool wrapInParent)
    {
        var original = new CountingConfiguration("original");
        var parent = wrapInParent
            ? ResolutionScope.Create(Wrap(original, "parent"))
            : ResolutionScope.Create();
        var child = parent.CreateChildScope();
        var childExecution = child.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);

        var parentExecution = parent.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);

        parentExecution.ShouldNotBeSameAs(childExecution);
        child.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe(childExecution.Name);
        parent.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe(parentExecution.Name);
        original.CreateCount.ShouldBe(2);
    }

    // ---------- Sharing with wrappers ----------

    [Fact]
    public void ChildAddingNoAdvice_ReconstructsTheParentsAdvisedExecution()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create(Wrap(original, "parent"));
        var parentExecution = parent.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);

        var childExecution = parent.CreateChildScope().Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);

        childExecution.ShouldNotBeSameAs(parentExecution);
        childExecution.Name.ShouldBe("parent(original)");
        original.CreateCount.ShouldBe(1);
    }

    [Fact]
    public void ChildWrappingAnotherConfiguration_RebindsTheOriginalWithoutPreparingItsStateAgain()
    {
        var original = new CountingConfiguration("original");
        var other = new CountingConfiguration("other");
        var parent = ResolutionScope.Create(Wrap(original, "parent"));
        var parentExecution = parent.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);
        var child = parent.CreateChildScope(Wrap(other, "child"));

        child.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).ShouldNotBeSameAs(parentExecution);
        child.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("parent(original)");
        child.Resolve(other, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("child(other)");
        original.CreateCount.ShouldBe(1);
        other.CreateCount.ShouldBe(1);
    }

    [Fact]
    public void ChildAddingItsOwnWrapper_BuildsItsOwnExecution()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create(Wrap(original, "parent"));
        var parentExecution = parent.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);

        var child = parent.CreateChildScope(Wrap(original, "child"));

        var childExecution = child.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);
        childExecution.ShouldNotBeSameAs(parentExecution);
        childExecution.Name.ShouldBe("parent(child(original))");
    }

    [Fact]
    public void ResolvingTwiceInOneScope_CreatesFreshAdviceOverSharedState()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(Wrap(original, "only"));

        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).ShouldNotBeSameAs(scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice));
        original.CreateCount.ShouldBe(1);
    }

    // ---------- Composition across scopes ----------

    [Fact]
    public void ChildWrapper_ComposesWithTheParentsRatherThanReplacingIt()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create(Wrap(original, "parent"));

        var child = parent.CreateChildScope(Wrap(original, "child"));

        child.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("parent(child(original))");
    }

    [Fact]
    public void ChildWrapper_DoesNotAffectTheParentsResolution()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create(Wrap(original, "parent"));

        parent.CreateChildScope(Wrap(original, "child")).Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);

        parent.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("parent(original)");
    }

    [Fact]
    public void UnwrappedConfiguration_IsBuiltPlain()
    {
        var wrapped = new CountingConfiguration("wrapped");
        var untouched = new CountingConfiguration("untouched");
        var scope = ResolutionScope.Create(Wrap(wrapped, "wrapper"));

        scope.Resolve(untouched, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("untouched");
    }

    // ---------- Ordering ----------

    [Fact]
    public void WithinOneScope_TheFirstWrapperDeclaredIsInnermost()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(Wrap(original, "first"),
            Wrap(original, "second"));

        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("second(first(original))");
    }

    [Fact]
    public void TheSameWrapperDeclaredTwice_StacksTwice()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(Wrap(original, "counted"),
            Wrap(original, "counted"));

        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("counted(counted(original))");
    }

    [Fact]
    public void ADeeperScopesWrapper_IsInnerThanAShallowerOne()
    {
        var original = new CountingConfiguration("original");
        var root = ResolutionScope.Create(Wrap(original, "shallow"));

        var deep = root.CreateChildScope(Wrap(original, "deep"));

        deep.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("shallow(deep(original))");
    }

    [Fact]
    public void AnExistingAspect_IsInnerThanARecipeInstalledAfterIt()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(Wrap(original, "module"),
            Wrap(original, "configuration"));

        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("configuration(module(original))");
    }

    [Fact]
    public void AnExistingAspectInADescendant_IsInnerThanAnAncestorRecipe()
    {
        var original = new CountingConfiguration("original");
        var root = ResolutionScope.Create(Wrap(original, "configuration"));

        var child = root.CreateChildScope(Wrap(original, "module"));

        child.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("configuration(module(original))");
    }

    [Fact]
    public void AmongModules_TheFirstInstalledIsInnermost()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(Wrap(original, "first"), Wrap(original, "second"));

        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("second(first(original))");
    }

    // ---------- Building the chain ----------

    [Fact]
    public void BuildingAChain_CreatesTheUnwrappedExecutionExactlyOnce()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(Wrap(original, "inner"),
            Wrap(original, "middle"),
            Wrap(original, "outer"));

        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("outer(middle(inner(original)))");
        original.CreateCount.ShouldBe(1);
    }

    [Fact]
    public void AfterAChainIsBuilt_TheUnwrappedConfigurationStillResolvesThroughItsWrappers()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(Wrap(original, "wrapper"));

        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("wrapper(original)");
        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("wrapper(original)");
        original.CreateCount.ShouldBe(1);
    }

    // ---------- Phase separation ----------


    // ---------- Resolving through a preparation adapter ----------

    [Fact]
    public void ResolvingThroughAPreparationAdapter_AppliesTheWrappers()
    {
        var original = new RunTypedConfiguration("original");
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference<IRunTypedConfiguration>(original), new LabelAdvice<IRunTypedConfiguration>("inner")),
            Aspect.Create(Pointcut.Reference<IRunTypedConfiguration>(original), new LabelAdvice<IRunTypedConfiguration>("outer")));

        var resolved = ResolveRunTyped(scope, original);

        resolved.Name.ShouldBe("outer(inner(original))");
        scope.CreateChildScope().Resolve(original, static source => source.CreateExecutionFactory(), PrepareRunAdvice).ShouldNotBeSameAs(resolved);
        original.CreateCount.ShouldBe(1);
    }

    [Fact]
    public void AnExecutionHeldAsAnotherType_IsReportedRatherThanCast()
    {
        var configuration = new RunTypedConfiguration("instance");
        var scope = ResolutionScope.Create();
        ResolveRunTyped(scope, configuration);

        var exception = Should.Throw<InvalidOperationException>(() =>
            scope.CreateChildScope().Resolve<IRunTypedConfiguration, OtherExecution>(configuration, static _ => _ => new OtherExecution(), static (_, _) => throw new InvalidOperationException("No compatible wrapper.")));

        exception.Message.ShouldBe(
            "RunTypedConfiguration was prepared for INamedExecution, not OtherExecution. " +
            "Resolve over a second search space or problem in its own scope.");
    }

    [Fact]
    public void MultipleAspectsSnapshotInputsAndDeduplicateTheirSharedInstallation()
    {
        var original = new CountingConfiguration("original");
        var installed = Wrap(original, "installed");
        var first = new NamedAspect(Wrap(original, "first"), installed);
        var second = new NamedAspect(Wrap(original, "second"), installed);
        Aspect[] inputs = [first, second, installed, first];
        var scope = ResolutionScope.Create(inputs);
        inputs[0] = Wrap(original, "replacement");
        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice)
            .Name.ShouldBe("second(installed(first(original)))");
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultipleAspectLayersPreserveInstallationOrdering(bool childState)
    {
        var original = new CountingConfiguration("original");
        IConfigurationNode<INamedExecution> selected = original;
        var installed = Wrap(selected, "installed");
        var first = new NamedAspect(Wrap(original, "first"), installed);
        var second = Wrap(selected, "second");
        var root = ResolutionScope.Create(first, second);
        root.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);
        var third = Wrap(selected, "third");
        var fourth = Wrap(selected, "fourth");
        var scope = childState ? root.CreateChildScope(third, fourth) : root.With(third, fourth);

        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice)
            .Name.ShouldBe("second(installed(first(fourth(third(original)))))");
        original.CreateCount.ShouldBe(1);
    }

    [Fact]
    public void ExistingAspectsFollowOneApplicationSequence()
    {
        var original = new CountingConfiguration("original");
        var existing = Wrap(original, "existing");
        var nested = Wrap(original, "nested");
        var combined = new NamedAspect(existing, Wrap(original, "inline"), nested, Wrap(original, "last"));
        ResolutionScope.Create(combined).Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice)
            .Name.ShouldBe("last(nested(inline(existing(original))))");
    }

    // ---------- Test doubles ----------

    private static Aspect Wrap(IConfigurationNode<INamedExecution> anchor, string label) =>
        Aspect.Create(Pointcut.Reference(anchor), new LabelAdvice<IConfigurationNode<INamedExecution>>(label));

    private static WrapperExecutionFactory<INamedExecution> PrepareNamedAdvice(Advice advice, IConfigurationNode<INamedExecution> configuration) =>
        ((LabelAdvice<IConfigurationNode<INamedExecution>>)advice).CreateWrapperFactory();

    private static WrapperExecutionFactory<INamedExecution> PrepareRunAdvice(Advice advice, IRunTypedConfiguration configuration) =>
        ((LabelAdvice<IRunTypedConfiguration>)advice).CreateWrapperFactory();

    private sealed class LabelAdvice<TConfiguration>(string label) : Advice<TConfiguration>
        where TConfiguration : class, IConfigurationNode
    {
        public WrapperExecutionFactory<INamedExecution> CreateWrapperFactory() =>
            inner => new NamedExecution($"{label}({inner.Name})");
    }

    private sealed class NamedAspect : Aspect
    {
        public NamedAspect(params IReadOnlyList<Aspect> aspects)
        {
            Advices = [.. aspects.SelectMany(aspect => aspect.Advices)];
        }
    }

    private interface INamedExecution : IExecutionNode
    {
        string Name { get; }
    }

    private sealed class NamedExecution(string name) : INamedExecution
    {
        public string Name { get; } = name;
    }

    private sealed class CountingConfiguration(string name) : IConfigurationNode<INamedExecution>
    {
        public int CreateCount { get; private set; }
        public ExecutionFactory<INamedExecution> CreateExecutionFactory()
        {
            CreateCount++;
            var execution = new NamedExecution(name);
            return _ => execution;
        }
    }

    private sealed class OtherExecution : IExecutionNode;

    private interface IRunTypedConfiguration : IConfigurationNode
    {
        ExecutionFactory<INamedExecution> CreateExecutionFactory();
    }

    private static INamedExecution ResolveRunTyped(ResolutionScope scope, IRunTypedConfiguration configuration) =>
        scope.Resolve(configuration, static source => source.CreateExecutionFactory(), PrepareRunAdvice);

    private sealed class RunTypedConfiguration(string name) : IRunTypedConfiguration
    {
        public int CreateCount { get; private set; }
        public ExecutionFactory<INamedExecution> CreateExecutionFactory()
        {
            CreateCount++;
            var execution = new NamedExecution(name);
            return _ => execution;
        }
    }
}
