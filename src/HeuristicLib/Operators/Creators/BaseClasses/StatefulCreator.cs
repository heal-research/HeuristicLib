using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, execution nodes or execution node resolution facilities.
/// <see cref="CreateInitialState"/> must return a fresh state object for every execution node. Calls are not inherently thread safe.
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

    public sealed override ICreatorExecution<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ResolutionScope scope) =>
        new Execution(this, CreateInitialState());

    private sealed class Execution(StatefulCreator<TCandidate, TSearchSpace, TProblem, TState> creator, TState state)
        : CreatorExecution<TCandidate, TSearchSpace, TProblem>
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

    public sealed override ICreatorExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutionInstance(ResolutionScope scope) =>
        new Execution(this, CreateInitialState());

    private sealed class Execution(StatefulCreator<TCandidate, TSearchSpace, TState> creator, TState state)
        : CreatorExecution<TCandidate, TSearchSpace>
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

    public sealed override ICreatorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutionInstance(ResolutionScope scope) =>
        new Execution(this, CreateInitialState());

    private sealed class Execution(StatefulCreator<TCandidate, TState> creator, TState state)
        : CreatorExecution<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random) =>
            creator.Create(count, state, random);
    }
}
