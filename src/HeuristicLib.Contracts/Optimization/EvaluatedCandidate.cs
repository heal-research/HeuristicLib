namespace HEAL.HeuristicLib.Optimization;

public record EvaluatedCandidate<TCandidate>(TCandidate Candidate, ObjectiveVector ObjectiveVector);

public static class EvaluatedCandidate
{
    public static EvaluatedCandidate<TCandidate>
        From<TCandidate>(TCandidate candidate, ObjectiveVector objectiveVector) =>
        new(candidate, objectiveVector);

    public static EvaluatedCandidate<TCandidate> ToEvaluated<TCandidate>(
        this TCandidate candidate, ObjectiveVector objectiveVector) =>
        new(candidate, objectiveVector);
}
