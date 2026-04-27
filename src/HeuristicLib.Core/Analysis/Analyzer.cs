using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public abstract record Analyzer<T, TS, TP, TSearchState, TExecutionState>(IAlgorithm<T, TS, TP, TSearchState> Algorithm) : Analyzer<TExecutionState>
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
  where TSearchState : class, ISearchState;

public abstract record Analyzer<TExecutionState> : IAnalyzer<Analyzer<TExecutionState>.AnalyzerRunInstance>
{
  public AnalyzerRunInstance CreateAnalyzerState() => new RunInstance(this, CreateInitialState());
  public abstract TExecutionState CreateInitialState();

  public abstract void RegisterObservations(IObservationRegistry observationRegistry, TExecutionState state);

  public abstract class AnalyzerRunInstance(Analyzer<TExecutionState> analyzer, TExecutionState state) : IAnalyzerRunState
  {
    protected Analyzer<TExecutionState> Analyzer { get; } = analyzer;
    public TExecutionState State { get; } = state;
    public abstract void RegisterObservations(IObservationRegistry observationRegistry);
  }

  private sealed class RunInstance(Analyzer<TExecutionState> analyzer, TExecutionState state) : AnalyzerRunInstance(analyzer, state)
  {
    public override void RegisterObservations(IObservationRegistry observationRegistry) => Analyzer.RegisterObservations(observationRegistry, State);
  }
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
