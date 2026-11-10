using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Algorithms;

/// <summary>Runs its child algorithm with aspects attached to the resolution scope it is resolved in.</summary>
/// <remarks>
/// <para>
/// The advice applies to the child and to everything resolved in this scope and its child scopes, while the advice never
/// applies to this configuration itself.
/// </para>
/// <para>
/// The first aspect listed wraps closest to the executors it selects. Nesting keeps the order: in
/// <c>algorithm.Advised(a).Advised(b)</c>, <c>a</c> wraps closer than <c>b</c>. Resolving this algorithm in another
/// scope, such as the next Cycle turn or another run, attaches the same aspects there.
/// </para>
/// </remarks>
public sealed record AdvisedAlgorithm<TCandidate, TSearchState> : WrappingAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public AdvisedAlgorithm(IAlgorithm<TCandidate, TSearchState> childAlgorithm, IReadOnlyList<Aspect> aspects)
        : base(childAlgorithm)
    {
        Aspects = aspects.ToValueArray();
    }

    public ValueArray<Aspect> Aspects { get; init; }

    public static Parameter<AdvisedAlgorithm<TCandidate, TSearchState>, ValueArray<Aspect>> AspectsParameter { get; } =
        new(nameof(Aspects), static algorithm => algorithm.Aspects, static (algorithm, value) => algorithm with { Aspects = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [AspectsParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    // A scope wraps the aspect added last closest, so the list is added last to first.
    protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        for (var index = Aspects.Count - 1; index >= 0; index--)
            scope.AddAspect(Aspects[index]);
        return scope.Resolve(ChildAlgorithm);
    }
}

public static class AdvisedAlgorithm
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <summary>Attaches aspects to everything this algorithm resolves.</summary>
        /// <remarks>The first aspect listed wraps closest.</remarks>
        public AdvisedAlgorithm<TCandidate, TSearchState> Advised(params IReadOnlyList<Aspect> aspects) => new(algorithm, aspects);
    }
}
