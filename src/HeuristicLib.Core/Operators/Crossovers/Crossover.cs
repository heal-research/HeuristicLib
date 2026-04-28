using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record Crossover<TGenotype, TSearchSpace, TProblem, TExecutionState>
  : ICrossover<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, TExecutionState executionState,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public ICrossoverInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new CrossoverInstance(this, CreateInitialState());

  private sealed class CrossoverInstance(Crossover<TGenotype, TSearchSpace, TProblem, TExecutionState> crossover, TExecutionState executionState)
    : ICrossoverInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return crossover.Cross(parents, executionState, random, searchSpace, problem);
    }
  }
}

public abstract record Crossover<TGenotype, TSearchSpace, TExecutionState>
  : ICrossover<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, TExecutionState executionState,
    IRandomNumberGenerator random, TSearchSpace searchSpace);

  public ICrossoverInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new CrossoverInstance(this, CreateInitialState());

  private sealed class CrossoverInstance(Crossover<TGenotype, TSearchSpace, TExecutionState> crossover, TExecutionState executionState)
    : ICrossoverInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  {
    public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem)
    {
      return crossover.Cross(parents, executionState, random, searchSpace);
    }
  }
}

public abstract record Crossover<TGenotype, TExecutionState>
  : ICrossover<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, TExecutionState executionState,
    IRandomNumberGenerator random);

  public ICrossoverInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new CrossoverInstance(this, CreateInitialState());

  private sealed class CrossoverInstance(Crossover<TGenotype, TExecutionState> crossover, TExecutionState executionState)
    : ICrossoverInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return crossover.Cross(parents, executionState, random);
    }
  }
}



