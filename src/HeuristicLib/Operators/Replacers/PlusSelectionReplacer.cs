using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Replacers;

public record PlusSelectionReplacer<TGenotype>
  : StatelessReplacer<TGenotype>
{
    public override IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, int count, IRandomNumberGenerator random)
    {
        return Replace(previousPopulation, offspringPopulation, objective, count);
    }

    public static IReadOnlyList<Solution<TGenotype>> Replace(
      IReadOnlyList<Solution<TGenotype>> previousPopulation,
      IReadOnlyList<Solution<TGenotype>> offspringPopulation,
      Objective objective,
      int count)
    {
        var combinedPopulation = previousPopulation.Concat(offspringPopulation).ToList();
        return combinedPopulation
               .OrderBy(p => p.ObjectiveVector, objective.TotalOrderComparer)
               .Take(count)
               .ToArray();
    }
}
