using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Algorithms;

/// <summary>Runs its child algorithm with one shared executor for each of the given configurations.</summary>
/// <remarks>
/// Every use of a shared configuration inside the child receives the same executor, for as long as this algorithm's
/// executor lives. Constructing this algorithm again, for example in the next Cycle turn or another run, creates new
/// shared executors. Other configurations are constructed afresh for every use, as usual.
/// </remarks>
public sealed record SharingAlgorithm<TCandidate, TSearchState> : WrappingAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public SharingAlgorithm(IAlgorithm<TCandidate, TSearchState> childAlgorithm, IReadOnlyList<IConfigurationNode> shared)
        : base(childAlgorithm)
    {
        if (shared.Count == 0)
            throw new ArgumentException("At least one configuration must be shared.", nameof(shared));
        Shared = shared.ToValueArray();
    }

    public ValueArray<IConfigurationNode> Shared { get; init; }

    protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        scope.Sharing([.. Shared]).Resolve(ChildAlgorithm);
}

public static class SharingAlgorithm
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <summary>Shares one executor of each given configuration across every use inside this algorithm.</summary>
        public SharingAlgorithm<TCandidate, TSearchState> Sharing(params IReadOnlyList<IConfigurationNode> shared) => new(algorithm, shared);
    }
}
