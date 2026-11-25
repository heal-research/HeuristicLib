using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Algorithms;

public static class Population
{
    public static Population<TCandidate> From<TCandidate>(IEnumerable<TCandidate> candidates, IEnumerable<ObjectiveVector> fitnesses) => new([.. candidates.Zip(fitnesses, EvaluatedCandidate.From)]);

    public static Population<TCandidate> From<TCandidate>(IEnumerable<EvaluatedCandidate<TCandidate>> evaluatedCandidates) => new([.. evaluatedCandidates]);
}

public record Population<TCandidate>
{
    public ValueArray<EvaluatedCandidate<TCandidate>> EvaluatedCandidates { get; init; }

    public ValueArray<TCandidate> Candidates => EvaluatedCandidates.Select(x => x.Candidate).ToValueArray();

    public Population(params IReadOnlyList<EvaluatedCandidate<TCandidate>> evaluatedCandidates)
    {
        EvaluatedCandidates = evaluatedCandidates.ToValueArray();
    }
}
