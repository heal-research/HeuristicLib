namespace HEAL.HeuristicLib.Algorithms;

public record Phenotype<TGenotype, TFitness>(TGenotype Genotype, TFitness Fitness) {
  //public bool IsEvaluated => Fitness is not null;
}
