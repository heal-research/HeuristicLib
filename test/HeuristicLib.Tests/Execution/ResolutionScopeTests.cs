namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

/// <summary>
/// Covers the resolution rules: which decorations apply, in which order they wrap, and when two resolves share one
/// instance. The cell names refer to the combinations of where decorations are declared and where resolves happen.
/// </summary>
public class ResolutionScopeTests
{
    // ---------- Sharing across scopes ----------

    /// <summary>Cell A3. Nothing is decorated, so the child reuses what the parent already built.</summary>
    [Fact]
    public void UndecoratedChild_ReusesTheExecutionItsParentAlreadyBuilt()
    {
        var parent = ResolutionScope.Create();
        var configuration = new CountingConfiguration("instance");
        var parentExecution = parent.Resolve(configuration);

        var childExecution = parent.CreateChildScope().Resolve(configuration);

        childExecution.ShouldBeSameAs(parentExecution);
        configuration.CreateCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SiblingScopes_BuildSeparateExecutionsWhenNoAncestorHasResolvedTheConfiguration(bool decorateInParent)
    {
        var configuration = new CountingConfiguration("instance");
        var parent = decorateInParent
            ? ResolutionScope.Create(builder => Wrap(builder, configuration, "parent"))
            : ResolutionScope.Create();

        var first = parent.CreateChildScope().Resolve(configuration);
        var second = parent.CreateChildScope().Resolve(configuration);

        second.ShouldNotBeSameAs(first);
        configuration.CreateCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SiblingScopes_ReuseAnExecutionAlreadyBuiltInAnAncestor(bool decorateInAncestor)
    {
        var configuration = new CountingConfiguration("instance");
        var ancestor = decorateInAncestor
            ? ResolutionScope.Create(builder => Wrap(builder, configuration, "ancestor"))
            : ResolutionScope.Create();
        var ancestorExecution = ancestor.Resolve(configuration);
        var parent = ancestor.CreateChildScope();

        var first = parent.CreateChildScope().Resolve(configuration);
        var second = parent.CreateChildScope().Resolve(configuration);

        first.ShouldBeSameAs(ancestorExecution);
        second.ShouldBeSameAs(ancestorExecution);
        configuration.CreateCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AParent_NeverSeesAnExecutionItsChildBuilt(bool decorateInParent)
    {
        var original = new CountingConfiguration("original");
        var parent = decorateInParent
            ? ResolutionScope.Create(builder => Wrap(builder, original, "parent"))
            : ResolutionScope.Create();
        var child = parent.CreateChildScope();
        var childExecution = child.Resolve(original);

        var parentExecution = parent.Resolve(original);

        parentExecution.ShouldNotBeSameAs(childExecution);
        child.Resolve(original).ShouldBeSameAs(childExecution);
        parent.Resolve(original).ShouldBeSameAs(parentExecution);
        original.CreateCount.ShouldBe(2);
    }

    // ---------- Sharing with decorations ----------

    /// <summary>
    /// Cell B2. The parent's decoration applies to the child unchanged, so the decorated instance the parent already
    /// built is exactly what the child would build and is reused rather than duplicated.
    /// </summary>
    [Fact]
    public void ChildAddingNoDecoration_ReusesTheParentsDecoratedExecution()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create(builder => Wrap(builder, original, "parent"));
        var parentExecution = parent.Resolve(original);

        var childExecution = parent.CreateChildScope().Resolve(original);

        childExecution.ShouldBeSameAs(parentExecution);
        childExecution.Name.ShouldBe("parent(original)");
        original.CreateCount.ShouldBe(1);
    }

    [Fact]
    public void ChildDecoratingAnotherConfiguration_ReusesTheParentsExecutionForTheOriginal()
    {
        var original = new CountingConfiguration("original");
        var other = new CountingConfiguration("other");
        var parent = ResolutionScope.Create(builder => Wrap(builder, original, "parent"));
        var parentExecution = parent.Resolve(original);
        var child = parent.CreateChildScope(builder => Wrap(builder, other, "child"));

        child.Resolve(original).ShouldBeSameAs(parentExecution);
        child.Resolve(other).Name.ShouldBe("child(other)");
        original.CreateCount.ShouldBe(1);
        other.CreateCount.ShouldBe(1);
    }

    /// <summary>
    /// Cell D1. The child contributes a decoration the parent does not have, so it must not reuse the parent's
    /// instance — that one is missing a wrapper the child requires.
    /// </summary>
    [Fact]
    public void ChildAddingItsOwnDecoration_BuildsItsOwnExecution()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create(builder => Wrap(builder, original, "parent"));
        var parentExecution = parent.Resolve(original);

        var child = parent.CreateChildScope(builder => Wrap(builder, original, "child"));

        var childExecution = child.Resolve(original);
        childExecution.ShouldNotBeSameAs(parentExecution);
        childExecution.Name.ShouldBe("parent(child(original))");
    }

    /// <summary>Resolving twice in one scope returns the same instance rather than rebuilding the chain.</summary>
    [Fact]
    public void ResolvingTwiceInOneScope_ReturnsTheSameDecoratedExecution()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(builder => Wrap(builder, original, "only"));

        scope.Resolve(original).ShouldBeSameAs(scope.Resolve(original));
        original.CreateCount.ShouldBe(1);
    }

