using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selectors;

public class RandomSelector<TGenotype> : BatchSelector<TGenotype> {
  public override IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random) {
    var selected = new ISolution<TGenotype>[count];
    var randoms = random.Integers(selected.Length, population.Count);
    for (var i = 0; i < selected.Length; i++) {
      var index = randoms[i];
      selected[i] = population[index];
    }

    return selected;
  }
}
