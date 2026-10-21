using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, executors or executor resolution facilities.
/// <see cref="CreateInitialState"/> must return a fresh state object for each preparation. Bindings of that execution share the state.
/// Calls are not inherently thread safe.
/// Use <see cref="Creator{TCandidate,TSearchSpace,TProblem}"/> when the creator needs execution graph dependencies.
/// </remarks>
public abstract record StatefulCreator<TCandidate, TSearchSpace, TProblem, TState>
    : Creator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Create(int count, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected sealed override ExecutorFactory<ICreationExecutor<TCandidate, TSearchSpace, TProblem>> CreateFactory()
    {
        var executor = new Executor(this, CreateInitialState());
        return _ => executor;
    }

    private sealed class Executor(StatefulCreator<TCandidate, TSearchSpace, TProblem, TState> creator, TState state)
        : CreationExecutor<TCandidate, TSearchSpace, TProblem>
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            creator.Create(count, state, random, searchSpace, problem);
    }
}

public abstract record StatefulCreator<TCandidate, TSearchSpace, TState>
    : Creator<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Create(int count, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

    protected sealed override ExecutorFactory<ICreationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>> CreateFactory()
    {
        var executor = new Executor(this, CreateInitialState());
        return _ => executor;
    }

    private sealed class Executor(StatefulCreator<TCandidate, TSearchSpace, TState> creator, TState state)
        : CreationExecutor<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
            creator.Create(count, state, random, searchSpace);
    }
}

public abstract record StatefulCreator<TCandidate, TState>
    : Creator<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Create(int count, TState state, IRandomNumberGenerator random);

    protected sealed override ExecutorFactory<ICreationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> CreateFactory()
    {
        var executor = new Executor(this, CreateInitialState());
        return _ => executor;
    }

    private sealed class Executor(StatefulCreator<TCandidate, TState> creator, TState state)
        : CreationExecutor<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random) =>
            creator.Create(count, state, random);
    }
}
