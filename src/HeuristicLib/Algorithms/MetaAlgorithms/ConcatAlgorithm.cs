using HEAL.HeuristicLib.Operators;
using MoreLinq;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

public abstract class MetaAlgorithm<TState> : AlgorithmBase<TState>
  where TState : class, IState {
  public IReadOnlyList<IAlgorithm<TState>> Algorithms { get; }
  protected MetaAlgorithm(IEnumerable<IAlgorithm<TState>> algorithms) {
    Algorithms = algorithms.ToList();
    if (Algorithms.Count == 0) throw new ArgumentException("At least one algorithm must be provided.", nameof(algorithms));
  }
    
}

public class ConcatAlgorithm<TState, TGenotype> : MetaAlgorithm<TState> where TState : class, IState<TState> {
  public ConcatAlgorithm(params IEnumerable<IAlgorithm<TState>> algorithms) : base(algorithms) { }

  public override TState? Execute(TState? initialState = null, ITerminator<TState>? termination = null) {
    TState? currentState = initialState;
    foreach (var remainingAlg in Algorithms) {
      if (termination is not null && currentState is not null && termination.ShouldTerminate(currentState)) {
        break;
      }
      currentState = remainingAlg.Execute(currentState);
      if (currentState is null) return null;
      currentState = currentState.Reset();
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
      if (currentState is null) yield break;
      currentState = currentState.Reset();
    }
  }
}

public class CyclicAlgorithm<TState, TGenotype> : MetaAlgorithm<TState> where TState : class, IState<TState> {
  public CyclicAlgorithm(params IEnumerable<IAlgorithm<TState>> algorithms) : base(algorithms) { }
  
  public override TState? Execute(TState? initialState = null, ITerminator<TState>? termination = null) {
    if (termination is null) throw new InvalidOperationException("Cyclic Algorithms require a termination to avoid infinite loops.");
    TState? currentState = initialState;
    while (true) {
      foreach (var algorithm in Algorithms) {
        if (currentState is not null && termination.ShouldTerminate(currentState)) {
          return currentState;
        }
        currentState = algorithm.Execute(currentState);
        if (currentState is null) return null; // If an algorithm returns null we break the cyclic algorithm
        currentState = currentState.Reset();
      }
    }
  }
  
  public override ExecutionStream<TState> CreateExecutionStream(TState? initialState = null, ITerminator<TState>? termination = null) {
    //if (termination is null) throw new InvalidOperationException("Cyclic Algorithms require a termination to avoid infinite loops.");
    var stream = InternalCreateExecutionStream(initialState);
    return new ExecutionStream<TState>(stream.TakeWhile(s => termination?.ShouldTerminate(s) ?? true));
  }
  
  #pragma warning disable S2190
  private IEnumerable<TState> InternalCreateExecutionStream(TState? initialState) {
    TState? currentState = initialState;
    while (true) {
      // CreateExecutionStream always returning an empty stream results in an infinite loop
      foreach (var algorithm in Algorithms) {
        var currentStream = algorithm.CreateExecutionStream(currentState);
        foreach (var state in currentStream) {
          yield return currentState = state;
        }
        if (currentState is null) yield break;
        currentState = currentState.Reset();
      }
    }
  }
  #pragma warning restore S2190
}


public class ConcatAlgorithm<TState, TSourceState, TTargetState> : AlgorithmBase<TState>
  where TState : class, IState
  where TSourceState : class, TState 
  where TTargetState : class, TState {
  
  public IAlgorithm<TSourceState> FirstAlgorithm { get; }
  public IAlgorithm<TTargetState> SecondAlgorithm { get; }
  public IStateTransformer<TSourceState, TTargetState> Transformer { get; }
  
  public ConcatAlgorithm(IAlgorithm<TSourceState> firstAlgorithm, IAlgorithm<TTargetState> secondAlgorithm, IStateTransformer<TSourceState, TTargetState> transformer) {
    FirstAlgorithm = firstAlgorithm;
    SecondAlgorithm = secondAlgorithm;
    Transformer = transformer;
  }
  
  public override TTargetState? Execute(TState? initialState = null, ITerminator<TState>? termination = null) {
    if (initialState is not null && initialState is not TSourceState) throw new ArgumentException("Initial state must be of type TSourceState.", nameof(initialState));
    
    var sourceState = FirstAlgorithm.Execute(initialState as TSourceState);
    if (sourceState is null) return null;
    var initialTargetState = Transformer.Transform(sourceState);
    if (termination is not null && termination.ShouldTerminate(initialTargetState)) 
      return initialTargetState;
    var targetState = SecondAlgorithm.Execute(initialTargetState);
    return targetState;
  }
  public override ExecutionStream<TState> CreateExecutionStream(TState? initialState = null, ITerminator<TState>? termination = null) {
    if (initialState is not null && initialState is not TSourceState) throw new ArgumentException("Initial state must be of type TSourceState.", nameof(initialState));
    var stream = InternalCreateExecutionStream(initialState as TSourceState);
    if (termination is not null) {
      stream = stream.TakeUntil(termination.ShouldTerminate);
    }
    return new ExecutionStream<TState>(stream);
  }

  private IEnumerable<TState> InternalCreateExecutionStream(TSourceState? initialState) {
    TSourceState? currentSourceState = initialState;
    var sourceStream = FirstAlgorithm.CreateExecutionStream(initialState);
    foreach (var sourceState in sourceStream) {
      yield return currentSourceState = sourceState;
    }
    var initialTargetState = currentSourceState is not null ? Transformer.Transform(currentSourceState) : null;
    var targetStream = SecondAlgorithm.CreateExecutionStream(initialTargetState);
    foreach (var targetState in targetStream) {
      yield return targetState;
    }
  }
}

