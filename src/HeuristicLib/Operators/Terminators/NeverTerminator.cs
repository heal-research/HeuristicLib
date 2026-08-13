using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

public sealed record NeverTerminator<TCandidate>
  : StatelessTerminator<TCandidate>
{
    public override bool IsTerminalState() => NeverTerminator.IsTerminalState();
}

public static class NeverTerminator
{
    public static NeverTerminator<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new();

#pragma warning disable S3400
    public static bool IsTerminalState() => false;
#pragma warning restore S3400
}
