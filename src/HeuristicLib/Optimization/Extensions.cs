namespace HEAL.HeuristicLib.Optimization;

public static class Extensions
{
    // ToDo: pack the extensions to the actual classes they belong to, not in a global extensions class.
    extension<TCandidate>(IReadOnlyList<TCandidate> parents)
    {
        public IReadOnlyList<IParents<TCandidate>> ToParentPairs()
        {
            var offspringCount = parents.Count / 2;
            var parentPairs = new IParents<TCandidate>[offspringCount];
            for (int i = 0, j = 0; i < offspringCount; i++, j += 2)
            {
                var p1 = parents[j];
                var p2 = parents[j + 1];
                parentPairs[i] = new Parents<TCandidate>(p1, p2);
            }

            return parentPairs;
        }
    }

    extension<TCandidate>(IReadOnlyList<EvaluatedCandidate<TCandidate>> parents)
    {
        public IParents<TCandidate>[] ToParents(ObjectiveDirections? objective = null)
        {
            var offspringCount = parents.Count / 2;
            var parentPairs = new IParents<TCandidate>[offspringCount];
            for (int i = 0, j = 0; i < offspringCount; i++, j += 2)
            {
                var p1 = parents[j];
                var p2 = parents[j + 1];
                if (objective is not null
                    && objective.TotalOrderComparer is not NoTotalOrderComparer
                    && objective.TotalOrderComparer.Compare(p1.ObjectiveVector, p2.ObjectiveVector) > 0)
                    (p1, p2) = (p2, p1);
                parentPairs[i] = new Parents<TCandidate>(p1.Candidate, p2.Candidate);
            }

            return parentPairs;
        }

        public (EvaluatedCandidate<TCandidate>, EvaluatedCandidate<TCandidate>)[] ToSolutionPairs()
        {
            var offspringCount = parents.Count / 2;
            var parentPairs = new (EvaluatedCandidate<TCandidate>, EvaluatedCandidate<TCandidate>)[offspringCount];
            for (int i = 0, j = 0; i < offspringCount; i++, j += 2)
            {
                parentPairs[i] = (parents[j], parents[j + 1]);
            }

            return parentPairs;
        }
    }

    public static bool IsAlmost(this double a, double b, double tolerance = 1E-10) => Math.Abs(a - b) <= tolerance;

    // Convenience overload using default comparer for T2

    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
    {
        if (dict.TryGetValue(key, out var v))
        {
            return v;
        }

        dict[key] = defaultValue;

        return defaultValue;
    }
}
