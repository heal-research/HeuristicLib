using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selectors;

public record LinearRankSelector<TGenotype>
  : StatelessSelector<TGenotype>
{
  public override IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random)
    => LinearRankSelector.Select(population, objective, count, random);
}

public static class LinearRankSelector
{
  public static IReadOnlyList<ISolution<TGenotype>> Select<TGenotype>(
    IReadOnlyList<ISolution<TGenotype>> population,
    Objective objective,
    int count,
    IRandomNumberGenerator random)
  {
    var list = population.OrderByDescending(x => x.ObjectiveVector, objective.TotalOrderComparer).ToList();

    int lotSum = list.Count * (list.Count + 1) / 2;
    var selected = new ISolution<TGenotype>[count];
    for (int i = 0; i < count; i++) {
      int selectedLot = random.NextInt(lotSum);
      var index = (int)((Math.Sqrt(1 + 8 * selectedLot) - 1) / 2.0) + 1;
      selected[i] = list[index];
    }

    return selected;
  }
}
