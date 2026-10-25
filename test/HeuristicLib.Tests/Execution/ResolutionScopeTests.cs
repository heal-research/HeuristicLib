namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

/// <summary>
/// Covers the resolution rules: which wrappers apply, in which order they wrap, and when two resolves share one
/// scoped state. Every resolve builds a new executor. The cell names refer to the combinations of where wrappers
/// are declared and where resolves happen.
/// </summary>
public class ResolutionScopeTests
{
    // ---------- Sharing across scopes ----------

    [Fact]
    public void UnwrappedChild_ReusesTheScopedStateItsParentOwns()
    {
        var parent = ResolutionScope.Create();
        var configuration = new CountingConfiguration("instance");
        var parentExecutor = parent.Resolve(configuration);

        var childExecutor = parent.CreateChildScope().Resolve(configuration);

        childExecutor.ShouldNotBeSameAs(parentExecutor);
        childExecutor.State.ShouldBeSameAs(parentExecutor.State);
        configuration.CreateCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SiblingScopes_CreateSeparateStatesWhenNoAncestorHasResolvedTheConfiguration(bool wrapInParent)
    {
        var configuration = new CountingConfiguration("instance");
        var parent = wrapInParent
            ? ResolutionScope.Create(Wrap(configuration, "parent"))
            : ResolutionScope.Create();

        var first = parent.CreateChildScope().Resolve(configuration);
        var second = parent.CreateChildScope().Resolve(configuration);

        second.State.ShouldNotBeSameAs(first.State);
        configuration.CreateCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SiblingScopes_ReuseTheStateOfAnAncestorThatAlreadyResolvedTheConfiguration(bool wrapInAncestor)
    {
        var configuration = new CountingConfiguration("instance");
        var ancestor = wrapInAncestor
            ? ResolutionScope.Create(Wrap(configuration, "ancestor"))
            : ResolutionScope.Create();
        var ancestorExecutor = ancestor.Resolve(configuration);
        var parent = ancestor.CreateChildScope();

        var first = parent.CreateChildScope().Resolve(configuration);
        var second = parent.CreateChildScope().Resolve(configuration);

        first.Name.ShouldBe(ancestorExecutor.Name);
        second.Name.ShouldBe(ancestorExecutor.Name);
        first.State.ShouldBeSameAs(ancestorExecutor.State);
        second.State.ShouldBeSameAs(ancestorExecutor.State);
        configuration.CreateCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AParent_NeverSeesStateItsChildCreated(bool wrapInParent)
    {
        var original = new CountingConfiguration("original");
        var parent = wrapInParent
            ? ResolutionScope.Create(Wrap(original, "parent"))
            : ResolutionScope.Create();
        var child = parent.CreateChildScope();
        var childExecutor = child.Resolve(original);

        var parentExecutor = parent.Resolve(original);

        parentExecutor.State.ShouldNotBeSameAs(childExecutor.State);
        child.Resolve(original).State.ShouldBeSameAs(childExecutor.State);
        parent.Resolve(original).State.ShouldBeSameAs(parentExecutor.State);
        original.CreateCount.ShouldBe(2);
    }

    // ---------- Sharing with wrappers ----------

    [Fact]
    public void ChildAddingNoAdvice_ReconstructsTheParentsAdvisedExecution()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create(Wrap(original, "parent"));
        var parentExecutor = parent.Resolve(original);

        var childExecutor = parent.CreateChildScope().Resolve(original);

        childExecutor.ShouldNotBeSameAs(parentExecutor);
        childExecutor.Name.ShouldBe("parent(original)");
        original.CreateCount.ShouldBe(1);
    }

    [Fact]
    public void ChildWrappingAnotherConfiguration_RebuildsTheOriginalWithoutCreatingItsStateAgain()
    {
        var original = new CountingConfiguration("original");
        var other = new CountingConfiguration("other");
        var parent = ResolutionScope.Create(Wrap(original, "parent"));
        var parentExecutor = parent.Resolve(original);
        var child = parent.CreateChildScope(Wrap(other, "child"));

        child.Resolve(original).ShouldNotBeSameAs(parentExecutor);
        child.Resolve(original).Name.ShouldBe("parent(original)");
        child.Resolve(other).Name.ShouldBe("child(other)");
        original.CreateCount.ShouldBe(1);
        other.CreateCount.ShouldBe(1);
    }

    [Fact]
    public void ChildAddingItsOwnWrapper_BuildsItsOwnExecution()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create(Wrap(original, "parent"));
        var parentExecutor = parent.Resolve(original);

        var child = parent.CreateChildScope(Wrap(original, "child"));

        var childExecutor = child.Resolve(original);
        childExecutor.ShouldNotBeSameAs(parentExecutor);
        childExecutor.Name.ShouldBe("parent(child(original))");
    }

    [Fact]
    public void ResolvingTwiceInOneScope_CreatesFreshAdviceOverSharedState()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(Wrap(original, "only"));

        scope.Resolve(original).ShouldNotBeSameAs(scope.Resolve(original));
        original.CreateCount.ShouldBe(1);
    }

