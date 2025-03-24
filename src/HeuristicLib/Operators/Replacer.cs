using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface IReplacer : IOperator {
  Phenotype<TGenotype>[] Replace<TGenotype>(Phenotype<TGenotype>[] previousPopulation, Phenotype<TGenotype>[] offspringPopulation, Objective objective, IRandomNumberGenerator random);
  int GetOffspringCount(int populationSize);
}

// public static class Replacer {
//   public static IReplacer<TFitness, TGoal> Create<TGenotype, TFitness, TGoal>(
//     Func<Phenotype<TGenotype, TFitness>[], Phenotype<TGenotype, TFitness>[], TGoal, IRandomNumberGenerator, Phenotype<TGenotype, TFitness>[]> replacer, 
//     Func<int, int> populationCount) =>
//     new Replacer<TFitness, TGoal>(replacer, populationCount);
// }
//
// public sealed class Replacer<TFitness, TGoal> : IReplacer<TFitness, TGoal> {
//   private readonly Func<Phenotype<TGenotype, TFitness>[], Phenotype<TGenotype, TFitness>[], TGoal, Phenotype<TGenotype, TFitness>[]> replacer;
//   private readonly Func<int, int> offspringCount;
//   
//   internal Replacer(Func<Phenotype<TGenotype, TFitness>[], Phenotype<TGenotype, TFitness>[], TGoal, Phenotype<TGenotype, TFitness>[]> replacer, Func<int, int> offspringCount) {
//     this.replacer = replacer;
//     this.offspringCount = offspringCount;
//   }
//   public Phenotype<TGenotype, TFitness>[] Replace<TGenotype>(Phenotype<TGenotype, TFitness>[] previousPopulation, Phenotype<TGenotype, TFitness>[] offspringPopulation, TGoal goal) => replacer(previousPopulation, offspringPopulation, goal);
//   public int GetOffspringCount(int populationSize) => offspringCount(populationSize);
// }


public abstract class ReplacerBase : IReplacer {
  public abstract int GetOffspringCount(int populationSize);
  public abstract Phenotype<TGenotype>[] Replace<TGenotype>(Phenotype<TGenotype>[] previousPopulation, Phenotype<TGenotype>[] offspringPopulation, Objective objective, IRandomNumberGenerator random);
}

public class PlusSelectionReplacer : ReplacerBase {
  public override int GetOffspringCount(int populationSize) {
    return populationSize;
  }

  public override Phenotype<TGenotype>[] Replace<TGenotype>(Phenotype<TGenotype>[] previousPopulation, Phenotype<TGenotype>[] offspringPopulation, Objective objective, IRandomNumberGenerator random) {
    var combinedPopulation = previousPopulation.Concat(offspringPopulation).ToList();
    return combinedPopulation
      .OrderBy(p => p.Fitness, objective.TotalOrderComparer)
      .Take(previousPopulation.Length) // if algorithm population differs from previousPopulation.Length, it is not detected
      .ToArray();
  }
}

public class ElitismReplacer : ReplacerBase {
  public int Elites { get; }

  public ElitismReplacer(int elites) {
    this.Elites = elites;
  }

  public override int GetOffspringCount(int populationSize) => populationSize - Elites;

  public override Phenotype<TGenotype>[] Replace<TGenotype>(Phenotype<TGenotype>[] previousPopulation, Phenotype<TGenotype>[] offspringPopulation, Objective objective, IRandomNumberGenerator random) {
    var elitesPopulation = previousPopulation
      .OrderBy(p => p.Fitness, objective.TotalOrderComparer)
      .Take(Elites);
    
    return elitesPopulation
      .Concat(offspringPopulation) // requires that offspring population size is correct
      .ToArray();
  }
}
