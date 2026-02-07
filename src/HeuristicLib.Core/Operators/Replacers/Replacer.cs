using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public abstract class Replacer<TGenotype, TSearchSpace, TProblem> 
  : IReplacer<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract IReplacerInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public abstract class ReplacerInstance<TGenotype, TSearchSpace, TProblem> 
  : IReplacerInstance<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
 
  public abstract int GetOffspringCount(int populationSize);
}


public abstract class Replacer<TGenotype, TSearchSpace> 
  : IReplacer<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract IReplacerInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public abstract class ReplacerInstance<TGenotype, TSearchSpace> 
  : IReplacerInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, TSearchSpace searchSpace);
  
  public abstract int GetOffspringCount(int populationSize);

  IReadOnlyList<ISolution<TGenotype>> IReplacerInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => 
    Replace(previousPopulation, offspringPopulation, objective, random, searchSpace);
}


public abstract class Replacer<TGenotype> 
  : IReplacer<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public abstract IReplacerInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public abstract class ReplacerInstance<TGenotype>
  : IReplacerInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random);

  public abstract int GetOffspringCount(int populationSize);

  IReadOnlyList<ISolution<TGenotype>> IReplacerInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
    Replace(previousPopulation, offspringPopulation, objective, random);
}


public abstract class StatelessReplacer<TGenotype, TSearchSpace, TProblem> 
  : IReplacer<TGenotype, TSearchSpace, TProblem>,
    IReplacerInstance<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public IReplacerInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
  
  public abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
 
  public abstract int GetOffspringCount(int populationSize);
}

public abstract class StatelessReplacer<TGenotype, TSearchSpace> 
  : IReplacer<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>,
    IReplacerInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public IReplacerInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
  
  public abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, TSearchSpace searchSpace);

  public abstract int GetOffspringCount(int populationSize);

  IReadOnlyList<ISolution<TGenotype>> IReplacerInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => 
    Replace(previousPopulation, offspringPopulation, objective, random, searchSpace);
}

public abstract class StatelessReplacer<TGenotype> 
  : IReplacer<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>,
    IReplacerInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public IReplacerInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
  
  public abstract IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random);

  public abstract int GetOffspringCount(int populationSize);

  IReadOnlyList<ISolution<TGenotype>> IReplacerInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => 
    Replace(previousPopulation, offspringPopulation, objective, random);
}
