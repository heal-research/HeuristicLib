namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

/// <summary>
/// Covers the resolution rules: which decorations apply, in which order they wrap, and when two resolves share one
/// instance. The cell names refer to the combinations of where decorations are declared and where resolves happen.
/// </summary>
public class ExecutionInstanceResolverTests
{
    // ---------- Sharing without decorations ----------

    /// <summary>Cell A3. Nothing is decorated, so the child reuses what the parent already built.</summary>
    [Fact]
    public void UndecoratedChild_ReusesTheInstanceItsParentAlreadyBuilt()
    {
        var parent = ExecutionInstanceResolver.Create();
        var resolvable = new CountingResolvable("instance");
        var parentInstance = parent.Resolve(resolvable);

        var childInstance = parent.CreateChildResolver().Resolve(resolvable);

        childInstance.ShouldBeSameAs(parentInstance);
        resolvable.CreateCount.ShouldBe(1);
    }

    /// <summary>
    /// Cell A4. Siblings never share, which is how a meta-algorithm that recreates its execution instances gets fresh
    /// ones per cycle.
    /// </summary>
    [Fact]
    public void SiblingResolvers_NeverShareAnInstance()
    {
        var parent = ExecutionInstanceResolver.Create();
        var resolvable = new CountingResolvable("instance");

        var first = parent.CreateChildResolver().Resolve(resolvable);
        var second = parent.CreateChildResolver().Resolve(resolvable);

        second.ShouldNotBeSameAs(first);
        resolvable.CreateCount.ShouldBe(2);
    }

    // ---------- Sharing with decorations ----------

    /// <summary>
    /// Cell B2. The parent's decoration applies to the child unchanged, so the decorated instance the parent already
    /// built is exactly what the child would build and is reused rather than duplicated.
    /// </summary>
    [Fact]
    public void ChildAddingNoDecoration_ReusesTheParentsDecoratedInstance()
    {
        var original = new CountingResolvable("original");
        var parent = ExecutionInstanceResolver.Create(builder => Wrap(builder, original, "parent"));
        var parentInstance = parent.Resolve(original);

        var childInstance = parent.CreateChildResolver().Resolve(original);

        childInstance.ShouldBeSameAs(parentInstance);
        childInstance.Name.ShouldBe("parent(original)");
        original.CreateCount.ShouldBe(1);
    }

    /// <summary>
    /// Cell D1. The child contributes a decoration the parent does not have, so it must not reuse the parent's
    /// instance — that one is missing a wrapper the child requires.
    /// </summary>
    [Fact]
    public void ChildAddingItsOwnDecoration_BuildsItsOwnInstance()
    {
        var original = new CountingResolvable("original");
        var parent = ExecutionInstanceResolver.Create(builder => Wrap(builder, original, "parent"));
        var parentInstance = parent.Resolve(original);

        var child = parent.CreateChildResolver(builder => Wrap(builder, original, "child"));

        var childInstance = child.Resolve(original);
        childInstance.ShouldNotBeSameAs(parentInstance);
        childInstance.Name.ShouldBe("parent(child(original))");
    }

    /// <summary>Resolving twice in one resolver returns the same instance rather than rebuilding the chain.</summary>
    [Fact]
    public void ResolvingTwiceInOneResolver_ReturnsTheSameDecoratedInstance()
    {
        var original = new CountingResolvable("original");
        var resolver = ExecutionInstanceResolver.Create(builder => Wrap(builder, original, "only"));

        resolver.Resolve(original).ShouldBeSameAs(resolver.Resolve(original));
        original.CreateCount.ShouldBe(1);
    }

    // ---------- Composition across resolvers ----------

    /// <summary>
    /// Cell D1. Decorations compose across resolvers rather than the nearer one replacing the farther. Both are
    /// present; which one ends up innermost is the depth rule, covered separately.
    /// </summary>
    [Fact]
    public void ChildDecoration_ComposesWithTheParentsRatherThanReplacingIt()
    {
        var original = new CountingResolvable("original");
        var parent = ExecutionInstanceResolver.Create(builder => Wrap(builder, original, "parent"));

        var child = parent.CreateChildResolver(builder => Wrap(builder, original, "child"));

        child.Resolve(original).Name.ShouldBe("parent(child(original))");
    }

