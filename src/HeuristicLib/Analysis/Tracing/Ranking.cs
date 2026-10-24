using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>Ranks the values a trace reads from one observation, such as the objective vectors of one call.</summary>
/// <remarks>
/// Ranking receives the objective directions it orders by. A trace passes the observed call's own, as in
/// <c>Analyzer.Trace(evaluator.Calls, observation =&gt; observation.ObjectiveVectors.BestMedianWorst(observation.Problem.Objective))</c>, so each call
/// is ranked by its own problem. An explicit comparer replaces the directions' total order.
/// </remarks>
public static class Ranking
{
    extension(IEnumerable<ObjectiveVector> values)
    {
        /// <summary>Orders the objective vectors and keeps the best, median and worst of them.</summary>
        public BestMedianWorst BestMedianWorst(ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
        {
            var ordered = values.Order(objective.RequireTotalOrder(objectiveComparer)).ToArray();
            if (ordered.Length == 0)
                throw new InvalidOperationException("There are no objective vectors, cannot determine best, median and worst.");

            return new BestMedianWorst(ordered[0], ordered[ordered.Length / 2], ordered[^1]);
        }
    }

    extension<TCandidate>(IEnumerable<EvaluatedCandidate<TCandidate>> values)
    {
        /// <summary>Keeps the best evaluated candidate.</summary>
        public EvaluatedCandidate<TCandidate> Best(ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            values.MinBy(evaluated => evaluated.ObjectiveVector, objective.RequireTotalOrder(objectiveComparer))
            ?? throw new InvalidOperationException("There are no evaluated candidates, cannot determine the best.");

        /// <summary>Orders the evaluated candidates and keeps the best, median and worst of them.</summary>
        public BestMedianWorstEntry<TCandidate> BestMedianWorst(ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
        {
            var ordered = values.OrderBy(evaluated => evaluated.ObjectiveVector, objective.RequireTotalOrder(objectiveComparer)).ToArray();
            if (ordered.Length == 0)
                throw new InvalidOperationException("There are no evaluated candidates, cannot determine best, median and worst.");

            return BestMedianWorstEntry.From(ordered[0], ordered[ordered.Length / 2], ordered[^1]);
        }
    }

    extension(IEnumerable<double> values)
    {
        /// <summary>Keeps the smallest, the arithmetic mean and the largest value.</summary>
        /// <remarks>This summarizes plain numbers, so it needs no objective directions.</remarks>
        public MinMeanMax MinMeanMax()
        {
            var numbers = values as IReadOnlyCollection<double> ?? values.ToArray();
            if (numbers.Count == 0)
                throw new InvalidOperationException("There are no values, cannot determine minimum, mean and maximum.");

            return new MinMeanMax(numbers.Min(), numbers.Average(), numbers.Max());
        }
    }
}

public readonly record struct BestMedianWorst(ObjectiveVector Best, ObjectiveVector Median, ObjectiveVector Worst);

public readonly record struct MinMeanMax(double Min, double Mean, double Max);

/// <summary>
/// The best, median and worst evaluated candidate of one set of values.
/// </summary>
public record BestMedianWorstEntry<TCandidate>(EvaluatedCandidate<TCandidate> Best, EvaluatedCandidate<TCandidate> Median, EvaluatedCandidate<TCandidate> Worst);

public static class BestMedianWorstEntry
{
    public static BestMedianWorstEntry<TCandidate> From<TCandidate>(EvaluatedCandidate<TCandidate> best, EvaluatedCandidate<TCandidate> median, EvaluatedCandidate<TCandidate> worst) => new(best, median, worst);
}
