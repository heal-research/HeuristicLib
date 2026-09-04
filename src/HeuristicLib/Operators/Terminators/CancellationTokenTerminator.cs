using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record CancellationTokenTerminator<TCandidate>(CancellationToken CancellationToken)
    : StatelessTerminator<TCandidate>
{
    public override bool IsTerminalState() => CancellationToken.IsCancellationRequested;
}

public static class CancellationTokenTerminator
{
    public static CancellationTokenTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, CancellationToken cancellationToken) => new(cancellationToken);
}
