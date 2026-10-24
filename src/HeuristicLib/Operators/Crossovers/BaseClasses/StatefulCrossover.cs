using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, executors or executor resolution facilities.
/// <see cref="CreateExecutionState"/> must return a fresh state object. Resolution requests it once per owning execution state, and every executor built there shares it.
/// Calls are not inherently thread safe.
/// Use <see cref="Crossover{TCandidate,TSearchSpace,TProblem}"/> when the crossover needs execution graph dependencies.
/// </remarks>
public abstract record StatefulCrossover<TCandidate, TSearchSpace, TProblem, TState>
    : Crossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateExecutionState();

    protected abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected sealed override ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetExecutionState(this, CreateExecutionState));

    private sealed class Executor(StatefulCrossover<TCandidate, TSearchSpace, TProblem, TState> crossover, TState state)
        : CrossoverExecutor<TCandidate, TSearchSpace, TProblem>
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
    protected abstract TState CreateExecutionState();

    protected abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

    protected sealed override ICrossoverExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetExecutionState(this, CreateExecutionState));

    private sealed class Executor(StatefulCrossover<TCandidate, TSearchSpace, TState> crossover, TState state)
        : CrossoverExecutor<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
            crossover.Cross(parents, state, random, searchSpace);
    }
}

public abstract record StatefulCrossover<TCandidate, TState>
    : Crossover<TCandidate>
    where TState : class
{
    protected abstract TState CreateExecutionState();

    protected abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, TState state, IRandomNumberGenerator random);

    protected sealed override ICrossoverExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetExecutionState(this, CreateExecutionState));

    private sealed class Executor(StatefulCrossover<TCandidate, TState> crossover, TState state)
        : CrossoverExecutor<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random) =>
            crossover.Cross(parents, state, random);
    }
}
