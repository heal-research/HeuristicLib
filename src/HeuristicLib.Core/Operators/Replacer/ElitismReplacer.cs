using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Replacer;

public class ElitismReplacer<TGenotype> : Replacer<TGenotype> {
  public int Elites { get; }

  public int? TargetedPopulationSize { get; }

  public ElitismReplacer(int elites, int? targetedPopulationSize = null) {
    ArgumentOutOfRangeException.ThrowIfNegative(elites);
    Elites = elites;
    TargetedPopulationSize = targetedPopulationSize;
  }

  public override IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random) {
    var sel = (TargetedPopulationSize ?? previousPopulation.Count) - Math.Min(previousPopulation.Count, Elites);
    var elitesPopulation = previousPopulation.OrderBy(p => p.ObjectiveVector, objective.TotalOrderComparer).Take(Elites);
    var nonElites = offspringPopulation.Take(sel);
    return elitesPopulation.Concat(nonElites).ToArray();
  }

  public override int GetOffspringCount(int populationSize) => populationSize - Elites;
}
