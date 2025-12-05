using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Replacer;

public class PlusSelectionReplacer<TGenotype> : Replacer<TGenotype> {
  public override IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random) {
    var combinedPopulation = previousPopulation.Concat(offspringPopulation).ToList();
    return combinedPopulation
           .OrderBy(p => p.ObjectiveVector, objective.TotalOrderComparer)
           .Take(previousPopulation.Count) // if algorithm population differs from previousPopulation.Length, it is not detected
           .ToArray();
  }

  public override int GetOffspringCount(int populationSize) => populationSize;
}
