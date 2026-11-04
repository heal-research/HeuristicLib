using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Records each successful call of its child into an instrument, such as a trace, through its source's observation wrapper.</summary>
/// <remarks>
/// This is the explicit placement of an observation: it records what attaching the instrument with <c>Advised</c>
/// records at the source, through the same wrapper, into the same run result. Each operation offers it as
/// <c>Observed</c> beside its <c>Invocations</c>, such as <c>mutator.Observed(trace)</c>.
/// </remarks>
public sealed record Observed<TOperation, TCandidate, TObservation> : WrappingOperator<TOperation, TCandidate>
    where TOperation : IOperation<TOperation>
{
    // The source supplies only the observation wrapper; the child it observes is ChildOperator.
    private readonly InvocationSource<TOperation, TCandidate, TObservation> source;

    /// <summary>Observes the operator of <paramref name="source"/> through the source's observation wrapper.</summary>
    public Observed(InvocationSource<TOperation, TCandidate, TObservation> source, IObservationInstrument<TObservation> instrument)
        : base(source.Operator)
    {
        this.source = source;
        Instrument = instrument;
    }

    public IObservationInstrument<TObservation> Instrument { get; init; }

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        ExplicitPlacement.Apply(scope, ChildOperator, source.Advise(Instrument.CreateRecorder(scope.Results)));
}
