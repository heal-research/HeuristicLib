using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

// ToDo: If we assume that a selector cannot select the whole requested number of solutions, the EliteSelector could simply be a PipelineSelector with a BestSelector and then another selector for the remaining.
public record EliteSelector<TGenotype, TSearchSpace, TProblem>
  : DecoratorSelector<TGenotype, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  private readonly int elites;

  public ISelector<TGenotype, TSearchSpace, TProblem> SelectorForRemaining => InnerSelector;

  public EliteSelector(ISelector<TGenotype, TSearchSpace, TProblem> selectorForRemaining, int elites = 1)
    : base(selectorForRemaining)
  {
    this.elites = elites;
  }

  protected override NoState CreateInitialState() => NoState.Instance;

  protected override IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population,
    Objective objective, int count, NoState _, ISelectorInstance<TGenotype, TSearchSpace, TProblem> selectorForRemaining,
    IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
  {
    var selectedElites = BestSelector.Select(population, objective, this.elites, random);
    var remainingCount = count - selectedElites.Count;
    var selecterdRemaining = selectorForRemaining.Select(population, objective, remainingCount, random, searchSpace, problem);
    
    return selectedElites.Concat(selecterdRemaining).ToArray();
  }
}

// public static class EliteSelector
// {
//   public static EliteSelector<TGenotype, TSearchSpace, TProblem> WithElites<TGenotype, TSearchSpace, TProblem>(this ISelector<TGenotype, TSearchSpace, TProblem> selector, int elites = 1) where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> => new(selector, elites);
// }
