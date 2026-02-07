using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public class EliteSelector<TGenotype, TSearchSpace, TProblem> 
  : Selector<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  private readonly ISelector<TGenotype, TSearchSpace, TProblem> selector;
  private readonly BestSelector<TGenotype> best = new();
  private readonly int elites;
  
  public EliteSelector(ISelector<TGenotype, TSearchSpace, TProblem> selector, int elites = 1)
  {
    this.selector = selector;
    this.elites = elites;
  }

  public override Instance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var selectorInstance = instanceRegistry.GetOrCreate(selector);
    var bestInstance = instanceRegistry.GetOrCreate(best);
    return new Instance(selectorInstance, bestInstance, elites);
  }

  public class Instance
    : SelectorInstance<TGenotype, TSearchSpace, TProblem>
  {
    private readonly ISelectorInstance<TGenotype, TSearchSpace, TProblem> selector;
    private readonly ISelectorInstance<TGenotype, TSearchSpace, TProblem> best;
    private readonly int elites;

    public Instance(ISelectorInstance<TGenotype, TSearchSpace, TProblem> selector, ISelectorInstance<TGenotype, TSearchSpace, TProblem> best, int elites)
    {
      this.selector = selector;
      this.best = best;
      this.elites = elites;
    }

    public override IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      return best.Select(population, objective, elites, random, searchSpace, problem).Concat(selector.Select(population, objective, count - elites, random, searchSpace, problem)).ToArray();
    }
  }
}

// public static class EliteSelector
// {
//   public static EliteSelector<TGenotype, TSearchSpace, TProblem> WithElites<TGenotype, TSearchSpace, TProblem>(this ISelector<TGenotype, TSearchSpace, TProblem> selector, int elites = 1) where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> => new(selector, elites);
// }
