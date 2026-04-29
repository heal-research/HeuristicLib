using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public abstract record Analyzer<T, TS, TP, TSearchState, TResult>(IAlgorithm<T, TS, TP, TSearchState> Algorithm) : Analyzer<TResult>
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
  where TSearchState : class, ISearchState
  where TResult : class;

public abstract record Analyzer<TResult> : IAnalyzer<TResult>
  where TResult : class
{
  public IAnalyzerRunState<TResult> CreateAnalyzerState() => new RunState(this, CreateInitialResult());
  public abstract TResult CreateInitialResult();

  public abstract void RegisterObservations(ObservationPlan observations, TResult result);

  private sealed class RunState(Analyzer<TResult> analyzer, TResult result) : IAnalyzerRunState<TResult>
  {
    public TResult Result { get; } = result;

    public void RegisterObservations(ObservationPlan observations)
    {
      analyzer.RegisterObservations(observations, Result);
    }
  }
}
