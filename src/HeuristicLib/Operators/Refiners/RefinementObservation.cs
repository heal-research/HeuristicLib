using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Refiners;
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
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem)
    : IInvocationObservation<Refinement, TCandidate, RefinementObservation<TCandidate>>
{
    static IExecutor IInvocationObservation<Refinement, TCandidate, RefinementObservation<TCandidate>>.Wrap<TSearchSpace, TProblem, TSearchState>(
        IExecutor executor, IOperator<Refinement, TCandidate> source, Action<RefinementObservation<TCandidate>> record, TProblem problem) =>
        new Executor<TSearchSpace, TProblem>((IRefinementExecutor<TCandidate>)executor, source, record, problem);

    private sealed class Executor<TSearchSpace, TProblem>(IRefinementExecutor<TCandidate> executor,
        IOperator<Refinement, TCandidate> refiner, Action<RefinementObservation<TCandidate>> observe, TProblem problem)
        : RefinementExecutor<TCandidate, TSearchSpace, TProblem>(problem)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random)
        {
            var refined = executor.Refine(candidates, random);
            observe(new RefinementObservation<TCandidate>(refiner, candidates, refined, SearchSpace, Problem));
            return refined;
        }
    }
}

public static class RefinementObservations
{
    extension<TCandidate>(IOperator<Refinement, TCandidate> refiner)
    {
        /// <summary>The observations of this refiner's successful calls.</summary>
        public InvocationSource<Refinement, TCandidate, RefinementObservation<TCandidate>> Invocations =>
            ObservationSource.Create<Refinement, TCandidate, RefinementObservation<TCandidate>>(refiner);

        /// <summary>Records this refiner's successful calls into the instrument, wherever this configuration is placed.</summary>
        public Observed<Refinement, TCandidate, RefinementObservation<TCandidate>> Observed(IObservationInstrument<RefinementObservation<TCandidate>> instrument) =>
            new(refiner, instrument);
    }
}
