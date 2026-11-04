namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

/// <summary>
/// Covers contextual wrapper selection, ordering and executor private data. A scope builds one executor per
/// configuration and wraps it with fresh advice for every request; child scopes build their own. The cell names refer
/// to the combinations of where wrappers are declared and where resolves happen.
/// </summary>
public class ResolutionScopeTests
{
    // ---------- Sharing across scopes ----------

    [Fact]
    public void UnwrappedChild_ConstructsIndependentPrivateData()
    {
        var parent = ResolutionScope.Create();
        var configuration = new CountingConfiguration("instance");
        var parentExecutor = parent.Resolve(configuration);

        var childExecutor = parent.CreateChildScope().Resolve(configuration);

        childExecutor.ShouldNotBeSameAs(parentExecutor);
        childExecutor.State.ShouldNotBeSameAs(parentExecutor.State);
        configuration.CreateCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SiblingContexts_ConstructIndependentData(bool wrapInParent)
    {
        var configuration = new CountingConfiguration("instance");
        var parent = ResolutionScope.Create();
        if (wrapInParent)
            parent.AddAspect(Wrap(configuration, "parent"));

        var first = parent.CreateChildScope().Resolve(configuration);
        var second = parent.CreateChildScope().Resolve(configuration);

        second.State.ShouldNotBeSameAs(first.State);
        configuration.CreateCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SiblingContexts_StayIndependentAfterRootResolution(bool wrapInAncestor)
    {
        var configuration = new CountingConfiguration("instance");
        var ancestor = ResolutionScope.Create();
        if (wrapInAncestor)
            ancestor.AddAspect(Wrap(configuration, "ancestor"));
        var ancestorExecutor = ancestor.Resolve(configuration);
        var parent = ancestor.CreateChildScope();

        var first = parent.CreateChildScope().Resolve(configuration);
        var second = parent.CreateChildScope().Resolve(configuration);

        first.Name.ShouldBe(ancestorExecutor.Name);
        second.Name.ShouldBe(ancestorExecutor.Name);
        first.State.ShouldNotBeSameAs(ancestorExecutor.State);
        second.State.ShouldNotBeSameAs(ancestorExecutor.State);
        configuration.CreateCount.ShouldBe(3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AParent_NeverSeesStateItsChildCreated(bool wrapInParent)
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create();
        if (wrapInParent)
            parent.AddAspect(Wrap(original, "parent"));
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
        var parent = ResolutionScope.Create();
        parent.AddAspect(Wrap(original, "parent"));
        var parentExecutor = parent.Resolve(original);

        var childExecutor = parent.CreateChildScope().Resolve(original);

        childExecutor.ShouldNotBeSameAs(parentExecutor);
        childExecutor.Name.ShouldBe("parent(original)");
        original.CreateCount.ShouldBe(2);
    }

    [Fact]
    public void ChildWrappingAnotherConfiguration_ReconstructsPrivateData()
    {
        var original = new CountingConfiguration("original");
        var other = new CountingConfiguration("other");
        var parent = ResolutionScope.Create();
        parent.AddAspect(Wrap(original, "parent"));
        var parentExecutor = parent.Resolve(original);
        var child = parent.CreateChildScope();
        child.AddAspect(Wrap(other, "child"));

        child.Resolve(original).ShouldNotBeSameAs(parentExecutor);
        child.Resolve(original).Name.ShouldBe("parent(original)");
        child.Resolve(other).Name.ShouldBe("child(other)");
        original.CreateCount.ShouldBe(2);
        other.CreateCount.ShouldBe(1);
    }

    [Fact]
    public void ChildAddingItsOwnWrapper_BuildsItsOwnExecution()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create();
        parent.AddAspect(Wrap(original, "parent"));
        var parentExecutor = parent.Resolve(original);

        var child = parent.CreateChildScope();
        child.AddAspect(Wrap(original, "child"));

        var childExecutor = child.Resolve(original);
        childExecutor.ShouldNotBeSameAs(parentExecutor);
        childExecutor.Name.ShouldBe("parent(child(original))");
    }

    [Fact]
    public void ResolvingTwiceInOneScope_CreatesFreshAdviceOverSharedState()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create();
        scope.AddAspect(Wrap(original, "only"));

        var first = scope.Resolve(original);
        var second = scope.Resolve(original);

        second.ShouldNotBeSameAs(first);
        second.State.ShouldBeSameAs(first.State);
        original.CreateCount.ShouldBe(1);
    }

    // ---------- Composition across scopes ----------

    [Fact]
    public void ChildWrapper_ComposesWithTheParentsRatherThanReplacingIt()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create();
        parent.AddAspect(Wrap(original, "parent"));

        var child = parent.CreateChildScope();
        child.AddAspect(Wrap(original, "child"));

        child.Resolve(original).Name.ShouldBe("parent(child(original))");
    }

