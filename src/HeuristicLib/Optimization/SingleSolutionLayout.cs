using System.Collections;

namespace HEAL.HeuristicLib.Optimization;

public record SingleSolutionLayout<TCandidate>(EvaluatedCandidate<TCandidate> EvaluatedCandidate) : ISolutionLayout<TCandidate>
{
    public IEnumerator<EvaluatedCandidate<TCandidate>> GetEnumerator()
    {
        yield return EvaluatedCandidate;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
