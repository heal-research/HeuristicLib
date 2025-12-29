namespace HEAL.HeuristicLib.States;

public interface IAlgorithmState 
{
  // ToDo: Think about having a more elaborate Tracing than just iteration counter
  int CurrentIteration { get; }
}
