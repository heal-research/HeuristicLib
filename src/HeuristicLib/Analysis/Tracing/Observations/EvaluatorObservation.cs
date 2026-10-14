using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public sealed record EvaluatorObservation<TCandidate, TSearchSpace, TProblem>(
    IEvaluator<TCandidate> Evaluator,
    IReadOnlyList<ObjectiveVector> ObjectiveVectors,
    IReadOnlyList<TCandidate> Candidates,
    TSearchSpace SearchSpace,
    TProblem Problem) : Observation<TProblem>(Problem)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public static class EvaluatorObservations
{
    extension(AspectBuilder builder)
    {
        /// <summary>Observes every call to one chosen evaluator.</summary>
        /// <remarks>
        /// The evaluator names only its candidate, so the observation names the search space and problem it reads. A
        /// run over types the observation was not written for is reported when the execution graph is built.
        /// </remarks>
        public void Observe<TCandidate, TSearchSpace, TProblem>(
            IEvaluator<TCandidate> evaluator,
            Action<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            builder.Install(new EvaluatorObservationAspect<TCandidate, TSearchSpace, TProblem>(evaluator, observe));

        /// <summary>Observes every call to one chosen evaluator, for an observer that reads no particular search space or problem.</summary>
        /// <remarks>
        /// An implicitly typed lambda binds here, because the overload naming the search space and problem cannot infer
        /// them from it. The observation fits every run over the candidate.
        /// </remarks>
        public void Observe<TCandidate>(
            IEvaluator<TCandidate> evaluator,
            Action<EvaluatorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> observe) =>
            builder.Observe<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>(evaluator, observe);
    }
}

internal sealed class EvaluatorObservationAspect<TCandidate, TSearchSpace, TProblem>(
    IEvaluator<TCandidate> evaluator,
    Action<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>> observe) : Aspect
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected override void Declare(AspectBuilder builder) =>
        builder.Install(evaluator, current => new ObservingEvaluator<TCandidate, TSearchSpace, TProblem>(evaluator, current, observe));
}

internal sealed record ObservingEvaluator<TCandidate, TSearchSpace, TProblem> : WrappingEvaluator<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private readonly IEvaluator<TCandidate> observedEvaluator;
    private readonly Action<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>> observe;

    public ObservingEvaluator(IEvaluator<TCandidate> observedEvaluator, IEvaluator<TCandidate> childEvaluator, Action<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
        : base(childEvaluator)
    {
        this.observedEvaluator = observedEvaluator;
        this.observe = observe;
    }

    public override bool Fits(ExecutionSignature execution) =>
        base.Fits(execution) && ObservationSignature.Fits<TSearchSpace, TProblem>(execution);

    protected override WrapperExecutionFactory<IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>()
    {
        ObservationSignature.Require<TSearchSpace, TProblem, TRunSearchSpace, TRunProblem>(this);
        return inner => new Execution<TRunSearchSpace, TRunProblem>(observedEvaluator, inner, observe);
    }

    private sealed class Execution<TRunSearchSpace, TRunProblem>(
        IEvaluator<TCandidate> observedEvaluator,
        IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem> childEvaluator,
        Action<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
        : WrappingEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem>(childEvaluator)
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
    }
}
