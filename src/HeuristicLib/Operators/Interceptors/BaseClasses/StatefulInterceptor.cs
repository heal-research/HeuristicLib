using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Interceptors;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, executors or executor resolution facilities.
/// <see cref="CreateInitialState"/> must return a fresh state object for each preparation. Bindings of that execution share the state.
/// Calls are not inherently thread safe.
/// </remarks>
public abstract record StatefulInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState, TState>
    : Interceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public sealed override ExecutorFactory<IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>> CreateExecutorFactory()
    {
        var executor = new Executor(this, CreateInitialState());
        return _ => executor;
    }

    private sealed class Executor(StatefulInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState, TState> interceptor, TState state)
        : InterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            interceptor.Transform(currentState, previousState, state, random, searchSpace, problem);
    }
}

public abstract record StatefulInterceptor<TCandidate, TSearchSpace, TSearchState, TState>
    : Interceptor<TCandidate, TSearchSpace, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

    public sealed override ExecutorFactory<IInterceptionExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>> CreateExecutorFactory()
    {
        var executor = new Executor(this, CreateInitialState());
        return _ => executor;
    }

    private sealed class Executor(StatefulInterceptor<TCandidate, TSearchSpace, TSearchState, TState> interceptor, TState state)
        : InterceptionExecutor<TCandidate, TSearchSpace, TSearchState>
    {
        public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
            interceptor.Transform(currentState, previousState, state, random, searchSpace);
    }
}

public abstract record StatefulInterceptor<TCandidate, TSearchState, TState>
    : Interceptor<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TState state, IRandomNumberGenerator random);

    public sealed override ExecutorFactory<IInterceptionExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>> CreateExecutorFactory()
    {
        var executor = new Executor(this, CreateInitialState());
        return _ => executor;
    }

    private sealed class Executor(StatefulInterceptor<TCandidate, TSearchState, TState> interceptor, TState state)
        : InterceptionExecutor<TCandidate, TSearchState>
    {
        public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random) =>
            interceptor.Transform(currentState, previousState, state, random);
    }
}
