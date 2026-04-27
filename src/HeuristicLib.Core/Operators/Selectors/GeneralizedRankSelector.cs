using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selectors;

public record GeneralizedRankSelector<TGenotype>(double Pressure) : StatelessSelector<TGenotype>
{
  public override IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random)
    => GeneralizedRankSelector.Select(population, objective, count, random, Pressure);
}

public static class GeneralizedRankSelector
{
  public static IReadOnlyList<ISolution<TGenotype>> Select<TGenotype>(
    IReadOnlyList<ISolution<TGenotype>> population,
    Objective objective,
    int count,
    IRandomNumberGenerator random,
    double pressure)
  {
    var selected = new ISolution<TGenotype>[count];
    var source = population.OrderBy(x => x.ObjectiveVector, objective.TotalOrderComparer).ToArray();
    var scale = Math.Pow(population.Count, 1.0 / pressure) - 1;
    for (var i = 0; i < count; i++) {
      var rand = 1 + random.NextDouble() * scale;
      var selIdx = (int)Math.Floor(Math.Pow(rand, pressure) - 1);
      selected[i] = source[selIdx];
    }

    return selected;
  }
}
