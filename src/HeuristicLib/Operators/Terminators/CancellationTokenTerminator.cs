namespace HEAL.HeuristicLib.Operators.Terminators;

public record CancellationTokenTerminator<TGenotype>(CancellationToken CancellationToken)
  : StatelessTerminator<TGenotype>
{
    public override bool IsTerminalState()
    {
        return CancellationToken.IsCancellationRequested;
    }
}
