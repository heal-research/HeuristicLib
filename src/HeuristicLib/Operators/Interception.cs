using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface IInterceptor<TState> where TState : IResultState {
  TState Transform(TState state);
}

public static class Interceptors {
  public static IdentityInterceptor<TState> Identity<TState>() where TState : IResultState => new IdentityInterceptor<TState>();
  public static IInterceptor<TState> Create<TState>(Func<TState, TState> transform) where TState : IResultState {
    return new CustomInterceptor<TState>(transform);
  }
}

public class CustomInterceptor<TState> : IInterceptor<TState> where TState : IResultState {
  private readonly Func<TState, TState> transform;
  internal CustomInterceptor(Func<TState, TState> transform) {
    this.transform = transform;
  }
  public TState Transform(TState state) => transform(state);
}

public class IdentityInterceptor<TState> : IInterceptor<TState> where TState : IResultState {
  public TState Transform(TState state) => state;
}
