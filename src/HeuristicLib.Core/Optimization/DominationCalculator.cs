namespace HEAL.HeuristicLib.Optimization;

public static class DominationCalculator {
  /// <summary>
  /// Calculates the best pareto front only. The fast non-dominated sorting algorithm is used
  /// as described in Deb, K., Pratap, A., Agarwal, S., and Meyarivan, T. (2002).
  /// A Fast and Elitist Multiobjective Genetic Algorithm: NSGA-II.
  /// IEEE Transactions on Evolutionary Computation, 6(2), 182-197.
  /// </summary>
  /// <remarks>
  /// When there are plateaus in the fitness landscape several ISolutions might have exactly
  /// the same fitness vector. In this case parameter <paramref name="dominateOnEqualQualities"/>
  /// can be set to true to avoid plateaus becoming too attractive for the search process.
  /// </remarks>
  /// <param name="solutions">The ISolutions of the population.</param>
  /// <param name="objective"></param>
  /// <param name="dominateOnEqualQualities">Whether ISolutions of exactly equal quality should dominate one another.</param>
  /// <returns>The pareto front containing the best ISolutions and their associated quality resp. fitness.</returns>
  public static List<ISolution<T>> CalculateBestParetoFront<T>(IReadOnlyList<ISolution<T>> solutions, Objective objective, bool dominateOnEqualQualities = true) => CalculateBestFront(solutions, objective, solutions.Count, dominateOnEqualQualities, out _, out _, out _);

  /// <summary>
  /// Calculates all pareto fronts. The first in the list is the best front.
  /// The fast non-dominated sorting algorithm is used as described in
  /// Deb, K., Pratap, A., Agarwal, S., and Meyarivan, T. (2002).
  /// A Fast and Elitist Multiobjective Genetic Algorithm: NSGA-II.
  /// IEEE Transactions on Evolutionary Computation, 6(2), 182-197.
  /// </summary>
  /// <remarks>
  /// When there are plateaus in the fitness landscape several ISolutions might have exactly
  /// the same fitness vector. In this case parameter <paramref name="dominateOnEqualQualities"/>
  /// can be set to true to avoid plateaus becoming too attractive for the search process.
  /// </remarks>
  /// <param name="solutions">The ISolutions of the population.</param>
  /// <param name="objective"></param>
  /// <param name="rank">The rank of each of the ISolutions, corresponds to the front it is put in.</param>
  /// <param name="dominateOnEqualQualities">Whether ISolutions of exactly equal quality should dominate one another.</param>
  /// <returns>A sorted list of the pareto fronts from best to worst.</returns>
  public static List<List<ISolution<T>>> CalculateAllParetoFronts<T>(IReadOnlyList<ISolution<T>> solutions, Objective objective, out int[] rank, bool dominateOnEqualQualities = true) {
    var populationSize = solutions.Count;
    var fronts = new List<List<ISolution<T>>>();
    if (solutions.Count == 0) {
      rank = [];
      return fronts;
    }

    fronts.Add(CalculateBestFront(solutions, objective, populationSize, dominateOnEqualQualities, out var dominatedIndividuals, out var dominationCounter, out rank));
    while (fronts[^1].Count > 0) {
      var nextFront = new List<ISolution<T>>();
      foreach (var p in fronts[^1]) {
        if (!dominatedIndividuals.TryGetValue(p, out var dominatedIndividualsByp))
          continue;
        foreach (var dominatedIndividual in dominatedIndividualsByp) {
          if (--dominationCounter[dominatedIndividual] != 0)
            continue;
          rank[dominatedIndividual] = fronts.Count;
          nextFront.Add(solutions[dominatedIndividual]);
        }
      }

      if (nextFront.Count == 0)
        break;
      fronts.Add(nextFront);
    }

    return fronts;
  }

  private static List<ISolution<T>> CalculateBestFront<T>(IReadOnlyList<ISolution<T>> solutions, Objective objective, int populationSize, bool dominateOnEquals, out Dictionary<ISolution<T>, List<int>> dominatedIndividuals, out int[] dominationCounter, out int[] rank) {
    var front = new List<ISolution<T>>();
    dominatedIndividuals = new Dictionary<ISolution<T>, List<int>>();
    dominationCounter = new int[populationSize];
    rank = new int[populationSize];
    for (var pI = 0; pI < populationSize - 1; pI++) {
      var p = solutions[pI];
      if (!dominatedIndividuals.TryGetValue(p, out var dominatedIndividualsByp))
        dominatedIndividuals[p] = dominatedIndividualsByp = [];
      for (var qI = pI + 1; qI < populationSize; qI++) {
        var test = solutions[pI].ObjectiveVector.CompareTo(solutions[qI].ObjectiveVector, objective); //Dominates(qualities[pI], qualities[qI], maximization, dominateOnEqualQualities);
        if (test == DominanceRelation.Equivalent) {
          test = dominateOnEquals ? DominanceRelation.Dominates : DominanceRelation.Incomparable;
        }

        switch (test) {
          case DominanceRelation.Dominates:
            dominatedIndividualsByp.Add(qI);
            dominationCounter[qI] += 1;
            break;
          case DominanceRelation.IsDominatedBy: {
            dominationCounter[pI] += 1;
            if (!dominatedIndividuals.ContainsKey(solutions[qI]))
              dominatedIndividuals.Add(solutions[qI], []);
            dominatedIndividuals[solutions[qI]].Add(pI);
            break;
          }
          case DominanceRelation.Incomparable:
            break;
          case DominanceRelation.Equivalent:
          default:
            throw new InvalidOperationException("Encountered invalid dominance relation");
        }

        if (pI != populationSize - 2
            || qI != populationSize - 1
            || dominationCounter[qI] != 0) {
          continue;
        }

        rank[qI] = 0;
        front.Add(solutions[qI]);
      }

      if (dominationCounter[pI] != 0)
        continue;

      rank[pI] = 0;
      front.Add(p);
    }

    return front;
  }
}
