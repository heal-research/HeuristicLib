namespace HEAL.HeuristicLib.Optimization;

public record EvaluatedCandidate<TCandidate>(TCandidate Candidate, ObjectiveVector ObjectiveVector);

public static class EvaluatedCandidate
{
    public static EvaluatedCandidate<TCandidate> From<TCandidate>(TCandidate candidate, ObjectiveVector objectiveVector) =>
        new(candidate, objectiveVector);

    extension<TCandidate>(TCandidate candidate)
    {
        public EvaluatedCandidate<TCandidate> ToEvaluated(ObjectiveVector objectiveVector) => new(candidate, objectiveVector);
    }

    extension<TCandidate>(IReadOnlyList<TCandidate> candidates)
    {
        /// <summary>
        /// Pairs candidates with the objective vectors an evaluator returned for them. Evaluators return objective vectors
        /// positionally aligned with their input candidates, so this is the ordinary way to build a population from an
        /// evaluation result.
        /// </summary>
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> ToEvaluated(IReadOnlyList<ObjectiveVector> objectiveVectors)
        {
            if (candidates.Count != objectiveVectors.Count)
                throw new ArgumentException($"Expected {candidates.Count} objective vectors but received {objectiveVectors.Count}.", nameof(objectiveVectors));

            var evaluatedCandidates = new EvaluatedCandidate<TCandidate>[candidates.Count];
            for (var i = 0; i < evaluatedCandidates.Length; i++)
                evaluatedCandidates[i] = new EvaluatedCandidate<TCandidate>(candidates[i], objectiveVectors[i]);

            return evaluatedCandidates;
        }
    }
}
