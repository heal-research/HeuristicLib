using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>The observations of one algorithm's executor calls, such as <c>algorithm.Iterations</c> or <c>algorithm.Calls</c>.</summary>
/// <remarks>
/// The source creates a substitution placing a counting wrapper for a supplied counter. An iteration source also
/// offers <c>Timing</c> for a supplied duration. Creating a source installs nothing.
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

    /// <summary>Places a wrapper counting this boundary into the counter: iterations count yielded states, and calls count calls that started.</summary>
    /// <remarks>
    /// In one substitution list, a later part sits closer to its target. List this placement after every analyzer that
    /// reads the counter so its reading includes the call; listed before them, it reads before the call.
    /// </remarks>
    public Substitution Counting(Counter counter) =>
        Substitution.Of(Algorithm, node => new CountedAlgorithm<TCandidate, TSearchState, TObservation>(node, counter));

    /// <inheritdoc/>
    public override Substitution RecordingInto(IObservationReceiver<TObservation> receiver) =>
        Substitution.Of<IAlgorithm<TCandidate, TSearchState>>(Algorithm, node => new ObservedAlgorithm<TCandidate, TSearchState, TObservation>(node, receiver));
}