    // ---------- Composition across scopes ----------

    [Fact]
    public void ChildWrapper_ComposesWithTheParentsRatherThanReplacingIt()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create(Wrap(original, "parent"));

        var child = parent.CreateChildScope(Wrap(original, "child"));

        child.Resolve(original).Name.ShouldBe("parent(child(original))");
    }

    [Fact]
    public void ChildWrapper_DoesNotAffectTheParentsResolution()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create(Wrap(original, "parent"));

        parent.CreateChildScope(Wrap(original, "child")).Resolve(original);

        parent.Resolve(original).Name.ShouldBe("parent(original)");
    }

    [Fact]
    public void UnwrappedConfiguration_IsBuiltPlain()
    {
        var wrapped = new CountingConfiguration("wrapped");
        var untouched = new CountingConfiguration("untouched");
        var scope = ResolutionScope.Create(Wrap(wrapped, "wrapper"));

        scope.Resolve(untouched).Name.ShouldBe("untouched");
    }

    // ---------- Ordering ----------

    [Fact]
    public void WithinOneScope_TheFirstWrapperDeclaredIsInnermost()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(Wrap(original, "first"),
            Wrap(original, "second"));

        scope.Resolve(original).Name.ShouldBe("second(first(original))");
    }

    [Fact]
    public void TheSameWrapperDeclaredTwice_StacksTwice()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(Wrap(original, "counted"),
            Wrap(original, "counted"));

        scope.Resolve(original).Name.ShouldBe("counted(counted(original))");
    }

    [Fact]
    public void ADeeperScopesWrapper_IsInnerThanAShallowerOne()
    {
        var original = new CountingConfiguration("original");
        var root = ResolutionScope.Create(Wrap(original, "shallow"));

        var deep = root.CreateChildScope(Wrap(original, "deep"));

        deep.Resolve(original).Name.ShouldBe("shallow(deep(original))");
    }

    [Fact]
    public void AnExistingAspect_IsInnerThanARecipeInstalledAfterIt()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(Wrap(original, "module"),
            Wrap(original, "configuration"));

        scope.Resolve(original).Name.ShouldBe("configuration(module(original))");
    }

    [Fact]
    public void AnExistingAspectInADescendant_IsInnerThanAnAncestorRecipe()
    {
        var original = new CountingConfiguration("original");
        var root = ResolutionScope.Create(Wrap(original, "configuration"));

        var child = root.CreateChildScope(Wrap(original, "module"));

        child.Resolve(original).Name.ShouldBe("configuration(module(original))");
    }

    [Fact]
    public void AmongModules_TheFirstInstalledIsInnermost()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(Wrap(original, "first"), Wrap(original, "second"));

        scope.Resolve(original).Name.ShouldBe("second(first(original))");
    }

    // ---------- Building the chain ----------

    [Fact]
    public void BuildingAChain_CreatesTheUnwrappedExecutionExactlyOnce()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(Wrap(original, "inner"),
            Wrap(original, "middle"),
            Wrap(original, "outer"));

        scope.Resolve(original).Name.ShouldBe("outer(middle(inner(original)))");
        original.CreateCount.ShouldBe(1);
    }

    [Fact]
    public void AfterAChainIsBuilt_TheUnwrappedConfigurationStillResolvesThroughItsWrappers()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(Wrap(original, "wrapper"));

        scope.Resolve(original).Name.ShouldBe("wrapper(original)");
        scope.Resolve(original).Name.ShouldBe("wrapper(original)");
        original.CreateCount.ShouldBe(1);
    }

    // ---------- Own roles ----------

    [Fact]
    public void RoleAdvice_ReadsTheSelectedConfigurationThroughItsRole()
    {
        ILabeledRole configuration = new LabeledConfiguration("role");
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(configuration), new RoleLabelAdvice()));

        scope.Resolve(configuration).Name.ShouldBe("role(labeled)");
    }

    /// <summary>
    /// <c>Aspect.Create</c> rejects this pairing at compile time; an aspect that lists the pair itself reaches
    /// resolution, which reports it.
    /// </summary>
    [Fact]
    public void RoleAdvice_ListedForAnotherRoleOfTheSameExecutorType_IsReported()
    {
        var other = new CountingConfiguration("other");
        var scope = ResolutionScope.Create(new PairAspect(Pointcut.Reference(other), new RoleLabelAdvice()));

        Should.Throw<InvalidOperationException>(() => scope.Resolve(other)).Message.ShouldBe(
            $"{nameof(RoleLabelAdvice)} cannot wrap {nameof(INamedExecutor)} of {nameof(CountingConfiguration)}. " +
            $"Advice for this role derives from Advice<TConfiguration, {nameof(INamedExecutor)}> for a TConfiguration the configuration implements.");
    }

    [Fact]
    public void RoleAdvice_WrapsAResolutionThroughAWiderExecutorType()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(Wrap(original, "named"));

        scope.Resolve<IExecutor>(original).ShouldBeOfType<NamedExecutor>().Name.ShouldBe("named(original)");
    }

    [Fact]
    public void AdviceForAnotherExecutorTypeOfTheConfiguration_IsReportedRatherThanCast()
    {
        var configuration = new TwoRoleConfiguration();
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(configuration), new OtherRoleAdvice()));

        var exception = Should.Throw<InvalidOperationException>(() => scope.Resolve<INamedExecutor>(configuration));

        exception.Message.ShouldBe(
            $"{nameof(OtherRoleAdvice)} cannot wrap {nameof(INamedExecutor)} of {nameof(TwoRoleConfiguration)}. " +
            $"Advice for this role derives from Advice<TConfiguration, {nameof(INamedExecutor)}> for a TConfiguration the configuration implements.");
        scope.Resolve<IOtherExecutor>(configuration).ShouldBeOfType<OtherExecutor>().Wrapped.ShouldBeTrue();
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
        scope.Resolve(original)
            .Name.ShouldBe("second(installed(first(original)))");
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultipleAspectLayersPreserveInstallationOrdering(bool childState)
    {
        var original = new CountingConfiguration("original");
        IConfigurationNode<INamedExecutor> selected = original;
        var installed = Wrap(selected, "installed");
        var first = new NamedAspect(Wrap(original, "first"), installed);
        var second = Wrap(selected, "second");
        var root = ResolutionScope.Create(first, second);
        root.Resolve(original);
        var third = Wrap(selected, "third");
        var fourth = Wrap(selected, "fourth");
        var scope = childState ? root.CreateChildScope(third, fourth) : root.With(third, fourth);

        scope.Resolve(original)
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
        ResolutionScope.Create(combined).Resolve(original)
            .Name.ShouldBe("last(nested(inline(existing(original))))");
    }

    // ---------- Test doubles ----------

    private static Aspect Wrap(IConfigurationNode<INamedExecutor> anchor, string label) =>
        Aspect.Create(Pointcut.Reference(anchor), new LabelAdvice(label));

    private sealed class LabelAdvice(string label) : Advice<IConfigurationNode<INamedExecutor>, INamedExecutor>
    {
        public override INamedExecutor Wrap(INamedExecutor executor, IConfigurationNode<INamedExecutor> source) =>
            new NamedExecutor($"{label}({executor.Name})", executor.State);
    }

    private sealed class NamedAspect : Aspect
    {
        public NamedAspect(params IReadOnlyList<Aspect> aspects)
        {
            Advices = [.. aspects.SelectMany(aspect => aspect)];
        }
    }

    private interface INamedExecutor : IExecutor
    {
        string Name { get; }
        object? State { get; }
    }

    private sealed class NamedExecutor(string name, object? state) : INamedExecutor
    {
        public string Name { get; } = name;
        public object? State { get; } = state;
    }

    // Counts the scoped states it creates, one per owning scope.
    private sealed class CountingConfiguration(string name) : IConfigurationNode<INamedExecutor>
    {
        public int CreateCount { get; private set; }

        public INamedExecutor CreateExecutor(ResolutionScope scope) =>
            new NamedExecutor(name, scope.GetScopedState(this, () =>
            {
                CreateCount++;
                return new object();
            }));
    }

    private interface ILabeledRole : IConfigurationNode<INamedExecutor>
    {
        string Label { get; }
    }

    private sealed class LabeledConfiguration(string label) : ILabeledRole
    {
        public string Label { get; } = label;

        public INamedExecutor CreateExecutor(ResolutionScope scope) => new NamedExecutor("labeled", null);
    }

    private sealed class RoleLabelAdvice : Advice<ILabeledRole, INamedExecutor>
    {
        public override INamedExecutor Wrap(INamedExecutor executor, ILabeledRole source) =>
            new NamedExecutor($"{source.Label}({executor.Name})", executor.State);
    }

    // Lists its pair directly, which bypasses the role check of Aspect.Create.
    private sealed class PairAspect : Aspect
    {
        public PairAspect(Pointcut pointcut, Advice advice)
        {
            Advices = [(pointcut, advice)];
        }
    }

    private interface IOtherExecutor : IExecutor;

    private sealed class OtherExecutor(bool wrapped) : IOtherExecutor
    {
        public bool Wrapped { get; } = wrapped;
    }

    // Resolves as either executor type; each build creates only the executor requested.
    private sealed class TwoRoleConfiguration : IConfigurationNode<INamedExecutor>, IConfigurationNode<IOtherExecutor>
    {
        INamedExecutor IConfigurationNode<INamedExecutor>.CreateExecutor(ResolutionScope scope) => new NamedExecutor("named", null);

        IOtherExecutor IConfigurationNode<IOtherExecutor>.CreateExecutor(ResolutionScope scope) => new OtherExecutor(false);
    }

    private sealed class OtherRoleAdvice : Advice<IConfigurationNode<IOtherExecutor>, IOtherExecutor>
    {
        public override IOtherExecutor Wrap(IOtherExecutor executor, IConfigurationNode<IOtherExecutor> source) => new OtherExecutor(true);
    }
}
