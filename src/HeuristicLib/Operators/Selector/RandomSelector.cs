using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selector;

public class RandomSelector<TGenotype> : BatchSelector<TGenotype> {
  public override IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random) {
    var selected = new Solution<TGenotype>[count];
    var randoms = random.Integers(selected.Length, population.Count);
    for (var i = 0; i < selected.Length; i++) {
      var index = randoms[i];
      selected[i] = population[index];
    }

    return selected;
  }
}
