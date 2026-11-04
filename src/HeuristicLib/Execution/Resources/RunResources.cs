namespace HEAL.HeuristicLib.Execution;

/// <summary>Looks up the resources shared within one run without exposing graph resolution or resource supply.</summary>
/// <remarks>Lookup is synchronized. Each returned resource defines its own concurrency and ownership contract.</remarks>
public sealed class RunResources
{
    private readonly RunResults table;

    internal RunResources(RunResults table)
    {
        this.table = table;
    }

    /// <summary>Returns the resource for this handle, creating its default instance lazily when needed.</summary>
    public TResource Resource<TResource>(ResourceHandle<TResource> handle) where TResource : class =>
        table.Resource(handle);
}
