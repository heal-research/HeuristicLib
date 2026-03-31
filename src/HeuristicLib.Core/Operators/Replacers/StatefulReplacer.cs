using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public abstract record StatefulReplacer<TGenotype, TSearchSpace, TProblem, TState>
  : IReplacer<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation,
    IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, TState state,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public IReplacerInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulReplacerInstance(this, CreateInitialState());

  private sealed class StatefulReplacerInstance(StatefulReplacer<TGenotype, TSearchSpace, TProblem, TState> replacer, TState state)
    : IReplacerInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return replacer.Replace(previousPopulation, offspringPopulation, objective, count, state, random, searchSpace, problem);
    }
  }
}

public abstract record StatefulReplacer<TGenotype, TSearchSpace, TState>
  : IReplacer<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation,
    IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, TState state,
    IRandomNumberGenerator random, TSearchSpace searchSpace);

  public IReplacerInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulReplacerInstance(this, CreateInitialState());

  private sealed class StatefulReplacerInstance(StatefulReplacer<TGenotype, TSearchSpace, TState> replacer, TState state)
    : IReplacerInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  {
    public IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem)
    {
      return replacer.Replace(previousPopulation, offspringPopulation, objective, count, state, random, searchSpace);
    }
  }
}

public abstract record StatefulReplacer<TGenotype, TState>
  : IReplacer<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation,
    IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, TState state,
    IRandomNumberGenerator random);

  public IReplacerInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulReplacerInstance(this, CreateInitialState());

  private sealed class StatefulReplacerInstance(StatefulReplacer<TGenotype, TState> replacer, TState state)
    : IReplacerInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    public IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return replacer.Replace(previousPopulation, offspringPopulation, objective, count, state, random);
    }
  }
}



