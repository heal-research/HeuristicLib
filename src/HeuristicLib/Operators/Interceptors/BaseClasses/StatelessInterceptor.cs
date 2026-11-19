using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract record StatelessInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> : Interceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    protected sealed override IInterceptionExecutor<TSearchState> CreateExecutor(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) => new Executor(this, scope.Problem);

    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    private sealed class Executor(StatelessInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> configuration, TProblem problem) : InterceptionExecutor<TSearchState>
    {
        public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random) => configuration.Transform(currentState, previousState, random, problem.SearchSpace, problem);
    }
}

public abstract record StatelessInterceptor<TCandidate, TSearchSpace, TSearchState> : Interceptor<TCandidate, TSearchSpace, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TSearchState : class, ISearchState
{
    protected sealed override IInterceptionExecutor<TSearchState> CreateExecutor(ResolutionScope<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState> scope) => new Executor(this, scope.SearchSpace);

    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace);

    private sealed class Executor(StatelessInterceptor<TCandidate, TSearchSpace, TSearchState> configuration, TSearchSpace searchSpace) : InterceptionExecutor<TSearchState>
    {
        public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random) => configuration.Transform(currentState, previousState, random, searchSpace);
    }
}

public abstract record StatelessInterceptor<TCandidate, TSearchState> : Interceptor<TCandidate, TSearchState>, IInterceptionExecutor<TSearchState>
    where TSearchState : class, ISearchState
{
    protected sealed override IInterceptionExecutor<TSearchState> CreateExecutor(ResolutionScope<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState> scope) => this;

    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random);
}
