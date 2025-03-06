namespace HEAL.HeuristicLib.Algorithms;

public interface IInterceptor<TState> where TState : class, IState {
  TState Transform(TState state);
}

public class IdentityInterceptor<TState> : IInterceptor<TState> where TState : class, IState {
  public TState Transform(TState state) => state;
}
