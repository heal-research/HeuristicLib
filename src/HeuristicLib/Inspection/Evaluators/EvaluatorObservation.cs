using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

public sealed record EvaluatorObservation<TCandidate, TSearchSpace, TProblem>(
    IOperator<Evaluation, TCandidate> Evaluator,
    IReadOnlyList<ObjectiveVector> ObjectiveVectors,
    IReadOnlyList<TCandidate> Candidates,
    TSearchSpace SearchSpace,
    TProblem Problem) : Observation<TProblem>(Problem)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public static class EvaluatorObservations
{
    extension(Aspect)
    {
        /// <summary>Observes every call to one chosen evaluator.</summary>
        /// <remarks>
        /// The evaluator names only its candidate, so the observation names the search space and problem it reads. A
        /// run over types the observation was not written for is reported when the execution graph is built.
        /// </remarks>
        public static Aspect Observe<TCandidate, TSearchSpace, TProblem>(
            IOperator<Evaluation, TCandidate> evaluator,
            Action<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            Aspect.Create(Pointcut.Reference(evaluator), new EvaluatorObservationAdvice<TCandidate, TSearchSpace, TProblem>(observe));

        /// <summary>Observes every call to one chosen evaluator, for an observer that reads no particular search space or problem.</summary>
        /// <remarks>
        /// An implicitly typed lambda binds here, because the overload naming the search space and problem cannot infer
        /// them from it. The observation fits every run over the candidate.
        /// </remarks>
        public static Aspect Observe<TCandidate>(
            IOperator<Evaluation, TCandidate> evaluator,
            Action<EvaluatorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> observe) =>
            Aspect.Observe<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>(evaluator, observe);
    }
}

/// <summary>Reports every call to each evaluator its pointcut selects.</summary>
/// <param name="observe">Receives each observation on the thread that called the evaluator.</param>
public sealed class EvaluatorObservationAdvice<TCandidate, TSearchSpace, TProblem>(
    Action<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>> observe) : EvaluatorAdvice<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected override void Check<TRunSearchSpace, TRunProblem>(IOperator<Evaluation, TCandidate> evaluator) =>
        ObservationSignature.Require<TSearchSpace, TProblem, TRunSearchSpace, TRunProblem>(this);

    protected override IEvaluationExecutor<TCandidate, TRunSearchSpace, TRunProblem> Wrap<TRunSearchSpace, TRunProblem>(
        IEvaluationExecutor<TCandidate, TRunSearchSpace, TRunProblem> executor, IOperator<Evaluation, TCandidate> evaluator) =>
        new Executor<TRunSearchSpace, TRunProblem>(evaluator, executor, observe);

    private sealed class Executor<TRunSearchSpace, TRunProblem>(
        IOperator<Evaluation, TCandidate> observedEvaluator,
        IEvaluationExecutor<TCandidate, TRunSearchSpace, TRunProblem> childEvaluator,
        Action<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
        : WrappingEvaluationExecutor<TCandidate, TRunSearchSpace, TRunProblem>(childEvaluator), IExecutorControlProvider
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random,
            TRunSearchSpace searchSpace, TRunProblem problem)
        {
            var objectiveVectors = ChildEvaluator.Evaluate(candidates, random, searchSpace, problem);
            observe(new EvaluatorObservation<TCandidate, TSearchSpace, TProblem>(
                observedEvaluator, objectiveVectors, candidates, (TSearchSpace)(object)searchSpace, (TProblem)(object)problem));
            return objectiveVectors;
        }

        public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(ChildEvaluator);
    }
}
