using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, execution nodes or execution node resolution facilities.
/// <see cref="CreateInitialState"/> must return a fresh state object for each preparation. Bindings of that execution share the state.
/// Calls are not inherently thread safe.
/// Use <see cref="Refiner{TCandidate,TSearchSpace,TProblem}"/> when the refiner needs execution graph dependencies.
/// </remarks>
public abstract record StatefulRefiner<TCandidate, TSearchSpace, TProblem, TState>
    : Refiner<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public sealed override ExecutionFactory<IRefinerExecution<TCandidate, TSearchSpace, TProblem>> CreateExecutionFactory()
    {
        var execution = new Execution(this, CreateInitialState());
        return _ => execution;
    }

    private sealed class Execution(StatefulRefiner<TCandidate, TSearchSpace, TProblem, TState> refiner, TState state)
        : RefinerExecution<TCandidate, TSearchSpace, TProblem>
    {
        public override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            refiner.Refine(candidates, state, random, searchSpace, problem);
    }
}

public abstract record StatefulRefiner<TCandidate, TSearchSpace, TState>
    : Refiner<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

    public sealed override ExecutionFactory<IRefinerExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>> CreateExecutionFactory()
    {
        var execution = new Execution(this, CreateInitialState());
        return _ => execution;
    }

    private sealed class Execution(StatefulRefiner<TCandidate, TSearchSpace, TState> refiner, TState state)
        : RefinerExecution<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
            refiner.Refine(candidates, state, random, searchSpace);
    }
}

public abstract record StatefulRefiner<TCandidate, TState>
    : Refiner<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, TState state, IRandomNumberGenerator random);

    public sealed override ExecutionFactory<IRefinerExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> CreateExecutionFactory()
    {
        var execution = new Execution(this, CreateInitialState());
        return _ => execution;
    }

    private sealed class Execution(StatefulRefiner<TCandidate, TState> refiner, TState state)
        : RefinerExecution<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) =>
            refiner.Refine(candidates, state, random);
    }
}
