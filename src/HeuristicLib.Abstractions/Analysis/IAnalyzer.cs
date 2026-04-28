namespace HEAL.HeuristicLib.Analysis;

public interface IAnalyzer
{
  IAnalyzerRunState CreateAnalyzerState();
}

public interface IAnalyzer<out TAnalyzerRunState> : IAnalyzer
  where TAnalyzerRunState : class, IAnalyzerRunState
{
  new TAnalyzerRunState CreateAnalyzerState();

  IAnalyzerRunState IAnalyzer.CreateAnalyzerState() => CreateAnalyzerState();
}

