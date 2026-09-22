using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Accumulates a Pareto front across observations and reports its hypervolume.
/// </summary>
/// <remarks>The front is this object's own state, so give each trace its own.</remarks>
public sealed class HyperVolumeAggregation<TCandidate>(ObjectiveVector referencePoint)
    : IAggregation<EvaluatedCandidate<TCandidate>, double>
{
    private ParetoFront<TCandidate>? front;
    private double hyperVolume;

    public ObjectiveVector ReferencePoint { get; } = referencePoint;

    public double Aggregate(IReadOnlyList<EvaluatedCandidate<TCandidate>> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
    {
        front ??= new ParetoFront<TCandidate>(ReferencePoint, objective);

        // The front only moves when a reading survives domination, and the calculation is the expensive part.
        if (front.AddPoints(readings))
            hyperVolume = HyperVolumeCalculator.Calculate(
                front.Points.Select(evaluated => evaluated.ObjectiveVector), ReferencePoint, objective);
        return hyperVolume;
    }
}

public static class HyperVolumeTraces
{
    extension(Analyzer)
    {
        /// <summary>
        /// Traces the hypervolume of the Pareto front accumulated over everything the evaluator produces.
        /// </summary>
        /// <remarks>
        /// The front is the reduction's private state, so the recorded entries are plain numbers.
        /// </remarks>
        public static TraceAnalyzer<double> TraceHyperVolume<T>(
            ObjectiveVector referencePoint,
            IEvaluator<T> evaluator,
            IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null) =>
            Analyzer.Trace(
                evaluator,
                new EvaluatedCandidatesFromEvaluationMeasurement<T>(),
                new HyperVolumeAggregation<T>(referencePoint),
                clocks, retention);
    }
}
