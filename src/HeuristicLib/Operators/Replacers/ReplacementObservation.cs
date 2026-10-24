using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>One successful replacement call, with the requested count and the survivors it returned.</summary>
/// <remarks>
/// The search space and problem are read at the candidate's interfaces, so an observation fits every run over the
/// candidate.
/// </remarks>
public sealed record ReplacementObservation<TCandidate>(
    IOperator<Replacement, TCandidate> Replacer,
    IReadOnlyList<EvaluatedCandidate<TCandidate>> PreviousPopulation,
    IReadOnlyList<EvaluatedCandidate<TCandidate>> OffspringPopulation,
    ObjectiveDirections Objective,
    int Count,
    IReadOnlyList<EvaluatedCandidate<TCandidate>> Replaced,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem);

file sealed class ReplacementObservationAdvice<TCandidate>(Action<ReplacementObservation<TCandidate>> observe) : ReplacerAdvice<TCandidate>
{
    protected override IReplacementExecutor<TCandidate, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(
        IReplacementExecutor<TCandidate, TSearchSpace, TProblem> executor, IOperator<Replacement, TCandidate> replacer) =>
        new Executor<TSearchSpace, TProblem>(executor, replacer, observe);

    private sealed class Executor<TSearchSpace, TProblem>(IReplacementExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IOperator<Replacement, TCandidate> replacer, Action<ReplacementObservation<TCandidate>> observe)
        : IReplacementExecutor<TCandidate, TSearchSpace, TProblem>, IExecutorControlProvider
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation,
            IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count,
            IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var replaced = executor.Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
            observe(new ReplacementObservation<TCandidate>(replacer, previousPopulation, offspringPopulation, objective, count, replaced,
                searchSpace, problem));
            return replaced;
        }

        public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(executor);
    }
}

public static class ReplacementObservations
{
    extension<TCandidate>(IOperator<Replacement, TCandidate> replacer)
    {
        /// <summary>The observations of this replacer's successful calls.</summary>
        public ObservationSource<ReplacementObservation<TCandidate>> Calls =>
            new(replacer, observe => Aspect.Create(Pointcut.Reference(replacer), new ReplacementObservationAdvice<TCandidate>(observe)));
    }
}
