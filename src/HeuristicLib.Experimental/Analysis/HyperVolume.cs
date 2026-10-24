using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Accumulates a Pareto front across calls and reports its hypervolume.
/// </summary>
/// <remarks>
/// The front is this object's own state, so give each trace its own. The first objective directions it receives build
/// the front and fix it: a later call with other directions throws, because its candidates do not belong on that front.
/// </remarks>
public sealed class HyperVolumeSoFar<TCandidate>(ObjectiveVector referencePoint)
{
    private ParetoFront<TCandidate>? front;
    private double hyperVolume;

    public ObjectiveVector ReferencePoint { get; } = referencePoint;

    /// <summary>Adds the evaluated candidates to the front and returns the front's hypervolume.</summary>
    public double Update(IReadOnlyList<EvaluatedCandidate<TCandidate>> values, ObjectiveDirections objective)
    {
        front ??= new ParetoFront<TCandidate>(ReferencePoint, objective);
        if (!ReferenceEquals(front.Objective, objective) && !front.Objective.Directions.SequenceEqual(objective.Directions))
            throw new InvalidOperationException(
                $"A hypervolume so far is fixed to the objective directions {front.Objective} it first received, but a call has {objective}.");

        // The front only moves when a candidate survives domination, and the calculation is the expensive part.
        if (front.AddPoints(values))
            hyperVolume = HyperVolumeCalculator.Calculate(
                front.Points.Select(evaluated => evaluated.ObjectiveVector), ReferencePoint, front.Objective);
        return hyperVolume;
    }
}

public static class HyperVolumeTraces
{
    extension<TCandidate>(ObservationSource<EvaluationObservation<TCandidate>> calls)
    {
        /// <summary>
        /// Traces the hypervolume of the Pareto front accumulated over every candidate the evaluator evaluates.
        /// </summary>
        /// <remarks>
        /// The front is private to the trace, so the recorded entries are plain numbers.
        /// </remarks>
        public TraceAnalyzer<double> TraceHyperVolume(ObjectiveVector referencePoint,
            IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null)
        {
            var hyperVolume = new HyperVolumeSoFar<TCandidate>(referencePoint);
            return Analyzer.Trace(calls,
                observation => hyperVolume.Update(observation.Candidates.ToEvaluated(observation.ObjectiveVectors), observation.Problem.Objective),
                clocks, retention);
        }
    }
}