public class CyclicAlgorithm<TState, TSourceState, TTargetState> : AlgorithmBase<TState>
  where TState : class, IState
  where TSourceState : class, TState
  where TTargetState : class, TState 
{
  public IAlgorithm<TSourceState> FirstAlgorithm { get; }
  public IAlgorithm<TTargetState> SecondAlgorithm { get; }
  public IStateTransformer<TSourceState, TTargetState> Transformer { get; }
  public IStateTransformer<TTargetState, TSourceState> RepetitionTransformer { get; }
  
  public CyclicAlgorithm(IAlgorithm<TSourceState> firstAlgorithm, IAlgorithm<TTargetState> secondAlgorithm, IStateTransformer<TSourceState, TTargetState> transformer, IStateTransformer<TTargetState, TSourceState> repetitionTransformer) {
    FirstAlgorithm = firstAlgorithm;
    SecondAlgorithm = secondAlgorithm;
    Transformer = transformer;
    RepetitionTransformer = repetitionTransformer;
  }

  public override TState? Execute(TState? initialState = null, ITerminator<TState>? termination = null) {
    if (termination is null) throw new InvalidOperationException("Cyclic Algorithms require a termination to avoid infinite loops.");
    if (initialState is not null && initialState is not TSourceState) throw new ArgumentException("Initial state must be of type TSourceState.", nameof(initialState));

    TSourceState? lastSourceState = initialState as TSourceState;
    TTargetState? lastTargetState = null;

    while (true) {
      if (lastSourceState is not null && termination.ShouldTerminate(lastSourceState))
        return lastSourceState;
      lastSourceState = FirstAlgorithm.Execute(lastSourceState);
      if (lastSourceState is null) return null;
      
      lastTargetState = Transformer.Transform(lastSourceState, lastTargetState);

      if (termination.ShouldTerminate(lastTargetState))
        return lastTargetState;
      
      lastTargetState = SecondAlgorithm.Execute(lastTargetState);
      if (lastTargetState is null) return null;

      lastSourceState = RepetitionTransformer.Transform(lastTargetState, lastSourceState);
    }
  }
  
  public override ExecutionStream<TState> CreateExecutionStream(TState? initialState = null, ITerminator<TState>? termination = null) {
    //if (termination is null) throw new InvalidOperationException("Cyclic Algorithms require a termination to avoid infinite loops.");
    if (initialState is not null && initialState is not TSourceState) throw new ArgumentException("Initial state must be of type TSourceState.", nameof(initialState));
    
    var stream = InternalCreateExecutionStream(initialState as TSourceState);
    return new ExecutionStream<TState>(stream.TakeWhile(state => termination?.ShouldContinue(state) ?? true));
  }

   #pragma warning disable S2190
  private IEnumerable<TState> InternalCreateExecutionStream(TSourceState? initialState = null) {
    TSourceState? lastSourceState = initialState;
    TTargetState? lastTargetState = null;

    while (true) {
      var sourceStream = FirstAlgorithm.CreateExecutionStream(lastSourceState);
      foreach (var sourceState in sourceStream) {
        yield return lastSourceState = sourceState;
      }
      if (lastSourceState is null) yield break;
      
      lastTargetState = Transformer.Transform(lastSourceState, lastTargetState);
      
      var targetStream = SecondAlgorithm.CreateExecutionStream(lastTargetState);
      foreach (var targetState in targetStream) {
        yield return lastTargetState = targetState;
      }
      if (lastTargetState is null) yield break;
      
      lastSourceState = RepetitionTransformer.Transform(lastTargetState, lastSourceState);
    }
  }
  #pragma warning restore S2190
}
