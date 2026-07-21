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
/// </remarks>
public abstract record StatefulCrossover<TCandidate, TSearchSpace, TProblem, TState>
    : Crossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected sealed override ICrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateCrossoverInstance(ExecutionInstanceRegistry registry) => new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulCrossover<TCandidate, TSearchSpace, TProblem, TState> crossover, TState state) : ICrossoverInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => crossover.Cross(parents, state, random, searchSpace, problem);
    }
}

public abstract record StatefulCrossover<TCandidate, TSearchSpace, TState>
    : Crossover<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

    protected sealed override ICrossoverInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateCrossoverInstance(ExecutionInstanceRegistry registry) => new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulCrossover<TCandidate, TSearchSpace, TState> crossover, TState state) : ICrossoverInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    {
        public IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) => crossover.Cross(parents, state, random, searchSpace);
    }
}

public abstract record StatefulCrossover<TCandidate, TState>
    : Crossover<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, TState state, IRandomNumberGenerator random);

    protected sealed override ICrossoverInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateCrossoverInstance(ExecutionInstanceRegistry registry) => new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulCrossover<TCandidate, TState> crossover, TState state) : ICrossoverInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
    {
        public IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => crossover.Cross(parents, state, random);
    }
}
