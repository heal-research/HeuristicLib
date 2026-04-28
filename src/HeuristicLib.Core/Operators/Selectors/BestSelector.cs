using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selectors;

public record BestSelector<TGenotype>
  : StatelessSelector<TGenotype>
{
  public override IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random)
    => BestSelector.Select(population, objective, count);
}

public static class BestSelector
{
  public static IReadOnlyList<int> Select(IReadOnlyList<ObjectiveVector> population, Objective objective, int count = 1)
    => population.Select((solution, index) => (solution, index)).OrderBy(x => x.solution, objective.TotalOrderComparer).Take(count).Select(x => x.index).ToList();

  public static IReadOnlyList<ISolution<TGenotype>> Select<TGenotype>(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count)
    => population.OrderBy(x => x.ObjectiveVector, objective.TotalOrderComparer).Take(count).ToList();
}