    [Fact]
    public void ChildWrapper_DoesNotAffectTheParentsResolution()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create();
        parent.AddAspect(Wrap(original, "parent"));

        var scope = parent.CreateChildScope();
        scope.AddAspect(Wrap(original, "child"));
        scope.Resolve(original);

        parent.Resolve(original).Name.ShouldBe("parent(original)");
    }

    [Fact]
    public void UnwrappedConfiguration_IsBuiltPlain()
    {
        var wrapped = new CountingConfiguration("wrapped");
        var untouched = new CountingConfiguration("untouched");
        var scope = ResolutionScope.Create();
        scope.AddAspect(Wrap(wrapped, "wrapper"));

        scope.Resolve(untouched).Name.ShouldBe("untouched");
    }

    // ---------- Ordering ----------

    [Fact]
    public void TheSameWrapperDeclaredTwice_StacksTwice()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create();
        scope.AddAspect(Wrap(original, "counted"));
        scope.AddAspect(Wrap(original, "counted"));

        scope.Resolve(original).Name.ShouldBe("counted(counted(original))");
    }

    [Fact]
    public void ADeeperScopesWrapper_IsInnerThanAShallowerOne()
    {
        var original = new CountingConfiguration("original");
        var root = ResolutionScope.Create();
        root.AddAspect(Wrap(original, "shallow"));

        var deep = root.CreateChildScope();
        deep.AddAspect(Wrap(original, "deep"));

        deep.Resolve(original).Name.ShouldBe("shallow(deep(original))");
    }

    [Fact]
    public void AnAddedAspect_WrapsCloserThanTheAspectsAddedBeforeIt()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create();
        scope.AddAspect(Wrap(original, "first"));
        scope.AddAspect(Wrap(original, "second"));
        scope.AddAspect(Wrap(original, "third"));

        scope.Resolve(original).Name.ShouldBe("first(second(third(original)))");
    }

    [Fact]
    public void AChildScopesAspects_WrapCloserThanItsParents()
    {
        var original = new CountingConfiguration("original");
        var root = ResolutionScope.Create();
        root.AddAspect(Wrap(original, "rootFirst"));
        root.AddAspect(Wrap(original, "rootSecond"));
        var child = root.CreateChildScope();
        child.AddAspect(Wrap(original, "childFirst"));
        child.AddAspect(Wrap(original, "childSecond"));

        child.Resolve(original).Name.ShouldBe("rootFirst(rootSecond(childFirst(childSecond(original))))");
    }

    // Run setup lists aspects in the order they are attached, and an aspect attached again keeps its first position.
    [Fact]
    public void ARunSetupScope_WrapsTheFirstAttachedAspectClosest()
    {
        var original = new CountingConfiguration("original");
        var first = Wrap(original, "first");
        var second = Wrap(original, "second");

        new ResolutionScopeBuilder(first, second, first).Build().Resolve(original).Name.ShouldBe("second(first(original))");
    }

    [Fact]
    public void AdviceInSeveralScopes_AppliesOnceAtItsClosestOccurrence()
    {
        var original = new CountingConfiguration("original");
        var shared = Wrap(original, "shared");
        var root = ResolutionScope.Create();
        root.AddAspect(new NamedAspect(Wrap(original, "root"), shared));
        var child = root.CreateChildScope();
        child.AddAspect(new NamedAspect(shared, Wrap(original, "child")));

        child.Resolve(original).Name.ShouldBe("root(child(shared(original)))");
        root.Resolve(original).Name.ShouldBe("shared(root(original))");
    }

    // ---------- Adding advice and sharing ----------

    [Fact]
    public void AddingAdviceAlreadyInEffect_ChangesNothing()
    {
        var original = new CountingConfiguration("original");
        var aspect = Wrap(original, "once");
        var scope = ResolutionScope.Create();
        scope.AddAspect(aspect);
        scope.Resolve(original);

        scope.AddAspect(aspect);
        scope.AddAspect(new NamedAspect(aspect));
        _ = scope.CreateChildScope();
        scope.AddAspect(aspect);

        scope.Resolve(original).Name.ShouldBe("once(original)");
        original.CreateCount.ShouldBe(1);
    }

    [Fact]
    public void AddingAdviceForAConfigurationTheScopeHasResolved_IsAnError()
    {
        var resolved = new CountingConfiguration("resolved");
        var later = new CountingConfiguration("later");
        var scope = ResolutionScope.Create();
        scope.Resolve(resolved);

        Should.Throw<InvalidOperationException>(() => scope.AddAspect(Wrap(resolved, "late"))).Message.ShouldContain("already resolved without it");
        scope.AddAspect(Wrap(later, "early"));

        scope.Resolve(later).Name.ShouldBe("early(later)");
        scope.Resolve(resolved).Name.ShouldBe("resolved");
    }

    // A rejected change leaves the scope tree usable.
    [Fact]
    public void ChangingAScopeThatHasChildScopes_IsAnError()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create();
        var child = scope.CreateChildScope();

        Should.Throw<InvalidOperationException>(() => scope.AddAspect(Wrap(original, "late"))).Message.ShouldContain("already has child scopes");
        Should.Throw<InvalidOperationException>(() => scope.Share(original)).Message.ShouldContain("already has child scopes");

        child.Resolve(original).Name.ShouldBe("original");
        scope.Resolve(original).Name.ShouldBe("original");
        original.CreateCount.ShouldBe(2);
    }

    [Fact]
    public void SharingAConfigurationTheScopeHasResolved_IsAnError()
    {
        var configuration = new CountingConfiguration("shared");
        var root = ResolutionScope.Create();
        root.Share(configuration);
        var child = root.CreateChildScope();
        child.Resolve(configuration);
        var local = ResolutionScope.Create();
        local.Resolve(configuration);

        Should.Throw<InvalidOperationException>(() => child.Share(configuration)).Message.ShouldContain("has already resolved it");
        Should.Throw<InvalidOperationException>(() => local.Share(configuration)).Message.ShouldContain("has already resolved it");
        // Sharing a configuration again changes nothing, even after child scopes exist.
        root.Share(configuration);
        configuration.CreateCount.ShouldBe(2);
    }

    // A scope fixes how it resolves a configuration when the resolution starts, so the configuration's own construction
    // can neither share it nor add advice for it.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AConfigurationChangingHowItIsResolvedWhileBuilding_IsAnError(bool share)
    {
        var leaf = new CountingConfiguration("leaf");
        CompositeConfiguration? composite = null;
        composite = new CompositeConfiguration("composite", leaf, scope =>
        {
            if (share)
                scope.Share(composite!);
            else
                scope.AddAspect(Wrap(composite!, "late"));
        });

        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().Resolve(composite))
            .Message.ShouldContain(share ? "has already resolved it" : "already resolved without it");
    }

    // ---------- Explicit sharing ----------

    [Fact]
    public void ASharedConfiguration_IsBuiltOnceInAChildScopeOfItsOwn()
    {
        var leaf = new CountingConfiguration("leaf");
        var composite = new CompositeConfiguration("composite", leaf);
        var root = ResolutionScope.Create();
        root.AddAspect(Wrap(leaf, "root"));
        root.Share(composite);
        var rootLeaf = root.Resolve(leaf);

        var first = root.CreateChildScope().Resolve(composite);
        var second = root.CreateChildScope().Resolve(composite);

        second.State.ShouldBeSameAs(first.State);
        root.Resolve(composite).State.ShouldBeSameAs(first.State);
        composite.CreateCount.ShouldBe(1);
        // The sharing scope's advice reaches inside the shared executor, but its children are its own.
        first.Name.ShouldBe("composite[root(leaf)]");
        first.State.ShouldBeOfType<NamedExecutor>().State.ShouldNotBeSameAs(rootLeaf.State);
        leaf.CreateCount.ShouldBe(2);
    }

    // Like a budget, the composite adds advice for its child to the scope it is built in. Sharing keeps that advice
    // inside the shared executor, whichever use of the child was built first.
    [Fact]
    public void AdviceASharedConfigurationAddsWhileBuilding_StaysInsideIt()
    {
        var leaf = new CountingConfiguration("leaf");
        var budgeted = new CompositeConfiguration("budgeted", leaf, scope => scope.AddAspect(Wrap(leaf, "budget")));
        var root = ResolutionScope.Create();
        root.Share(budgeted);
        var earlierTurn = root.CreateChildScope().Resolve(leaf);

        var shared = root.CreateChildScope().Resolve(budgeted);
        var laterTurn = root.CreateChildScope().Resolve(leaf);

        earlierTurn.Name.ShouldBe("leaf");
        shared.Name.ShouldBe("budgeted[budget(leaf)]");
        laterTurn.Name.ShouldBe("leaf");
        root.Resolve(leaf).Name.ShouldBe("leaf");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedConfigurations_ShareAChildOnlyWhenItIsSharedToo(bool shareChild)
    {
        var leaf = new CountingConfiguration("leaf");
        var first = new CompositeConfiguration("first", leaf);
        var second = new CompositeConfiguration("second", leaf);
        var root = ResolutionScope.Create();
        IConfigurationNode[] shared = shareChild ? [first, second, leaf] : [first, second];
        root.Share(shared);
        var turn = root.CreateChildScope();

        var firstLeaf = turn.Resolve(first).State;
        var secondLeaf = turn.Resolve(second).State;

        ReferenceEquals(firstLeaf, secondLeaf).ShouldBe(shareChild);
        leaf.CreateCount.ShouldBe(shareChild ? 1 : 2);
    }

    [Fact]
    public void AFailureWhileBuildingASharedConfiguration_EndsTheScopeTree()
    {
        var fault = new InvalidOperationException("shared failure");
        var leaf = new CountingConfiguration("leaf");
        var failing = new CompositeConfiguration("failing", leaf, _ => throw fault);
        var root = ResolutionScope.Create();
        root.Share(failing);
        var turn = root.CreateChildScope();

        Should.Throw<InvalidOperationException>(() => turn.Resolve(failing)).ShouldBeSameAs(fault);
        Should.Throw<InvalidOperationException>(() => root.Resolve(leaf)).Message.ShouldContain("Resolve through a new scope");
        Should.Throw<InvalidOperationException>(() => root.CreateChildScope().Resolve(leaf)).Message.ShouldContain("Resolve through a new scope");
    }

    // The parent's construction catches its child's failure, but the tree has ended, so the parent is not returned either.
    [Fact]
    public void ACaughtNestedConstructionFailure_StillFailsTheEnclosingResolution()
    {
        var fault = new InvalidOperationException("child failure");
        var failing = new CompositeConfiguration("failing", new CountingConfiguration("leaf"), _ => throw fault);
        var fallback = new FallbackConfiguration(failing);
        var scope = ResolutionScope.Create();

        Should.Throw<InvalidOperationException>(() => scope.TryResolve(fallback, out _, out _)).Message.ShouldContain("Resolve through a new scope");
        fallback.Caught.ShouldBeSameAs(fault);
    }

    // ---------- Building the chain ----------

    [Fact]
    public void BuildingAChain_CreatesTheUnwrappedExecutionExactlyOnce()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create();
        scope.AddAspect(Wrap(original, "outer"));
        scope.AddAspect(Wrap(original, "middle"));
        scope.AddAspect(Wrap(original, "inner"));

        scope.Resolve(original).Name.ShouldBe("outer(middle(inner(original)))");
        original.CreateCount.ShouldBe(1);
    }

    [Fact]
    public void AfterAChainIsBuilt_TheUnwrappedConfigurationStillResolvesThroughItsWrappers()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create();
        scope.AddAspect(Wrap(original, "wrapper"));

        scope.Resolve(original).Name.ShouldBe("wrapper(original)");
        scope.Resolve(original).Name.ShouldBe("wrapper(original)");
        original.CreateCount.ShouldBe(1);
    }

    // ---------- Own roles ----------

    [Fact]
    public void RoleAdvice_ReadsTheSelectedConfigurationThroughItsRole()
    {
        ILabeledRole configuration = new LabeledConfiguration("role");
        var scope = ResolutionScope.Create();
        scope.AddAspect(Aspect.Create(Pointcut.Reference(configuration), new RoleLabelAdvice()));

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
        var scope = ResolutionScope.Create();
        scope.AddAspect(new PairAspect(Pointcut.Reference(other), new RoleLabelAdvice()));

        Should.Throw<InvalidOperationException>(() => scope.Resolve(other)).Message.ShouldBe(
            $"{nameof(RoleLabelAdvice)} cannot wrap {nameof(INamedExecutor)} of {nameof(CountingConfiguration)}. " +
            $"Advice for this role derives from Advice<TConfiguration, {nameof(INamedExecutor)}> for a TConfiguration the configuration implements.");
    }

    [Fact]
    public void RoleAdvice_WrapsAResolutionThroughAWiderExecutorType()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create();
        scope.AddAspect(Wrap(original, "named"));

        scope.Resolve<IExecutor>(original).ShouldBeOfType<NamedExecutor>().Name.ShouldBe("named(original)");
    }

    [Fact]
    public void AdviceForAnotherExecutorTypeOfTheConfiguration_IsReportedRatherThanCast()
    {
        var configuration = new TwoRoleConfiguration();
        var aspect = Aspect.Create(Pointcut.Reference(configuration), new OtherRoleAdvice());

        var namedScope = ResolutionScope.Create();
        namedScope.AddAspect(aspect);
        var exception = Should.Throw<InvalidOperationException>(() => namedScope.Resolve<INamedExecutor>(configuration));

        exception.Message.ShouldBe(
            $"{nameof(OtherRoleAdvice)} cannot wrap {nameof(INamedExecutor)} of {nameof(TwoRoleConfiguration)}. " +
            $"Advice for this role derives from Advice<TConfiguration, {nameof(INamedExecutor)}> for a TConfiguration the configuration implements.");
        var otherScope = ResolutionScope.Create();
        otherScope.AddAspect(aspect);
        otherScope.Resolve<IOtherExecutor>(configuration).ShouldBeOfType<OtherExecutor>().Wrapped.ShouldBeTrue();
    }

    [Fact]
    public void AdviceListedByAspectsOfOneScope_AppliesOnceAtItsClosestOccurrence()
    {
        var original = new CountingConfiguration("original");
        var installed = Wrap(original, "installed");
        var first = new NamedAspect(Wrap(original, "first"), installed);
        var second = new NamedAspect(Wrap(original, "second"), installed);
        var scope = ResolutionScope.Create();
        scope.AddAspect(first);
        scope.AddAspect(second);
        scope.AddAspect(installed);
        scope.AddAspect(first);

        scope.Resolve(original).Name.ShouldBe("first(second(installed(original)))");
    }

    [Fact]
    public void AspectsOfSeveralScopesFollowOneApplicationSequence()
    {
        var original = new CountingConfiguration("original");
        IConfigurationNode<INamedExecutor> selected = original;
        var installed = Wrap(selected, "installed");
        var first = new NamedAspect(Wrap(original, "first"), installed);
        var second = Wrap(selected, "second");
        var root = ResolutionScope.Create();
        root.AddAspect(first);
        root.AddAspect(second);
        root.Resolve(original);
        var third = Wrap(selected, "third");
        var fourth = Wrap(selected, "fourth");
        var scope = root.CreateChildScope();
        scope.AddAspect(third);
        scope.AddAspect(fourth);

        scope.Resolve(original)
            .Name.ShouldBe("installed(first(second(third(fourth(original)))))");
        original.CreateCount.ShouldBe(2);
    }

    [Fact]
    public void PairsWithinOneAspect_ApplyInListedOrder()
    {
        var original = new CountingConfiguration("original");
        var existing = Wrap(original, "existing");
        var nested = Wrap(original, "nested");
        var combined = new NamedAspect(existing, Wrap(original, "inline"), nested, Wrap(original, "last"));
        var scope = ResolutionScope.Create();
        scope.AddAspect(combined);
        scope.Resolve(original)
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

    // Counts private data construction for each executor.
    private sealed class CountingConfiguration(string name) : IConfigurationNode<INamedExecutor>
    {
        public int CreateCount { get; private set; }

        public INamedExecutor CreateExecutor(ResolutionScope scope)
        {
            CreateCount++;
            return new NamedExecutor(name, new object());
        }

    }

    // Resolves its child in the scope it is built in, after an optional action on that scope. Its state is the child's
    // executor.
    private sealed class CompositeConfiguration(string name, IConfigurationNode<INamedExecutor> child, Action<ResolutionScope>? building = null)
        : IConfigurationNode<INamedExecutor>
    {
        public int CreateCount { get; private set; }

        public INamedExecutor CreateExecutor(ResolutionScope scope)
        {
            CreateCount++;
            building?.Invoke(scope);
            var executor = scope.Resolve(child);
            return new NamedExecutor($"{name}[{executor.Name}]", executor);
        }
    }

    // Falls back to an executor of its own when its child cannot be built.
    private sealed class FallbackConfiguration(IConfigurationNode<INamedExecutor> child) : IConfigurationNode<INamedExecutor>
    {
        public Exception? Caught { get; private set; }

        public INamedExecutor CreateExecutor(ResolutionScope scope)
        {
            try
            {
                return scope.Resolve(child);
            }
            catch (InvalidOperationException exception)
            {
                Caught = exception;
                return new NamedExecutor("fallback", null);
            }
        }
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
