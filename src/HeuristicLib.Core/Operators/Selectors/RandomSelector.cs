using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selectors;

public class RandomSelector<TGenotype> : Selector<TGenotype>
{
  public override IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random)
  {
    var selected = new ISolution<TGenotype>[count];
    var randoms = random.NextInts(selected.Length, population.Count);
    for (var i = 0; i < selected.Length; i++) {
      var index = randoms[i];
      selected[i] = population[index];
    }

    return selected;
  }
}
