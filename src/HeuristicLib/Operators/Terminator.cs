using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public abstract record class Terminator<TAlgorithmResult> : Operator<ITerminatorInstance<TAlgorithmResult>>
  where TAlgorithmResult : IAlgorithmResult
{
}

public interface ITerminatorInstance<in TAlgorithmResult>
  where TAlgorithmResult : IAlgorithmResult 
{
  bool ShouldTerminate(TAlgorithmResult iterationResult);
  bool ShouldContinue(TAlgorithmResult iterationResult);
}

public abstract class TerminatorInstance<TAlgorithmResult, TTerminator> : OperatorInstance<TTerminator>, ITerminatorInstance<TAlgorithmResult> 
  where TAlgorithmResult : IAlgorithmResult
{
  protected TerminatorInstance(TTerminator parameters) : base(parameters) { }
  
  public abstract bool ShouldTerminate(TAlgorithmResult iterationResult);
  public bool ShouldContinue(TAlgorithmResult iterationResult) {
    return !ShouldTerminate(iterationResult);
  }
}

// public static class TerminatorExtensions {
//   public static bool ShouldContinue<TIterationResult>(this ITerminator<TIterationResult> terminator, TIterationResult iterationResult) where TIterationResult : IIterationResult 
//   {
//     return !terminator.ShouldTerminate(iterationResult);
//   }
// }

public static class Terminator {
  // public static CustomTerminator<TIterationResult> Create<TIterationResult>(Func<TIterationResult, bool> shouldTerminatePredicate) where TIterationResult : IIterationResult {
  //   return new CustomTerminator<TIterationResult>(shouldTerminatePredicate);
  // }
  public static OnMaximumGenerationTerminator<TAlgorithmResult> OnGeneration<TAlgorithmResult>(int maxGenerations) where TAlgorithmResult : IAlgorithmResult {
    return new OnMaximumGenerationTerminator<TAlgorithmResult>(maxGenerations);
  }
  public static AfterIterationsTerminator<TAlgorithmResult> AfterIterations<TAlgorithmResult>(int iterations) where TAlgorithmResult : IAlgorithmResult {
    return new AfterIterationsTerminator<TAlgorithmResult>(iterations);
  }
  public static MaximumExecutionTimeTerminator<TAlgorithmResult> OnExecutionTime<TAlgorithmResult>(TimeSpan maxTime) where TAlgorithmResult : IAlgorithmResult {
    return new MaximumExecutionTimeTerminator<TAlgorithmResult>(maxTime);
  }
  public static NeverTerminator<TAlgorithmResult> NeverTerminate<TAlgorithmResult>() where TAlgorithmResult : IAlgorithmResult {
    return new NeverTerminator<TAlgorithmResult>();
  }
}

// public sealed class CustomTerminator<TIterationResult> : ITerminator<TIterationResult> where TIterationResult : IIterationResult {
//   private readonly Func<TIterationResult, bool> predicate;
//   internal CustomTerminator(Func<TIterationResult, bool> predicate) {
//     this.predicate = predicate;
//   }
//   public bool ShouldTerminate(TIterationResult iterationResult) => predicate(iterationResult);
// }


public record class OnMaximumGenerationTerminator<TAlgorithmResult> : Terminator<TAlgorithmResult> where TAlgorithmResult : IAlgorithmResult {
  public int MaximumGeneration { get; }
  public OnMaximumGenerationTerminator(int maximumGeneration) {
    MaximumGeneration = maximumGeneration;
  }

  public override OnMaximumGenerationTerminatorInstance<TAlgorithmResult> CreateInstance() {
    return new OnMaximumGenerationTerminatorInstance<TAlgorithmResult>(this);
  }
}

