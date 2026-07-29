namespace HEAL.HeuristicLib.Optimization;

public readonly record struct Parents<T>(T Parent1, T Parent2);

public static class Parents
{
    public static Parents<T> From<T>(T parent1, T parent2) => new(parent1, parent2);
}

public static class ParentsExtensions
{
    extension<T>((T Parent1, T Parent2) parents)
    {
        public Parents<T> ToParents() => Parents.From(parents.Parent1, parents.Parent2);
    }

    extension<TCandidate>(IReadOnlyList<TCandidate> parents)
    {
        public IReadOnlyList<Parents<TCandidate>> ToParentPairs()
        {
            var offspringCount = parents.Count / 2;
            var parentPairs = new Parents<TCandidate>[offspringCount];
            for (int i = 0, j = 0; i < offspringCount; i++, j += 2)
            {
                var p1 = parents[j];
                var p2 = parents[j + 1];
                parentPairs[i] = Parents.From(p1, p2);
            }

            return parentPairs;
        }
    }

    extension<TCandidate>(IReadOnlyList<EvaluatedCandidate<TCandidate>> parents)
    {
        public Parents<TCandidate>[] ToParents(ObjectiveDirections? objective = null)
        {
            var offspringCount = parents.Count / 2;
            var parentPairs = new Parents<TCandidate>[offspringCount];
            for (int i = 0, j = 0; i < offspringCount; i++, j += 2)
            {
                var p1 = parents[j];
                var p2 = parents[j + 1];
                if (objective is not null
                    && objective.TotalOrderComparer is not NoTotalOrderComparer
                    && objective.TotalOrderComparer.Compare(p1.ObjectiveVector, p2.ObjectiveVector) > 0)
                    (p1, p2) = (p2, p1);
                parentPairs[i] = Parents.From(p1.Candidate, p2.Candidate);
            }

            return parentPairs;
        }

        public Parents<EvaluatedCandidate<TCandidate>>[] ToEvaluatedCandidatesPairs()
        {
            var offspringCount = parents.Count / 2;
            var parentPairs = new Parents<EvaluatedCandidate<TCandidate>>[offspringCount];
            for (int i = 0, j = 0; i < offspringCount; i++, j += 2)
            {
                parentPairs[i] = Parents.From(parents[j], parents[j + 1]);
            }

            return parentPairs;
        }
    }
}
