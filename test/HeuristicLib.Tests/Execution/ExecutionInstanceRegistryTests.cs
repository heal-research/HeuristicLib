namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public class ExecutionInstanceRegistryTests
{
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

    [Fact]
    public void Decorate_AppliesTheFirstRegisteredDecorationInnermost()
    {
        var registry = new ExecutionInstanceRegistry();
        IExecutionInstanceResolvable<INamedInstance> original = new CountingResolvable("original");

        registry.Decorate(original, current => new LabelledResolvable("first", current));
        registry.Decorate(original, current => new LabelledResolvable("second", current));

        registry.Resolve(original).Name.ShouldBe("second(first(original))");
    }

    [Fact]
    public void Decorate_InChildRegistryComposesWithParentDecoration()
    {
        var parentRegistry = new ExecutionInstanceRegistry();
        var childRegistry = parentRegistry.CreateChildRegistry();
        IExecutionInstanceResolvable<INamedInstance> original = new CountingResolvable("original");

        parentRegistry.Decorate(original, current => new LabelledResolvable("parent", current));
        childRegistry.Decorate(original, current => new LabelledResolvable("child", current));

        childRegistry.Resolve(original).Name.ShouldBe("child(parent(original))");
    }

    [Fact]
    public void Decorate_InChildRegistryLeavesParentResolutionUndecorated()
    {
        var parentRegistry = new ExecutionInstanceRegistry();
        var childRegistry = parentRegistry.CreateChildRegistry();
        IExecutionInstanceResolvable<INamedInstance> original = new CountingResolvable("original");

        parentRegistry.Decorate(original, current => new LabelledResolvable("parent", current));
        childRegistry.Decorate(original, current => new LabelledResolvable("child", current));

        parentRegistry.Resolve(original).Name.ShouldBe("parent(original)");
    }

    [Fact]
    public void Decorate_StacksTheSameDecorationWhenItIsRegisteredTwice()
    {
        var registry = new ExecutionInstanceRegistry();
        IExecutionInstanceResolvable<INamedInstance> original = new CountingResolvable("original");

        registry.Decorate(original, current => new LabelledResolvable("counted", current));
        registry.Decorate(original, current => new LabelledResolvable("counted", current));

        registry.Resolve(original).Name.ShouldBe("counted(counted(original))");
    }

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

        public INamedInstance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        {
            CreateCount++;
            return new NamedInstance(name);
        }
    }

    private sealed class LabelledResolvable(string label, IExecutionInstanceResolvable<INamedInstance> inner)
        : IExecutionInstanceResolvable<INamedInstance>
    {
        public INamedInstance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
            new NamedInstance($"{label}({instanceRegistry.Resolve(inner).Name})");
    }
}
