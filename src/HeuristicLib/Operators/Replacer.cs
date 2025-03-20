using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface IReplacer<TFitness, in TGoal> : IOperator {
  Phenotype<TGenotype, TFitness>[] Replace<TGenotype>(Phenotype<TGenotype, TFitness>[] previousPopulation, Phenotype<TGenotype, TFitness>[] offspringPopulation, TGoal goal, IRandomNumberGenerator random);
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


public abstract class ReplacerBase<TFitness, TGoal> : IReplacer<TFitness, TGoal> {
  public abstract int GetOffspringCount(int populationSize);
  public abstract Phenotype<TGenotype, TFitness>[] Replace<TGenotype>(Phenotype<TGenotype, TFitness>[] previousPopulation, Phenotype<TGenotype, TFitness>[] offspringPopulation, TGoal goal, IRandomNumberGenerator random);
}

public class PlusSelectionReplacer : ReplacerBase<Fitness, Goal> {
  public override int GetOffspringCount(int populationSize) {
    return populationSize;
  }

  public override Phenotype<TGenotype, Fitness>[] Replace<TGenotype>(Phenotype<TGenotype, Fitness>[] previousPopulation, Phenotype<TGenotype, Fitness>[] offspringPopulation, Goal goal, IRandomNumberGenerator random) {
    var combinedPopulation = previousPopulation.Concat(offspringPopulation).ToList();

    var comparer = Fitness.CreateSingleObjectiveComparer(goal);
    return combinedPopulation
      .OrderBy(p => p.Fitness, comparer)
      .Take(previousPopulation.Length) // if algorithm population differs from previousPopulation.Length, it is not detected
      .ToArray();
  }
}

public class ElitismReplacer : ReplacerBase<Fitness, Goal> {
  public int Elites { get; }

  public ElitismReplacer(int elites) {
    this.Elites = elites;
  }

  public override int GetOffspringCount(int populationSize) => populationSize - Elites;

  public override Phenotype<TGenotype, Fitness>[] Replace<TGenotype>(Phenotype<TGenotype, Fitness>[] previousPopulation, Phenotype<TGenotype, Fitness>[] offspringPopulation, Goal goal, IRandomNumberGenerator random) {
    var comparer = Fitness.CreateSingleObjectiveComparer(goal);
    
    var elitesPopulation = previousPopulation
      .OrderBy(p => p.Fitness, comparer)
      .Take(Elites);
    
    return elitesPopulation
      .Concat(offspringPopulation) // requires that offspring population size is correct
      .ToArray();
  }
}
