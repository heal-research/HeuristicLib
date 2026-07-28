using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

public record CancellationTokenTerminator<TCandidate>(CancellationToken CancellationToken)
  : StatelessTerminator<TCandidate>
{
    public override bool IsTerminalState()
    {
        return CancellationToken.IsCancellationRequested;
    }
}

public static class CancellationTokenTerminator
{
    public static CancellationTokenTerminator<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem, CancellationToken cancellationToken)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new(cancellationToken);
}
