using System.Collections;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithm { }

public interface IAlgorithm<TState> : IAlgorithm where TState : class {
  TState? Execute(TState? initialState = null, ITerminator<TState>? termination = null);
  ExecutionStream<TState> CreateExecutionStream(TState? initialState = null, ITerminator<TState>? termination = null);
}

public abstract class AlgorithmBase<TState> : IAlgorithm<TState> where TState : class {
  public abstract TState? Execute(TState? initialState = null, ITerminator<TState>? termination = null);
  
  public abstract ExecutionStream<TState> CreateExecutionStream(TState? initialState = null, ITerminator<TState>? termination = null);
}

public class ExecutionStream<TState> : IEnumerable<TState> {
  private readonly IEnumerable<TState> internalStream;
  public ExecutionStream(IEnumerable<TState> internalStream) {
    this.internalStream = internalStream;
  }
  public IEnumerator<TState> GetEnumerator() => internalStream.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
