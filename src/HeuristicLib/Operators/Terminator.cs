using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Operators;

public abstract record class Terminator<TGenotype, TSearchSpace, TProblem, TAlgorithmResult> : Operator<TGenotype, TSearchSpace, TProblem, ITerminatorInstance<TAlgorithmResult>>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
  where TAlgorithmResult : IAlgorithmResult
{
  [return: NotNullIfNotNull(nameof(problemAgnosticOperator))]
  public static implicit operator Terminator<TGenotype, TSearchSpace, TProblem, TAlgorithmResult>?(Terminator<TGenotype, TSearchSpace, TAlgorithmResult>? problemAgnosticOperator) {
    if (problemAgnosticOperator is null) return null;
    return new ProblemSpecificTerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmResult>(problemAgnosticOperator);
  }
}

public abstract record class Terminator<TGenotype, TSearchSpace, TAlgorithmResult> : Operator<TGenotype, TSearchSpace, ITerminatorInstance<TAlgorithmResult>>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TAlgorithmResult : IAlgorithmResult
{
}

public interface ITerminatorInstance<in TAlgorithmResult> 
  where TAlgorithmResult : IAlgorithmResult 
{
  bool ShouldTerminate(TAlgorithmResult iterationResult);
  bool ShouldContinue(TAlgorithmResult iterationResult);
}

public abstract class TerminatorExecution<TGenotype, TSearchSpace, TProblem, TAlgorithmResult, TTerminator> : OperatorExecution<TGenotype, TSearchSpace, TProblem, TTerminator>, ITerminatorInstance<TAlgorithmResult> 
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
  where TAlgorithmResult : IAlgorithmResult
{
  protected TerminatorExecution(TTerminator parameters, TSearchSpace searchSpace, TProblem problem) : base(parameters, searchSpace, problem) { }
  
  public abstract bool ShouldTerminate(TAlgorithmResult iterationResult);
  public bool ShouldContinue(TAlgorithmResult iterationResult) {
    return !ShouldTerminate(iterationResult);
  }
}

public abstract class TerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult, TTerminator> : OperatorExecution<TGenotype, TSearchSpace, TTerminator>, ITerminatorInstance<TAlgorithmResult> 
  where TSearchSpace : ISearchSpace<TGenotype>
  where TAlgorithmResult : IAlgorithmResult
{
  protected TerminatorExecution(TTerminator parameters, TSearchSpace searchSpace) : base(parameters, searchSpace) { }
  
  public abstract bool ShouldTerminate(TAlgorithmResult iterationResult);
  public bool ShouldContinue(TAlgorithmResult iterationResult) {
    return !ShouldTerminate(iterationResult);
  }
}

