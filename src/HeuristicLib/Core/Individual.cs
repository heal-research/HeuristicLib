namespace HEAL.HeuristicLib;

public record EvaluatedIndividual<TGenotype>(TGenotype Genotype, Fitness Fitness);

public record EvaluatedIndividual<TGenotype, TPhenotype>(TGenotype Genotype, TPhenotype Phenotype, Fitness Fitness); // : EvaluatedIndividual<TGenotype>(Genotype, Fitness);

public static class EvaluatedIndividual {
  public static EvaluatedIndividual<TGenotype> From<TGenotype>(TGenotype genotype, Fitness fitness) => new(genotype, fitness);
  public static EvaluatedIndividual<TGenotype, TPhenotype> From<TGenotype, TPhenotype>(TGenotype genotype, TPhenotype phenotype, Fitness fitness) => new(genotype, phenotype, fitness);
}

public static class Population {
  public static IReadOnlyList<EvaluatedIndividual<TGenotype>> From<TGenotype>(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<Fitness> fitnesses) {
    if (genotypes.Count != fitnesses.Count)
      throw new ArgumentException("Genotypes and fitnesses must have the same length.");
    
    return Enumerable.Zip(genotypes, fitnesses)
      .Select(x => EvaluatedIndividual.From(x.First, x.Second))
      .ToArray();
  }
  public static IReadOnlyList<EvaluatedIndividual<TGenotype, TPhenotype>> From<TGenotype, TPhenotype>(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<TPhenotype> phenotypes, IReadOnlyList<Fitness> fitnesses) {
    if (genotypes.Count != phenotypes.Count || genotypes.Count != fitnesses.Count)
      throw new ArgumentException("Genotypes, phenotypes and fitnesses must have the same length.");
    
    return Enumerable.Zip(genotypes, phenotypes, fitnesses)
      .Select(x => EvaluatedIndividual.From(x.First, x.Second, x.Third))
      .ToArray();
  }
  
  public static IReadOnlyList<T> ExtractParetoFront<T>(IEnumerable<T> population, Func<T, Fitness> fitnessSelector, Objective objective)
    where T : IEquatable<T>
  {
    var uniqueSolutions = population.Distinct().ToList();
    return uniqueSolutions
      .Where(ind => !uniqueSolutions.Any(other => !ind.Equals(other) && fitnessSelector(ind).IsDominatedBy(fitnessSelector(other), objective)))
      .ToList();
  }
  
  public static IReadOnlyList<EvaluatedIndividual<TGenotype>> ExtractParetoFront<TGenotype>(
    IEnumerable<EvaluatedIndividual<TGenotype>> population, 
    Objective objective) 
  {
    var uniqueSolutions = population.Distinct().ToList();
    return uniqueSolutions
      .Where(ind => !uniqueSolutions.Any(other => ind != other && ind.Fitness.IsDominatedBy(other.Fitness, objective)))
      .ToList();
  }
  
  public static IReadOnlyList<EvaluatedIndividual<TGenotype, TPhenotype>> ExtractParetoFront<TGenotype, TPhenotype>(
    IEnumerable<EvaluatedIndividual<TGenotype, TPhenotype>> population, 
    Objective objective)
  {
    var uniqueSolutions = population.Distinct().ToList();
    return uniqueSolutions
      .Where(ind => !uniqueSolutions.Any(other => ind != other && ind.Fitness.IsDominatedBy(other.Fitness, objective)))
      .ToList();
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
