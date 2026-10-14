namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

/// <summary>
/// Covers the resolution rules: which wrappers apply, in which order they wrap, and when two resolves share one
/// instance. The cell names refer to the combinations of where wrappers are declared and where resolves happen.
/// </summary>
public class ResolutionScopeTests
{
    // ---------- Sharing across scopes ----------

    /// <summary>Cell A3. Nothing is wrapped, so the child reuses what the parent already built.</summary>
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
            ? ResolutionScope.Create(builder => Wrap(builder, configuration, "parent"))
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
            ? ResolutionScope.Create(builder => Wrap(builder, configuration, "ancestor"))
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
            ? ResolutionScope.Create(builder => Wrap(builder, original, "parent"))
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

    /// <summary>
    /// Cell B2. The parent's wrapper applies to the child unchanged, so the wrapped instance the parent already
    /// built is exactly what the child would build and is reused rather than duplicated.
    /// </summary>
    [Fact]
    public void ChildAddingNoAdvice_ReconstructsTheParentsAdvisedExecution()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create(builder => Wrap(builder, original, "parent"));
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
        var parent = ResolutionScope.Create(builder => Wrap(builder, original, "parent"));
        var parentExecution = parent.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);
        var child = parent.CreateChildScope(builder => Wrap(builder, other, "child"));

        child.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).ShouldNotBeSameAs(parentExecution);
        child.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("parent(original)");
        child.Resolve(other, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("child(other)");
        original.CreateCount.ShouldBe(1);
        other.CreateCount.ShouldBe(1);
    }

    /// <summary>
    /// Cell D1. The child contributes a wrapper the parent does not have, so it must not reuse the parent's
    /// instance — that one is missing a wrapper the child requires.
    /// </summary>
    [Fact]
    public void ChildAddingItsOwnWrapper_BuildsItsOwnExecution()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create(builder => Wrap(builder, original, "parent"));
        var parentExecution = parent.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);

        var child = parent.CreateChildScope(builder => Wrap(builder, original, "child"));

        var childExecution = child.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);
        childExecution.ShouldNotBeSameAs(parentExecution);
        childExecution.Name.ShouldBe("parent(child(original))");
    }

    /// <summary>Resolving twice in one scope returns the same instance rather than rebuilding the chain.</summary>
    [Fact]
    public void ResolvingTwiceInOneScope_CreatesFreshAdviceOverSharedState()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(builder => Wrap(builder, original, "only"));

        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).ShouldNotBeSameAs(scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice));
        original.CreateCount.ShouldBe(1);
    }

    // ---------- Composition across scopes ----------

    /// <summary>
    /// Cell D1. Wrappers compose across scopes rather than the nearer one replacing the farther. Both are
    /// present; which one ends up innermost is the depth rule, covered separately.
    /// </summary>
    [Fact]
    public void ChildWrapper_ComposesWithTheParentsRatherThanReplacingIt()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create(builder => Wrap(builder, original, "parent"));

        var child = parent.CreateChildScope(builder => Wrap(builder, original, "child"));

        child.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("parent(child(original))");
    }

    /// <summary>Cells C1 and C2. A wrapper declared in a child never reaches the parent's own resolution.</summary>
    [Fact]
    public void ChildWrapper_DoesNotAffectTheParentsResolution()
    {
        var original = new CountingConfiguration("original");
        var parent = ResolutionScope.Create(builder => Wrap(builder, original, "parent"));

        parent.CreateChildScope(builder => Wrap(builder, original, "child")).Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);

        parent.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("parent(original)");
    }

    /// <summary>An unwrapped configuration is untouched no matter what else the scope wraps.</summary>
    [Fact]
    public void UnwrappedConfiguration_IsBuiltPlain()
    {
        var wrapped = new CountingConfiguration("wrapped");
        var untouched = new CountingConfiguration("untouched");
        var scope = ResolutionScope.Create(builder => Wrap(builder, wrapped, "wrapper"));

        scope.Resolve(untouched, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("untouched");
    }

    // ---------- Ordering ----------

    /// <summary>
    /// Within one scope the first wrapper declared binds tightest, which is what lets a trace install the clocks
    /// it reads before installing itself.
    /// </summary>
    [Fact]
    public void WithinOneScope_TheFirstWrapperDeclaredIsInnermost()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(builder =>
        {
            Wrap(builder, original, "first");
            Wrap(builder, original, "second");
        });

        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("second(first(original))");
    }

    /// <summary>The same wrapper declared twice stacks twice rather than being treated as one.</summary>
    [Fact]
    public void TheSameWrapperDeclaredTwice_StacksTwice()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(builder =>
        {
            Wrap(builder, original, "counted");
            Wrap(builder, original, "counted");
        });

        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("counted(counted(original))");
    }

    /// <summary>
    /// A deeper scope binds more tightly, so the most local budget wraps the operator closest and is the least
    /// disturbed by the ones around it.
    /// </summary>
    [Fact]
    public void ADeeperScopesWrapper_IsInnerThanAShallowerOne()
    {
        var original = new CountingConfiguration("original");
        var root = ResolutionScope.Create(builder => Wrap(builder, original, "shallow"));

        var deep = root.CreateChildScope(builder => Wrap(builder, original, "deep"));

        deep.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("shallow(deep(original))");
    }

    /// <summary>
    /// Existing aspects and inline recipes use the same sequence, so the first installation binds closest.
    /// </summary>
    [Fact]
    public void AnExistingAspect_IsInnerThanARecipeInstalledAfterIt()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(builder =>
        {
            builder.Install(new WrappingModule<INamedExecution>(original, current => new LabelledConfiguration("module", current)));
            Wrap(builder, original, "configuration");
        });

        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("configuration(module(original))");
    }

    /// <summary>Child-scope aspects precede ancestor aspects, regardless of how each contribution was installed.</summary>
    [Fact]
    public void AnExistingAspectInADescendant_IsInnerThanAnAncestorRecipe()
    {
        var original = new CountingConfiguration("original");
        var root = ResolutionScope.Create(builder => Wrap(builder, original, "configuration"));

        var child = root.CreateChildScope(builder =>
            builder.Install(new WrappingModule<INamedExecution>(original, current => new LabelledConfiguration("module", current))));

        child.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("configuration(module(original))");
    }

    /// <summary>Among modules the ordinary install order applies, so a module installed first observes first.</summary>
    [Fact]
    public void AmongModules_TheFirstInstalledIsInnermost()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(builder => builder
            .Install(new WrappingModule<INamedExecution>(original, current => new LabelledConfiguration("first", current)))
            .Install(new WrappingModule<INamedExecution>(original, current => new LabelledConfiguration("second", current))));

        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("second(first(original))");
    }

    // ---------- Building the chain ----------

    /// <summary>
    /// A wrapper resolves what it wraps through the scope. That must hand back the instance already built for
    /// the link below it rather than starting the chain again, so the raw configuration is created exactly once.
    /// </summary>
    [Fact]
    public void BuildingAChain_CreatesTheUnwrappedExecutionExactlyOnce()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(builder =>
        {
            Wrap(builder, original, "inner");
            Wrap(builder, original, "middle");
            Wrap(builder, original, "outer");
        });

        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("outer(middle(inner(original)))");
        original.CreateCount.ShouldBe(1);
    }

    /// <summary>The predecessor override belongs to its wrapper frame; ordinary resolves see the completed chain.</summary>
    [Fact]
    public void AfterAChainIsBuilt_TheUnwrappedConfigurationStillResolvesThroughItsWrappers()
    {
        var original = new CountingConfiguration("original");
        var scope = ResolutionScope.Create(builder => Wrap(builder, original, "wrapper"));

        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("wrapper(original)");
        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("wrapper(original)");
        original.CreateCount.ShouldBe(1);
    }

    // ---------- Phase separation ----------

    [Fact]
    public void ABuilderKeptPastItsDeclaration_CannotChangeTheScopeItProduced()
    {
        var original = new CountingConfiguration("original");
        AspectBuilder? escaped = null;
        var scope = ResolutionScope.Create(builder =>
        {
            escaped = builder;
            Wrap(builder, original, "declared");
        });

        Wrap(escaped!, original, "late");

        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice).Name.ShouldBe("declared(original)");
    }

    // ---------- Resolving through a preparation adapter ----------

    /// <summary>
    /// A configuration whose execution type depends on the run needs a typed preparation adapter. Each source and
    /// generated wrapper prepares once through that adapter, then binds for the requesting scope.
    /// </summary>
    [Fact]
    public void ResolvingThroughAPreparationAdapter_AppliesTheWrappers()
    {
        var original = new RunTypedConfiguration("original");
        var scope = ResolutionScope.Create(builder =>
        {
            builder.Install<IRunTypedConfiguration>(original, current => new RunTypedWrapper("inner", current));
            builder.Install<IRunTypedConfiguration>(original, current => new RunTypedWrapper("outer", current));
        });

        var resolved = ResolveRunTyped(scope, original);

        resolved.Name.ShouldBe("outer(inner(original))");
        scope.CreateChildScope().Resolve(original, static source => source.CreateExecutionFactory(), PrepareRunAdvice).ShouldNotBeSameAs(resolved);
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
            scope.CreateChildScope().Resolve<IRunTypedConfiguration, OtherExecution>(configuration, static _ => _ => new OtherExecution(), static _ => throw new InvalidOperationException("No compatible wrapper.")));

        exception.Message.ShouldBe(
            "RunTypedConfiguration was prepared for INamedExecution, not OtherExecution. " +
            "Resolve over a second search space or problem in its own scope.");
    }

    /// <summary>
    /// Every link of a chain is prepared through the caller's adapter, so a wrapper must produce a configuration
    /// that adapter accepts.
    /// </summary>
    [Fact]
    public void AWrapperThePreparationAdapterCannotAccept_IsReported()
    {
        var original = new RunTypedConfiguration("original");
        var scope = ResolutionScope.Create(builder =>
            builder.Install<IConfigurationNode>(original, _ => new CountingConfiguration("foreign")));

        var exception = Should.Throw<InvalidOperationException>(() => ResolveRunTyped(scope, original));

        exception.Message.ShouldContain("single-child wrapper");
    }

    [Fact]
    public void MultipleAspectsSnapshotInputsAndDeduplicateTheirSharedInstallation()
    {
        var original = new CountingConfiguration("original");
        var declarations = new List<string>();
        var installed = Aspect.Create(builder =>
        {
            declarations.Add("installed");
            Wrap(builder, original, "installed");
        });
        var first = Aspect.Create(builder =>
        {
            declarations.Add("first");
            Wrap(builder, original, "first");
            builder.Install(installed);
        });
        var second = Aspect.Create(builder =>
        {
            declarations.Add("second");
            Wrap(builder, original, "second");
            builder.Install(installed);
        });
        Aspect[] inputs = [first, second, installed, first];
        var scope = ResolutionScope.Create(inputs);
        inputs[0] = Aspect.Create(builder => Wrap(builder, original, "replacement"));

        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice)
            .Name.ShouldBe("second(installed(first(original)))");
        declarations.ShouldBe(["first", "installed", "second"]);
    }

    [Fact]
    public void CyclicAspectInstallationsApplyEachAspectOnceInInstallationOrder()
    {
        var original = new CountingConfiguration("original");
        Aspect first = null!;
        var third = Aspect.Create(builder => Wrap(builder, original, "third"));
        var fourth = Aspect.Create(builder => Wrap(builder, original, "fourth"));
        var second = Aspect.Create(builder =>
        {
            Wrap(builder, original, "second");
            builder.Install(first);
            builder.Install(fourth);
        });
        first = Aspect.Create(builder =>
        {
            Wrap(builder, original, "first");
            builder.Install(second);
            builder.Install(third);
        });

        var scope = ResolutionScope.Create(first);
        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice)
            .Name.ShouldBe("third(fourth(second(first(original))))");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultipleAspectLayersPreserveInstallationOrdering(bool childState)
    {
        var original = new CountingConfiguration("original");
        IConfigurationNode<INamedExecution> selected = original;
        var installed = Aspect.Create(selected, current => new LabelledConfiguration("installed", current));
        var first = Aspect.Create(builder =>
        {
            Wrap(builder, original, "first");
            builder.Install(installed);
        });
        var second = Aspect.Create(selected, current => new LabelledConfiguration("second", current));
        var root = ResolutionScope.Create(first, second);
        root.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice);
        var third = Aspect.Create(selected, current => new LabelledConfiguration("third", current));
        var fourth = Aspect.Create(selected, current => new LabelledConfiguration("fourth", current));
        var scope = childState ? root.CreateChildScope(third, fourth) : root.With(third, fourth);

        scope.Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice)
            .Name.ShouldBe("second(installed(first(fourth(third(original)))))");
        original.CreateCount.ShouldBe(1);
    }

    [Fact]
    public void ExistingAspectsAndRecipesFollowOneInstallationSequence()
    {
        var original = new CountingConfiguration("original");
        IConfigurationNode<INamedExecution> selected = original;
        var existing = Aspect.Create(selected, current => new LabelledConfiguration("existing", current));
        var nested = Aspect.Create(builder => builder.Install(selected, current => new LabelledConfiguration("nested", current)));
        var composition = Aspect.Create(builder => builder
            .Install(existing)
            .Install(selected, current => new LabelledConfiguration("inline", current))
            .Install(nested)
            .Install(Pointcut.Reference(selected), current => new LabelledConfiguration("last", current)));

        ResolutionScope.Create(composition).Resolve(original, static source => source.CreateExecutionFactory(), PrepareNamedAdvice)
            .Name.ShouldBe("last(nested(inline(existing(original))))");
    }

    // ---------- Test doubles ----------

    private static void Wrap(AspectBuilder builder, IConfigurationNode<INamedExecution> anchor, string label) =>
        builder.Install(anchor, current => new LabelledConfiguration(label, current));

    private static Advice<INamedExecution> PrepareNamedAdvice(IConfigurationNode wrapper) =>
        wrapper is LabelledConfiguration typed ? typed.CreateAdvice()
            : throw new InvalidOperationException("Not a single-child wrapper for INamedExecution.");

    private static Advice<INamedExecution> PrepareRunAdvice(IConfigurationNode wrapper) =>
        wrapper is RunTypedWrapper typed ? typed.CreateAdvice()
            : throw new InvalidOperationException("Not a single-child wrapper for INamedExecution.");


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

    private sealed class LabelledConfiguration(string label, IConfigurationNode<INamedExecution> inner)
        : IConfigurationNode<INamedExecution>
    {
        public Advice<INamedExecution> CreateAdvice() => innerExecution => new NamedExecution($"{label}({innerExecution.Name})");
        public ExecutionFactory<INamedExecution> CreateExecutionFactory()
        {
            var advice = CreateAdvice();
            return scope => advice(scope.Resolve(inner, static source => source.CreateExecutionFactory(), PrepareNamedAdvice));
        }
    }

    private sealed class OtherExecution : IExecutionNode;

    /// <summary>Stands in for a role configuration: configuration, but not able to create itself without its caller.</summary>
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

    private sealed class RunTypedWrapper(string label, IRunTypedConfiguration inner) : IRunTypedConfiguration
    {
        public Advice<INamedExecution> CreateAdvice() => innerExecution => new NamedExecution($"{label}({innerExecution.Name})");
        public ExecutionFactory<INamedExecution> CreateExecutionFactory()
        {
            var advice = CreateAdvice();
            return scope => advice(ResolveRunTyped(scope, inner));
        }
    }

    private sealed class WrappingModule<TExecution>(
        IConfigurationNode<TExecution> anchor,
        Func<IConfigurationNode<TExecution>, IConfigurationNode<TExecution>> wrap)
        : Aspect
        where TExecution : class, IExecutionNode
    {
        protected override void Declare(AspectBuilder builder) => builder.Install(anchor, wrap);
    }
}
