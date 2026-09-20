using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public sealed record IdentityInterceptor<TCandidate, TSearchState> : StatelessInterceptor<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random) => IdentityInterceptor.Transform(currentState, previousState);
}

public static class IdentityInterceptor
{

    public static TSearchState Transform<TSearchState>(TSearchState currentState, TSearchState? previousState)
        where TSearchState : class, ISearchState => currentState;
}
