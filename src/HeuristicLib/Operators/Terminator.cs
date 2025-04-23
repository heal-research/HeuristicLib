using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface ITerminator<in TIterationResult> 
  : IOperator
{
  bool ShouldTerminate(TIterationResult iterationResult);
}

public static class TerminatorExtensions {
  public static bool ShouldContinue<TIterationResult>(this ITerminator<TIterationResult> terminator, TIterationResult iterationResult) where TIterationResult : IIterationResult 
  {
    return !terminator.ShouldTerminate(iterationResult);
  }
}

public static class Terminator {
  public static CustomTerminator<TIterationResult> Create<TIterationResult>(Func<TIterationResult, bool> shouldTerminatePredicate) where TIterationResult : IIterationResult {
    return new CustomTerminator<TIterationResult>(shouldTerminatePredicate);
  }
  public static MaximumGenerationTerminator<TIterationResult> OnGeneration<TIterationResult>(int maxGenerations) where TIterationResult : IIterationResult {
    return new MaximumGenerationTerminator<TIterationResult>(maxGenerations);
  }
  public static MaximumExecutionTimeTerminator<TIterationResult> OnExecutionTime<TIterationResult>(TimeSpan maxTime) where TIterationResult : IIterationResult {
    return new MaximumExecutionTimeTerminator<TIterationResult>(maxTime);
  }
  public static NeverTerminator<TIterationResult> NeverTerminate<TIterationResult>() where TIterationResult : IIterationResult {
    return new NeverTerminator<TIterationResult>();
  }
}

public sealed class CustomTerminator<TIterationResult> : ITerminator<TIterationResult> where TIterationResult : IIterationResult {
  private readonly Func<TIterationResult, bool> predicate;
  internal CustomTerminator(Func<TIterationResult, bool> predicate) {
    this.predicate = predicate;
  }
  public bool ShouldTerminate(TIterationResult iterationResult) => predicate(iterationResult);
}

public class MaximumGenerationTerminator<TIterationResult> : ITerminator<TIterationResult> where TIterationResult : IIterationResult {
  public int MaximumGeneration { get; }
  public MaximumGenerationTerminator(int maximumGeneration) {
    MaximumGeneration = maximumGeneration;
  }
  public bool ShouldTerminate(TIterationResult iterationResult) {
    return iterationResult.Iteration >= MaximumGeneration;
  }
}
public class MaximumExecutionTimeTerminator<TIterationResult> : ITerminator<TIterationResult> where TIterationResult : IIterationResult {
  public TimeSpan MaximumExecutionTime { get; }
  public MaximumExecutionTimeTerminator(TimeSpan maximumExecutionTime) {
    MaximumExecutionTime = maximumExecutionTime;
  }
  public bool ShouldTerminate(TIterationResult iterationResult) {
    return iterationResult.TotalDuration >= MaximumExecutionTime;
  }
}

public class PauseToken {
  public bool IsPaused { get; private set; }
  public void RequestPause() => IsPaused = true;
}

public class PauseTokenTerminator<TIterationResult> : ITerminator<TIterationResult> where TIterationResult : IIterationResult {
  public PauseToken Token { get; }
  public PauseTokenTerminator(PauseToken pauseToken) {
    Token = pauseToken;
  }
  public bool ShouldTerminate(TIterationResult iterationResult) => Token.IsPaused;
}

public class NeverTerminator<TIterationResult> : ITerminator<TIterationResult> where TIterationResult : IIterationResult {
  public bool ShouldTerminate(TIterationResult iterationResult) => false;
}

public class AnyTerminator<TIterationResult> : ITerminator<TIterationResult> where TIterationResult : IIterationResult {
  public IReadOnlyList<ITerminator<TIterationResult>> Terminators { get; }
  public AnyTerminator(IReadOnlyList<ITerminator<TIterationResult>> terminators) {
    Terminators = terminators;
  }
  public bool ShouldTerminate(TIterationResult iterationResult) {
    return Terminators.Any(criterion => criterion.ShouldTerminate(iterationResult));
  }
}

public class AllTerminator<TIterationResult> : ITerminator<TIterationResult> where TIterationResult : IIterationResult {
  public IReadOnlyList<ITerminator<TIterationResult>> Terminators { get; }
  public AllTerminator(IReadOnlyList<ITerminator<TIterationResult>> terminators) {
    Terminators = terminators;
  }
  public bool ShouldTerminate(TIterationResult iterationResult) {
    return Terminators.All(criterion => criterion.ShouldTerminate(iterationResult));
  }
}
