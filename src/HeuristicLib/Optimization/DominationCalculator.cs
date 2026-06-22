namespace HEAL.HeuristicLib.Optimization;

public static class DominationCalculator
{
    /// <summary>
    ///   Calculates the best pareto front only. The fast non-dominated sorting algorithm is used
    ///   as described in Deb, K., Pratap, A., Agarwal, S., and Meyarivan, T. (2002).
    ///   A Fast and Elitist Multiobjective Genetic Algorithm: NSGA-II.
    ///   IEEE Transactions on Evolutionary Computation, 6(2), 182-197.
    /// </summary>
    /// <remarks>
    ///   When there are plateaus in the fitness landscape several ISolutions might have exactly
    ///   the same fitness vector. In this case parameter <paramref name="dominateOnEqualQualities" />
    ///   can be set to true to avoid plateaus becoming too attractive for the search process.
    /// </remarks>
    /// <param name="solutions">The ISolutions of the population.</param>
    /// <param name="objective"></param>
    /// <param name="dominateOnEqualQualities">Whether ISolutions of exactly equal quality should dominate one another.</param>
    /// <returns>The pareto front containing the best ISolutions and their associated quality resp. fitness.</returns>
    public static List<Solution<T>> CalculateBestParetoFront<T>(IReadOnlyList<Solution<T>> solutions, Objective objective, bool dominateOnEqualQualities = true) => CalculateBestFront(solutions, objective, solutions.Count, dominateOnEqualQualities, out _, out _, out _);

    /// <summary>
    ///   Calculates all pareto fronts. The first in the list is the best front.
    ///   The fast non-dominated sorting algorithm is used as described in
    ///   Deb, K., Pratap, A., Agarwal, S., and Meyarivan, T. (2002).
    ///   A Fast and Elitist Multiobjective Genetic Algorithm: NSGA-II.
    ///   IEEE Transactions on Evolutionary Computation, 6(2), 182-197.
    /// </summary>
    /// <remarks>
    ///   When there are plateaus in the fitness landscape several ISolutions might have exactly
    ///   the same fitness vector. In this case parameter <paramref name="dominateOnEqualQualities" />
    ///   can be set to true to avoid plateaus becoming too attractive for the search process.
    /// </remarks>
    /// <param name="solutions">The ISolutions of the population.</param>
    /// <param name="objective"></param>
    /// <param name="rank">The rank of each of the ISolutions, corresponds to the front it is put in.</param>
    /// <param name="dominateOnEqualQualities">Whether ISolutions of exactly equal quality should dominate one another.</param>
    /// <returns>A sorted list of the pareto fronts from best to worst.</returns>
    public static List<List<Solution<T>>> CalculateAllParetoFronts<T>(IReadOnlyList<Solution<T>> solutions, Objective objective, out int[] rank, bool dominateOnEqualQualities = true)
    {
        var populationSize = solutions.Count;
        var fronts = new List<List<Solution<T>>>();
        if (solutions.Count == 0)
        {
            rank = [];
            return fronts;
        }

        fronts.Add(CalculateBestFront(solutions, objective, populationSize, dominateOnEqualQualities, out var dominatedIndividuals, out var dominationCounter, out rank));
        while (fronts[^1].Count > 0)
        {
            var nextFront = new List<Solution<T>>();
            foreach (var p in fronts[^1])
            {
                if (!dominatedIndividuals.TryGetValue(p, out var dominatedIndividualsByp))
                {
                    continue;
                }

                foreach (var dominatedIndividual in dominatedIndividualsByp)
                {
                    if (--dominationCounter[dominatedIndividual] != 0)
                    {
                        continue;
                    }

                    rank[dominatedIndividual] = fronts.Count;
                    nextFront.Add(solutions[dominatedIndividual]);
                }
            }

            if (nextFront.Count == 0)
            {
                break;
            }

            fronts.Add(nextFront);
        }

        return fronts;
    }

