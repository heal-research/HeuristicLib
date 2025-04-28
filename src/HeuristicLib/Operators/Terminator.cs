using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public abstract record class Terminator<TIterationResult> : Operator<ITerminatorInstance<TIterationResult>>
  where TIterationResult : IIterationResult
{
}

public interface ITerminatorInstance<in TIterationResult>
  where TIterationResult : IIterationResult 
{
  bool ShouldTerminate(TIterationResult iterationResult);
  bool ShouldContinue(TIterationResult iterationResult);
}

public abstract class TerminatorInstance<TIterationResult, TTerminator> : OperatorInstance<TTerminator>, ITerminatorInstance<TIterationResult> 
  where TIterationResult : IIterationResult
{
  protected TerminatorInstance(TTerminator parameters) : base(parameters) { }
  
  public abstract bool ShouldTerminate(TIterationResult iterationResult);
  public bool ShouldContinue(TIterationResult iterationResult) {
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
  public static MaximumGenerationTerminator<TIterationResult> OnGeneration<TIterationResult>(int maxGenerations) where TIterationResult : IIterationResult {
    return new MaximumGenerationTerminator<TIterationResult>(maxGenerations);
  }
  public static Terminator<TIterationResult> AfterIterations<TIterationResult>(int iterations) where TIterationResult : IIterationResult {
    // return new MaximumGenerationTerminator<TIterationResult>(iterations);
    throw new NotImplementedException("Not implemented yet");
  }
  public static MaximumExecutionTimeTerminator<TIterationResult> OnExecutionTime<TIterationResult>(TimeSpan maxTime) where TIterationResult : IIterationResult {
    return new MaximumExecutionTimeTerminator<TIterationResult>(maxTime);
  }
  public static NeverTerminator<TIterationResult> NeverTerminate<TIterationResult>() where TIterationResult : IIterationResult {
    return new NeverTerminator<TIterationResult>();
  }
}

// public sealed class CustomTerminator<TIterationResult> : ITerminator<TIterationResult> where TIterationResult : IIterationResult {
//   private readonly Func<TIterationResult, bool> predicate;
//   internal CustomTerminator(Func<TIterationResult, bool> predicate) {
//     this.predicate = predicate;
//   }
//   public bool ShouldTerminate(TIterationResult iterationResult) => predicate(iterationResult);
// }


public record class MaximumGenerationTerminator<TIterationResult> : Terminator<TIterationResult> where TIterationResult : IIterationResult {
  public int MaximumGeneration { get; }
  public MaximumGenerationTerminator(int maximumGeneration) {
    MaximumGeneration = maximumGeneration;
  }

  public override MaximumGenerationTerminatorInstance<TIterationResult> CreateInstance() {
    return new MaximumGenerationTerminatorInstance<TIterationResult>(this);
  }
}

public class MaximumGenerationTerminatorInstance<TIterationResult> : TerminatorInstance<TIterationResult, MaximumGenerationTerminator<TIterationResult>>
  where TIterationResult : IIterationResult 
{
  public MaximumGenerationTerminatorInstance(MaximumGenerationTerminator<TIterationResult> parameters) : base(parameters) {}  

  public override bool ShouldTerminate(TIterationResult iterationResult) {
    return iterationResult.Iteration >= Parameters.MaximumGeneration;
  }
}


public record class MaximumExecutionTimeTerminator<TIterationResult> : Terminator<TIterationResult> where TIterationResult : IIterationResult {
  public TimeSpan MaximumExecutionTime { get; }
  public MaximumExecutionTimeTerminator(TimeSpan maximumExecutionTime) {
    MaximumExecutionTime = maximumExecutionTime;
  }
  
  public override MaximumExecutionTimeTerminatorInstance<TIterationResult> CreateInstance() {
    return new MaximumExecutionTimeTerminatorInstance<TIterationResult>(this);
  }
}

public class MaximumExecutionTimeTerminatorInstance<TIterationResult> : TerminatorInstance<TIterationResult, MaximumExecutionTimeTerminator<TIterationResult>> 
  where TIterationResult : IIterationResult 
{
  public MaximumExecutionTimeTerminatorInstance(MaximumExecutionTimeTerminator<TIterationResult> parameters) : base(parameters) { }
  
  public override bool ShouldTerminate(TIterationResult iterationResult) {
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


public record class NeverTerminator<TIterationResult> : Terminator<TIterationResult> where TIterationResult : IIterationResult {
  public override NeverTerminatorInstance<TIterationResult> CreateInstance() {
    return new NeverTerminatorInstance<TIterationResult>(this);
  }
}

public class NeverTerminatorInstance<TIterationResult> : TerminatorInstance<TIterationResult, NeverTerminator<TIterationResult>> 
  where TIterationResult : IIterationResult
{
  public NeverTerminatorInstance(NeverTerminator<TIterationResult> parameters) : base(parameters) { }
  public override bool ShouldTerminate(TIterationResult iterationResult) => false;
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

public record class AnyTerminator<TIterationResult> : Terminator<TIterationResult> where TIterationResult : IIterationResult {
  public ImmutableList<Terminator<TIterationResult>> Terminators { get; }
  public AnyTerminator(ImmutableList<Terminator<TIterationResult>> terminators) {
    Terminators = terminators;
  }
  public override AnyTerminatorInstance<TIterationResult> CreateInstance() {
    return new AnyTerminatorInstance<TIterationResult>(this);
  }
}

public class AnyTerminatorInstance<TIterationResult> : TerminatorInstance<TIterationResult, AnyTerminator<TIterationResult>> 
  where TIterationResult : IIterationResult 
{
  public IReadOnlyList<ITerminatorInstance<TIterationResult>> Terminators { get; }
  public AnyTerminatorInstance(AnyTerminator<TIterationResult> parameters) : base(parameters) {
    Terminators = parameters.Terminators.Select(t => t.CreateInstance()).ToList();
  }
  public override bool ShouldTerminate(TIterationResult iterationResult) {
    return Terminators.Any(criterion => criterion.ShouldTerminate(iterationResult));
  }
}

public record class AllTerminator<TIterationResult> : Terminator<TIterationResult> where TIterationResult : IIterationResult {
  public ImmutableList<Terminator<TIterationResult>> Terminators { get; }
  public AllTerminator(ImmutableList<Terminator<TIterationResult>> terminators) {
    Terminators = terminators;
  }
  
  public override AllTerminatorInstance<TIterationResult> CreateInstance() {
    return new AllTerminatorInstance<TIterationResult>(this);
  }
}

public class AllTerminatorInstance<TIterationResult> : TerminatorInstance<TIterationResult, AllTerminator<TIterationResult>>
  where TIterationResult : IIterationResult 
{
  public IReadOnlyList<ITerminatorInstance<TIterationResult>> Terminators { get; }
  public AllTerminatorInstance(AllTerminator<TIterationResult> parameters) : base(parameters) {
    Terminators = parameters.Terminators.Select(t => t.CreateInstance()).ToList();
  }

  public override bool ShouldTerminate(TIterationResult iterationResult) {
    return Terminators.All(criterion => criterion.ShouldTerminate(iterationResult));
  }
}
