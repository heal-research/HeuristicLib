using System.Runtime.CompilerServices;

namespace HEAL.HeuristicLib.Execution;

/// <summary>Identifies one resource within a run by reference identity.</summary>
/// <remarks>
/// A handle holds immutable creation settings, never its live resource. Copies of a configuration retain its handles.
/// Separate runs create independent resources unless the caller explicitly supplies the same instance to them.
/// </remarks>
public abstract class ResourceHandle<TResource> where TResource : class
{
    /// <summary>Creates managed data needing no deterministic cleanup when no resource was supplied.</summary>
    /// <remarks>
    /// Override using only immutable creation settings. Do not retain executors, configurations, scopes or live resources.
    /// Resources needing cleanup must be supplied and remain caller owned, including while a run is paused.
    /// </remarks>
    protected internal abstract TResource CreateResource();

    public sealed override bool Equals(object? obj) => ReferenceEquals(this, obj);

    public sealed override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}
