using HEAL.HeuristicLib.Execution;
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

/// <summary>Reusable settings for accumulating a Pareto front and reporting its hypervolume.</summary>
public sealed record HyperVolumeAggregation<TCandidate> : IAggregation<EvaluatedCandidate<TCandidate>, double>
{
    public ObjectiveVector ReferencePoint { get; init; }

    public HyperVolumeAggregation(ObjectiveVector referencePoint)
    {
        ReferencePoint = referencePoint;
    }

    public IAggregationInstance<EvaluatedCandidate<TCandidate>, double> CreateExecutionInstance(ExecutionInstanceResolver resolver) => new ExecutionInstance(ReferencePoint);

    private sealed class ExecutionInstance(ObjectiveVector referencePoint) : IAggregationInstance<EvaluatedCandidate<TCandidate>, double>
    {
        private HyperVolumeState<TCandidate>? front;

        public double Aggregate(IReadOnlyList<EvaluatedCandidate<TCandidate>> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
        {
            front ??= new HyperVolumeState<TCandidate>(referencePoint, objective);
            front.AddPoints(readings);
            return front.HyperVolume;
        }
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
        public static TraceAnalyzer<double> TraceHyperVolume<T, TS, TP>(
            ObjectiveVector referencePoint,
            IEvaluator<T, TS, TP> evaluator,
            IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null)
            where TS : class, ISearchSpace<T>
            where TP : class, IProblem<T, TS> =>
            Analyzer.Trace(
                evaluator,
                new EvaluatedCandidatesFromEvaluationMeasurement<T, TS, TP>(),
                new HyperVolumeAggregation<T>(referencePoint),
                clocks, retention);
    }
}
