namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithm { }

public interface IAlgorithm<TState> : IAlgorithm where TState : class {
  TState Run(TState? initialState = null);
}

public abstract class AlgorithmBase<TState> : IAlgorithm<TState> where TState : class {
  public abstract TState Run(TState? initialState = null);
}
