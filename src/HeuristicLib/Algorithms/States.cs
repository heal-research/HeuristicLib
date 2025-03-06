namespace HEAL.HeuristicLib.Algorithms;

public record PopulationState<TSolution>(
  int Generation,
  TSolution[] Population,
  ObjectiveValue[] Objectives
);
