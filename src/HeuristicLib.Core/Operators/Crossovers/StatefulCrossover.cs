using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record StatefulCrossover<TGenotype, TSearchSpace, TProblem, TState>
  : ICrossover<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, TState state,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public ICrossoverInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulCrossoverInstance(this, CreateInitialState());

  private sealed class StatefulCrossoverInstance(StatefulCrossover<TGenotype, TSearchSpace, TProblem, TState> crossover, TState state)
    : ICrossoverInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return crossover.Cross(parents, state, random, searchSpace, problem);
    }
  }
}

public abstract record StatefulCrossover<TGenotype, TSearchSpace, TState>
  : ICrossover<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, TState state,
    IRandomNumberGenerator random, TSearchSpace searchSpace);

  public ICrossoverInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulCrossoverInstance(this, CreateInitialState());

  private sealed class StatefulCrossoverInstance(StatefulCrossover<TGenotype, TSearchSpace, TState> crossover, TState state)
    : ICrossoverInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  {
    public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem)
    {
      return crossover.Cross(parents, state, random, searchSpace);
    }
  }
}

public abstract record StatefulCrossover<TGenotype, TState>
  : ICrossover<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, TState state,
    IRandomNumberGenerator random);

  public ICrossoverInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulCrossoverInstance(this, CreateInitialState());

  private sealed class StatefulCrossoverInstance(StatefulCrossover<TGenotype, TState> crossover, TState state)
    : ICrossoverInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return crossover.Cross(parents, state, random);
    }
  }
}



