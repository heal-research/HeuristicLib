namespace HEAL.HeuristicLib.Objectives;

public static class DominationCalculator
{
    /// <summary>
    ///   Calculates the best pareto front only. The fast non-dominated sorting algorithm is used
    ///   as described in Deb, K., Pratap, A., Agarwal, S., and Meyarivan, T. (2002).
    ///   A Fast and Elitist Multiobjective Genetic Algorithm: NSGA-II.
    ///   IEEE Transactions on Evolutionary Computation, 6(2), 182-197.
    /// </summary>
    /// <remarks>
    ///   When there are plateaus in the objective landscape several evaluated candidates might have exactly
    ///   the same objective vector. In this case parameter <paramref name="dominateOnEqualQualities" />
    ///   can be set to true to avoid plateaus becoming too attractive for the search process.
    /// </remarks>
    /// <param name="evaluatedCandidates">The evaluated candidates of the population.</param>
    /// <param name="objective">The problem's objective directions.</param>
    /// <param name="dominateOnEqualQualities">Whether evaluated candidates with exactly equal objective vectors should dominate one another.</param>
    /// <returns>The pareto front containing the best evaluated candidates.</returns>
    public static List<EvaluatedCandidate<T>> CalculateBestParetoFront<T>(IReadOnlyList<EvaluatedCandidate<T>> evaluatedCandidates, ObjectiveDirections objective, bool dominateOnEqualQualities = true) => CalculateBestFront(evaluatedCandidates, objective, evaluatedCandidates.Count, dominateOnEqualQualities, out _, out _, out _);

    /// <summary>
    ///   Calculates all pareto fronts. The first in the list is the best front.
    ///   The fast non-dominated sorting algorithm is used as described in
    ///   Deb, K., Pratap, A., Agarwal, S., and Meyarivan, T. (2002).
    ///   A Fast and Elitist Multiobjective Genetic Algorithm: NSGA-II.
    ///   IEEE Transactions on Evolutionary Computation, 6(2), 182-197.
    /// </summary>
    /// <remarks>
    ///   When there are plateaus in the objective landscape several evaluated candidates might have exactly
    ///   the same objective vector. In this case parameter <paramref name="dominateOnEqualQualities" />
    ///   can be set to true to avoid plateaus becoming too attractive for the search process.
    /// </remarks>
    /// <param name="evaluatedCandidates">The evaluated candidates of the population.</param>
    /// <param name="objective">The problem's objective directions.</param>
    /// <param name="rank">The rank of each evaluated candidate, corresponding to the front it is put in.</param>
    /// <param name="dominateOnEqualQualities">Whether evaluated candidates with exactly equal objective vectors should dominate one another.</param>
    /// <returns>A sorted list of the pareto fronts from best to worst.</returns>
    public static List<List<EvaluatedCandidate<T>>> CalculateAllParetoFronts<T>(IReadOnlyList<EvaluatedCandidate<T>> evaluatedCandidates, ObjectiveDirections objective, out int[] rank, bool dominateOnEqualQualities = true)
    {
        var populationSize = evaluatedCandidates.Count;
        var fronts = new List<List<EvaluatedCandidate<T>>>();
        if (evaluatedCandidates.Count == 0)
        {
            rank = [];
            return fronts;
        }

        fronts.Add(CalculateBestFront(evaluatedCandidates, objective, populationSize, dominateOnEqualQualities, out var dominatedIndividuals, out var dominationCounter, out rank));
        while (fronts[^1].Count > 0)
        {
            var nextFront = new List<EvaluatedCandidate<T>>();
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
                    nextFront.Add(evaluatedCandidates[dominatedIndividual]);
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

    private static List<EvaluatedCandidate<T>> CalculateBestFront<T>(IReadOnlyList<EvaluatedCandidate<T>> evaluatedCandidates, ObjectiveDirections objective, int populationSize, bool dominateOnEquals, out Dictionary<EvaluatedCandidate<T>, List<int>> dominatedIndividuals, out int[] dominationCounter, out int[] rank)
    {
        var front = new List<EvaluatedCandidate<T>>();
        dominatedIndividuals = new Dictionary<EvaluatedCandidate<T>, List<int>>(ReferenceEqualityComparer.Instance);
        dominationCounter = new int[populationSize];
        rank = new int[populationSize];
        for (var pI = 0; pI < populationSize - 1; pI++)
        {
            var p = evaluatedCandidates[pI];
            if (!dominatedIndividuals.TryGetValue(p, out var dominatedIndividualsByp))
            {
                dominatedIndividuals[p] = dominatedIndividualsByp = [];
            }

            for (var qI = pI + 1; qI < populationSize; qI++)
            {
                var test = evaluatedCandidates[pI].ObjectiveVector.CompareTo(evaluatedCandidates[qI].ObjectiveVector, objective);
                if (test == DominanceRelation.Equal)
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
                            if (!dominatedIndividuals.ContainsKey(evaluatedCandidates[qI]))
                            {
                                dominatedIndividuals.Add(evaluatedCandidates[qI], []);
                            }

                            dominatedIndividuals[evaluatedCandidates[qI]].Add(pI);
                            break;
                        }
                    case DominanceRelation.Incomparable:
                        break;
                    case DominanceRelation.Equal:
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
                front.Add(evaluatedCandidates[qI]);
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

    public static List<EvaluatedCandidate<T>> AddToParetoFront<T>(
      IReadOnlyList<EvaluatedCandidate<T>> front,
      EvaluatedCandidate<T> evaluatedCandidate,
      ObjectiveDirections objective,
      bool dominateOnEqualQualities = true)
    {
        var result = new List<EvaluatedCandidate<T>>(front.Count + 1);
        var isDominated = false;

        foreach (var existing in front)
        {
            var relation = evaluatedCandidate.ObjectiveVector.CompareTo(existing.ObjectiveVector, objective);

            if (relation == DominanceRelation.Equal)
            {
                relation = dominateOnEqualQualities
                    ? DominanceRelation.Dominates
                    : DominanceRelation.Incomparable;
            }

            switch (relation)
            {
                case DominanceRelation.Dominates:
                    // New evaluated candidate dominates the existing one, so skip existing.
                    break;

                case DominanceRelation.IsDominatedBy:
                    // Existing evaluated candidate dominates the new one, so it must not be added.
                    isDominated = true;
                    result.Add(existing);
                    break;

                case DominanceRelation.Incomparable:
                    result.Add(existing);
                    break;

                case DominanceRelation.Equal:
                default:
                    throw new InvalidOperationException("Encountered invalid dominance relation");
            }
        }

        if (!isDominated)
        {
            result.Add(evaluatedCandidate);
        }

        return result;
    }

    public static bool TryAddToParetoFrontInPlace<T>(
      IList<EvaluatedCandidate<T>> front,
      EvaluatedCandidate<T> evaluatedCandidate,
      ObjectiveDirections objective,
      bool dominateOnEqualQualities = true)
    {
        for (var i = front.Count - 1; i >= 0; i--)
        {
            var relation = evaluatedCandidate.ObjectiveVector.CompareTo(front[i].ObjectiveVector, objective);

            if (relation == DominanceRelation.Equal)
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

                case DominanceRelation.Equal:
                default:
                    throw new InvalidOperationException("Encountered invalid dominance relation");
            }
        }

        front.Add(evaluatedCandidate);
        return true;
    }
}
