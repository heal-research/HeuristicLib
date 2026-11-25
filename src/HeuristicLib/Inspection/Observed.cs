using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Records each successful call of its child into a receiver, such as a trace analyzer.</summary>
/// <remarks>
/// Each call produces one <typeparamref name="TObservation"/> with the call's inputs and outputs, and the receiver
/// receives it. Each operation offers this wrapper as <c>Observed</c> beside its <c>Invocations</c>, such as
/// <c>mutator.Observed(trace)</c>.
/// </remarks>
public sealed record Observed<TOperation, TCandidate, TObservation> : WrappingOperator<TOperation, TCandidate>
    where TOperation : IOperation<TOperation>
    where TObservation : IInvocationObservation<TOperation, TCandidate, TObservation>
{
    public Observed(IOperator<TOperation, TCandidate> childOperator, IObservationReceiver<TObservation> receiver)
        : base(childOperator)
    {
        Receiver = receiver;
    }

    public IObservationReceiver<TObservation> Receiver { get; init; }

    public static Parameter<Observed<TOperation, TCandidate, TObservation>, IObservationReceiver<TObservation>> ReceiverParameter { get; } =
        new(nameof(Receiver), static observed => observed.Receiver, static (observed, value) => observed with { Receiver = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [ReceiverParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        TObservation.Wrap<TSearchState>(scope.ResolveOperator(ChildOperator), ChildOperator, Receiver.CreateRecorder(scope.Results), scope.Problem);
}
