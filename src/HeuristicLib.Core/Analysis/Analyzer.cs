using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public abstract record Analyzer<T, TS, TP, TR, TState>(IAlgorithm<T, TS, TP, TR> Algorithm) : Analyzer<TState> where TS : class, ISearchSpace<T> where TP : class, IProblem<T, TS> where TR : class, IAlgorithmState;

public abstract record Analyzer<TState> : IAnalyzer<AnalyzerRunInstance<TState>>
{
  public AnalyzerRunInstance<TState> CreateAnalyzerState() => new RunInstance(this, CreateInitialState());
  public abstract TState CreateInitialState();

  public abstract void RegisterObservations(IObservationRegistry observationRegistry, TState state);

  private sealed class RunInstance(Analyzer<TState> analyzer, TState state) : AnalyzerRunInstance<TState>(analyzer, state)
  {
    public override void RegisterObservations(IObservationRegistry observationRegistry) => Analyzer.RegisterObservations(observationRegistry, State);
  }
}

public abstract class AnalyzerRunInstance<TState>(Analyzer<TState> analyzer, TState state) : IAnalyzerRunState
{
  protected Analyzer<TState> Analyzer { get; } = analyzer;
  public TState State { get; } = state;
  public abstract void RegisterObservations(IObservationRegistry observationRegistry);
}

public static class AnalyzerExtensions
{
  extension(Run run)
  {
    public T GetAnalyzerResult<T>(Analyzer<T> analyzer)
    {
      return run.GetAnalyzerResult(analyzer).State;
    }

    public bool TryGetAnalyzerResult<T>(Analyzer<T> analyzer, out T? analyzerRunState) where T : class
    {
      if (run.TryGetAnalyzerResult(analyzer, out var state)) {
        analyzerRunState = state.State;
        return true;
      }

      analyzerRunState = null;
      return false;
    }
  }
}