    private static List<Solution<T>> CalculateBestFront<T>(IReadOnlyList<Solution<T>> solutions, Objective objective, int populationSize, bool dominateOnEquals, out Dictionary<Solution<T>, List<int>> dominatedIndividuals, out int[] dominationCounter, out int[] rank)
    {
        var front = new List<Solution<T>>();
        dominatedIndividuals = new Dictionary<Solution<T>, List<int>>(ReferenceEqualityComparer.Instance);
        dominationCounter = new int[populationSize];
        rank = new int[populationSize];
        for (var pI = 0; pI < populationSize - 1; pI++)
        {
            var p = solutions[pI];
            if (!dominatedIndividuals.TryGetValue(p, out var dominatedIndividualsByp))
            {
                dominatedIndividuals[p] = dominatedIndividualsByp = [];
            }

            for (var qI = pI + 1; qI < populationSize; qI++)
            {
                var test = solutions[pI].ObjectiveVector.CompareTo(solutions[qI].ObjectiveVector, objective); // Dominates(qualities[pI], qualities[qI], maximization, dominateOnEqualQualities);
                if (test == DominanceRelation.Equivalent)
                {
                    test = dominateOnEquals ? DominanceRelation.Dominates : DominanceRelation.Incomparable;
                }

                switch (test)
                {
                    case DominanceRelation.Dominates:
                        dominatedIndividualsByp.Add(qI);
                        dominationCounter[qI] += 1;
                        break;
                    case DominanceRelation.IsDominatedBy:
                        {
                            dominationCounter[pI] += 1;
                            if (!dominatedIndividuals.ContainsKey(solutions[qI]))
                            {
                                dominatedIndividuals.Add(solutions[qI], []);
                            }

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
                    || dominationCounter[qI] != 0)
                {
                    continue;
                }

                rank[qI] = 0;
                front.Add(solutions[qI]);
            }

            if (dominationCounter[pI] != 0)
            {
                continue;
            }

            rank[pI] = 0;
            front.Add(p);
        }

        return front;
    }

    public static List<Solution<T>> AddToParetoFront<T>(
      IReadOnlyList<Solution<T>> front,
      Solution<T> solution,
      Objective objective,
      bool dominateOnEqualQualities = true)
    {
        var result = new List<Solution<T>>(front.Count + 1);
        var isDominated = false;

        foreach (var existing in front)
        {
            var relation = solution.ObjectiveVector.CompareTo(existing.ObjectiveVector, objective);

            if (relation == DominanceRelation.Equivalent)
            {
                relation = dominateOnEqualQualities
                  ? DominanceRelation.Dominates
                  : DominanceRelation.Incomparable;
            }

            switch (relation)
            {
                case DominanceRelation.Dominates:
                    // New solution dominates the existing one, so skip existing.
                    break;

                case DominanceRelation.IsDominatedBy:
                    // Existing solution dominates the new one, so new solution must not be added.
                    isDominated = true;
                    result.Add(existing);
                    break;

                case DominanceRelation.Incomparable:
                    result.Add(existing);
                    break;

                case DominanceRelation.Equivalent:
                default:
                    throw new InvalidOperationException("Encountered invalid dominance relation");
            }
        }

        if (!isDominated)
        {
            result.Add(solution);
        }

        return result;
    }

    public static bool TryAddToParetoFrontInPlace<T>(
      IList<Solution<T>> front,
      Solution<T> solution,
      Objective objective,
      bool dominateOnEqualQualities = true)
    {
        for (var i = front.Count - 1; i >= 0; i--)
        {
            var relation = solution.ObjectiveVector.CompareTo(front[i].ObjectiveVector, objective);

            if (relation == DominanceRelation.Equivalent)
            {
                relation = dominateOnEqualQualities
                  ? DominanceRelation.Dominates
                  : DominanceRelation.Incomparable;
            }

            switch (relation)
            {
                case DominanceRelation.Dominates:
                    front.RemoveAt(i);
                    break;

                case DominanceRelation.IsDominatedBy:
                    return false;

                case DominanceRelation.Incomparable:
                    break;

                case DominanceRelation.Equivalent:
                default:
                    throw new InvalidOperationException("Encountered invalid dominance relation");
            }
        }

        front.Add(solution);
        return true;
    }
}
