using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public abstract record Analyzer<TCandidate, TSearchSpace, TProblem, TSearchState, TResult>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm) : Analyzer<TResult>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
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
