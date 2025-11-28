using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Replacer;

public class ElitismReplacer<TGenotype> : Replacer<TGenotype> {
  public int Elites { get; }

  public ElitismReplacer(int elites) {
    ArgumentOutOfRangeException.ThrowIfNegative(elites);
    Elites = elites;
  }

  public override IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random) {
    var elitesPopulation = previousPopulation.OrderBy(p => p.ObjectiveVector, objective.TotalOrderComparer).Take(Elites);
    var nonElites = offspringPopulation.Take(previousPopulation.Count - Math.Min(previousPopulation.Count, Elites));
    return elitesPopulation.Concat(nonElites).ToArray();
  }

  public override int GetOffspringCount(int populationSize) => populationSize - Elites;
}
