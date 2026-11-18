namespace HEAL.HeuristicLib.Execution;

/// <summary>Substitutions that belong together, expanded depth-first in the order of their parts.</summary>
/// <remarks>
/// A later part sits closer to its target. List a shared part, such as a clock, after what reads it.
/// Expansion is cached; a group that contains itself is an error.
/// </remarks>
public abstract class SubstitutionGroup : ISubstitution
{
    private IReadOnlyList<Substitution>? substitutions;

    protected SubstitutionGroup() { }

    /// <summary>Lists the parts in application order; a part may itself be a group.</summary>
    /// <remarks>May be called concurrently during first expansion. Return immutable parts and keep no execution state here.</remarks>
    protected abstract IReadOnlyList<ISubstitution> CreateSubstitutions();

    /// <summary>Returns the cached depth-first expansion, preserving repeated single substitutions.</summary>
    /// <remarks>
    /// Threads expanding a group concurrently may each build its parts, but every call returns the one list published
    /// first, so the same instances are returned on every call.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Expansion reaches this group again through its parts.</exception>
    public IReadOnlyList<Substitution> Expand() => Expand(new HashSet<SubstitutionGroup>(ReferenceEqualityComparer.Instance));

    // The path holds the groups being expanded above this one, so a group that contains itself fails instead of recursing.
    // A cycle that runs through a part that is not a group is not seen, because such a part starts a path of its own.
    private IReadOnlyList<Substitution> Expand(HashSet<SubstitutionGroup> path)
    {
        if (Volatile.Read(ref substitutions) is { } expanded)
            return expanded;
        if (!path.Add(this))
            throw new InvalidOperationException($"The substitution group {ExecutionSignature.Name(GetType())} contains itself.");
        try
        {
            IReadOnlyList<Substitution> built =
                [.. CreateSubstitutions().SelectMany(part => part is SubstitutionGroup group ? group.Expand(path) : part.Expand())];
            return Interlocked.CompareExchange(ref substitutions, built, null) ?? built;
        }
        finally
        {
            path.Remove(this);
        }
    }
}
