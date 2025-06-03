namespace HEAL.HeuristicLib.Optimization;

public static class ParetoFront {
  public static IReadOnlyList<T> ExtractFrom<T>(IEnumerable<T> population, Func<T, ObjectiveVector> fitnessSelector, Objective objective)
    where T : IEquatable<T>
  {
    var uniqueSolutions = population.Distinct().ToList();
    return uniqueSolutions
      .Where(ind => !uniqueSolutions.Any(other => !ind.Equals(other) && fitnessSelector(ind).IsDominatedBy(fitnessSelector(other), objective)))
      .ToList();
  }
  
  public static IReadOnlyList<Solution<TGenotype>> ExtractFrom<TGenotype>(IEnumerable<Solution<TGenotype>> population, Objective objective) {
    var uniqueSolutions = population.Distinct().ToList();
    return uniqueSolutions
      .Where(ind => !uniqueSolutions.Any(other => ind != other && ind.ObjectiveVector.IsDominatedBy(other.ObjectiveVector, objective)))
      .ToList();
  }
  
  // public static IReadOnlyList<Solution<TGenotype, TPhenotype>> ExtractFrom<TGenotype, TPhenotype>(IEnumerable<Solution<TGenotype, TPhenotype>> population, Objective objective) {
  //   var uniqueSolutions = population.Distinct().ToList();
  //   return uniqueSolutions
  //     .Where(ind => !uniqueSolutions.Any(other => ind != other && ind.Fitness.IsDominatedBy(other.Fitness, objective)))
  //     .ToList();
  // }
}
