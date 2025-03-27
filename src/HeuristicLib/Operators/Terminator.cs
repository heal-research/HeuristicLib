using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface ITerminator<in TState> 
  : IOperator
  where TState : IResultState
{
  bool ShouldTerminate(TState state);
}

public static class TerminatorExtensions {
  public static bool ShouldContinue<TState>(this ITerminator<TState> terminator, TState state) where TState : IResultState 
  {
    return !terminator.ShouldTerminate(state);
  }
}

public static class Terminator {
  public static CustomTerminator<TState> Create<TState>(Func<TState, bool> shouldTerminatePredicate) where TState : IResultState {
    return new CustomTerminator<TState>(shouldTerminatePredicate);
  }
  public static MaximumGenerationTerminator<TState> OnGeneration<TState>(int maxGenerations) where TState : IResultState {
    return new MaximumGenerationTerminator<TState>(maxGenerations);
  }
  public static MaximumExecutionTimeTerminator<TState> OnExecutionTime<TState>(TimeSpan maxTime) where TState : IResultState {
    return new MaximumExecutionTimeTerminator<TState>(maxTime);
  }
}

public sealed class CustomTerminator<TState> : ITerminator<TState> where TState : IResultState {
  private readonly Func<TState, bool> predicate;
  internal CustomTerminator(Func<TState, bool> predicate) {
    this.predicate = predicate;
  }
  public bool ShouldTerminate(TState state) => predicate(state);
}

public class MaximumGenerationTerminator<TState> : ITerminator<TState> where TState : IResultState {
  public int MaximumGeneration { get; }
  public MaximumGenerationTerminator(int maximumGeneration) {
    MaximumGeneration = maximumGeneration;
  }
  public bool ShouldTerminate(TState state) {
    return state.Generation >= MaximumGeneration;
  }
}
public class MaximumExecutionTimeTerminator<TState> : ITerminator<TState> where TState : IResultState {
  public TimeSpan MaximumExecutionTime { get; }
  public MaximumExecutionTimeTerminator(TimeSpan maximumExecutionTime) {
    MaximumExecutionTime = maximumExecutionTime;
  }
  public bool ShouldTerminate(TState state) {
    return state.TotalDuration >= MaximumExecutionTime;
  }
}

public class PauseToken {
  public bool IsPaused { get; private set; }
  public void RequestPause() => IsPaused = true;
}

public class PauseTokenTerminator<TState> : ITerminator<TState> where TState : IResultState {
  public PauseToken Token { get; }
  public PauseTokenTerminator(PauseToken pauseToken) {
    Token = pauseToken;
  }
  public bool ShouldTerminate(TState state) => Token.IsPaused;
}

public class AnyTerminator<TState> : ITerminator<TState> where TState : IResultState {
  public IReadOnlyList<ITerminator<TState>> Terminators { get; }
  public AnyTerminator(IReadOnlyList<ITerminator<TState>> terminators) {
    Terminators = terminators;
  }
  public bool ShouldTerminate(TState state) {
    return Terminators.Any(criterion => criterion.ShouldTerminate(state));
  }
}

public class AllTerminator<TState> : ITerminator<TState> where TState : IResultState {
  public IReadOnlyList<ITerminator<TState>> Terminators { get; }
  public AllTerminator(IReadOnlyList<ITerminator<TState>> terminators) {
    Terminators = terminators;
  }
  public bool ShouldTerminate(TState state) {
    return Terminators.All(criterion => criterion.ShouldTerminate(state));
  }
}
