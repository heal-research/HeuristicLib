namespace HEAL.HeuristicLib.Optimization;

public interface ISolution<out TGenotype> {
  TGenotype Genotype { get; }
  ObjectiveVector ObjectiveVector { get; }
}

public record Solution<TGenotype>(TGenotype Genotype, ObjectiveVector ObjectiveVector) : ISolution<TGenotype>;

public static class Solution {
  public static ISolution<TGenotype> From<TGenotype>(TGenotype genotype, ObjectiveVector objectiveVector) =>
    new Solution<TGenotype>(genotype, objectiveVector);
}
