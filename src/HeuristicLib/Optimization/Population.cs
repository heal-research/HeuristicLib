using System.Collections;
using Generator.Equals;

namespace HEAL.HeuristicLib.Optimization;

public static class Population
{
    public static Population<TCandidate> From<TCandidate>(IEnumerable<TCandidate> candidates, IEnumerable<ObjectiveVector> fitnesses) => new([.. candidates.Zip(fitnesses, EvaluatedCandidate.From)]);

    public static Population<TCandidate> From<TCandidate>(IEnumerable<EvaluatedCandidate<TCandidate>> solutions) => new([.. solutions]);
}

[Equatable]
public partial record Population<TCandidate> : ISolutionLayout<TCandidate>
{
    [OrderedEquality]
    public ImmutableArray<EvaluatedCandidate<TCandidate>> EvaluatedCandidates { get; init; }

    public IEnumerable<TCandidate> Candidates => EvaluatedCandidates.Select(x => x.Candidate);

    public Population(params IReadOnlyList<EvaluatedCandidate<TCandidate>> solutions)
    {
        EvaluatedCandidates = solutions.ToImmutableArray();
    }

    public IEnumerator<EvaluatedCandidate<TCandidate>> GetEnumerator() => EvaluatedCandidates.AsReadOnly().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
