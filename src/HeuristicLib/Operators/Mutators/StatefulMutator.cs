using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, execution instances or execution instance resolution facilities.
/// <see cref="CreateInitialState"/> must return a fresh state object for every execution instance. Calls are not inherently thread safe.
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

    protected sealed override IMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateMutatorInstance(ExecutionInstanceRegistry registry) =>
        new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulMutator<TCandidate, TSearchSpace, TProblem, TState> mutator, TState state)
      : IMutatorInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
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

    protected sealed override IMutatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateMutatorInstance(ExecutionInstanceRegistry registry) =>
        new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulMutator<TCandidate, TSearchSpace, TState> mutator, TState state)
      : IMutatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
            mutator.Mutate(parents, state, random, searchSpace);
    }
}

public abstract record StatefulMutator<TCandidate, TState>
    : Mutator<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, TState state, IRandomNumberGenerator random);

    protected sealed override IMutatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateMutatorInstance(ExecutionInstanceRegistry registry) =>
        new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulMutator<TCandidate, TState> mutator, TState state)
        : IMutatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
            mutator.Mutate(parents, state, random);
    }
}
