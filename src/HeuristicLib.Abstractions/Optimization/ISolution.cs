namespace HEAL.HeuristicLib.Optimization;

public interface ISolution<out TGenotype> {
  TGenotype Genotype { get; }
  ObjectiveVector ObjectiveVector { get; }
}
