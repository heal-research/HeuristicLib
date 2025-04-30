namespace HEAL.HeuristicLib.Optimization;

public record Solution<TGenotype>(TGenotype Genotype, Fitness Fitness);

public record Solution<TGenotype, TPhenotype>(TGenotype Genotype, TPhenotype Phenotype, Fitness Fitness); // : EvaluatedIndividual<TGenotype>(Genotype, Fitness);

public static class Solution {
  public static Solution<TGenotype> From<TGenotype>(TGenotype genotype, Fitness fitness) => new(genotype, fitness);
  public static Solution<TGenotype, TPhenotype> From<TGenotype, TPhenotype>(TGenotype genotype, TPhenotype phenotype, Fitness fitness) => new(genotype, phenotype, fitness);
}

// ToDo: either generalize to "Multi-Solution" since Population is mostly EA specific
public static class Population {
  public static IReadOnlyList<Solution<TGenotype>> From<TGenotype>(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<Fitness> fitnesses) {
    if (genotypes.Count != fitnesses.Count)
      throw new ArgumentException("Genotypes and fitnesses must have the same length.");
    
    return Enumerable.Zip(genotypes, fitnesses)
      .Select(x => Solution.From(x.First, x.Second))
      .ToArray();
  }
  public static IReadOnlyList<Solution<TGenotype, TPhenotype>> From<TGenotype, TPhenotype>(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<TPhenotype> phenotypes, IReadOnlyList<Fitness> fitnesses) {
    if (genotypes.Count != phenotypes.Count || genotypes.Count != fitnesses.Count)
      throw new ArgumentException("Genotypes, phenotypes and fitnesses must have the same length.");
    
    return Enumerable.Zip(genotypes, phenotypes, fitnesses)
      .Select(x => Solution.From(x.First, x.Second, x.Third))
      .ToArray();
  }
}
//
// public static class IndividualExtensions {
//   public static DominanceRelation CompareTo<TGenotype>(this EvaluatedIndividual<TGenotype> individual, EvaluatedIndividual<TGenotype> other, Objective objective) {
//     return individual.Fitness.CompareTo(other.Fitness, objective);
//   }
//   public static DominanceRelation CompareTo<TGenotype, TPhenotype>(this EvaluatedIndividual<TGenotype, TPhenotype> individual, EvaluatedIndividual<TGenotype, TPhenotype> other, Objective objective) {
//     return individual.Fitness.CompareTo(other.Fitness, objective);
//   }
// }
//
