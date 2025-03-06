using HEAL.HeuristicLib.Operators;
using MoreLinq;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

public abstract class MetaAlgorithm<TState> : AlgorithmBase<TState>
  where TState : class {
  public IAlgorithm<TState>[] Algorithms { get; }
  protected MetaAlgorithm(IAlgorithm<TState>[] algorithms) {
    if (algorithms.Length == 0) throw new ArgumentException("At least one algorithm must be provided.", nameof(algorithms));
    Algorithms = algorithms;
  }
    
}

public class ConcatAlgorithm<TState> : MetaAlgorithm<TState> where TState : class {
  public ConcatAlgorithm(IAlgorithm<TState>[] algorithms) : base(algorithms) { }

  public override TState? Execute(TState? initialState = null, ITerminator<TState>? termination = null) {
    TState? currentState = initialState;
    foreach (var remainingAlg in Algorithms) {
      if (termination is not null && currentState is not null && termination.ShouldTerminate(currentState)) {
        break;
      }
      currentState = remainingAlg.Execute(currentState);
    }
    return currentState;
  }

  public override ExecutionStream<TState> CreateExecutionStream(TState? initialState = null, ITerminator<TState>? termination = null) {
    var stream = InternalCreateExecutionStream(initialState);
    if (termination is not null) {
      stream = stream.TakeUntil(termination.ShouldTerminate);
    }
    return new ExecutionStream<TState>(stream);
  }
  private IEnumerable<TState> InternalCreateExecutionStream(TState? initialState) {
    TState? currentState = initialState;
    foreach (var algorithm in Algorithms) {
      var currentStream = algorithm.CreateExecutionStream(currentState);
      foreach (var state in currentStream) {
        yield return currentState = state;
      } 
    }
  }
}

public class CyclicAlgorithm<TState> : MetaAlgorithm<TState> where TState : class {
  public CyclicAlgorithm(IAlgorithm<TState>[] algorithms) : base(algorithms) { }
  
  public override TState? Execute(TState? initialState = null, ITerminator<TState>? termination = null) {
    if (termination is null) throw new InvalidOperationException("Cyclic Algorithms require a termination to avoid infinite loops.");
    TState? currentState = initialState;
    while (true) {
      foreach (var algorithm in Algorithms) {
        if (currentState is not null && termination.ShouldTerminate(currentState)) {
          return currentState;
        }
        currentState = algorithm.Execute(currentState);
        if (currentState is null) {
          // If an algorithm returns null we break the cyclic algorithm
          return null;
        }
      }
    }
  }
  
  public override ExecutionStream<TState> CreateExecutionStream(TState? initialState = null, ITerminator<TState>? termination = null) {
    if (termination is null) throw new InvalidOperationException("Cyclic Algorithms require a termination to avoid infinite loops.");
    var stream = InternalCreateExecutionStream(initialState);
    return new ExecutionStream<TState>(stream.TakeUntil(termination.ShouldTerminate));
  }
 #pragma warning disable S2190
  private IEnumerable<TState> InternalCreateExecutionStream(TState? initialState) {
    TState? currentState = initialState;
    while (true) {
      foreach (var algorithm in Algorithms) {
        var currentStream = algorithm.CreateExecutionStream(currentState);
        foreach (var state in currentStream) {
          yield return currentState = state;
        }
      }
    }
  }
  #pragma warning restore S2190
}