    /// <summary>Cells C1 and C2. A decoration declared in a child never reaches the parent's own resolution.</summary>
    [Fact]
    public void ChildDecoration_DoesNotAffectTheParentsResolution()
    {
        var original = new CountingResolvable("original");
        var parent = ExecutionInstanceResolver.Create(builder => Wrap(builder, original, "parent"));

        parent.CreateChildResolver(builder => Wrap(builder, original, "child")).Resolve(original);

        parent.Resolve(original).Name.ShouldBe("parent(original)");
    }

    /// <summary>An undecorated resolvable is untouched no matter what else the resolver decorates.</summary>
    [Fact]
    public void UndecoratedResolvable_IsBuiltPlain()
    {
        var decorated = new CountingResolvable("decorated");
        var untouched = new CountingResolvable("untouched");
        var resolver = ExecutionInstanceResolver.Create(builder => Wrap(builder, decorated, "wrapper"));

        resolver.Resolve(untouched).Name.ShouldBe("untouched");
    }

    // ---------- Ordering ----------

    /// <summary>
    /// Within one resolver the first decoration declared binds tightest, which is what lets a trace install the clocks
    /// it reads before installing itself.
    /// </summary>
    [Fact]
    public void WithinOneResolver_TheFirstDecorationDeclaredIsInnermost()
    {
        var original = new CountingResolvable("original");
        var resolver = ExecutionInstanceResolver.Create(builder =>
        {
            Wrap(builder, original, "first");
            Wrap(builder, original, "second");
        });

        resolver.Resolve(original).Name.ShouldBe("second(first(original))");
    }

    /// <summary>The same decoration declared twice stacks twice rather than being treated as one.</summary>
    [Fact]
    public void TheSameDecorationDeclaredTwice_StacksTwice()
    {
        var original = new CountingResolvable("original");
        var resolver = ExecutionInstanceResolver.Create(builder =>
        {
            Wrap(builder, original, "counted");
            Wrap(builder, original, "counted");
        });

        resolver.Resolve(original).Name.ShouldBe("counted(counted(original))");
    }

    /// <summary>
    /// A deeper resolver binds more tightly, so the most local budget wraps the operator closest and is the least
    /// disturbed by the ones around it.
    /// </summary>
    [Fact]
    public void ADeeperResolversDecoration_IsInnerThanAShallowerOne()
    {
        var original = new CountingResolvable("original");
        var root = ExecutionInstanceResolver.Create(builder => Wrap(builder, original, "shallow"));

        var deep = root.CreateChildResolver(builder => Wrap(builder, original, "deep"));

        deep.Resolve(original).Name.ShouldBe("shallow(deep(original))");
    }

    /// <summary>
    /// A hook's decoration always sits outside the configuration's, so a wrapper that measures an operator never
    /// measures the hook observing it. Install order does not override this.
    /// </summary>
    [Fact]
    public void AHooksDecoration_IsOuterThanConfigurationEvenWhenInstalledFirst()
    {
        var original = new CountingResolvable("original");
        var resolver = ExecutionInstanceResolver.Create(builder =>
        {
            builder.Install(new DecoratingHook<INamedInstance>(original, current => new LabelledResolvable("hook", current)));
            Wrap(builder, original, "configuration");
        });

        resolver.Resolve(original).Name.ShouldBe("hook(configuration(original))");
    }

    /// <summary>Origin outranks depth, so configuration in an ancestor still binds tighter than a hook below it.</summary>
    [Fact]
    public void ConfigurationInAnAncestor_IsStillInnerThanAHookInADescendant()
    {
        var original = new CountingResolvable("original");
        var root = ExecutionInstanceResolver.Create(builder => Wrap(builder, original, "configuration"));

        var child = root.CreateChildResolver(builder =>
            builder.Install(new DecoratingHook<INamedInstance>(original, current => new LabelledResolvable("hook", current))));

        child.Resolve(original).Name.ShouldBe("hook(configuration(original))");
    }

