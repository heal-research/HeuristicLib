namespace HEAL.HeuristicLib.Algorithms;

public record Phenotype<TGenotype, TFitness>(TGenotype Genotype, TFitness Fitness) {
}

public static class PhenotypeExtensions {
  public static int CompareTo<TGenotype>(this Phenotype<TGenotype, Fitness> phenotype, Phenotype<TGenotype, Fitness> other, Goal goal) {
    return phenotype.Fitness.CompareTo(other.Fitness, goal);
  }
}
