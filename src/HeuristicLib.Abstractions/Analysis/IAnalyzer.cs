namespace HEAL.HeuristicLib.Analysis;

public interface IAnalyzer
{
  IAnalyzerRunState CreateAnalyzerState();
}

public interface IAnalyzer<out TResult> : IAnalyzer
  where TResult : class
{
  new IAnalyzerRunState<TResult> CreateAnalyzerState();

  IAnalyzerRunState IAnalyzer.CreateAnalyzerState() => CreateAnalyzerState();
}

