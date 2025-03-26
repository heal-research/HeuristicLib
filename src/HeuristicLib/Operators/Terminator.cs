using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface ITerminatorOperator<in TState> : IExecutableOperator {
  bool ShouldTerminate(TState state);
  bool ShouldContinue(TState state) => !ShouldTerminate(state);
}

public static class TerminatorOperator {
  public static TerminatorOperator<TState> Create<TState>(Func<TState, bool> shouldTerminatePredicate) => new TerminatorOperator<TState>(shouldTerminatePredicate);
  public static ThresholdTerminatorOperator<IGenerationalState> OnGeneration(int maxGenerations) => new ThresholdTerminatorOperator<IGenerationalState>(maxGenerations, state => state.Generation);
  
  public static ITerminatorOperator<object> OnExecutionTime(TimeSpan time) => throw new NotImplementedException();
  public static ITerminatorOperator<object> OnExecutionTime(int milliseconds) => throw new NotImplementedException();
}

public sealed class TerminatorOperator<TState> : ITerminatorOperator<TState> {
  private readonly Func<TState, bool> predicate;
  internal TerminatorOperator(Func<TState, bool> predicate) {
    this.predicate = predicate;
  }
  public bool ShouldTerminate(TState state) => predicate(state);
}

public class ThresholdTerminatorOperator<TState> : ITerminatorOperator<TState> {
  public int Threshold { get; }
  public Func<TState, int> Accessor { get; }
  public ThresholdTerminatorOperator(int threshold, Func<TState, int> accessor) {
    Threshold = threshold;
    Accessor = accessor;
  }
  public bool ShouldTerminate(TState state) {
    return Accessor(state) >= Threshold;
  }
}

public class PauseToken {
  public bool IsPaused { get; private set; }
  public void RequestPause() => IsPaused = true;
}

public class PauseTokenTerminatorOperator<TState> : ITerminatorOperator<TState>
{
  public PauseTokenTerminatorOperator(PauseToken pauseToken) {
    Token = pauseToken;
  }

  public PauseToken Token { get; }

  public bool ShouldTerminate(TState state) => Token.IsPaused;
}

public class AnyTerminatorOperator<TState> : ITerminatorOperator<TState> {
  public IReadOnlyList<ITerminatorOperator<TState>> Terminators { get; }
  public AnyTerminatorOperator(IReadOnlyList<ITerminatorOperator<TState>> terminators) {
    Terminators = terminators;
  }
  public bool ShouldTerminate(TState state) {
    return Terminators.Any(criterion => criterion.ShouldTerminate(state));
  }
}

public class AllTerminatorOperator<TState> : ITerminatorOperator<TState> {
  public IReadOnlyList<ITerminatorOperator<TState>> Terminators { get; }
  public AllTerminatorOperator(IReadOnlyList<ITerminatorOperator<TState>> terminators) {
    Terminators = terminators;
  }
  public bool ShouldTerminate(TState state) {
    return Terminators.All(criterion => criterion.ShouldTerminate(state));
  }
}
