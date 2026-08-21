using System.Collections;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Algorithms;

public record SingleSolutionLayout<TCandidate>(EvaluatedCandidate<TCandidate> EvaluatedCandidate) : IEnumerable<EvaluatedCandidate<TCandidate>>
{
    public IEnumerator<EvaluatedCandidate<TCandidate>> GetEnumerator()
    {
        yield return EvaluatedCandidate;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
