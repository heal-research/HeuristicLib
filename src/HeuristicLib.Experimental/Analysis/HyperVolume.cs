using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// The Pareto front a hypervolume aggregation accumulates while a run executes.
/// </summary>
public class HyperVolumeState<T>(ObjectiveVector referencePoint, ObjectiveDirections objective)
    : ParetoState<T>(referencePoint, objective)
{
    private Lazy<double>? hyperVolumeLazy;
    public double HyperVolume => hyperVolumeLazy?.Value ?? 0;

    public override bool AddPoints(IEnumerable<EvaluatedCandidate<T>> evaluatedCandidates)
    {
        if (!base.AddPoints(evaluatedCandidates))
            return false;

        hyperVolumeLazy = new Lazy<double>(() =>
            HyperVolumeCalculator.Calculate(
                Front.Select(x => x.ObjectiveVector),
                ReferencePoint,
                Objective));
        return true;
    }

    public override void Clear()
    {
        base.Clear();
        hyperVolumeLazy = null;
    }
}

/// <summary>
/// Accumulates a Pareto front across firings and reports its hypervolume at each of them.
/// </summary>
/// <remarks>
/// This aggregation keeps the front it builds, so it belongs to one trace and one run, the same way a retention does.
/// The recorded value is a plain number, so the trace's entries stay immutable even though the front behind them grows.
/// Dominance is what a front is made of, so this reads the objective directions rather than only an ordering.
/// </remarks>
public sealed class HyperVolumeAggregation<TCandidate>(ObjectiveVector referencePoint)
    : IAggregation<EvaluatedCandidate<TCandidate>, double>
{
    private HyperVolumeState<TCandidate>? front;

    public double Aggregate(IReadOnlyList<EvaluatedCandidate<TCandidate>> readings, ObjectiveDirections objective)
    {
        // The objective is the run's, so the front cannot be built before the first firing.
        front ??= new HyperVolumeState<TCandidate>(referencePoint, objective);
        front.AddPoints(readings);
        return front.HyperVolume;
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
        /// The front is the aggregation's own state, so the recorded entries are plain numbers.
        /// </remarks>
        public static TraceAnalyzer<double> TraceHyperVolume<T, TS, TP>(
            ObjectiveVector referencePoint,
            IEvaluator<T, TS, TP> evaluator,
            params IReadOnlyList<Clock> clocks)
            where TS : class, ISearchSpace<T>
            where TP : class, IProblem<T, TS> =>
            Analyzer.Trace(
                new EvaluatedCandidatesFromEvaluationMeasurement<T, TS, TP>(),
                new HyperVolumeAggregation<T>(referencePoint),
                Anchor.At(evaluator),
                clocks);
    }
}
