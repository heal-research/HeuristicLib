using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>One successful evaluation call, with the objective vectors it returned in candidate order.</summary>
/// <remarks>
/// The search space and problem are read at the candidate's interfaces, so an observation fits every run over the
/// candidate.
/// </remarks>
public sealed record EvaluationObservation<TCandidate>(
    IOperator<Evaluation, TCandidate> Evaluator,
    IReadOnlyList<TCandidate> Candidates,
    IReadOnlyList<ObjectiveVector> ObjectiveVectors,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem);

file sealed class EvaluationObservationAdvice<TCandidate>(Action<EvaluationObservation<TCandidate>> observe) : EvaluatorAdvice<TCandidate>
{
    protected override IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(
        IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> executor, IOperator<Evaluation, TCandidate> evaluator) =>
        new Executor<TSearchSpace, TProblem>(executor, evaluator, observe);

    private sealed class Executor<TSearchSpace, TProblem>(IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IOperator<Evaluation, TCandidate> evaluator, Action<EvaluationObservation<TCandidate>> observe)
        : IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>, IExecutorControlProvider
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var objectiveVectors = executor.Evaluate(candidates, random, searchSpace, problem);
            observe(new EvaluationObservation<TCandidate>(evaluator, candidates, objectiveVectors, searchSpace, problem));
            return objectiveVectors;
        }

        public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(executor);
    }
}

public static class EvaluationObservations
{
    extension<TCandidate>(IOperator<Evaluation, TCandidate> evaluator)
    {
        /// <summary>The observations of this evaluator's successful calls.</summary>
        public ObservationSource<EvaluationObservation<TCandidate>> Calls =>
            new(evaluator, observe => Aspect.Create(Pointcut.Reference(evaluator), new EvaluationObservationAdvice<TCandidate>(observe)));
    }
}
