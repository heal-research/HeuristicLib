using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selectors;

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
    switch (population.Count) {
      case 0:
        throw new ArgumentException("Population is empty.");
      case 1:
        return [0];
      case 2:
        if (count == 1) {
          return objective.TotalOrderComparer.Compare(population[0], population[1]) <= 0 ? [0] : [1];
        }

        break;
    }

    return count == 1
      ? [Enumerable.Range(0, population.Count).MinBy(x => population[x], objective.TotalOrderComparer)]
      : Enumerable.Range(0, population.Count).GetMinBy(x => population[x], objective.TotalOrderComparer, count);
  }
}

public class BestSelector<TGenotype> : Selector<TGenotype>
{
  public override IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random)
  {
    return count == 1
      ? [population.MinBy(x => x.ObjectiveVector, objective.TotalOrderComparer)!]
      : population.GetMinBy(x => x.ObjectiveVector, objective.TotalOrderComparer, count);
  }
}