public class OnMaximumGenerationTerminatorInstance<TAlgorithmResult> : TerminatorInstance<TAlgorithmResult, OnMaximumGenerationTerminator<TAlgorithmResult>>
  where TAlgorithmResult : IAlgorithmResult 
{
  public OnMaximumGenerationTerminatorInstance(OnMaximumGenerationTerminator<TAlgorithmResult> parameters) : base(parameters) {}  

  public override bool ShouldTerminate(TAlgorithmResult iterationResult) {
    return iterationResult.CurrentIteration >= Parameters.MaximumGeneration;
  }
}


public record class AfterIterationsTerminator<TAlgorithmResult> : Terminator<TAlgorithmResult> where TAlgorithmResult : IAlgorithmResult {
  public int MaximumIterations { get; }
  public AfterIterationsTerminator(int maximumIterations) {
    MaximumIterations = maximumIterations;
  }

  public override AfterIterationsTerminatorInstance<TAlgorithmResult> CreateInstance() {
    return new AfterIterationsTerminatorInstance<TAlgorithmResult>(this);
  }
}

public class AfterIterationsTerminatorInstance<TAlgorithmResult> : TerminatorInstance<TAlgorithmResult, AfterIterationsTerminator<TAlgorithmResult>>
  where TAlgorithmResult : IAlgorithmResult 
{
  public int CurrentCount { get; private set; }
  
  public AfterIterationsTerminatorInstance(AfterIterationsTerminator<TAlgorithmResult> parameters) : base(parameters) {
    CurrentCount = 0;
  }  

  public override bool ShouldTerminate(TAlgorithmResult iterationResult) {
    CurrentCount += 1;
    return CurrentCount >= Parameters.MaximumIterations;
  }
}


public record class MaximumExecutionTimeTerminator<TAlgorithmResult> : Terminator<TAlgorithmResult> where TAlgorithmResult : IAlgorithmResult {
  public TimeSpan MaximumExecutionTime { get; }
  public MaximumExecutionTimeTerminator(TimeSpan maximumExecutionTime) {
    MaximumExecutionTime = maximumExecutionTime;
  }
  
  public override MaximumExecutionTimeTerminatorInstance<TAlgorithmResult> CreateInstance() {
    return new MaximumExecutionTimeTerminatorInstance<TAlgorithmResult>(this);
  }
}

public class MaximumExecutionTimeTerminatorInstance<TAlgorithmResult> : TerminatorInstance<TAlgorithmResult, MaximumExecutionTimeTerminator<TAlgorithmResult>> 
  where TAlgorithmResult : IAlgorithmResult 
{
  public MaximumExecutionTimeTerminatorInstance(MaximumExecutionTimeTerminator<TAlgorithmResult> parameters) : base(parameters) { }
  
  public override bool ShouldTerminate(TAlgorithmResult iterationResult) {
    return iterationResult.TotalDuration >= Parameters.MaximumExecutionTime;
  }
}


// public class PauseToken {
//   public bool IsPaused { get; private set; }
//   public void RequestPause() => IsPaused = true;
// }
//
// public class PauseTokenTerminator<TIterationResult> : ITerminator<TIterationResult> where TIterationResult : IIterationResult {
//   public PauseToken Token { get; }
//   public PauseTokenTerminator(PauseToken pauseToken) {
//     Token = pauseToken;
//   }
//   public bool ShouldTerminate(TIterationResult iterationResult) => Token.IsPaused;
// }


public record class NeverTerminator<TAlgorithmResult> : Terminator<TAlgorithmResult> where TAlgorithmResult : IAlgorithmResult {
  public override NeverTerminatorInstance<TAlgorithmResult> CreateInstance() {
    return new NeverTerminatorInstance<TAlgorithmResult>(this);
  }
}

