namespace HEAL.HeuristicLib.Operators.Terminators;

public record NeverTerminator<TGenotype>
  : StatelessTerminator<TGenotype>
{
    public override bool IsTerminalState() => NeverTerminator.IsTerminalState();
}

public static class NeverTerminator
{
    public static bool IsTerminalState() => false;
}
