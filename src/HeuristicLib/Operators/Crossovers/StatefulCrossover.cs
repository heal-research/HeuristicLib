using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, execution instances or execution instance resolution facilities.
/// <see cref="CreateInitialState"/> must return a fresh state object for every execution instance. Calls are not inherently thread safe.
/// Use <see cref="Crossover{TCandidate,TSearchSpace,TProblem}"/> when the crossover needs execution graph dependencies.
/// </remarks>
public abstract record StatefulCrossover<TCandidate, TSearchSpace, TProblem, TState>
    : Crossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public sealed override ICrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulCrossover<TCandidate, TSearchSpace, TProblem, TState> crossover, TState state)
        : CrossoverInstance<TCandidate, TSearchSpace, TProblem>
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            crossover.Cross(parents, state, random, searchSpace, problem);
    }
}

public abstract record StatefulCrossover<TCandidate, TSearchSpace, TState>
    : Crossover<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

    public sealed override ICrossoverInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulCrossover<TCandidate, TSearchSpace, TState> crossover, TState state)
        : CrossoverInstance<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
            crossover.Cross(parents, state, random, searchSpace);
    }
}

public abstract record StatefulCrossover<TCandidate, TState>
    : Crossover<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, TState state, IRandomNumberGenerator random);

    public sealed override ICrossoverInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulCrossover<TCandidate, TState> crossover, TState state)
        : CrossoverInstance<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random) =>
            crossover.Cross(parents, state, random);
    }
}
