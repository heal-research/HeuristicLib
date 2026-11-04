using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Records each call or each yielded state of its child algorithm into an instrument, through its source's observation wrapper.</summary>
/// <remarks>
/// This is the explicit placement of an observation: it records what attaching the instrument with <c>Advised</c>
/// records at the source, through the same wrapper, into the same run result. An algorithm offers it as
/// <c>Observed</c>, taking an instrument of its iterations or of its calls.
/// </remarks>
public sealed record ObservedAlgorithm<TCandidate, TSearchState, TObservation> : WrappingAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    // The source supplies only the observation wrapper; the child it observes is ChildAlgorithm.
    private readonly AlgorithmSource<TCandidate, TSearchState, TObservation> source;

    /// <summary>Observes the algorithm of <paramref name="source"/> through the source's observation wrapper.</summary>
    public ObservedAlgorithm(AlgorithmSource<TCandidate, TSearchState, TObservation> source, IObservationInstrument<TObservation> instrument)
        : base(source.Algorithm)
    {
        this.source = source;
        Instrument = instrument;
    }

    public IObservationInstrument<TObservation> Instrument { get; init; }

    protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        source.Advise(Instrument.CreateRecorder(scope.Results)).Wrap(scope.Resolve(ChildAlgorithm), ChildAlgorithm);
}
