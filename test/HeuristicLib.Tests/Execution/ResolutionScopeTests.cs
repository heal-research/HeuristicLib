namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

/// <summary>
/// Covers executor private data, explicit sharing and construction failures across scopes. A scope builds one executor
/// per configuration, and child scopes build their own unless an ancestor shares the configuration. The cell names
/// refer to the combinations of whether a wrapper is resolved and where resolves happen.
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
    public void SiblingContexts_ConstructIndependentData(bool wrapped)
    {
        var configuration = new CountingConfiguration("instance");
        var resolved = wrapped ? Wrap(configuration, "parent") : configuration;
        var parent = ResolutionScope.Create();

        var first = parent.CreateChildScope().Resolve(resolved);
        var second = parent.CreateChildScope().Resolve(resolved);

        second.State.ShouldNotBeSameAs(first.State);
        configuration.CreateCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SiblingContexts_StayIndependentAfterRootResolution(bool wrapped)
    {
        var configuration = new CountingConfiguration("instance");
        var resolved = wrapped ? Wrap(configuration, "ancestor") : configuration;
        var ancestor = ResolutionScope.Create();
        var ancestorExecutor = ancestor.Resolve(resolved);
        var parent = ancestor.CreateChildScope();

        var first = parent.CreateChildScope().Resolve(resolved);
        var second = parent.CreateChildScope().Resolve(resolved);

        first.Name.ShouldBe(ancestorExecutor.Name);
        second.Name.ShouldBe(ancestorExecutor.Name);
        first.State.ShouldNotBeSameAs(ancestorExecutor.State);
        second.State.ShouldNotBeSameAs(ancestorExecutor.State);
        configuration.CreateCount.ShouldBe(3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AParent_NeverSeesStateItsChildCreated(bool wrapped)
    {
        var original = new CountingConfiguration("original");
        var resolved = wrapped ? Wrap(original, "parent") : original;
        var parent = ResolutionScope.Create();
        var child = parent.CreateChildScope();
        var childExecutor = child.Resolve(resolved);

        var parentExecutor = parent.Resolve(resolved);

        parentExecutor.State.ShouldNotBeSameAs(childExecutor.State);
        child.Resolve(resolved).State.ShouldBeSameAs(childExecutor.State);
        parent.Resolve(resolved).State.ShouldBeSameAs(parentExecutor.State);
        original.CreateCount.ShouldBe(2);
    }

    // ---------- Sharing with wrappers ----------

    [Fact]
    public void ChildResolvingTheParentsWrapper_ReconstructsTheWrappedExecution()
    {
        var original = new CountingConfiguration("original");
        var wrapped = Wrap(original, "parent");
        var parent = ResolutionScope.Create();
        var parentExecutor = parent.Resolve(wrapped);

        var childExecutor = parent.CreateChildScope().Resolve(wrapped);

        childExecutor.ShouldNotBeSameAs(parentExecutor);
        childExecutor.Name.ShouldBe("parent(original)");
        original.CreateCount.ShouldBe(2);
    }

    // ---------- Explicit sharing ----------

    [Fact]
    public void SharingOnceTheScopeHasChildScopes_WorksUntilAScopeBelowResolvedTheConfiguration()
    {
        var late = new CountingConfiguration("late");
        var resolvedBelow = new CountingConfiguration("resolved below");
        var scope = ResolutionScope.Create();
        var child = scope.CreateChildScope();
        var grandchild = child.CreateChildScope();
        grandchild.Resolve(resolvedBelow);

        scope.Share(late);

        child.Resolve(late).ShouldBeSameAs(scope.Resolve(late));
        late.CreateCount.ShouldBe(1);
        Should.Throw<InvalidOperationException>(() => scope.Share(resolvedBelow)).Message.ShouldContain("a scope below it");
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
    // cannot share it.
    [Fact]
    public void AConfigurationSharingItselfWhileBuilding_IsAnError()
    {
        var leaf = new CountingConfiguration("leaf");
        CompositeConfiguration? composite = null;
        composite = new CompositeConfiguration("composite", leaf, scope => scope.Share(composite!));

        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().Resolve(composite))
            .Message.ShouldContain("has already resolved it");
    }

    [Fact]
    public void ASharedConfiguration_IsBuiltOnceInAChildScopeOfItsOwn()
    {
        var leaf = new CountingConfiguration("leaf");
        var composite = new CompositeConfiguration("composite", leaf);
        var root = ResolutionScope.Create();
        root.Share(composite);
        var rootLeaf = root.Resolve(leaf);

        var first = root.CreateChildScope().Resolve(composite);
        var second = root.CreateChildScope().Resolve(composite);

        second.State.ShouldBeSameAs(first.State);
        root.Resolve(composite).State.ShouldBeSameAs(first.State);
        composite.CreateCount.ShouldBe(1);
        // The shared executor's children are its own.
        first.Name.ShouldBe("composite[leaf]");
        first.State.ShouldBeOfType<NamedExecutor>().State.ShouldNotBeSameAs(rootLeaf.State);
        leaf.CreateCount.ShouldBe(2);
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

    // ---------- Test doubles ----------

    private static IConfigurationNode<INamedExecutor> Wrap(IConfigurationNode<INamedExecutor> child, string label) =>
        new LabelWrapper(child, label);

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

    // Labels the executor of its child and keeps that executor's state.
    private sealed record LabelWrapper(IConfigurationNode<INamedExecutor> Child, string Label) : IConfigurationNode<INamedExecutor>
    {
        public INamedExecutor CreateExecutor(ResolutionScope scope)
        {
            var executor = scope.Resolve(Child);
            return new NamedExecutor($"{Label}({executor.Name})", executor.State);
        }
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
}
