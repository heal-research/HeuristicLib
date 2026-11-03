using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Execution;

/// <summary>Collects the resources and aspects for one run before it is returned to the caller.</summary>
/// <remarks>Later changes to this setup cannot affect a run that has already been created.</remarks>
public sealed class AlgorithmRunSetup<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    private readonly ResolutionScopeBuilder scope = new();
    private readonly HashSet<Aspect> aspects = new(ReferenceEqualityComparer.Instance);

    public IAlgorithm<TCandidate, TSearchState> Algorithm { get; }

    internal AlgorithmRunSetup(IAlgorithm<TCandidate, TSearchState> algorithm)
    {
        Algorithm = algorithm;
    }

    /// <summary>Adds an aspect in insertion order, deduplicating by reference.</summary>
    public AlgorithmRunSetup<TCandidate, TSearchState> AddAspect(Aspect aspect)
    {
        if (aspects.Add(aspect))
            scope.AddAspect(aspect);
        return this;
    }

    /// <summary>Supplies a caller owned resource. Duplicate handle bindings are rejected.</summary>
    public AlgorithmRunSetup<TCandidate, TSearchState> AddResource<TResource>(ResourceHandle<TResource> handle, TResource resource)
        where TResource : class
    {
        scope.AddResource(handle, resource);
        return this;
    }

    internal void CheckResourceSupply<TResource>(ResourceHandle<TResource> handle) where TResource : class =>
        scope.CheckResourceSupply(handle);

    internal ResolutionScope BuildScope() => scope.Build();
}
