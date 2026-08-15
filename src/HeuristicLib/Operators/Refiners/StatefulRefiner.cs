using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, execution instances or execution instance resolution facilities.
/// <see cref="CreateInitialState"/> must return a fresh state object for every execution instance. Calls are not inherently thread safe.
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

    public sealed override IRefinerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulRefiner<TCandidate, TSearchSpace, TProblem, TState> refiner, TState state)
        : RefinerInstance<TCandidate, TSearchSpace, TProblem>
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

    public sealed override IRefinerInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulRefiner<TCandidate, TSearchSpace, TState> refiner, TState state)
        : RefinerInstance<TCandidate, TSearchSpace>
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

    public sealed override IRefinerInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulRefiner<TCandidate, TState> refiner, TState state)
        : RefinerInstance<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) =>
            refiner.Refine(candidates, state, random);
    }
}
