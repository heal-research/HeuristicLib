using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Algorithms;

/// <summary>Runs its child algorithm with one shared executor for each of the given configurations.</summary>
/// <remarks>
/// Within one resolution scope every use of a configuration already receives one executor. Sharing extends that to the
/// child scopes created below this algorithm, such as Cycle turns and Pipeline stages: every use of a shared
/// configuration there receives one executor, kept by the scope this algorithm is resolved in for as long as that scope
/// lives. Each shared executor is built in a child scope of its own, as for a child activation, so sharing changes how
/// long it lives but not what it holds: budgets inside it count only the calls made inside it, and two shared
/// configurations share a child only when that child is shared too. Resolving this algorithm in another scope, for
/// example in the next Cycle turn or another run, creates new shared executors.
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

    public static ReferenceListParameter<SharingAlgorithm<TCandidate, TSearchState>> SharedParameter { get; } =
        new(nameof(Shared), static sharing => sharing.Shared, static (sharing, value) => sharing with { Shared = value.ToValueArray() });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [SharedParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IAlgorithmExecutor<TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        scope.Share([.. Shared]);
        return scope.Resolve(ChildAlgorithm);
    }
}

public static class SharingAlgorithm
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <summary>Shares one executor of each given configuration across every use inside this algorithm, including its child scopes.</summary>
        public SharingAlgorithm<TCandidate, TSearchState> Sharing(params IReadOnlyList<IConfigurationNode> shared) => new(algorithm, shared);
    }
}
