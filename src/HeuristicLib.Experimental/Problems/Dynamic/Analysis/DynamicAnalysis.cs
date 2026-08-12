using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic.Analysis;

public interface IDynamicAnalysisResult<TCandidate>
{
    void AfterEvaluationLog(object? sender,
                            IReadOnlyList<(TCandidate candidate, ObjectiveVector objective,
                                EvaluationTiming timing)> evaluationLog);
}

public abstract record DynamicAnalysis<TCandidate, TSearchSpace, TProblem, TResult>
    : Analyzer<TResult>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TCandidate, TSearchSpace>
    where TResult : class, IDynamicAnalysisResult<TCandidate>
{
    protected DynamicAnalysis(TProblem problem,
                              params IReadOnlyList<IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluators)
    {
        Problem = problem;
        Evaluators = [..evaluators];
    }

    public TProblem Problem { get; }

    public ImmutableArray<IEvaluator<TCandidate, TSearchSpace, TProblem>> Evaluators { get; }

    public override IAnalyzerRunState<TResult> CreateAnalyzerState() => new RunState(this, CreateInitialResult());

    public override void RegisterObservations(ObservationPlan observations, TResult result)
    {
        foreach (var evaluator in Evaluators) observations.Observe(evaluator, Problem);
    }

    private sealed class RunState(
        DynamicAnalysis<TCandidate, TSearchSpace, TProblem, TResult> analyzer,
        TResult result) : IAnalyzerRunState<TResult>, IDisposable
    {
        private bool disposed;

        public TResult Result { get; } = result;

        public void RegisterObservations(ObservationPlan observations)
        {
            analyzer.Problem.OnEvaluation += Result.AfterEvaluationLog;
            analyzer.RegisterObservations(observations, Result);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            analyzer.Problem.OnEvaluation -= Result.AfterEvaluationLog;
            disposed = true;
        }
    }
}
