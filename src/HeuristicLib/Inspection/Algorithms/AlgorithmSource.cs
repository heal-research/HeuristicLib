using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>The observations of one algorithm's executor calls, such as <c>algorithm.Iterations</c> or <c>algorithm.Calls</c>.</summary>
/// <remarks>
/// A source of iterations or calls also creates a counter of them through its <c>Count</c> extension, and a source of
/// iterations a duration through <c>Duration</c>. Each call creates a new node with its own result.
/// </remarks>
public sealed class AlgorithmSource<TCandidate, TSearchState, TObservation> : ObservationSource<TObservation>
    where TSearchState : class, ISearchState
    where TObservation : IAlgorithmObservation<TCandidate, TSearchState, TObservation>
{
    internal AlgorithmSource(IAlgorithm<TCandidate, TSearchState> algorithm)
    {
        Algorithm = algorithm;
    }

    /// <summary>The observed algorithm.</summary>
    public IAlgorithm<TCandidate, TSearchState> Algorithm { get; }

    public override IConfigurationNode Configuration => Algorithm;

    public override Substitution RecordingInto(IObservationInstrument<TObservation> instrument) =>
        Substitution.Of<IAlgorithm<TCandidate, TSearchState>>(Algorithm, node => new ObservedAlgorithm<TCandidate, TSearchState, TObservation>(node, instrument));

    internal override Aspect Observe(Action<TObservation> callback) =>
        Aspect.Create(Pointcut.Reference(Algorithm), new AlgorithmObservationAdvice<TCandidate, TSearchState, TObservation>(callback));
}
