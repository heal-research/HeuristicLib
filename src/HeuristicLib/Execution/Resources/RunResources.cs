namespace HEAL.HeuristicLib.Execution;

/// <summary>Looks up the resources shared within one run without exposing graph resolution or resource supply.</summary>
/// <remarks>Lookup is synchronized. Each returned resource defines its own concurrency and ownership contract.</remarks>
public sealed class RunResources
{
    private readonly Lock sync = new();
    private readonly Dictionary<object, object> resources;

    internal RunResources(IEnumerable<KeyValuePair<object, object>> supplied)
    {
        resources = new(supplied, ReferenceEqualityComparer.Instance);
    }

    /// <summary>Returns the resource for this handle, creating its default instance lazily when needed.</summary>
    public TResource Resource<TResource>(ResourceHandle<TResource> handle) where TResource : class
    {
        lock (sync)
        {
            if (resources.TryGetValue(handle, out var resource))
                return (TResource)resource;
            var created = handle.CreateResource();
            resources.Add(handle, created);
            return created;
        }
    }
}
