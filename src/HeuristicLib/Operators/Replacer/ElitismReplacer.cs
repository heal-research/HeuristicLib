using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Replacer;

public class ElitismReplacer<TGenotype> : Replacer<TGenotype> {
  public int Elites { get; }

  public ElitismReplacer(int elites) {
    if (elites < 0)
      throw new ArgumentOutOfRangeException(nameof(elites), "Elites must be non-negative.");
    Elites = elites;
  }

  public override IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random) {
    if (Elites < 0)
      throw new ArgumentOutOfRangeException(nameof(Elites), "Elites must be non-negative.");

    var elitesPopulation = previousPopulation
                           .OrderBy(p => p.ObjectiveVector, objective.TotalOrderComparer)
                           .Take(Elites);

    return elitesPopulation
           .Concat(offspringPopulation) // requires that offspring population size is correct
           .ToArray();
  }

  public override int GetOffspringCount(int populationSize) => populationSize - Elites;
}
