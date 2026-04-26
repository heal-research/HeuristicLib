using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public abstract record Selector<TGenotype, TSearchSpace, TProblem, TExecutionState>
  : ISelector<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population,
    Objective objective, int count, TExecutionState executionState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public ISelectorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new SelectorInstance(this, CreateInitialState());

  private sealed class SelectorInstance(Selector<TGenotype, TSearchSpace, TProblem, TExecutionState> selector, TExecutionState executionState)
    : ISelectorInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return selector.Select(population, objective, count, executionState, random, searchSpace, problem);
    }
  }
}

public abstract record Selector<TGenotype, TSearchSpace, TExecutionState>
  : ISelector<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population,
    Objective objective, int count, TExecutionState executionState, IRandomNumberGenerator random, TSearchSpace searchSpace);

  public ISelectorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new SelectorInstance(this, CreateInitialState());

  private sealed class SelectorInstance(Selector<TGenotype, TSearchSpace, TExecutionState> selector, TExecutionState executionState)
    : ISelectorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  {
    public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem)
    {
      return selector.Select(population, objective, count, executionState, random, searchSpace);
    }
  }
}

public abstract record Selector<TGenotype, TExecutionState>
  : ISelector<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population,
    Objective objective, int count, TExecutionState executionState, IRandomNumberGenerator random);

  public ISelectorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new SelectorInstance(this, CreateInitialState());

  private sealed class SelectorInstance(Selector<TGenotype, TExecutionState> selector, TExecutionState executionState)
    : ISelectorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return selector.Select(population, objective, count, executionState, random);
    }
  }
}

