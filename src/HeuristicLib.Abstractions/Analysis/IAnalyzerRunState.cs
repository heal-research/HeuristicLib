namespace HEAL.HeuristicLib.Analysis;

public interface IAnalyzerRunState
{
  void RegisterObservations(ObservationPlan observations);
}

public interface IAnalyzerRunState<out TResult> : IAnalyzerRunState
  where TResult : class
{
  TResult Result { get; }
}
