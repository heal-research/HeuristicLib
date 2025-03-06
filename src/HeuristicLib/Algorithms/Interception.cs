namespace HEAL.HeuristicLib.Algorithms;

public interface IInterceptor<TState> where TState : class, IState {
  TState Transform(TState state);
}

public static class Interceptor {
  public static IInterceptor<TState> Create<TState>(Func<TState, TState> transform) where TState : class, IState {
    return new Interceptor<TState>(transform);
  }
}

public class Interceptor<TState> : IInterceptor<TState> where TState : class, IState {
  private readonly Func<TState, TState> transform;
  internal Interceptor(Func<TState, TState> transform) {
    this.transform = transform;
  }
  public TState Transform(TState state) => transform(state);
}

public class IdentityInterceptor<TState> : IInterceptor<TState> where TState : class, IState {
  public TState Transform(TState state) => state;
}
