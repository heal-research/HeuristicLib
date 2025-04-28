using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

public abstract record class MetaAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
  : StreamableAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
  where TEncoding : IEncoding<TGenotype>
  where TState : class 
  where TIterationResult : class, IContinuableIterationResult<TState>
  where TAlgorithmResult : class, IAlgorithmResult
  // where TAlgorithmInstance : IStreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
{
  public ImmutableList<StreamableAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>> Algorithms { get; }
  
  protected MetaAlgorithm(ImmutableList<StreamableAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>> algorithms) {
    if (algorithms.Count == 0) throw new ArgumentException("At least one algorithm must be provided.", nameof(algorithms)); 
    Algorithms = algorithms;
  }
  
  //public abstract IEnumerable<TIterationResult> ExecuteStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);
}

public abstract class MetaAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult, TAlgorithm>
  : StreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult, TAlgorithm>
  where TEncoding : IEncoding<TGenotype>
  where TState : class
  where TIterationResult : class, IContinuableIterationResult<TState>
  where TAlgorithmResult : class, IAlgorithmResult
  where TAlgorithm : MetaAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult> 
{
  public IEnumerable<IStreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>> Algorithms { get; }

  protected MetaAlgorithmInstance(TAlgorithm parameters) : base(parameters) {
    Algorithms = parameters.Algorithms.Select(a => a.CreateInstance()).ToList();
  }
}

public record class ConcatAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
  : MetaAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
  where TEncoding : IEncoding<TGenotype>
  where TState : class
  where TIterationResult : class, IContinuableIterationResult<TState>
  where TAlgorithmResult : class, IAlgorithmResult 
  //where TAlgorithmInstance : IStreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
{
  public ConcatAlgorithm(ImmutableList<StreamableAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>> algorithms) : base(algorithms) { }

  public override ConcatAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult> CreateInstance() {
    return new ConcatAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>(this);
  }
}

public class ConcatAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
  : MetaAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult, ConcatAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>>
  where TEncoding : IEncoding<TGenotype>
  where TState : class
  where TIterationResult : class, IContinuableIterationResult<TState>
  where TAlgorithmResult : class, IAlgorithmResult 
{
  public ConcatAlgorithmInstance(ConcatAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult> parameters) : base(parameters) { }
  
  public override TAlgorithmResult Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null) {
    throw new NotImplementedException();
    // TState? currentState = initialState;
    // TAlgorithmResult? lastResult = null;
    // foreach (var algorithm in Algorithms) {
    //   lastResult = algorithm.Execute(problem, currentState);
    //   currentState = lastResult.GetRestartState();
    // }
    // return lastResult;
  }
  
  public override IEnumerable<TIterationResult> ExecuteStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null) {
    TState? currentState = initialState;
    foreach (var algorithm in Algorithms) {
      TIterationResult? lastIterationResult = null;
      foreach (var iterationResult in algorithm.ExecuteStreaming(problem, currentState)) {
        yield return iterationResult;
        lastIterationResult = iterationResult;
      }
      if (lastIterationResult is null) {
        yield break; // no result -> break concat algorithm
      }
      currentState = lastIterationResult.GetRestartState();
    }
  }
  
  
  //
  // public override ResultStream<TState> CreateExecutionStream(TState? initialState = null) {
  //   var stream = InternalCreateExecutionStream(initialState);
  //   // if (termination is not null) {
  //   //   stream = stream.TakeUntil(termination.ShouldTerminate);
  //   // }
  //   return new ResultStream<TState>(stream);
  // }
  // private IEnumerable<TState> InternalCreateExecutionStream(TState? initialState) {
  //   TState? currentState = initialState;
  //   foreach (var algorithm in Algorithms) {
  //     var currentStream = algorithm.CreateExecutionStream(currentState);
  //     foreach (var state in currentStream) {
  //       yield return currentState = state;
  //     }
  //     if (currentState is null) yield break;
  //     currentState = currentState.Reset<TState>();
  //   }
  // }

}

