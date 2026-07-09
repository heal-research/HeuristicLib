namespace HEAL.HeuristicLib.Operators.Terminators;

public record CancellationTokenTerminator<TCandidate>(CancellationToken CancellationToken)
  : StatelessTerminator<TCandidate>
{
    public override bool IsTerminalState()
    {
        return CancellationToken.IsCancellationRequested;
    }
}
