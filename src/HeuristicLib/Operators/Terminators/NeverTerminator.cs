namespace HEAL.HeuristicLib.Operators.Terminators;

public record NeverTerminator<TCandidate>
  : StatelessTerminator<TCandidate>
{
    public override bool IsTerminalState() => NeverTerminator.IsTerminalState();
}

public static class NeverTerminator
{
    public static bool IsTerminalState() => false;
}
