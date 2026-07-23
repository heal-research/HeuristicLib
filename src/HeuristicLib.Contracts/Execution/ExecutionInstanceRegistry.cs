using System.Diagnostics.CodeAnalysis;

namespace HEAL.HeuristicLib.Execution;

public class ExecutionInstanceRegistry
{
    private readonly ExecutionInstanceRegistry? parentRegistry;

    private readonly Dictionary<IExecutionInstanceResolvable<IExecutionInstance>, IExecutionInstance> registry = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<IExecutionInstanceResolvable<IExecutionInstance>, IExecutionInstanceResolvable<IExecutionInstance>> replacementResolvables = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<IExecutionInstanceResolvable<IExecutionInstance>> resolvablesBeingCreated = new(ReferenceEqualityComparer.Instance);

    public ExecutionInstanceRegistry()
    {
    }

    private ExecutionInstanceRegistry(ExecutionInstanceRegistry parentRegistry) => this.parentRegistry = parentRegistry;

    public ExecutionInstanceRegistry CreateChildRegistry()
    {
        return new ExecutionInstanceRegistry(this);
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
        if (registry.TryGetValue(resolvable, out var localInstance))
        {
            return (TExecutionInstance)localInstance;
        }

        if (TryGetReplacementResolvable(resolvable, out var replacementResolvable))
        {
            if (!resolvablesBeingCreated.Add(resolvable))
            {
                return resolvable.CreateExecutionInstance(this);
            }

            try
            {
                var createdInstance = replacementResolvable.CreateExecutionInstance(this);
                StoreInstance(resolvable, createdInstance);
                return (TExecutionInstance)createdInstance;
            }
            finally
            {
                resolvablesBeingCreated.Remove(resolvable);
            }
        }

        if (parentRegistry is not null && parentRegistry.TryResolve(resolvable, out var parentInstance))
        {
            return (TExecutionInstance)parentInstance;
        }

        var instance = resolvable.CreateExecutionInstance(this);
        StoreInstance(resolvable, instance);
        return instance;
    }

    public void RegisterInstance<TExecutionInstance>(IExecutionInstanceResolvable<TExecutionInstance> resolvable, TExecutionInstance instance)
        where TExecutionInstance : class, IExecutionInstance
    {
        StoreInstance(resolvable, instance);
    }

    private void StoreInstance(IExecutionInstanceResolvable<IExecutionInstance> resolvable, IExecutionInstance instance)
    {
        if (!registry.TryAdd(resolvable, instance))
        {
            throw new InvalidOperationException("Execution instance has already been registered for this resolvable.");
        }
    }

    public void RegisterReplacement<TExecutionInstance>(IExecutionInstanceResolvable<TExecutionInstance> resolvable, IExecutionInstanceResolvable<TExecutionInstance> replacementResolvable)
        where TExecutionInstance : class, IExecutionInstance
    {
        if (!replacementResolvables.TryAdd(resolvable, replacementResolvable))
        {
            throw new InvalidOperationException("Replacement has already been registered for this resolvable.");
        }
    }
}