public sealed record class ProblemSpecificTerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmResult> : Terminator<TGenotype, TSearchSpace, TProblem, TAlgorithmResult>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
  where TAlgorithmResult : IAlgorithmResult
{
  public Terminator<TGenotype, TSearchSpace, TAlgorithmResult> ProblemAgnosticTerminator { get; }
  
  public ProblemSpecificTerminator(Terminator<TGenotype, TSearchSpace, TAlgorithmResult> problemAgnosticTerminator) {
    ProblemAgnosticTerminator = problemAgnosticTerminator;
  }
  
  public override ITerminatorInstance<TAlgorithmResult> CreateExecution(TSearchSpace searchSpace, TProblem problem) {
    return ProblemAgnosticTerminator.CreateExecution(searchSpace);
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
  public static OnMaximumGenerationTerminator<TGenotype, TSearchSpace, TAlgorithmResult> OnGeneration<TGenotype, TSearchSpace, TAlgorithmResult>(int maxGenerations) 
    where TSearchSpace : ISearchSpace<TGenotype>
    where TAlgorithmResult : IAlgorithmResult 
  {
    return new OnMaximumGenerationTerminator<TGenotype, TSearchSpace, TAlgorithmResult>(maxGenerations);
  }
  public static AfterIterationsTerminator<TGenotype, TSearchSpace, TAlgorithmResult> AfterIterations<TGenotype, TSearchSpace, TAlgorithmResult>(int iterations)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TAlgorithmResult : IAlgorithmResult 
  {
    return new AfterIterationsTerminator<TGenotype, TSearchSpace, TAlgorithmResult>(iterations);
  }
  public static MaximumExecutionTimeTerminator<TGenotype, TSearchSpace, TAlgorithmResult> OnExecutionTime<TGenotype, TSearchSpace, TAlgorithmResult>(TimeSpan maxTime)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TAlgorithmResult : IAlgorithmResult 
  {
    return new MaximumExecutionTimeTerminator<TGenotype, TSearchSpace, TAlgorithmResult>(maxTime);
  }
  public static NeverTerminator<TGenotype, TSearchSpace, TAlgorithmResult> NeverTerminate<TGenotype, TSearchSpace, TAlgorithmResult>()
    where TSearchSpace : ISearchSpace<TGenotype>
    where TAlgorithmResult : IAlgorithmResult 
  {
    return new NeverTerminator<TGenotype, TSearchSpace, TAlgorithmResult>();
  }
}

// public sealed class CustomTerminator<TIterationResult> : ITerminator<TIterationResult> where TIterationResult : IIterationResult {
//   private readonly Func<TIterationResult, bool> predicate;
//   internal CustomTerminator(Func<TIterationResult, bool> predicate) {
//     this.predicate = predicate;
//   }
//   public bool ShouldTerminate(TIterationResult iterationResult) => predicate(iterationResult);
// }


public record class OnMaximumGenerationTerminator<TGenotype, TSearchSpace, TAlgorithmResult> : Terminator<TGenotype, TSearchSpace, TAlgorithmResult>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TAlgorithmResult : IAlgorithmResult 
{
  public int MaximumGeneration { get; }
  public OnMaximumGenerationTerminator(int maximumGeneration) {
    MaximumGeneration = maximumGeneration;
  }

  public override OnMaximumGenerationTerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult> CreateExecution(TSearchSpace searchSpace) {
    return new OnMaximumGenerationTerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult>(this, searchSpace);
  }
}

public class OnMaximumGenerationTerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult> : TerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult, OnMaximumGenerationTerminator<TGenotype, TSearchSpace, TAlgorithmResult>>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TAlgorithmResult : IAlgorithmResult 
{
  public OnMaximumGenerationTerminatorExecution(OnMaximumGenerationTerminator<TGenotype, TSearchSpace, TAlgorithmResult> parameters, TSearchSpace searchSpace) : base(parameters, searchSpace) {}  

  public override bool ShouldTerminate(TAlgorithmResult iterationResult) {
    return iterationResult.CurrentIteration >= Parameters.MaximumGeneration;
  }
}


public record class AfterIterationsTerminator<TGenotype, TSearchSpace, TAlgorithmResult> : Terminator<TGenotype, TSearchSpace, TAlgorithmResult> 
  where TSearchSpace : ISearchSpace<TGenotype>
  where TAlgorithmResult : IAlgorithmResult 
{
  public int MaximumIterations { get; }
  public AfterIterationsTerminator(int maximumIterations) {
    MaximumIterations = maximumIterations;
  }

  public override AfterIterationsTerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult> CreateExecution(TSearchSpace searchSpace) {
    return new AfterIterationsTerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult>(this, searchSpace);
  }
}

public class AfterIterationsTerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult> : TerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult, AfterIterationsTerminator<TGenotype, TSearchSpace, TAlgorithmResult>>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TAlgorithmResult : IAlgorithmResult 
{
  public int CurrentCount { get; private set; }
  
  public AfterIterationsTerminatorExecution(AfterIterationsTerminator<TGenotype, TSearchSpace, TAlgorithmResult> parameters, TSearchSpace searchSpace) : base(parameters, searchSpace) {
    CurrentCount = 0;
  }  

  public override bool ShouldTerminate(TAlgorithmResult iterationResult) {
    CurrentCount += 1;
    return CurrentCount >= Parameters.MaximumIterations;
  }
}


public record class MaximumExecutionTimeTerminator<TGenotype, TSearchSpace, TAlgorithmResult> : Terminator<TGenotype, TSearchSpace, TAlgorithmResult>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TAlgorithmResult : IAlgorithmResult 
{
  public TimeSpan MaximumExecutionTime { get; }
  public MaximumExecutionTimeTerminator(TimeSpan maximumExecutionTime) {
    MaximumExecutionTime = maximumExecutionTime;
  }
  
  public override MaximumExecutionTimeTerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult> CreateExecution(TSearchSpace searchSpace) {
    return new MaximumExecutionTimeTerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult>(this, searchSpace);
  }
}

public class MaximumExecutionTimeTerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult> : TerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult, MaximumExecutionTimeTerminator<TGenotype, TSearchSpace, TAlgorithmResult>> 
  where TSearchSpace : ISearchSpace<TGenotype>
  where TAlgorithmResult : IAlgorithmResult 
{
  public MaximumExecutionTimeTerminatorExecution(MaximumExecutionTimeTerminator<TGenotype, TSearchSpace, TAlgorithmResult> parameters, TSearchSpace searchSpace) : base(parameters, searchSpace) { }
  
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


public record class NeverTerminator<TGenotype, TSearchSpace, TAlgorithmResult> : Terminator<TGenotype, TSearchSpace, TAlgorithmResult> where TAlgorithmResult : IAlgorithmResult 
  where TSearchSpace : ISearchSpace<TGenotype>
{
  public override NeverTerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult> CreateExecution(TSearchSpace searchSpace) {
    return new NeverTerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult>(this, searchSpace);
  }
}

public class NeverTerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult> : TerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult, NeverTerminator<TGenotype, TSearchSpace, TAlgorithmResult>> 
  where TSearchSpace : ISearchSpace<TGenotype>
  where TAlgorithmResult : IAlgorithmResult
{
  public NeverTerminatorExecution(NeverTerminator<TGenotype, TSearchSpace, TAlgorithmResult> parameters, TSearchSpace searchSpace) : base(parameters, searchSpace) { }
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

public record class AnyTerminator<TGenotype, TSearchSpace, TAlgorithmResult> : Terminator<TGenotype, TSearchSpace, TAlgorithmResult> where TAlgorithmResult : IAlgorithmResult 
  where TSearchSpace : ISearchSpace<TGenotype>
{
  public ImmutableList<Terminator<TGenotype, TSearchSpace, TAlgorithmResult>> Terminators { get; }
  public AnyTerminator(ImmutableList<Terminator<TGenotype, TSearchSpace, TAlgorithmResult>> terminators) {
    Terminators = terminators;
  }
  public override AnyTerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult> CreateExecution(TSearchSpace searchSpace) {
    return new AnyTerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult>(this, searchSpace);
  }
}

public class AnyTerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult> : TerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult, AnyTerminator<TGenotype, TSearchSpace, TAlgorithmResult>> 
  where TSearchSpace : ISearchSpace<TGenotype>
  where TAlgorithmResult : IAlgorithmResult 
{
  public IReadOnlyList<ITerminatorInstance<TAlgorithmResult>> Terminators { get; }
  public AnyTerminatorExecution(AnyTerminator<TGenotype, TSearchSpace, TAlgorithmResult> parameters, TSearchSpace searchSpace) : base(parameters, searchSpace) {
    Terminators = parameters.Terminators.Select(t => t.CreateExecution(searchSpace)).ToList();
  }
  public override bool ShouldTerminate(TAlgorithmResult iterationResult) {
    return Terminators.Any(criterion => criterion.ShouldTerminate(iterationResult));
  }
}

public record class AllTerminator<TGenotype, TSearchSpace, TAlgorithmResult> : Terminator<TGenotype, TSearchSpace, TAlgorithmResult>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TAlgorithmResult : IAlgorithmResult 
{
  public ImmutableList<Terminator<TGenotype, TSearchSpace, TAlgorithmResult>> Terminators { get; }
  public AllTerminator(ImmutableList<Terminator<TGenotype, TSearchSpace, TAlgorithmResult>> terminators) {
    Terminators = terminators;
  }
  
  public override AllTerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult> CreateExecution(TSearchSpace searchSpace) {
    return new AllTerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult>(this, searchSpace);
  }
}

public class AllTerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult> : TerminatorExecution<TGenotype, TSearchSpace, TAlgorithmResult, AllTerminator<TGenotype, TSearchSpace, TAlgorithmResult>>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TAlgorithmResult : IAlgorithmResult 
{
  public IReadOnlyList<ITerminatorInstance<TAlgorithmResult>> Terminators { get; }
  public AllTerminatorExecution(AllTerminator<TGenotype, TSearchSpace, TAlgorithmResult> parameters, TSearchSpace searchSpace) : base(parameters, searchSpace) {
    Terminators = parameters.Terminators.Select(t => t.CreateExecution(searchSpace)).ToList();
  }

  public override bool ShouldTerminate(TAlgorithmResult iterationResult) {
    return Terminators.All(criterion => criterion.ShouldTerminate(iterationResult));
  }
}
