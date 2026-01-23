namespace HEAL.HeuristicLib.States;

public record AlgorithmState : IAlgorithmState
{
  public required int CurrentIteration { get; init; }
}
