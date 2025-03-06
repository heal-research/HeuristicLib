namespace HEAL.HeuristicLib.Algorithms;

public interface IState {}

public interface IPopulationState<TGenotype, TObjective> : IState {
  TGenotype[] Population { get; } 
  TObjective[] Objectives { get; }
}

public interface IGenerationalState : IState {
  int Generation { get; }
}

public record PopulationState<TGenotype> : IPopulationState<TGenotype, ObjectiveValue>, IGenerationalState {
  public required int Generation { get; init; }
  public required TGenotype[] Population { get; init; }
  public required ObjectiveValue[] Objectives { get; init; }
}
