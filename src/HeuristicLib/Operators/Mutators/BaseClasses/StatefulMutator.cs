using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, executors or executor resolution facilities.
/// <see cref="CreateExecutionState"/> must return a fresh state object. Resolution requests it once per owning execution state, and every executor built there shares it.
/// Calls are not inherently thread safe.
/// Use <see cref="Mutator{TCandidate,TSearchSpace,TProblem}"/> when the mutator needs execution graph dependencies.
/// </remarks>
public abstract record StatefulMutator<TCandidate, TSearchSpace, TProblem, TState>
    : Mutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateExecutionState();

    protected abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected sealed override IMutationExecutor<TCandidate, TSearchSpace, TProblem> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetExecutionState(this, CreateExecutionState));

    private sealed class Executor(StatefulMutator<TCandidate, TSearchSpace, TProblem, TState> mutator, TState state)
        : MutationExecutor<TCandidate, TSearchSpace, TProblem>
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
    protected abstract TState CreateExecutionState();

    protected abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

    protected sealed override IMutationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetExecutionState(this, CreateExecutionState));

    private sealed class Executor(StatefulMutator<TCandidate, TSearchSpace, TState> mutator, TState state)
        : MutationExecutor<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
            mutator.Mutate(parents, state, random, searchSpace);
    }
}

public abstract record StatefulMutator<TCandidate, TState>
    : Mutator<TCandidate>
    where TState : class
{
    protected abstract TState CreateExecutionState();

    protected abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, TState state, IRandomNumberGenerator random);

    protected sealed override IMutationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetExecutionState(this, CreateExecutionState));

    private sealed class Executor(StatefulMutator<TCandidate, TState> mutator, TState state)
        : MutationExecutor<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random) =>
            mutator.Mutate(parents, state, random);
    }
}
