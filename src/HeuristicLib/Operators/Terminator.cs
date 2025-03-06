namespace HEAL.HeuristicLib.Operators;

public interface ITerminator<in TState> {
  bool ShouldTerminate(TState state);
  bool ShouldContinue(TState state) => !ShouldTerminate(state);
}

public class ThresholdTerminator<TState> : ITerminator<TState> {
  public ThresholdTerminator(int threshold, Func<TState, int> accessor) {
    Threshold = threshold;
    Accessor = accessor;
  }

  public int Threshold { get; }
  public Func<TState, int> Accessor { get; }

  public bool ShouldTerminate(TState state) {
    return Accessor(state) >= Threshold;
  }
}

public class PauseToken {
  public bool IsPaused { get; private set; }
  public void RequestPause() => IsPaused = true;
}

public class PauseTokenTerminator<TState> : ITerminator<TState>
{
  public PauseTokenTerminator(PauseToken pauseToken) {
    Token = pauseToken;
  }

  public PauseToken Token { get; }

  public bool ShouldTerminate(TState state) => Token.IsPaused;
}

public class AnyTerminator<TState> : ITerminator<TState>
{
  public AnyTerminator(IReadOnlyList<ITerminator<TState>> criteria) {
    this.Criteria = criteria;
  }

  public IReadOnlyList<ITerminator<TState>> Criteria { get; }

  public bool ShouldTerminate(TState state) {
    return Criteria.Any(criterion => criterion.ShouldTerminate(state));
  }
}
