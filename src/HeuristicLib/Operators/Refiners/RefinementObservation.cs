using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>One successful refinement call, with the refined candidates it returned.</summary>
/// <remarks>
/// The search space and problem are read at the candidate's interfaces, so an observation fits every run over the
/// candidate.
/// </remarks>
public sealed record RefinementObservation<TCandidate>(
    IOperator<Refinement, TCandidate> Refiner,
    IReadOnlyList<TCandidate> Candidates,
    IReadOnlyList<TCandidate> Refined,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem);

file sealed class RefinementObservationAdvice<TCandidate>(Action<RefinementObservation<TCandidate>> observe) : RefinerAdvice<TCandidate>
{
    protected override IRefinementExecutor<TCandidate, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(
        IRefinementExecutor<TCandidate, TSearchSpace, TProblem> executor, IOperator<Refinement, TCandidate> refiner) =>
        new Executor<TSearchSpace, TProblem>(executor, refiner, observe);

    private sealed class Executor<TSearchSpace, TProblem>(IRefinementExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IOperator<Refinement, TCandidate> refiner, Action<RefinementObservation<TCandidate>> observe)
        : IRefinementExecutor<TCandidate, TSearchSpace, TProblem>, IExecutorControlProvider
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var refined = executor.Refine(candidates, random, searchSpace, problem);
            observe(new RefinementObservation<TCandidate>(refiner, candidates, refined, searchSpace, problem));
            return refined;
        }

        public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(executor);
    }
}

public static class RefinementObservations
{
    extension<TCandidate>(IOperator<Refinement, TCandidate> refiner)
    {
        /// <summary>The observations of this refiner's successful calls.</summary>
        public ObservationSource<RefinementObservation<TCandidate>> Invocations =>
            ObservationSource.Create<RefinementObservation<TCandidate>>(refiner, observe => Aspect.Create(Pointcut.Reference(refiner), new RefinementObservationAdvice<TCandidate>(observe)));
    }
}
