using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public abstract record Replacer<TGenotype, TSearchSpace, TProblem, TExecutionState>
  : IReplacer<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation,
    IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, TExecutionState executionState,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public IReplacerInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new ReplacerInstance(this, CreateInitialState());

  private sealed class ReplacerInstance(Replacer<TGenotype, TSearchSpace, TProblem, TExecutionState> replacer, TExecutionState executionState)
    : IReplacerInstance<TGenotype, TSearchSpace, TProblem>
  {
    public IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return replacer.Replace(previousPopulation, offspringPopulation, objective, count, executionState, random, searchSpace, problem);
    }
  }
}

public abstract record Replacer<TGenotype, TSearchSpace, TExecutionState>
  : IReplacer<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation,
    IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, TExecutionState executionState,
    IRandomNumberGenerator random, TSearchSpace searchSpace);

  public IReplacerInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new ReplacerInstance(this, CreateInitialState());

  private sealed class ReplacerInstance(Replacer<TGenotype, TSearchSpace, TExecutionState> replacer, TExecutionState executionState)
    : IReplacerInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  {
    public IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem)
    {
      return replacer.Replace(previousPopulation, offspringPopulation, objective, count, executionState, random, searchSpace);
    }
  }
}

public abstract record Replacer<TGenotype, TExecutionState>
  : IReplacer<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation,
    IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, TExecutionState executionState,
    IRandomNumberGenerator random);

  public IReplacerInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new ReplacerInstance(this, CreateInitialState());

  private sealed class ReplacerInstance(Replacer<TGenotype, TExecutionState> replacer, TExecutionState executionState)
    : IReplacerInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    public IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return replacer.Replace(previousPopulation, offspringPopulation, objective, count, executionState, random);
    }
  }
}



