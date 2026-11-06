using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Algorithms;

/// <summary>Runs its child algorithm with instruments and aspects attached to the resolution scope it is resolved in.</summary>
/// <remarks>
/// <para>
/// Attaching an instrument adds the run's aspect for it, which collects into the run's one result of the instrument
/// wherever its pointcuts select, such as a trace at its sources or a counter at its operator. The advice applies to the
/// child and to everything resolved in this scope and its child scopes, while the advice never applies to this
/// configuration itself. Read the results from the run, such as <c>run.Result(trace)</c>.
/// </para>
/// <para>
/// The first instrument or aspect listed wraps closest to the executors it selects, and instruments wrap closer than
/// aspects. Nesting keeps the order: in <c>algorithm.Advised(a).Advised(b)</c>, <c>a</c> wraps closer than <c>b</c>.
/// Resolving this algorithm in another scope, such as the next Cycle turn or another run, attaches the same aspects
/// there, and instruments still collect into the run's one result.
/// </para>
/// </remarks>
public sealed record AdvisedAlgorithm<TCandidate, TSearchState> : WrappingAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public AdvisedAlgorithm(IAlgorithm<TCandidate, TSearchState> childAlgorithm, IReadOnlyList<Instrument> instruments, IReadOnlyList<Aspect> aspects)
        : base(childAlgorithm)
    {
        Instruments = instruments.ToValueArray();
        Aspects = aspects.ToValueArray();
    }

    public ValueArray<Instrument> Instruments { get; init; }

    public ValueArray<Aspect> Aspects { get; init; }

    public static Parameter<AdvisedAlgorithm<TCandidate, TSearchState>, ValueArray<Instrument>> InstrumentsParameter { get; } =
        new(nameof(Instruments), static algorithm => algorithm.Instruments, static (algorithm, value) => algorithm with { Instruments = value });

    public static Parameter<AdvisedAlgorithm<TCandidate, TSearchState>, ValueArray<Aspect>> AspectsParameter { get; } =
        new(nameof(Aspects), static algorithm => algorithm.Aspects, static (algorithm, value) => algorithm with { Aspects = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [InstrumentsParameter, AspectsParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    // A scope wraps the aspect added last closest, so both lists are added last to first, and the instruments last.
    protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        for (var index = Aspects.Count - 1; index >= 0; index--)
            scope.AddAspect(Aspects[index]);
        for (var index = Instruments.Count - 1; index >= 0; index--)
            scope.AddAspect(scope.Results.Aspect(Instruments[index]));
        return scope.Resolve(ChildAlgorithm);
    }
}

public static class AdvisedAlgorithm
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <summary>Attaches instruments, such as traces and counters, to everything this algorithm resolves.</summary>
        /// <remarks>The first instrument listed wraps closest.</remarks>
        public AdvisedAlgorithm<TCandidate, TSearchState> Advised(params IReadOnlyList<Instrument> instruments) => new(algorithm, instruments, []);

        /// <summary>Attaches aspects to everything this algorithm resolves.</summary>
        /// <remarks>The first aspect listed wraps closest.</remarks>
        public AdvisedAlgorithm<TCandidate, TSearchState> Advised(params IReadOnlyList<Aspect> aspects) => new(algorithm, [], aspects);
    }
}
