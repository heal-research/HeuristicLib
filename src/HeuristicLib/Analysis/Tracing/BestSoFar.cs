using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Keeps the best value seen so far, across calls, such as the best objective vector or the best evaluated candidate.
/// </summary>
/// <remarks>
/// Each value is ranked by the objective vector the selector reads from it. The running best is this object's own
/// state, so give each trace its own. The first objective directions it receives fix it: a later call with other
/// directions, or with objective vectors of another dimensionality, throws, because their best is not comparable to the
/// running one.
/// </remarks>
/// <param name="objectiveVector">
/// Reads the objective vector a value is ranked by, such as <c>candidate => candidate.ObjectiveVector</c>.
/// </param>
/// <param name="objectiveComparer">Orders objective vectors instead of the fixed directions' total order.</param>
public class BestSoFar<T>(Func<T, ObjectiveVector> objectiveVector, IComparer<ObjectiveVector>? objectiveComparer = null)
{
    private ObjectiveDirections? directions;
    private IComparer<ObjectiveVector>? order;
    private T? best;
    private ObjectiveVector? bestObjectiveVector;

    /// <summary>Takes the best of the values into the running best and returns the running best.</summary>
    public T Update(IEnumerable<T> values, ObjectiveDirections objective)
    {
        var fixedOrder = Require(objective);
        foreach (var value in values)
        {
            var vector = objectiveVector(value);
            if (vector.Count != objective.Directions.Length)
                throw new InvalidOperationException(
                    $"A best so far is fixed to {objective.Directions.Length} objectives, but a call returned an objective vector of {vector.Count}.");
            if (bestObjectiveVector is null || fixedOrder.Compare(vector, bestObjectiveVector) < 0)
            {
                best = value;
                bestObjectiveVector = vector;
            }
        }

        return bestObjectiveVector is null
            ? throw new InvalidOperationException("There are no values, cannot determine the best so far.")
            : best!;
    }

    // The objective directions the running best was first ranked by fix the order of every later call.
    private IComparer<ObjectiveVector> Require(ObjectiveDirections objective)
    {
        if (directions is null)
        {
            order = objective.RequireTotalOrder(objectiveComparer);
            directions = objective;
        }
        else if (!ReferenceEquals(directions, objective) && !directions.Directions.SequenceEqual(objective.Directions))
            throw new InvalidOperationException(
                $"A best so far is fixed to the objective directions {directions} it first received, but a call has {objective}.");

        return order!;
    }
}

/// <summary>Keeps the best objective vector seen so far, across calls, ranking each vector by itself.</summary>
/// <remarks>The first objective directions it receives fix it, as for <see cref="BestSoFar{T}"/>.</remarks>
/// <param name="objectiveComparer">Orders objective vectors instead of the fixed directions' total order.</param>
public sealed class BestSoFar(IComparer<ObjectiveVector>? objectiveComparer = null)
    : BestSoFar<ObjectiveVector>(static vector => vector, objectiveComparer);

public static class BestSoFarTraces
{
    extension<TCandidate>(ObservationSource<EvaluationObservation<TCandidate>> invocations)
    {
        /// <summary>
        /// Accumulates the best objective vector across the evaluator's invocations, each ranked by its own problem's objective.
        /// By default, records only improvements.
        /// </summary>
        public TraceAnalyzer<ObjectiveVector> TraceBestSoFar(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            TraceBestSoFar([invocations], clocks, retention, objectiveComparer);

        /// <summary>
        /// Accumulates the best evaluated candidate across the evaluator's invocations, each ranked by its own problem's
        /// objective. By default, records only improvements.
        /// </summary>
        public TraceAnalyzer<EvaluatedCandidate<TCandidate>> TraceBestCandidateSoFar(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            TraceBestCandidateSoFar([invocations], clocks, retention, objectiveComparer);
    }

    extension<TCandidate>(IReadOnlyList<ObservationSource<EvaluationObservation<TCandidate>>> invocations)
    {
        /// <summary>Accumulates the best objective vector across the invocations of several evaluators into one trace.</summary>
        public TraceAnalyzer<ObjectiveVector> TraceBestSoFar(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        {
            var best = new BestSoFar(objectiveComparer);
            return Analyzer.Trace(invocations, observation => best.Update(observation.ObjectiveVectors, observation.Problem.Objective),
                clocks, retention ?? TraceRetention.OnChange());
        }

        /// <summary>Accumulates the best evaluated candidate across the invocations of several evaluators into one trace.</summary>
        public TraceAnalyzer<EvaluatedCandidate<TCandidate>> TraceBestCandidateSoFar(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        {
            var best = new BestSoFar<EvaluatedCandidate<TCandidate>>(static candidate => candidate.ObjectiveVector, objectiveComparer);
            return Analyzer.Trace(invocations,
                observation => best.Update(observation.Candidates.ToEvaluated(observation.ObjectiveVectors), observation.Problem.Objective),
                clocks, retention ?? TraceRetention.OnChange());
        }
    }
}
