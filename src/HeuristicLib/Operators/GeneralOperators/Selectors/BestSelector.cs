using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public class BestSelector<TGenotype> : BatchSelector<TGenotype> {
  public override IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random) {
    return count == 1 ? [population.MinBy(x => x.ObjectiveVector, objective.TotalOrderComparer)!] : population.GetMinBy(x => x.ObjectiveVector, objective.TotalOrderComparer, count);

    return population.OrderBy(x => x.ObjectiveVector, objective.TotalOrderComparer).Take(count).ToArray();
  }
}
