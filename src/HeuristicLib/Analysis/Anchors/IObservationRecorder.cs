using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Receives one typed observation each time the anchor it was installed at completes an operation.
/// </summary>
/// <remarks>
/// A recorder is the analysis-side half of an anchor. It must only read the observation: an observation must not change
/// the observed operation's inputs, outputs or computation. The observing wrapper that delivers observations holds no
/// data of its own, so everything a recorder accumulates belongs to the analyzer that installed it.
/// </remarks>
/// <remarks>
/// Implement this together with <see cref="IExecutionHook"/> to write an analyzer the library does not provide, and
/// install the recorder with <see cref="Anchors.Observe"/> at an anchor from <see cref="Anchors"/>. For a boundary the
/// library does not cover, derive an anchor of your own from <see cref="Anchor{TObservation}"/> rather than decorating
/// by hand.
/// </remarks>
public interface IObservationRecorder<in TObservation>
    where TObservation : Observation
{
    void Record(TObservation observation);
}
