using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
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
    extension(ExecutionInstanceResolverBuilder builder)
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
            builder.Install(new EvaluatorObservationHook<TCandidate, TSearchSpace, TProblem>(evaluator, observe));

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

    extension<TCandidate, TSearchSpace, TProblem>(TypedObservationBuilder<TCandidate, TSearchSpace, TProblem> builder)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public void Observe(IEvaluator<TCandidate> evaluator, Action<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>> observe) =>
            builder.Builder.Observe<TCandidate, TSearchSpace, TProblem>(evaluator, observe);
    }
}

internal sealed class EvaluatorObservationHook<TCandidate, TSearchSpace, TProblem>(
    IEvaluator<TCandidate> evaluator,
    Action<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>> observe) : IExecutionHook
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void Install(ExecutionInstanceResolverBuilder builder) =>
        builder.Decorate(evaluator, current => new ObservingEvaluator<TCandidate, TSearchSpace, TProblem>(evaluator, current, observe));
}

internal sealed class ObservingEvaluator<TCandidate, TSearchSpace, TProblem>(
    IEvaluator<TCandidate> observedEvaluator,
    IEvaluator<TCandidate> childEvaluator,
    Action<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
    : IEvaluator<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public bool Fits(ExecutionSignature execution) =>
        ObservationSignature.Fits<TSearchSpace, TProblem>(execution) && execution.Fits(childEvaluator);

    public IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceResolver resolver)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        ObservationSignature.Require<TSearchSpace, TProblem, TRunSearchSpace, TRunProblem>(this);
        return new Instance<TRunSearchSpace, TRunProblem>(observedEvaluator, resolver.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(childEvaluator), observe);
    }

    private sealed class Instance<TRunSearchSpace, TRunProblem>(
        IEvaluator<TCandidate> observedEvaluator,
        IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childEvaluator,
        Action<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
        : IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem>
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random,
            TRunSearchSpace searchSpace, TRunProblem problem)
        {
            var objectiveVectors = childEvaluator.Evaluate(candidates, random, searchSpace, problem);
            observe(new EvaluatorObservation<TCandidate, TSearchSpace, TProblem>(
                observedEvaluator, objectiveVectors, candidates, (TSearchSpace)(object)searchSpace, (TProblem)(object)problem));
            return objectiveVectors;
        }
    }
}
