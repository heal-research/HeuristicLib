namespace HEAL.HeuristicLib.States;

public record IterationState : AlgorithmState, IIterationState {
  public required int CurrentIteration { get; init; }
}
