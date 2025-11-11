namespace HEAL.HeuristicLib.Optimization;

public record Solution<TGenotype>(TGenotype Genotype, ObjectiveVector ObjectiveVector);

public static class Solution {
  public static Solution<TGenotype> From<TGenotype>(TGenotype genotype, ObjectiveVector objectiveVector) =>
    new(genotype, objectiveVector);
}
