using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Replacers;

public record CommaSelectionReplacer<TGenotype>
  : StatelessReplacer<TGenotype>
{
    public override IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, int count, IRandomNumberGenerator random)
      => CommaSelectionReplacer.Replace(offspringPopulation, objective, count);
}

public static class CommaSelectionReplacer
{
    public static IReadOnlyList<Solution<TGenotype>> Replace<TGenotype>(
      IReadOnlyList<Solution<TGenotype>> offspringPopulation,
      Objective objective,
      int count)
    {
        return offspringPopulation
          .OrderBy(p => p.ObjectiveVector, objective.TotalOrderComparer)
          .Take(count)
          .ToArray();
    }
}