    // ---------- Composition across scopes ----------

    /// <summary>
    /// Cell D1. Decorations compose across scopes rather than the nearer one replacing the farther. Both are
    /// present; which one ends up innermost is the depth rule, covered separately.
    /// </summary>
    [Fact]
    public void ChildDecoration_ComposesWithTheParentsRatherThanReplacingIt()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create(builder => Wrap(builder, original, "parent"));

        var child = parent.CreateChildScope(builder => Wrap(builder, original, "child"));

        child.Resolve(original).Name.ShouldBe("parent(child(original))");
    }

    /// <summary>Cells C1 and C2. A decoration declared in a child never reaches the parent's own resolution.</summary>
    [Fact]
    public void ChildDecoration_DoesNotAffectTheParentsResolution()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create(builder => Wrap(builder, original, "parent"));

        parent.CreateChildScope(builder => Wrap(builder, original, "child")).Resolve(original);

        parent.Resolve(original).Name.ShouldBe("parent(original)");
    }

    /// <summary>An undecorated configuration is untouched no matter what else the scope decorates.</summary>
    [Fact]
    public void UndecoratedConfiguration_IsBuiltPlain()
    {
        var decorated = new CountingConfiguration("decorated");
        var untouched = new CountingConfiguration("untouched");
        var scope = ResolutionScope.Create(builder => Wrap(builder, decorated, "wrapper"));

        scope.Resolve(untouched).Name.ShouldBe("untouched");
    }

    // ---------- Ordering ----------

    /// <summary>
    /// Within one scope the first decoration declared binds tightest, which is what lets a trace install the clocks
    /// it reads before installing itself.
    /// </summary>
    [Fact]
    public void WithinOneScope_TheFirstDecorationDeclaredIsInnermost()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(builder =>
        {
            Wrap(builder, original, "first");
            Wrap(builder, original, "second");
        });

        scope.Resolve(original).Name.ShouldBe("second(first(original))");
    }

    /// <summary>The same decoration declared twice stacks twice rather than being treated as one.</summary>
    [Fact]
    public void TheSameDecorationDeclaredTwice_StacksTwice()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(builder =>
        {
            Wrap(builder, original, "counted");
            Wrap(builder, original, "counted");
        });

        scope.Resolve(original).Name.ShouldBe("counted(counted(original))");
    }

    /// <summary>
    /// A deeper scope binds more tightly, so the most local budget wraps the operator closest and is the least
    /// disturbed by the ones around it.
    /// </summary>
    [Fact]
    public void ADeeperScopesDecoration_IsInnerThanAShallowerOne()
    {
        var original = new CountingConfiguration("original");
        var root = ResolutionScope.Create(builder => Wrap(builder, original, "shallow"));

        var deep = root.CreateChildScope(builder => Wrap(builder, original, "deep"));

        deep.Resolve(original).Name.ShouldBe("shallow(deep(original))");
    }

    /// <summary>
    /// A module's decoration always sits outside the configuration's, so a wrapper that measures an operator never
    /// measures the module observing it. Install order does not override this.
    /// </summary>
    [Fact]
    public void AModulesDecoration_IsOuterThanConfigurationEvenWhenInstalledFirst()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(builder =>
        {
            builder.Install(new DecoratingModule<INamedExecution>(original, current => new LabelledConfiguration("module", current)));
            Wrap(builder, original, "configuration");
        });

        scope.Resolve(original).Name.ShouldBe("module(configuration(original))");
    }

    /// <summary>Origin outranks depth, so configuration in an ancestor still binds tighter than a module below it.</summary>
    [Fact]
    public void ConfigurationInAnAncestor_IsStillInnerThanAModuleInADescendant()
    {
        var original = new CountingConfiguration("original");
        var root = ResolutionScope.Create(builder => Wrap(builder, original, "configuration"));

        var child = root.CreateChildScope(builder =>
            builder.Install(new DecoratingModule<INamedExecution>(original, current => new LabelledConfiguration("module", current))));

        child.Resolve(original).Name.ShouldBe("module(configuration(original))");
    }

    /// <summary>Among modules the ordinary install order applies, so a module installed first observes first.</summary>
    [Fact]
    public void AmongModules_TheFirstInstalledIsInnermost()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(builder => builder
            .Install(new DecoratingModule<INamedExecution>(original, current => new LabelledConfiguration("first", current)))
            .Install(new DecoratingModule<INamedExecution>(original, current => new LabelledConfiguration("second", current))));

        scope.Resolve(original).Name.ShouldBe("second(first(original))");
    }

    // ---------- Building the chain ----------

    /// <summary>
    /// A decoration resolves what it wraps through the scope. That must hand back the instance already built for
    /// the link below it rather than starting the chain again, so the raw configuration is created exactly once.
    /// </summary>
    [Fact]
    public void BuildingAChain_CreatesTheUndecoratedExecutionExactlyOnce()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(builder =>
        {
            Wrap(builder, original, "inner");
            Wrap(builder, original, "middle");
            Wrap(builder, original, "outer");
        });

        scope.Resolve(original).Name.ShouldBe("outer(middle(inner(original)))");
        original.CreateCount.ShouldBe(1);
    }

    /// <summary>Once a chain is built, nothing it pinned while building stays behind to answer later resolves.</summary>
    [Fact]
    public void AfterAChainIsBuilt_TheUndecoratedConfigurationStillResolvesThroughItsDecorations()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(builder => Wrap(builder, original, "wrapper"));

        scope.Resolve(original).Name.ShouldBe("wrapper(original)");
        scope.Resolve(original).Name.ShouldBe("wrapper(original)");
        original.CreateCount.ShouldBe(1);
    }

    // ---------- Phase separation ----------

    [Fact]
    public void ABuilderKeptPastItsDeclaration_CannotChangeTheScopeItProduced()
    {
        var original = new CountingConfiguration("original");
        ResolutionScopeBuilder? escaped = null;
        var scope = ResolutionScope.Create(builder =>
        {
            escaped = builder;
            Wrap(builder, original, "declared");
        });

        Wrap(escaped!, original, "late");

        scope.Resolve(original).Name.ShouldBe("declared(original)");
    }

    // ---------- Resolving through a create function ----------

    /// <summary>
    /// A configuration whose instance type depends on the run cannot create itself, so the caller passes the creation
    /// step. The decorations still apply, each link created through that step.
    /// </summary>
    [Fact]
    public void ResolvingThroughACreateFunction_AppliesTheDecorationsBeforeCreating()
    {
        var original = new RunTypedConfiguration("original");
        var scope = ResolutionScope.Create(builder =>
        {
            builder.Decorate<IRunTypedConfiguration>(original, current => new RunTypedWrapper("inner", current));
            builder.Decorate<IRunTypedConfiguration>(original, current => new RunTypedWrapper("outer", current));
        });

        var resolved = ResolveRunTyped(scope, original);

        resolved.Name.ShouldBe("outer(inner(original))");
        scope.CreateChildScope().Resolve(original, static (target, childScope) => target.Create(childScope)).ShouldBeSameAs(resolved);
        original.CreateCount.ShouldBe(1);
    }

    /// <summary>
    /// A scope serves one run, so asking for a held instance as another type means the caller resolves over a
    /// different search space or problem. That is reported rather than cast.
    /// </summary>
    [Fact]
    public void AnExecutionHeldAsAnotherType_IsReportedRatherThanCast()
    {
        var configuration = new RunTypedConfiguration("instance");
        var scope = ResolutionScope.Create();
        ResolveRunTyped(scope, configuration);

        var exception = Should.Throw<InvalidOperationException>(() =>
            scope.CreateChildScope().Resolve(configuration, static (_, _) => new OtherExecution()));

        exception.Message.ShouldBe(
            "This scope already holds a NamedExecution for RunTypedConfiguration, which is not a OtherExecution. " +
            "A scope serves one run, so resolve over a second search space or problem in its own scope.");
    }

    /// <summary>
    /// Every link of a chain is created through the caller's creation step, so a decoration must produce what that
    /// step accepts.
    /// </summary>
    [Fact]
    public void ADecorationTheCreateFunctionCannotAccept_IsReported()
    {
        var original = new RunTypedConfiguration("original");
        var scope = ResolutionScope.Create(builder =>
            builder.Decorate<IConfigurationNode>(original, _ => new CountingConfiguration("foreign")));

        var exception = Should.Throw<InvalidOperationException>(() => ResolveRunTyped(scope, original));

        exception.Message.ShouldBe(
            "CountingConfiguration was declared to decorate RunTypedConfiguration, but it is not a IRunTypedConfiguration and cannot stand in for it.");
    }

    // ---------- Test doubles ----------

    private static void Wrap(ResolutionScopeBuilder builder, IConfigurationNode<INamedExecution> anchor, string label) =>
        builder.Decorate(anchor, current => new LabelledConfiguration(label, current));

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

        public INamedExecution CreateExecutionInstance(ResolutionScope scope)
        {
            CreateCount++;
            return new NamedExecution(name);
        }
    }

    private sealed class LabelledConfiguration(string label, IConfigurationNode<INamedExecution> inner)
        : IConfigurationNode<INamedExecution>
    {
        public INamedExecution CreateExecutionInstance(ResolutionScope scope) =>
            new NamedExecution($"{label}({scope.Resolve(inner).Name})");
    }

    private sealed class OtherExecution : IExecutionNode;

    /// <summary>Stands in for a role configuration: configuration, but not able to create itself without its caller.</summary>
    private interface IRunTypedConfiguration : IConfigurationNode
    {
        INamedExecution Create(ResolutionScope scope);
    }

    private static INamedExecution ResolveRunTyped(ResolutionScope scope, IRunTypedConfiguration configuration) =>
        scope.Resolve(configuration, static (target, targetScope) => target.Create(targetScope));

    private sealed class RunTypedConfiguration(string name) : IRunTypedConfiguration
    {
        public int CreateCount { get; private set; }

        public INamedExecution Create(ResolutionScope scope)
        {
            CreateCount++;
            return new NamedExecution(name);
        }
    }

    private sealed class RunTypedWrapper(string label, IRunTypedConfiguration inner) : IRunTypedConfiguration
    {
        public INamedExecution Create(ResolutionScope scope) =>
            new NamedExecution($"{label}({ResolveRunTyped(scope, inner).Name})");
    }

    private sealed class DecoratingModule<TExecution>(
        IConfigurationNode<TExecution> anchor,
        Func<IConfigurationNode<TExecution>, IConfigurationNode<TExecution>> decorate)
        : IExecutionModule
        where TExecution : class, IExecutionNode
    {
        public void Install(ResolutionScopeBuilder builder) => builder.Decorate(anchor, decorate);
    }
}
