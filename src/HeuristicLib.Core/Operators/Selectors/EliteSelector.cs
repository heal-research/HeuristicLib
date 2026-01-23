using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public class EliteSelector<TGenotype, TSearchSpace, TProblem> : Selector<TGenotype, TSearchSpace, TProblem> where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  private readonly BestSelector<TGenotype> best = new();

  public EliteSelector(ISelector<TGenotype, TSearchSpace, TProblem> selector, int elites = 1) {
    Selector = selector;
    Elites = elites;
  }

  public ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; }
  public int Elites { get; }

  public override IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) {
    return best.Select(population, objective, Elites, random).Concat(Selector.Select(population, objective, count - Elites, random, searchSpace, problem)).ToArray();
  }
}

public static class EliteSelector {
  public static EliteSelector<TGenotype, TSearchSpace, TProblem> WithElites<TGenotype, TSearchSpace, TProblem>(this ISelector<TGenotype, TSearchSpace, TProblem> selector, int elites = 1)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace> {
    return new EliteSelector<TGenotype, TSearchSpace, TProblem>(selector, elites);
  }
}
