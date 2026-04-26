using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public record IdentityInterceptor<TG, TSearchState> : StatelessInterceptor<TG, TSearchState>
  where TSearchState : class, ISearchState
{
  public override TSearchState Transform(TSearchState currentState, TSearchState? previousState) => IdentityInterceptor.Transform(currentState, previousState);
}

public static class IdentityInterceptor
{
  public static TSearchState Transform<TSearchState>(TSearchState currentState, TSearchState? previousState)
    where TSearchState : class, ISearchState => currentState;
}
