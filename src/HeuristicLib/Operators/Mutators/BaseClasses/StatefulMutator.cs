using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, execution nodes or execution node resolution facilities.
/// <see cref="CreateInitialState"/> must return a fresh state object for each preparation. Bindings of that execution share the state.
/// Calls are not inherently thread safe.
/// Use <see cref="Mutator{TCandidate,TSearchSpace,TProblem}"/> when the mutator needs execution graph dependencies.
/// </remarks>
public abstract record StatefulMutator<TCandidate, TSearchSpace, TProblem, TState>
    : Mutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public sealed override ExecutionFactory<IMutatorExecution<TCandidate, TSearchSpace, TProblem>> CreateExecutionFactory()
    {
        var execution = new Execution(this, CreateInitialState());
        return _ => execution;
    }

    private sealed class Execution(StatefulMutator<TCandidate, TSearchSpace, TProblem, TState> mutator, TState state)
        : MutatorExecution<TCandidate, TSearchSpace, TProblem>
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            mutator.Mutate(parents, state, random, searchSpace, problem);
    }
}

public abstract record StatefulMutator<TCandidate, TSearchSpace, TState>
    : Mutator<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

    public sealed override ExecutionFactory<IMutatorExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>> CreateExecutionFactory()
    {
        var execution = new Execution(this, CreateInitialState());
        return _ => execution;
    }

    private sealed class Execution(StatefulMutator<TCandidate, TSearchSpace, TState> mutator, TState state)
        : MutatorExecution<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
            mutator.Mutate(parents, state, random, searchSpace);
    }
}

public abstract record StatefulMutator<TCandidate, TState>
    : Mutator<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, TState state, IRandomNumberGenerator random);

    public sealed override ExecutionFactory<IMutatorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> CreateExecutionFactory()
    {
        var execution = new Execution(this, CreateInitialState());
        return _ => execution;
    }

    private sealed class Execution(StatefulMutator<TCandidate, TState> mutator, TState state)
        : MutatorExecution<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random) =>
            mutator.Mutate(parents, state, random);
    }
}