public class NeverTerminatorInstance<TAlgorithmResult> : TerminatorInstance<TAlgorithmResult, NeverTerminator<TAlgorithmResult>> 
  where TAlgorithmResult : IAlgorithmResult
{
  public NeverTerminatorInstance(NeverTerminator<TAlgorithmResult> parameters) : base(parameters) { }
  public override bool ShouldTerminate(TAlgorithmResult iterationResult) => false;
}
//
// // ToDo: generic immutable list for operators
// public record class TerminatorList<TIterationResult> : IReadOnlyList<Terminator<TIterationResult>>, IEquatable<TerminatorList<TIterationResult>>
//   where TIterationResult : IIterationResult {
//   private readonly Terminator<TIterationResult>[] terminators;
//   public TerminatorList(IEnumerable<Terminator<TIterationResult>> terminators) {
//     this.terminators = terminators.ToArray();
//   }
//   public Terminator<TIterationResult> this[int index] => terminators[index];
//   public int Count => terminators.Length;
//   public IEnumerator<Terminator<TIterationResult>> GetEnumerator() => ((IEnumerable<Terminator<TIterationResult>>)terminators).GetEnumerator();
//   System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => terminators.GetEnumerator();
//   
//   public virtual bool Equals(TerminatorList<TIterationResult>? other) {
//     if (other == null) return false;
//     if (Count != other.Count) return false;
//     for (int i = 0; i < Count; i++) {
//       if (!this[i].Equals(other[i])) return false;
//     }
//     return true;
//   }
//   public override int GetHashCode() {
//     var hash = new HashCode();
//     foreach (var terminator in terminators) {
//       hash.Add(terminator);
//     }
//     return hash.ToHashCode();
//   }
// } 

public record class AnyTerminator<TAlgorithmResult> : Terminator<TAlgorithmResult> where TAlgorithmResult : IAlgorithmResult {
  public ImmutableList<Terminator<TAlgorithmResult>> Terminators { get; }
  public AnyTerminator(ImmutableList<Terminator<TAlgorithmResult>> terminators) {
    Terminators = terminators;
  }
  public override AnyTerminatorInstance<TAlgorithmResult> CreateInstance() {
    return new AnyTerminatorInstance<TAlgorithmResult>(this);
  }
}

public class AnyTerminatorInstance<TAlgorithmResult> : TerminatorInstance<TAlgorithmResult, AnyTerminator<TAlgorithmResult>> 
  where TAlgorithmResult : IAlgorithmResult 
{
  public IReadOnlyList<ITerminatorInstance<TAlgorithmResult>> Terminators { get; }
  public AnyTerminatorInstance(AnyTerminator<TAlgorithmResult> parameters) : base(parameters) {
    Terminators = parameters.Terminators.Select(t => t.CreateInstance()).ToList();
  }
  public override bool ShouldTerminate(TAlgorithmResult iterationResult) {
    return Terminators.Any(criterion => criterion.ShouldTerminate(iterationResult));
  }
}

public record class AllTerminator<TAlgorithmResult> : Terminator<TAlgorithmResult> where TAlgorithmResult : IAlgorithmResult {
  public ImmutableList<Terminator<TAlgorithmResult>> Terminators { get; }
  public AllTerminator(ImmutableList<Terminator<TAlgorithmResult>> terminators) {
    Terminators = terminators;
  }
  
  public override AllTerminatorInstance<TAlgorithmResult> CreateInstance() {
    return new AllTerminatorInstance<TAlgorithmResult>(this);
  }
}

public class AllTerminatorInstance<TAlgorithmResult> : TerminatorInstance<TAlgorithmResult, AllTerminator<TAlgorithmResult>>
  where TAlgorithmResult : IAlgorithmResult 
{
  public IReadOnlyList<ITerminatorInstance<TAlgorithmResult>> Terminators { get; }
  public AllTerminatorInstance(AllTerminator<TAlgorithmResult> parameters) : base(parameters) {
    Terminators = parameters.Terminators.Select(t => t.CreateInstance()).ToList();
  }

  public override bool ShouldTerminate(TAlgorithmResult iterationResult) {
    return Terminators.All(criterion => criterion.ShouldTerminate(iterationResult));
  }
}
