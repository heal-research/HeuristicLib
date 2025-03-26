using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface IReplacerOperator : IExecutableOperator {
  Phenotype<TGenotype>[] Replace<TGenotype>(Phenotype<TGenotype>[] previousPopulation, Phenotype<TGenotype>[] offspringPopulation, Objective objective);
  int GetOffspringCount(int populationSize);
}

public abstract class ReplacerOperatorBase : IReplacerOperator {
  public abstract int GetOffspringCount(int populationSize);
  public abstract Phenotype<TGenotype>[] Replace<TGenotype>(Phenotype<TGenotype>[] previousPopulation, Phenotype<TGenotype>[] offspringPopulation, Objective objective);
}

public class PlusSelectionReplacerOperator : ReplacerOperatorBase {
  public override int GetOffspringCount(int populationSize) {
    return populationSize;
  }
  public override Phenotype<TGenotype>[] Replace<TGenotype>(Phenotype<TGenotype>[] previousPopulation, Phenotype<TGenotype>[] offspringPopulation, Objective objective) {
    var combinedPopulation = previousPopulation.Concat(offspringPopulation).ToList();
    return combinedPopulation
      .OrderBy(p => p.Fitness, objective.TotalOrderComparer)
      .Take(previousPopulation.Length) // if algorithm population differs from previousPopulation.Length, it is not detected
      .ToArray();
  }
}

public class ElitismReplacerOperator : ReplacerOperatorBase {
  public int Elites { get; }

  public ElitismReplacerOperator(int elites) {
    Elites = elites;
  }

  public override int GetOffspringCount(int populationSize) => populationSize - Elites;

  public override Phenotype<TGenotype>[] Replace<TGenotype>(Phenotype<TGenotype>[] previousPopulation, Phenotype<TGenotype>[] offspringPopulation, Objective objective) {
    var elitesPopulation = previousPopulation
      .OrderBy(p => p.Fitness, objective.TotalOrderComparer)
      .Take(Elites);
    
    return elitesPopulation
      .Concat(offspringPopulation) // requires that offspring population size is correct
      .ToArray();
  }
}
