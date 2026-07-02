using System.Diagnostics.CodeAnalysis;

namespace HEAL.HeuristicLib.Execution;

public class ExecutionInstanceRegistry : IExecutionInstanceResolver
{
    public Run Run { get; }

    private readonly ExecutionInstanceRegistry? parentRegistry;

    private readonly Dictionary<IExecutionInstanceResolvable<IExecutionInstance>, IExecutionInstance> registry = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<IExecutionInstanceResolvable<IExecutionInstance>, IExecutionInstanceResolvable<IExecutionInstance>> replacementResolvables = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<IExecutionInstanceResolvable<IExecutionInstance>> resolvablesBeingCreated = new(ReferenceEqualityComparer.Instance);

    public ExecutionInstanceRegistry(Run run, ExecutionInstanceRegistry? parentRegistry = null)
    {
        Run = run;
        this.parentRegistry = parentRegistry;
    }

    public ExecutionInstanceRegistry CreateChildRegistry()
    {
        return new ExecutionInstanceRegistry(Run, this);
    }

    private bool TryResolve(IExecutionInstanceResolvable<IExecutionInstance> resolvable, [MaybeNullWhen(false)] out IExecutionInstance instance)
    {
        if (registry.TryGetValue(resolvable, out instance))
        {
            return true;
        }

        if (parentRegistry is not null && parentRegistry.TryResolve(resolvable, out instance))
        {
            return true;
        }

        return false;
    }

    private bool TryGetReplacementResolvable(IExecutionInstanceResolvable<IExecutionInstance> resolvable, [MaybeNullWhen(false)] out IExecutionInstanceResolvable<IExecutionInstance> replacementResolvable)
    {
        if (replacementResolvables.TryGetValue(resolvable, out replacementResolvable))
        {
            return true;
        }

        if (parentRegistry is not null && parentRegistry.TryGetReplacementResolvable(resolvable, out replacementResolvable))
        {
            return true;
        }

        return false;
    }

    public TExecutionInstance Resolve<TExecutionInstance>(IExecutionInstanceResolvable<TExecutionInstance> resolvable)
      where TExecutionInstance : class, IExecutionInstance
    {
        IExecutionInstanceResolvable<IExecutionInstance> untypedResolvable = resolvable;

        if (registry.TryGetValue(untypedResolvable, out var localInstance))
        {
            return (TExecutionInstance)localInstance;
        }

        if (TryGetReplacementResolvable(untypedResolvable, out var replacementResolvable))
        {
            if (!resolvablesBeingCreated.Add(untypedResolvable))
            {
                return resolvable.CreateExecutionInstance(this);
            }

            try
            {
                var createdInstance = replacementResolvable.CreateExecutionInstance(this);
                registry.Add(untypedResolvable, createdInstance);
                return (TExecutionInstance)createdInstance;
            }
            finally
            {
                resolvablesBeingCreated.Remove(untypedResolvable);
            }
        }

        if (parentRegistry is not null && parentRegistry.TryResolve(untypedResolvable, out var parentInstance))
        {
            return (TExecutionInstance)parentInstance;
        }

        var instance = resolvable.CreateExecutionInstance(this);
        registry.Add(untypedResolvable, instance);
        return instance;
    }

    public void PreRegister<TExecutionInstance>(IExecutionInstanceResolvable<TExecutionInstance> resolvable, TExecutionInstance instance)
      where TExecutionInstance : class, IExecutionInstance
    {
        IExecutionInstanceResolvable<IExecutionInstance> untypedResolvable = resolvable;
        if (!registry.TryAdd(untypedResolvable, instance))
        {
            throw new InvalidOperationException("Object has already been registered");
        }
    }

    public void PreRegister<TExecutionInstance>(IExecutionInstanceResolvable<TExecutionInstance> resolvable, IExecutionInstanceResolvable<TExecutionInstance> replacementResolvable)
      where TExecutionInstance : class, IExecutionInstance
    {
        IExecutionInstanceResolvable<IExecutionInstance> untypedResolvable = resolvable;
        if (!replacementResolvables.TryAdd(untypedResolvable, replacementResolvable))
        {
            throw new InvalidOperationException("Replacement resolvable has already been registered");
        }
    }
}
