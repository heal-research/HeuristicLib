using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Records each call or each yielded state of its child algorithm into a receiver, such as a trace analyzer.</summary>
/// <remarks>
/// <typeparamref name="TObservation"/> says which boundary is observed. An algorithm offers this wrapper as <c>Observed</c>,
/// taking a receiver of its iterations or of its calls.
/// </remarks>
public sealed record ObservedAlgorithm<TCandidate, TSearchState, TObservation> : WrappingAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
    where TObservation : IAlgorithmObservation<TCandidate, TSearchState, TObservation>
{
    public ObservedAlgorithm(IAlgorithm<TCandidate, TSearchState> childAlgorithm, IObservationReceiver<TObservation> receiver)
        : base(childAlgorithm)
    {
        Receiver = receiver;
    }

    public IObservationReceiver<TObservation> Receiver { get; init; }

    public static Parameter<ObservedAlgorithm<TCandidate, TSearchState, TObservation>, IObservationReceiver<TObservation>> ReceiverParameter { get; } =
        new(nameof(Receiver), static observed => observed.Receiver, static (observed, value) => observed with { Receiver = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [ReceiverParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IAlgorithmExecutor<TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        TObservation.Wrap<TSearchSpace, TProblem>(scope.Resolve(ChildAlgorithm), ChildAlgorithm, Receiver.CreateRecorder(scope.Results), scope.Problem);
}
