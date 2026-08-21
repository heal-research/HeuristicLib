using System.Collections;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Algorithms;

public static class Population
{
    public static Population<TCandidate> From<TCandidate>(IEnumerable<TCandidate> candidates, IEnumerable<ObjectiveVector> fitnesses) => new([.. candidates.Zip(fitnesses, EvaluatedCandidate.From)]);

    public static Population<TCandidate> From<TCandidate>(IEnumerable<EvaluatedCandidate<TCandidate>> evaluatedCandidates) => new([.. evaluatedCandidates]);
}

public record Population<TCandidate> : IEnumerable<EvaluatedCandidate<TCandidate>>
{
    public ValueArray<EvaluatedCandidate<TCandidate>> EvaluatedCandidates { get; init; }

    public IEnumerable<TCandidate> Candidates => EvaluatedCandidates.Select(x => x.Candidate);

    public Population(params IReadOnlyList<EvaluatedCandidate<TCandidate>> evaluatedCandidates)
    {
        EvaluatedCandidates = evaluatedCandidates.ToValueArray();
    }

    public IEnumerator<EvaluatedCandidate<TCandidate>> GetEnumerator() =>
        ((IEnumerable<EvaluatedCandidate<TCandidate>>)EvaluatedCandidates).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
