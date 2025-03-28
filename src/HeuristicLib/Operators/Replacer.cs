using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface IReplacer : IOperator {
  Individual<TGenotype, TPhenotype>[] Replace<TGenotype, TPhenotype>(Individual<TGenotype, TPhenotype>[] previousPopulation, Individual<TGenotype, TPhenotype>[] offspringPopulation, Objective objective, IRandomNumberGenerator random);
  int GetOffspringCount(int populationSize);
}

public abstract class ReplacerBase : IReplacer {
  public abstract Individual<TGenotype, TPhenotype>[] Replace<TGenotype, TPhenotype>(Individual<TGenotype, TPhenotype>[] previousPopulation, Individual<TGenotype, TPhenotype>[] offspringPopulation, Objective objective, IRandomNumberGenerator random);
  public abstract int GetOffspringCount(int populationSize);
}

public class PlusSelectionReplacer : ReplacerBase {
  public override Individual<TGenotype, TPhenotype>[] Replace<TGenotype, TPhenotype>(Individual<TGenotype, TPhenotype>[] previousPopulation, Individual<TGenotype, TPhenotype>[] offspringPopulation, Objective objective, IRandomNumberGenerator random) {
    var combinedPopulation = previousPopulation.Concat(offspringPopulation).ToList();
    return combinedPopulation
      .OrderBy(p => p.Fitness, objective.TotalOrderComparer)
      .Take(previousPopulation.Length) // if algorithm population differs from previousPopulation.Length, it is not detected
      .ToArray();
  }
  
  public override int GetOffspringCount(int populationSize) {
    return populationSize;
  }
}

public class ElitismReplacer : ReplacerBase {
  public int Elites { get; }

  public ElitismReplacer(int elites) {
    Elites = elites;
  }

  public override Individual<TGenotype, TPhenotype>[] Replace<TGenotype, TPhenotype>(Individual<TGenotype, TPhenotype>[] previousPopulation, Individual<TGenotype, TPhenotype>[] offspringPopulation, Objective objective, IRandomNumberGenerator random) {
    var elitesPopulation = previousPopulation
      .OrderBy(p => p.Fitness, objective.TotalOrderComparer)
      .Take(Elites);
    
    return elitesPopulation
      .Concat(offspringPopulation) // requires that offspring population size is correct
      .ToArray();
  }
  
  public override int GetOffspringCount(int populationSize) => populationSize - Elites;
}