    /// <summary>Among hooks the ordinary install order applies, so a hook installed first observes first.</summary>
    [Fact]
    public void AmongHooks_TheFirstInstalledIsInnermost()
    {
        var original = new CountingResolvable("original");
        var resolver = ExecutionInstanceResolver.Create(builder => builder
            .Install(new DecoratingHook<INamedInstance>(original, current => new LabelledResolvable("first", current)))
            .Install(new DecoratingHook<INamedInstance>(original, current => new LabelledResolvable("second", current))));

        resolver.Resolve(original).Name.ShouldBe("second(first(original))");
    }

    // ---------- Building the chain ----------

    /// <summary>
    /// A decoration resolves what it wraps through the resolver. That must hand back the instance already built for
    /// the link below it rather than starting the chain again, so the raw resolvable is created exactly once.
    /// </summary>
    [Fact]
    public void BuildingAChain_CreatesTheUndecoratedInstanceExactlyOnce()
    {
        var original = new CountingResolvable("original");
        var resolver = ExecutionInstanceResolver.Create(builder =>
        {
            Wrap(builder, original, "inner");
            Wrap(builder, original, "middle");
            Wrap(builder, original, "outer");
        });

        resolver.Resolve(original).Name.ShouldBe("outer(middle(inner(original)))");
        original.CreateCount.ShouldBe(1);
    }

    /// <summary>Once a chain is built, nothing it pinned while building stays behind to answer later resolves.</summary>
    [Fact]
    public void AfterAChainIsBuilt_TheUndecoratedResolvableStillResolvesThroughItsDecorations()
    {
        var original = new CountingResolvable("original");
        var resolver = ExecutionInstanceResolver.Create(builder => Wrap(builder, original, "wrapper"));

        resolver.Resolve(original).Name.ShouldBe("wrapper(original)");
        resolver.Resolve(original).Name.ShouldBe("wrapper(original)");
        original.CreateCount.ShouldBe(1);
    }

    // ---------- Phase separation ----------

    /// <summary>
    /// The builder belongs to its declaration callback. A caller that keeps it cannot reach the resolver that callback
    /// produced, so declaring after resolving has begun is not a mistake the resolver has to defend against.
    /// </summary>
    [Fact]
    public void ABuilderKeptPastItsDeclaration_CannotChangeTheResolverItProduced()
    {
        var original = new CountingResolvable("original");
        ExecutionInstanceResolverBuilder? escaped = null;
        var resolver = ExecutionInstanceResolver.Create(builder =>
        {
            escaped = builder;
            Wrap(builder, original, "declared");
        });

        Wrap(escaped!, original, "late");

        resolver.Resolve(original).Name.ShouldBe("declared(original)");
    }

    // ---------- Test doubles ----------

    private static void Wrap(ExecutionInstanceResolverBuilder builder, IExecutionInstanceResolvable<INamedInstance> anchor, string label) =>
        builder.Decorate(anchor, current => new LabelledResolvable(label, current));

    private interface INamedInstance : IExecutionInstance
    {
        string Name { get; }
    }

    private sealed class NamedInstance(string name) : INamedInstance
    {
        public string Name { get; } = name;
    }

    private sealed class CountingResolvable(string name) : IExecutionInstanceResolvable<INamedInstance>
    {
        public int CreateCount { get; private set; }

        public INamedInstance CreateExecutionInstance(ExecutionInstanceResolver resolver)
        {
            CreateCount++;
            return new NamedInstance(name);
        }
    }

    private sealed class LabelledResolvable(string label, IExecutionInstanceResolvable<INamedInstance> inner)
        : IExecutionInstanceResolvable<INamedInstance>
    {
        public INamedInstance CreateExecutionInstance(ExecutionInstanceResolver resolver) =>
            new NamedInstance($"{label}({resolver.Resolve(inner).Name})");
    }

    private sealed class DecoratingHook<TInstance>(
        IExecutionInstanceResolvable<TInstance> anchor,
        Func<IExecutionInstanceResolvable<TInstance>, IExecutionInstanceResolvable<TInstance>> decorate)
        : IExecutionHook
        where TInstance : class, IExecutionInstance
    {
        public void Install(ExecutionInstanceResolverBuilder builder) => builder.Decorate(anchor, decorate);
    }
}
