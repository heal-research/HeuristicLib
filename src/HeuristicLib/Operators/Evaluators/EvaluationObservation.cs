using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Evaluators;
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
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem)
    : IInvocationObservation<Evaluation, TCandidate, EvaluationObservation<TCandidate>>
{
    static IExecutor IInvocationObservation<Evaluation, TCandidate, EvaluationObservation<TCandidate>>.Wrap<TSearchSpace, TProblem, TSearchState>(
        IExecutor executor, IOperator<Evaluation, TCandidate> source, Action<EvaluationObservation<TCandidate>> record, TProblem problem) =>
        new Executor<TSearchSpace, TProblem>((IEvaluationExecutor<TCandidate>)executor, source, record, problem);

    private sealed class Executor<TSearchSpace, TProblem>(IEvaluationExecutor<TCandidate> executor,
        IOperator<Evaluation, TCandidate> evaluator, Action<EvaluationObservation<TCandidate>> observe, TProblem problem)
        : EvaluationExecutor<TCandidate, TSearchSpace, TProblem>(problem)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random)
        {
            var objectiveVectors = executor.Evaluate(candidates, random);
            observe(new EvaluationObservation<TCandidate>(evaluator, candidates, objectiveVectors, SearchSpace, Problem));
            return objectiveVectors;
        }
    }
}

public static class EvaluationObservations
{
    extension<TCandidate>(IOperator<Evaluation, TCandidate> evaluator)
    {
        /// <summary>The observations of this evaluator's successful calls.</summary>
        public InvocationSource<Evaluation, TCandidate, EvaluationObservation<TCandidate>> Invocations =>
            ObservationSource.Create<Evaluation, TCandidate, EvaluationObservation<TCandidate>>(evaluator);

        /// <summary>Records this evaluator's successful calls into the receiver, wherever this configuration is placed.</summary>
        public Observed<Evaluation, TCandidate, EvaluationObservation<TCandidate>> Observed(IObservationReceiver<EvaluationObservation<TCandidate>> receiver) =>
            new(evaluator, receiver);
    }
}
