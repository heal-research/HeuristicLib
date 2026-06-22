using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selectors;

public record WorstSelector<TGenotype>
  : StatelessSelector<TGenotype>
{
    public override IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random)
      => WorstSelector.Select(population, objective, count);
}

public static class WorstSelector
{
    public static IReadOnlyList<int> Select(IReadOnlyList<ObjectiveVector> population, Objective objective, int count = 1)
      => population.Select((solution, index) => (solution, index)).OrderByDescending(x => x.solution, objective.TotalOrderComparer).Take(count).Select(x => x.index).ToList();

    public static IReadOnlyList<Solution<TGenotype>> Select<TGenotype>(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count)
      => population.OrderByDescending(x => x.ObjectiveVector, objective.TotalOrderComparer).Take(count).ToList();
}
