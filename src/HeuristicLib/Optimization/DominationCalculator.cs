namespace HEAL.HeuristicLib.Optimization;

public static class DominationCalculator {
  /// <summary>
  /// Calculates the best pareto front only. The fast non-dominated sorting algorithm is used
  /// as described in Deb, K., Pratap, A., Agarwal, S., and Meyarivan, T. (2002).
  /// A Fast and Elitist Multiobjective Genetic Algorithm: NSGA-II.
  /// IEEE Transactions on Evolutionary Computation, 6(2), 182-197.
  /// </summary>
  /// <remarks>
  /// When there are plateaus in the fitness landscape several solutions might have exactly
  /// the same fitness vector. In this case parameter <paramref name="dominateOnEqualQualities"/>
  /// can be set to true to avoid plateaus becoming too attractive for the search process.
  /// </remarks>
  /// <param name="solutions">The solutions of the population.</param>
  /// <param name="objective"></param>
  /// <param name="dominateOnEqualQualities">Whether solutions of exactly equal quality should dominate one another.</param>
  /// <returns>The pareto front containing the best solutions and their associated quality resp. fitness.</returns>
  public static List<Solution<T>> CalculateBestParetoFront<T>(Solution<T>[] solutions, Objective objective, bool dominateOnEqualQualities = true) => CalculateBestFront(solutions, objective, solutions.Length, dominateOnEqualQualities, out _, out _, out _);

  /// <summary>
  /// Calculates all pareto fronts. The first in the list is the best front.
  /// The fast non-dominated sorting algorithm is used as described in
  /// Deb, K., Pratap, A., Agarwal, S., and Meyarivan, T. (2002).
  /// A Fast and Elitist Multiobjective Genetic Algorithm: NSGA-II.
  /// IEEE Transactions on Evolutionary Computation, 6(2), 182-197.
  /// </summary>
  /// <remarks>
  /// When there are plateaus in the fitness landscape several solutions might have exactly
  /// the same fitness vector. In this case parameter <paramref name="dominateOnEqualQualities"/>
  /// can be set to true to avoid plateaus becoming too attractive for the search process.
  /// </remarks>
  /// <param name="solutions">The solutions of the population.</param>
  /// <param name="objective"></param>
  /// <param name="rank">The rank of each of the solutions, corresponds to the front it is put in.</param>
  /// <param name="dominateOnEqualQualities">Whether solutions of exactly equal quality should dominate one another.</param>
  /// <returns>A sorted list of the pareto fronts from best to worst.</returns>
  public static List<List<Solution<T>>> CalculateAllParetoFronts<T>(Solution<T>[] solutions, Objective objective, out int[] rank, bool dominateOnEqualQualities = true) {
    var populationSize = solutions.Length;
    var fronts = new List<List<Solution<T>>>();
    if (solutions.Length == 0) {
      rank = [];
      return fronts;
    }

    fronts.Add(CalculateBestFront(solutions, objective, populationSize, dominateOnEqualQualities, out Dictionary<Solution<T>, List<int>> dominatedIndividuals, out int[] dominationCounter, out rank));
    while (fronts[^1].Count > 0) {
      var nextFront = new List<Solution<T>>();
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

  private static List<Solution<T>> CalculateBestFront<T>(Solution<T>[] solutions, Objective objective, int populationSize, bool dominateOnEquals, out Dictionary<Solution<T>, List<int>> dominatedIndividuals, out int[] dominationCounter, out int[] rank) {
    var front = new List<Solution<T>>();
    dominatedIndividuals = new Dictionary<Solution<T>, List<int>>();
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
