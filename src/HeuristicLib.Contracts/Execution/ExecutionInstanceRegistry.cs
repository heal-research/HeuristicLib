using System.Diagnostics.CodeAnalysis;

namespace HEAL.HeuristicLib.Execution;

public class ExecutionInstanceRegistry
{
    private readonly ExecutionInstanceRegistry? parentRegistry;

    private readonly Dictionary<IExecutionInstanceResolvable, IExecutionInstance> registry = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<IExecutionInstanceResolvable, IExecutionInstanceResolvable> replacementResolvables = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<IExecutionInstanceResolvable> resolvablesBeingCreated = new(ReferenceEqualityComparer.Instance);

    public ExecutionInstanceRegistry()
    {
    }

    private ExecutionInstanceRegistry(ExecutionInstanceRegistry parentRegistry)
    {
        this.parentRegistry = parentRegistry;
    }

    public ExecutionInstanceRegistry CreateChildRegistry()
    {
        return new ExecutionInstanceRegistry(this);
    }

    private bool TryResolve(IExecutionInstanceResolvable resolvable, [MaybeNullWhen(false)] out IExecutionInstance instance)
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

    private bool TryGetReplacementResolvable(IExecutionInstanceResolvable resolvable, [MaybeNullWhen(false)] out IExecutionInstanceResolvable replacementResolvable)
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

    /// <summary>
    /// Resolves a configuration whose creation call this registry cannot write itself, applying the same policy as the
    /// other overload — cached instance, registered replacement, parent registry, otherwise create — and calling
    /// <paramref name="create"/> for the creation step.
    /// </summary>
    /// <remarks>
    /// <paramref name="create"/> receives the resolvable to create from: the one passed in, or the registered
    /// replacement when there is one, which is how observation keeps working. Pass a <see langword="static"/> lambda,
    /// so the compiler caches the delegate instead of allocating one per resolution.
    /// </remarks>
    public TExecutionInstance Resolve<TResolvable, TExecutionInstance>(TResolvable resolvable, Func<TResolvable, ExecutionInstanceRegistry, TExecutionInstance> create)
        where TResolvable : class, IExecutionInstanceResolvable
        where TExecutionInstance : class, IExecutionInstance
    {
        if (registry.TryGetValue(resolvable, out var localInstance))
        {
            return RequireInstanceOf<TExecutionInstance>(resolvable, localInstance);
        }

        if (TryGetReplacementResolvable(resolvable, out var replacementResolvable))
        {
            if (!resolvablesBeingCreated.Add(resolvable))
            {
                return create(resolvable, this);
            }

            try
            {
                if (replacementResolvable is not TResolvable typedReplacement)
                {
                    throw new InvalidOperationException(
                        $"{replacementResolvable.GetType().Name} was registered to replace {resolvable.GetType().Name}, but it is not a {typeof(TResolvable).Name} and cannot stand in for it.");
                }

                var createdInstance = create(typedReplacement, this);
                StoreInstance(resolvable, createdInstance);
                return createdInstance;
            }
            finally
            {
                resolvablesBeingCreated.Remove(resolvable);
            }
        }

        if (parentRegistry is not null && parentRegistry.TryResolve(resolvable, out var parentInstance))
        {
            return RequireInstanceOf<TExecutionInstance>(resolvable, parentInstance);
        }

        var instance = create(resolvable, this);
        StoreInstance(resolvable, instance);
        return instance;
    }

    /// <summary>
    /// Registers a ready-made instance for a resolvable, so resolution returns it instead of creating one.
    /// </summary>
    public void RegisterInstance(IExecutionInstanceResolvable resolvable, IExecutionInstance instance)
    {
        StoreInstance(resolvable, instance);
    }

    /// <summary>
    /// Returns an instance already held here as the type the caller asked for, or explains why it is not that type.
    /// </summary>
    /// <remarks>A registry serves one run, so finding an instance of another type here means it was built for a
    /// different search space or problem.</remarks>
    private static TExecutionInstance RequireInstanceOf<TExecutionInstance>(IExecutionInstanceResolvable resolvable, IExecutionInstance instance)
        where TExecutionInstance : class, IExecutionInstance
    {
        if (instance is not TExecutionInstance cached)
        {
            throw new InvalidOperationException(
                $"This registry already holds a {instance.GetType().Name} for {resolvable.GetType().Name}, which is not a {typeof(TExecutionInstance).Name}. " +
                "A registry serves one run, so resolve over a second search space or problem in its own registry.");
        }

        return cached;
    }

    private void StoreInstance(IExecutionInstanceResolvable resolvable, IExecutionInstance instance)
    {
        if (!registry.TryAdd(resolvable, instance))
        {
            throw new InvalidOperationException("Execution instance has already been registered for this resolvable.");
        }
    }

    /// <summary>
    /// Registers <paramref name="replacementResolvable"/> to be created in place of <paramref name="resolvable"/>,
    /// which is how an observer wraps an operator already referenced by a resolvable.
    /// </summary>
    /// <remarks>Keyed by reference identity, like every other lookup here.</remarks>
    public void RegisterReplacement(IExecutionInstanceResolvable resolvable, IExecutionInstanceResolvable replacementResolvable)
    {
        if (!replacementResolvables.TryAdd(resolvable, replacementResolvable))
        {
            throw new InvalidOperationException("Replacement has already been registered for this resolvable.");
        }
    }
}
