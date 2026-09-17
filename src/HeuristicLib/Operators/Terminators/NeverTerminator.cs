using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record NeverTerminator<TCandidate>
    : StatelessTerminator<TCandidate>
{
    public override bool IsTerminalState() => NeverTerminator.IsTerminalState();
}

public static class NeverTerminator
{
    public static NeverTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => new();

#pragma warning disable S3400
    public static bool IsTerminalState() => false;
#pragma warning restore S3400
}
