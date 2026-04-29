namespace HEAL.HeuristicLib.Analysis;

public abstract class AnalyzerRunState<TAnalyzer>(TAnalyzer analyzer) : IAnalyzerRunState
  where TAnalyzer : IAnalyzer
{
  protected TAnalyzer Analyzer { get; } = analyzer;

  public abstract void RegisterObservations(ObservationPlan observations);
}
