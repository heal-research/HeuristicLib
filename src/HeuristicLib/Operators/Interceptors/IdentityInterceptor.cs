using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public sealed record IdentityInterceptor<TCandidate, TSearchState> : StatelessInterceptor<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random) => IdentityInterceptor.Transform(currentState, previousState);
}

public static class IdentityInterceptor
{
    public static IdentityInterceptor<TCandidate, TSearchState> For<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState => new();

    public static TSearchState Transform<TSearchState>(TSearchState currentState, TSearchState? previousState)
        where TSearchState : class, ISearchState => currentState;
}
