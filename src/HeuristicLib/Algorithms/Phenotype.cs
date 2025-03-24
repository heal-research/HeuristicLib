namespace HEAL.HeuristicLib.Algorithms;

public record Phenotype<TGenotype>(TGenotype Genotype, Fitness Fitness) {
}

public static class PhenotypeExtensions {
  public static DominanceRelation CompareTo<TGenotype>(this Phenotype<TGenotype> phenotype, Phenotype<TGenotype> other, Objective objective) {
    return phenotype.Fitness.CompareTo(other.Fitness, objective);
  }
}
