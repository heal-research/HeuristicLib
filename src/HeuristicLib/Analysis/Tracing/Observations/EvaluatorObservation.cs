using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public sealed record EvaluatorObservation<TCandidate, TSearchSpace, TProblem>(
    IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator,
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
        public void Observe<TCandidate, TSearchSpace, TProblem>(
            IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
            Action<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            builder.Install(new EvaluatorObservationHook<TCandidate, TSearchSpace, TProblem>(evaluator, observe));
    }
}

internal sealed class EvaluatorObservationHook<TCandidate, TSearchSpace, TProblem>(
    IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
    Action<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>> observe) : IExecutionHook
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void Install(ExecutionInstanceResolverBuilder builder) =>
        builder.Decorate(evaluator, current => new ObservingEvaluator<TCandidate, TSearchSpace, TProblem>(evaluator, current, observe));
}

internal sealed class ObservingEvaluator<TCandidate, TSearchSpace, TProblem>(
    IEvaluator<TCandidate, TSearchSpace, TProblem> observedEvaluator,
    IEvaluator<TCandidate, TSearchSpace, TProblem> childEvaluator,
    Action<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
    : IEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceResolver resolver) =>
        new Instance(observedEvaluator, resolver.Resolve(childEvaluator), observe);

    private sealed class Instance(
        IEvaluator<TCandidate, TSearchSpace, TProblem> observedEvaluator,
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator,
        Action<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
        : IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random,
            TSearchSpace searchSpace, TProblem problem)
        {
            var objectiveVectors = childEvaluator.Evaluate(candidates, random, searchSpace, problem);
            observe(new EvaluatorObservation<TCandidate, TSearchSpace, TProblem>(
                observedEvaluator, objectiveVectors, candidates, searchSpace, problem));
            return objectiveVectors;
        }
    }
}
