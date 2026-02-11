using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public abstract record Selector<TGenotype, TSearchSpace, TProblem>
  : ISelector<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract ISelectorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
  
  public abstract class Instance
    : ISelectorInstance<TGenotype, TSearchSpace, TProblem>
  {
    public abstract IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
  }
}

public abstract record Selector<TGenotype, TSearchSpace>
  : ISelector<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract ISelectorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
  
  public abstract class Instance
    : ISelectorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  {
    public abstract IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<ISolution<TGenotype>> ISelectorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) =>
      Select(population, objective, count, random, searchSpace);
  }
}

public abstract record Selector<TGenotype>
  : ISelector<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public abstract ISelectorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
  
  public abstract class Instance
    : ISelectorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    public abstract IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random);

    public IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
      Select(population, objective, count, random);
  }
}

