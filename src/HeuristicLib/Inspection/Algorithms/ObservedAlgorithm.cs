using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Records each call or each yielded state of its child algorithm into an instrument, such as a trace.</summary>
/// <remarks>
/// <typeparamref name="TObservation"/> says which boundary is observed. An algorithm offers this wrapper as <c>Observed</c>,
/// taking an instrument of its iterations or of its calls.
/// </remarks>
public sealed record ObservedAlgorithm<TCandidate, TSearchState, TObservation> : WrappingAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
    where TObservation : IAlgorithmObservation<TCandidate, TSearchState, TObservation>
{
    public ObservedAlgorithm(IAlgorithm<TCandidate, TSearchState> childAlgorithm, IObservationInstrument<TObservation> instrument)
        : base(childAlgorithm)
    {
        Instrument = instrument;
    }

    public IObservationInstrument<TObservation> Instrument { get; init; }

    public static Parameter<ObservedAlgorithm<TCandidate, TSearchState, TObservation>, IObservationInstrument<TObservation>> InstrumentParameter { get; } =
        new(nameof(Instrument), static observed => observed.Instrument, static (observed, value) => observed with { Instrument = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [InstrumentParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IAlgorithmExecutor<TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        TObservation.Wrap<TSearchSpace, TProblem>(scope.Resolve(ChildAlgorithm), ChildAlgorithm, Instrument.CreateRecorder(scope.Results), scope.Problem);
}
