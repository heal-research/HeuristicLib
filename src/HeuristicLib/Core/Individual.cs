namespace HEAL.HeuristicLib.Core;

public record Individual<TGenotype, TPhenotype>(TGenotype Genotype, TPhenotype Phenotype, Fitness Fitness) {}

public static class Individual {
  public static Individual<TGenotype, TPhenotype> From<TGenotype, TPhenotype>(TGenotype genotype, TPhenotype phenotype, Fitness fitness) => new(genotype, phenotype, fitness);
}

public static class Population {
  public static Individual<TGenotype, TPhenotype>[] From<TGenotype, TPhenotype>(IEnumerable<TGenotype> genotypes, IEnumerable<TPhenotype> phenotypes, IEnumerable<Fitness> fitnesses) {
    return Enumerable.Zip(genotypes, phenotypes, fitnesses)
      .Select(x => Individual.From(x.First, x.Second, x.Third))
      .ToArray();
  }
}

public static class IndividualExtensions {
  public static DominanceRelation CompareTo<TGenotype, TPhenotype>(this Individual<TGenotype, TPhenotype> individual, Individual<TGenotype, TPhenotype> other, Objective objective) {
    return individual.Fitness.CompareTo(other.Fitness, objective);
  }
}

