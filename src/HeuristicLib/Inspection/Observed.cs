using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Records each successful call of its child into an instrument, such as a trace.</summary>
/// <remarks>
/// Each call produces one <typeparamref name="TObservation"/> with the call's inputs and outputs, and the instrument
/// receives it. Each operation offers this wrapper as <c>Observed</c> beside its <c>Invocations</c>, such as
/// <c>mutator.Observed(trace)</c>.
/// </remarks>
public sealed record Observed<TOperation, TCandidate, TObservation> : WrappingOperator<TOperation, TCandidate>
    where TOperation : IOperation<TOperation>
    where TObservation : IInvocationObservation<TOperation, TCandidate, TObservation>
{
    public Observed(IOperator<TOperation, TCandidate> childOperator, IObservationInstrument<TObservation> instrument)
        : base(childOperator)
    {
        Instrument = instrument;
    }

    public IObservationInstrument<TObservation> Instrument { get; init; }

    public static Parameter<Observed<TOperation, TCandidate, TObservation>, IObservationInstrument<TObservation>> InstrumentParameter { get; } =
        new(nameof(Instrument), static observed => observed.Instrument, static (observed, value) => observed with { Instrument = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [InstrumentParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        TObservation.Wrap<TSearchSpace, TProblem, TSearchState>(scope.ResolveOperator(ChildOperator), ChildOperator, Instrument.CreateRecorder(scope.Results));
}
