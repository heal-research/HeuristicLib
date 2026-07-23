using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Tests.TestSupport.Execution;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public class ExecutionInstanceRegistryTests
{
    [Fact]
    public void RegisterInstance_ReturnsRegisteredInstance()
    {
        var registry = new ExecutionInstanceRegistry();
        var resolvable = new CountingResolvable("created");
        var registeredInstance = new NamedInstance("registered");

        registry.RegisterInstance(resolvable, registeredInstance);

        registry.Resolve(resolvable).ShouldBeSameAs(registeredInstance);
        resolvable.CreateCount.ShouldBe(0);
    }

    [Fact]
    public void RegisterInstance_ThrowsWhenInstanceWasAlreadyRegistered()
    {
        var registry = new ExecutionInstanceRegistry();
        var resolvable = new CountingResolvable("created");

        registry.RegisterInstance(resolvable, new NamedInstance("first"));

        var exception = Should.Throw<InvalidOperationException>(() =>
            registry.RegisterInstance(resolvable, new NamedInstance("second")));

        exception.Message.ShouldBe("Execution instance has already been registered for this resolvable.");
    }

    [Fact]
    public void RegisterReplacement_StoresReplacementUnderOriginalIdentity()
    {
        var registry = new ExecutionInstanceRegistry();
        var original = new CountingResolvable("original");
        var replacement = new CountingResolvable("replacement");

        registry.RegisterReplacement(original, replacement);

        var resolved = registry.Resolve(original);

        resolved.Name.ShouldBe("replacement");
        original.CreateCount.ShouldBe(0);
        replacement.CreateCount.ShouldBe(1);
        registry.Resolve(original).ShouldBeSameAs(resolved);
    }

    [Fact]
    public void RegisterReplacement_ThrowsWhenReplacementWasAlreadyRegistered()
    {
        var registry = new ExecutionInstanceRegistry();
        var original = new CountingResolvable("original");

        registry.RegisterReplacement(original, new CountingResolvable("first"));

        var exception = Should.Throw<InvalidOperationException>(() =>
            registry.RegisterReplacement(original, new CountingResolvable("second")));

        exception.Message.ShouldBe("Replacement has already been registered for this resolvable.");
    }

    [Fact]
    public void RegisterReplacement_AllowsReplacementToResolveOriginal()
    {
        var registry = new ExecutionInstanceRegistry();
        var original = new CountingResolvable("original");
        var replacement = new WrappingResolvable(original);

        registry.RegisterReplacement(original, replacement);

        var resolved = registry.Resolve(original);

        var wrapped = resolved.ShouldBeOfType<WrappedInstance>();
        wrapped.Inner.Name.ShouldBe("original");
        original.CreateCount.ShouldBe(1);
        replacement.CreateCount.ShouldBe(1);
        registry.Resolve(original).ShouldBeSameAs(resolved);
    }

    [Fact]
    public void RegisterReplacement_IsInheritedByChildRegistry()
    {
        var parentRegistry = new ExecutionInstanceRegistry();
        var childRegistry = parentRegistry.CreateChildRegistry();
        var original = new CountingResolvable("original");
        var replacement = new CountingResolvable("replacement");

        parentRegistry.RegisterReplacement(original, replacement);

        var resolved = childRegistry.Resolve(original);

        resolved.Name.ShouldBe("replacement");
        original.CreateCount.ShouldBe(0);
        replacement.CreateCount.ShouldBe(1);
        parentRegistry.Resolve(original).ShouldNotBeSameAs(resolved);
        childRegistry.Resolve(original).ShouldBeSameAs(resolved);
    }

    [Fact]
    public void RegisterReplacement_InChildRegistryOverridesParentReplacement()
    {
        var parentRegistry = new ExecutionInstanceRegistry();
        var childRegistry = parentRegistry.CreateChildRegistry();
        var original = new CountingResolvable("original");
        var parentReplacement = new CountingResolvable("parent replacement");
        var childReplacement = new CountingResolvable("child replacement");

        parentRegistry.RegisterReplacement(original, parentReplacement);
        childRegistry.RegisterReplacement(original, childReplacement);

        var childResolved = childRegistry.Resolve(original);
        var parentResolved = parentRegistry.Resolve(original);

        childResolved.Name.ShouldBe("child replacement");
        parentResolved.Name.ShouldBe("parent replacement");
        original.CreateCount.ShouldBe(0);
        parentReplacement.CreateCount.ShouldBe(1);
        childReplacement.CreateCount.ShouldBe(1);
    }

    [Fact]
    public void CreateChildRegistry_ReusesResolvedParentInstance()
    {
        var parentRegistry = new ExecutionInstanceRegistry();
        var resolvable = new CountingResolvable("instance");
        var parentInstance = parentRegistry.Resolve(resolvable);

        var childInstance = parentRegistry.CreateChildRegistry().Resolve(resolvable);

        childInstance.ShouldBeSameAs(parentInstance);
        resolvable.CreateCount.ShouldBe(1);
    }

    private interface INamedInstance : IExecutionInstance
    {
        string Name { get; }
    }

    private sealed class NamedInstance(string name) : INamedInstance
    {
        public string Name { get; } = name;
    }

    private sealed class WrappedInstance(INamedInstance inner) : INamedInstance
    {
        public string Name => $"wrapped {Inner.Name}";

        public INamedInstance Inner { get; } = inner;
    }

    private sealed class CountingResolvable(string name) : IExecutionInstanceResolvable<INamedInstance>
    {
        public int CreateCount { get; private set; }

        public INamedInstance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        {
            CreateCount++;
            return new NamedInstance(name);
        }
    }

    private sealed class WrappingResolvable(IExecutionInstanceResolvable<INamedInstance> inner)
        : IExecutionInstanceResolvable<INamedInstance>
    {
        public int CreateCount { get; private set; }

        public INamedInstance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        {
            CreateCount++;
            return new WrappedInstance(instanceRegistry.Resolve(inner));
        }
    }
}
