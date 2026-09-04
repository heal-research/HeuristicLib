using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public interface IDynamicAnalysisResult<TCandidate>
{
    void AfterEvaluationLog(object? sender, IReadOnlyList<(TCandidate candidate, ObjectiveVector objective, EvaluationTiming timing)> evaluationLog);
}

/// <summary>
/// Reads the problem's evaluation log for one run.
/// </summary>
/// <remarks>
/// The analysis subscribes to the problem's evaluation event while it is installed and unsubscribes when the run
/// disposes it. It also observes the evaluators so that the problem resolves its pending updates before the log fires.
/// This analyzer holds its own data and is used for one run.
/// </remarks>
public abstract class DynamicAnalysis<TCandidate, TSearchSpace, TProblem, TResult> : IAnalyzer, IDisposable
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TCandidate, TSearchSpace>
    where TResult : class, IDynamicAnalysisResult<TCandidate>
{
    private bool subscribed;

    protected DynamicAnalysis(TProblem problem, params IReadOnlyList<IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluators)
    {
        if (evaluators.Count == 0)
            throw new ArgumentException("An analysis needs at least one anchor to observe.", nameof(evaluators));

        Problem = problem;
        Evaluators = [.. evaluators];
    }

    public TProblem Problem { get; }
    public ImmutableArray<IEvaluator<TCandidate, TSearchSpace, TProblem>> Evaluators { get; }

    /// <summary>
    /// Gets the data this analysis collected. Created on first access so that a derived analysis can finish
    /// construction before its result is built.
    /// </summary>
    public TResult Result => result ??= CreateInitialResult();
    private TResult? result;

    protected abstract TResult CreateInitialResult();

    public void Install(ExecutionInstanceResolverBuilder builder)
    {
        Problem.OnEvaluation += Result.AfterEvaluationLog;
        subscribed = true;

        foreach (var evaluator in Evaluators)
            builder.Observe(evaluator, observation =>
                Problem.AfterEvaluation(observation.ObjectiveVectors, observation.Candidates, observation.SearchSpace, observation.Problem));
    }

    public void Dispose()
    {
        if (!subscribed)
            return;

        Problem.OnEvaluation -= Result.AfterEvaluationLog;
        subscribed = false;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Lets the problem resolve pending updates at the evaluator boundary, which is what fires its evaluation log.
    /// </summary>
}
