using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public abstract record StatefulSelector<TGenotype, TSearchSpace, TProblem, TState>
  : ISelector<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population,
    Objective objective, int count, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public ISelectorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulSelectorInstance(this, CreateInitialState());

  private sealed class StatefulSelectorInstance(StatefulSelector<TGenotype, TSearchSpace, TProblem, TState> selector, TState state)
    : ISelectorInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return selector.Select(population, objective, count, state, random, searchSpace, problem);
    }
  }
}

public abstract record StatefulSelector<TGenotype, TSearchSpace, TState>
  : ISelector<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population,
    Objective objective, int count, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

  public ISelectorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulSelectorInstance(this, CreateInitialState());

  private sealed class StatefulSelectorInstance(StatefulSelector<TGenotype, TSearchSpace, TState> selector, TState state)
    : ISelectorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  {
    public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem)
    {
      return selector.Select(population, objective, count, state, random, searchSpace);
    }
  }
}

public abstract record StatefulSelector<TGenotype, TState>
  : ISelector<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population,
    Objective objective, int count, TState state, IRandomNumberGenerator random);

  public ISelectorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulSelectorInstance(this, CreateInitialState());

  private sealed class StatefulSelectorInstance(StatefulSelector<TGenotype, TState> selector, TState state)
    : ISelectorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return selector.Select(population, objective, count, state, random);
    }
  }
}

