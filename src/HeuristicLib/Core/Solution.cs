namespace HEAL.HeuristicLib.Core;

public record Solution<TGenotype, TPhenotype>(TGenotype Genotype, TPhenotype Phenotype, Fitness Fitness) {
}

public static class SolutionExtensions {
  public static DominanceRelation CompareTo<TGenotype, TPhenotype>(this Solution<TGenotype, TPhenotype> solution, Solution<TGenotype, TPhenotype> other, Objective objective) {
    return solution.Fitness.CompareTo(other.Fitness, objective);
  }
}
