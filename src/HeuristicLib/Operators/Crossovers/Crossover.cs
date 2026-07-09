using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record Crossover<TCandidate, TSearchSpace, TProblem, TExecutionState>
  : ICrossover<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, TExecutionState executionState,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public ICrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new CrossoverInstance(this, CreateInitialState());

    private sealed class CrossoverInstance(Crossover<TCandidate, TSearchSpace, TProblem, TExecutionState> crossover, TExecutionState executionState)
      : ICrossoverInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            return crossover.Cross(parents, executionState, random, searchSpace, problem);
        }
    }
}

public abstract record Crossover<TCandidate, TSearchSpace, TExecutionState>
  : ICrossover<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, TExecutionState executionState,
      IRandomNumberGenerator random, TSearchSpace searchSpace);

    public ICrossoverInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new CrossoverInstance(this, CreateInitialState());

    private sealed class CrossoverInstance(Crossover<TCandidate, TSearchSpace, TExecutionState> crossover, TExecutionState executionState)
      : ICrossoverInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    {
        public IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem)
        {
            return crossover.Cross(parents, executionState, random, searchSpace);
        }
    }
}

public abstract record Crossover<TCandidate, TExecutionState>
  : ICrossover<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, TExecutionState executionState,
      IRandomNumberGenerator random);

    public ICrossoverInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new CrossoverInstance(this, CreateInitialState());

    private sealed class CrossoverInstance(Crossover<TCandidate, TExecutionState> crossover, TExecutionState executionState)
      : ICrossoverInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
    {
        public IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
        {
            return crossover.Cross(parents, executionState, random);
        }
    }
}



