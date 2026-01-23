namespace HEAL.HeuristicLib.Optimization;

public static class ParetoFront
{
  public static IReadOnlyList<T> ExtractFrom<T>(IEnumerable<T> population, Func<T, ObjectiveVector> fitnessSelector, Objective objective)
    where T : IEquatable<T>
  {
    var uniqueISolutions = population.Distinct().ToList();
    return uniqueISolutions
      .Where(ind => !uniqueISolutions.Any(other => !ind.Equals(other) && fitnessSelector(ind).IsDominatedBy(fitnessSelector(other), objective)))
      .ToList();
  }

  public static IReadOnlyList<ISolution<TGenotype>> ExtractFrom<TGenotype>(IEnumerable<ISolution<TGenotype>> population, Objective objective)
    where TGenotype : IEquatable<TGenotype>
  {
    var uniqueISolutions = population.Distinct().ToList();
    return uniqueISolutions
      .Where(ind => !uniqueISolutions.Any(other => ind != other && ind.ObjectiveVector.IsDominatedBy(other.ObjectiveVector, objective)))
      .ToList();
  }

  // public static IReadOnlyList<Solution<TGenotype, TPhenotype>> ExtractFrom<TGenotype, TPhenotype>(IEnumerable<Solution<TGenotype, TPhenotype>> population, Objective objective) {
  //   var uniqueISolutions = population.Distinct().ToList();
  //   return uniqueISolutions
  //     .Where(ind => !uniqueISolutions.Any(other => ind != other && ind.Fitness.IsDominatedBy(other.Fitness, objective)))
  //     .ToList();
  // }
}
