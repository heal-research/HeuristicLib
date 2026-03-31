using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selectors;

public record BestSelector<TGenotype>
  : StatelessSelector<TGenotype>
{
  public override IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random)
  {
    return BestSelector.Select(population, objective, count, random);
  }
}

public static class BestSelector
{
  public static int Select(IReadOnlyList<ObjectiveVector> population, Objective objective, IRandomNumberGenerator random)
  {
    return population.Count switch {
      0 => throw new ArgumentException("Population is empty."),
      1 => 0,
      2 => objective.TotalOrderComparer.Compare(population[0], population[1]) <= 0 ? 0 : 1,
      _ => Enumerable.Range(0, population.Count).MinBy(x => population[x], objective.TotalOrderComparer)
    };
  }

  public static IReadOnlyList<int> Select(IReadOnlyList<ObjectiveVector> population, Objective objective, int count, IRandomNumberGenerator random)
  {
    var comparer = objective.TotalOrderComparer;
    return population.Count switch {
      0 => throw new ArgumentException("Population is empty."),
      1 => [0],
      2 when count == 1 => objective.TotalOrderComparer.Compare(population[0], population[1]) <= 0 ? [0] : [1],
      _ => population.Select((solution, index) => (solution, index)).OrderBy(x => x.solution, comparer).Take(count).Select(x => x.index).ToList()
    };
  }

  public static IReadOnlyList<ISolution<TGenotype>> Select<TGenotype>(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random)
  {
    var comparer = objective.TotalOrderComparer;
    return population.OrderBy(x => x.ObjectiveVector, comparer).Take(count).ToList();
  }
}
