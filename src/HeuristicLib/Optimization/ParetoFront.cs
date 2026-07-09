namespace HEAL.HeuristicLib.Optimization;

public static class ParetoFront
{
    public static IReadOnlyList<T> ExtractFrom<T>(IEnumerable<T> population, Func<T, ObjectiveVector> fitnessSelector, ObjectiveDirections objective)
      where T : IEquatable<T>
    {
        var uniqueItems = population.Distinct().ToList();

        return uniqueItems
          .Where(ind => !uniqueItems.Any(other => !ind.Equals(other) && fitnessSelector(ind).IsDominatedBy(fitnessSelector(other), objective)))
          .ToList();
    }

    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> ExtractFrom<TCandidate>(IEnumerable<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective)
      where TCandidate : IEquatable<TCandidate>
    {
        var uniqueEvaluatedCandidates = population.Distinct().ToList();

        return uniqueEvaluatedCandidates
          .Where(ind => !uniqueEvaluatedCandidates.Any(other => ind != other && ind.ObjectiveVector.IsDominatedBy(other.ObjectiveVector, objective)))
          .ToList();
    }
}
