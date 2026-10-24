using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, executors or executor resolution facilities.
/// <see cref="CreateExecutionState"/> must return a fresh state object. Resolution requests it once per owning execution state, and every executor built there shares it.
/// Calls are not inherently thread safe.
/// Use <see cref="Refiner{TCandidate,TSearchSpace,TProblem}"/> when the refiner needs execution graph dependencies.
/// </remarks>
public abstract record StatefulRefiner<TCandidate, TSearchSpace, TProblem, TState>
    : Refiner<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateExecutionState();

    protected abstract IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected sealed override IRefinementExecutor<TCandidate, TSearchSpace, TProblem> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetExecutionState(this, CreateExecutionState));

    private sealed class Executor(StatefulRefiner<TCandidate, TSearchSpace, TProblem, TState> refiner, TState state)
        : RefinementExecutor<TCandidate, TSearchSpace, TProblem>
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
    protected abstract TState CreateExecutionState();

    protected abstract IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

    protected sealed override IRefinementExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetExecutionState(this, CreateExecutionState));

    private sealed class Executor(StatefulRefiner<TCandidate, TSearchSpace, TState> refiner, TState state)
        : RefinementExecutor<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
            refiner.Refine(candidates, state, random, searchSpace);
    }
}

public abstract record StatefulRefiner<TCandidate, TState>
    : Refiner<TCandidate>
    where TState : class
{
    protected abstract TState CreateExecutionState();

    protected abstract IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, TState state, IRandomNumberGenerator random);

    protected sealed override IRefinementExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetExecutionState(this, CreateExecutionState));

    private sealed class Executor(StatefulRefiner<TCandidate, TState> refiner, TState state)
        : RefinementExecutor<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) =>
            refiner.Refine(candidates, state, random);
    }
}
