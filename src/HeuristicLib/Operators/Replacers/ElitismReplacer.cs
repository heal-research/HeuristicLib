using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Replacers;

public record ElitismReplacer<TGenotype>
  : StatelessReplacer<TGenotype>
{
    public int Elites { get; }

    public ElitismReplacer(int elites)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(elites);
        Elites = elites;
    }

    public override IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, int count, IRandomNumberGenerator random)
    {
        return Replace(previousPopulation, offspringPopulation, objective, count, Elites);
    }

    public static IReadOnlyList<Solution<TGenotype>> Replace(
      IReadOnlyList<Solution<TGenotype>> previousPopulation,
      IReadOnlyList<Solution<TGenotype>> offspringPopulation,
      Objective objective,
      int count,
      int elites)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(elites);

        var elitesPopulation = previousPopulation.OrderBy(p => p.ObjectiveVector, objective.TotalOrderComparer).Take(elites);
        var remainingCount = count - Math.Min(previousPopulation.Count, elites);
        var nonElites = offspringPopulation.Take(remainingCount);

        return elitesPopulation.Concat(nonElites).ToArray();
    }
}