public record class CyclicAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
  : MetaAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
  where TEncoding : IEncoding<TGenotype>
  where TState : class
  where TIterationResult : class, IContinuableIterationResult<TState>
  where TAlgorithmResult : class, IAlgorithmResult {
  public Terminator<TIterationResult> Terminator { get; }

  public CyclicAlgorithm(ImmutableList<StreamableAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>> algorithms, Terminator<TIterationResult> terminator)
    : base(algorithms) {
    Terminator = terminator;
  }
  public override CyclicAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult> CreateInstance() {
    return new CyclicAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>(this);
  }
}

public class CyclicAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
  : MetaAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult, CyclicAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>>
  where TEncoding : IEncoding<TGenotype>
  where TState : class
  where TIterationResult : class, IContinuableIterationResult<TState>
  where TAlgorithmResult : class, IAlgorithmResult 
{
  public ITerminatorInstance<TIterationResult> Terminator { get; }

  public CyclicAlgorithmInstance(CyclicAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult> parameters) : base(parameters) {
    Terminator = parameters.Terminator.CreateInstance();
  }

  public override TAlgorithmResult Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null) {
    throw new NotImplementedException();
  }
  
  public override IEnumerable<TIterationResult> ExecuteStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null) {
    TState? currentState = initialState;
    while (true) {
      foreach (var algorithm in Algorithms) {
        TIterationResult? lastIterationResult = null;
        foreach (var iterationResult in algorithm.ExecuteStreaming(problem, currentState)) {
          yield return iterationResult;
          lastIterationResult = iterationResult;
          if (Terminator.ShouldTerminate(iterationResult)) {
            yield break;
          }
        }
        if (lastIterationResult is null) {
          yield break; // no result -> break concat algorithm
        }
        currentState = lastIterationResult.GetRestartState();
      }
    }
  }
  //
  // public override ResultStream<TState> CreateExecutionStream(TState? initialState = null) {
  //   //if (termination is null) throw new InvalidOperationException("Cyclic Algorithms require a termination to avoid infinite loops.");
  //   var stream = InternalCreateExecutionStream(initialState);
  //   //return new ExecutionStream<TState>(stream.TakeWhile(s => termination?.ShouldTerminate(s) ?? true));
  //   return new ResultStream<TState>(stream);
  // }
  //
  // #pragma warning disable S2190
  // private IEnumerable<TState> InternalCreateExecutionStream(TState? initialState) {
  //   TState? currentState = initialState;
  //   while (true) {
  //     // CreateExecutionStream always returning an empty stream results in an infinite loop
  //     foreach (var algorithm in Algorithms) {
  //       var currentStream = algorithm.CreateExecutionStream(currentState);
  //       foreach (var state in currentStream) {
  //         yield return currentState = state;
  //       }
  //       if (currentState is null) yield break;
  //       currentState = currentState.Reset<TState>();
  //     }
  //   }
  // }
  // #pragma warning restore S2190
}
//
// public interface IStateTransformer<TSourceState, TTargetState> where TSourceState : class where TTargetState : class {
//   TTargetState Transform(TSourceState sourceState, TTargetState? previousTargetState = null);
// }
//
// public class ConcatAlgorithm<TGenotype, TEncoding, TSourceState, TTargetState, TSourceIterationResult, TTargetIterationResult> 
//   : IStreamableAlgorithm<TGenotype, TEncoding, object, IContinuableIterationResult<object>>
//   where TEncoding : IEncoding<TGenotype>
//   where TSourceState : class
//   where TTargetState : class
//   where TSourceIterationResult : class, IContinuableIterationResult<TSourceState>
//   where TTargetIterationResult : class, IContinuableIterationResult<TTargetState>
// {
//   public IStreamableAlgorithm<TGenotype, TEncoding, TSourceState, TSourceIterationResult> FirstAlgorithm { get; }
//   public IStreamableAlgorithm<TGenotype, TEncoding, TTargetState, TTargetIterationResult> SecondAlgorithm { get; }
//   public IStateTransformer<TSourceState, TTargetState> Transformer { get; }
//   
//   public ConcatAlgorithm(IStreamableAlgorithm<TGenotype, TEncoding, TSourceState, TSourceIterationResult> firstAlgorithm, IStreamableAlgorithm<TGenotype, TEncoding, TTargetState, TTargetIterationResult> secondAlgorithm, IStateTransformer<TSourceState, TTargetState> transformer) {
//     FirstAlgorithm = firstAlgorithm;
//     SecondAlgorithm = secondAlgorithm;
//     Transformer = transformer;
//   }
//
//   public IEnumerable<IContinuableIterationResult<object>> ExecuteStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, object? initialState = null) {
//     if (initialState is not null && initialState is not TSourceState) throw new ArgumentException("Initial state must be of type TSourceState.", nameof(initialState));
//
//     TSourceIterationResult? lastSourceIterationResult = null;
//     foreach (var iterationResult in FirstAlgorithm.ExecuteStreaming(problem, initialState as TSourceState)) {
//       yield return iterationResult;
//       lastSourceIterationResult = iterationResult;
//     }
//     
//     if (lastSourceIterationResult is null) yield break; // no result -> break concat algorithm
//     var lastSourceState = lastSourceIterationResult.GetRestartState();
//     
//     var initialTargetState = Transformer.Transform(lastSourceState);
//     
//     foreach (var iterationResult in SecondAlgorithm.ExecuteStreaming(problem, initialTargetState)) {
//       yield return iterationResult;
//     }
//   }
//
//   // public override ResultStream<TState> CreateExecutionStream(TState? initialState = null) {
//   //   if (initialState is not null && initialState is not TSourceState) throw new ArgumentException("Initial state must be of type TSourceState.", nameof(initialState));
//   //   var stream = InternalCreateExecutionStream(initialState as TSourceState);
//   //   // if (termination is not null) {
//   //   //   stream = stream.TakeUntil(termination.ShouldTerminate);
//   //   // }
//   //   return new ResultStream<TState>(stream);
//   // }
//   //
//   // private IEnumerable<TState> InternalCreateExecutionStream(TSourceState? initialState) {
//   //   TSourceState? currentSourceState = initialState;
//   //   var sourceStream = FirstAlgorithm.CreateExecutionStream(initialState);
//   //   foreach (var sourceState in sourceStream) {
//   //     yield return currentSourceState = sourceState;
//   //   }
//   //   var initialTargetState = currentSourceState is not null ? Transformer.Transform(currentSourceState) : null;
//   //   var targetStream = SecondAlgorithm.CreateExecutionStream(initialTargetState);
//   //   foreach (var targetState in targetStream) {
//   //     yield return targetState;
//   //   }
//   // }
// }
//
// public class CyclicAlgorithm<TGenotype, TEncoding, TSourceState, TTargetState, TSourceIterationResult, TTargetIterationResult> 
//   : IStreamableAlgorithm<TGenotype, TEncoding, object, IContinuableIterationResult<object>>
//   where TEncoding : IEncoding<TGenotype>
//   where TSourceState : class
//   where TTargetState : class
//   where TSourceIterationResult : class, IContinuableIterationResult<TSourceState>
//   where TTargetIterationResult : class, IContinuableIterationResult<TTargetState>
// {
//   public IStreamableAlgorithm<TGenotype, TEncoding, TSourceState, TSourceIterationResult> FirstAlgorithm { get; }
//   public IStreamableAlgorithm<TGenotype, TEncoding, TTargetState, TTargetIterationResult> SecondAlgorithm { get; }
//   public IStateTransformer<TSourceState, TTargetState> Transformer { get; }
//   public IStateTransformer<TTargetState, TSourceState> RepetitionTransformer { get; }
//   public ITerminator<IContinuableIterationResult<object>> Terminator { get; }
//   
//   public CyclicAlgorithm(
//     IStreamableAlgorithm<TGenotype, TEncoding, TSourceState, TSourceIterationResult> firstAlgorithm, IStreamableAlgorithm<TGenotype, TEncoding, TTargetState, TTargetIterationResult> secondAlgorithm,
//     IStateTransformer<TSourceState, TTargetState> transformer, IStateTransformer<TTargetState, TSourceState> repetitionTransformer, 
//     ITerminator<IContinuableIterationResult<object>> terminator) 
//   {
//     FirstAlgorithm = firstAlgorithm;
//     SecondAlgorithm = secondAlgorithm;
//     Transformer = transformer;
//     RepetitionTransformer = repetitionTransformer;
//     Terminator = terminator;
//   }
//   
//   public IEnumerable<IContinuableIterationResult<object>> ExecuteStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, object? initialState = null) {
//     if (initialState is not null && initialState is not TSourceState) throw new ArgumentException("Initial state must be of type TSourceState.", nameof(initialState));
//
//     TSourceState? sourceState = initialState as TSourceState;
//     TTargetState? targetState = null;
//     
//     while (true) {
//       TSourceIterationResult? lastSourceIterationResult = null;
//       foreach (var iterationResult in FirstAlgorithm.ExecuteStreaming(problem, sourceState)) {
//         yield return iterationResult;
//         lastSourceIterationResult = iterationResult;
//       }
//
//       if (lastSourceIterationResult is null) yield break;// no result -> break concat algorithm
//       sourceState = lastSourceIterationResult.GetRestartState();
//
//       targetState = Transformer.Transform(sourceState, targetState);
//
//       TTargetIterationResult? lastTargetIterationResult = null;
//       foreach (var iterationResult in SecondAlgorithm.ExecuteStreaming(problem, targetState)) {
//         yield return iterationResult;
//         lastTargetIterationResult = iterationResult;
//       }
//       
//       if (lastTargetIterationResult is null) yield break; // no result -> break concat algorithm
//       targetState = lastTargetIterationResult.GetRestartState();
//       
//       sourceState = RepetitionTransformer.Transform(targetState, sourceState);
//     }
//   }
//   //
//   // public override ResultStream<TState> CreateExecutionStream(TState? initialState = null) {
//   //   //if (termination is null) throw new InvalidOperationException("Cyclic Algorithms require a termination to avoid infinite loops.");
//   //   if (initialState is not null && initialState is not TSourceState) throw new ArgumentException("Initial state must be of type TSourceState.", nameof(initialState));
//   //   
//   //   var stream = InternalCreateExecutionStream(initialState as TSourceState);
//   //   return new ResultStream<TState>(stream/*.TakeWhile(state => termination?.ShouldContinue(state) ?? true)*/);
//   // }
//   //
//   //  #pragma warning disable S2190
//   // private IEnumerable<TState> InternalCreateExecutionStream(TSourceState? initialState) {
//   //   TSourceState? lastSourceState = initialState;
//   //   TTargetState? lastTargetState = null;
//   //
//   //   while (true) {
//   //     var sourceStream = FirstAlgorithm.CreateExecutionStream(lastSourceState);
//   //     foreach (var sourceState in sourceStream) {
//   //       yield return lastSourceState = sourceState;
//   //     }
//   //     if (lastSourceState is null) yield break;
//   //     
//   //     lastTargetState = Transformer.Transform(lastSourceState, lastTargetState);
//   //     
//   //     var targetStream = SecondAlgorithm.CreateExecutionStream(lastTargetState);
//   //     foreach (var targetState in targetStream) {
//   //       yield return lastTargetState = targetState;
//   //     }
//   //     if (lastTargetState is null) yield break;
//   //     
//   //     lastSourceState = RepetitionTransformer.Transform(lastTargetState, lastSourceState);
//   //   }
//   // }
//   // #pragma warning restore S2190
//
// }
