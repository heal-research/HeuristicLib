namespace HEAL.HeuristicLib.States;

public interface IIterationState : IAlgorithmState {
  int CurrentIteration { get; }
};
