using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Replacers;
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
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem)
    : IInvocationObservation<Replacement, TCandidate, ReplacementObservation<TCandidate>>
{
    static IExecutor IInvocationObservation<Replacement, TCandidate, ReplacementObservation<TCandidate>>.Wrap<TSearchSpace, TProblem, TSearchState>(
        IExecutor executor, IOperator<Replacement, TCandidate> source, Action<ReplacementObservation<TCandidate>> record, TProblem problem) =>
        new Executor<TSearchSpace, TProblem>((IReplacementExecutor<TCandidate>)executor, source, record, problem);

    private sealed class Executor<TSearchSpace, TProblem>(IReplacementExecutor<TCandidate> executor,
        IOperator<Replacement, TCandidate> replacer, Action<ReplacementObservation<TCandidate>> observe, TProblem problem)
        : ReplacementExecutor<TCandidate, TSearchSpace, TProblem>(problem)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random)
        {
            var replaced = executor.Replace(previousPopulation, offspringPopulation, objective, count, random);
            observe(new ReplacementObservation<TCandidate>(replacer, previousPopulation, offspringPopulation, objective, count, replaced,
                SearchSpace, Problem));
            return replaced;
        }
    }
}

public static class ReplacementObservations
{
    extension<TCandidate>(IOperator<Replacement, TCandidate> replacer)
    {
        /// <summary>The observations of this replacer's successful calls.</summary>
        public InvocationSource<Replacement, TCandidate, ReplacementObservation<TCandidate>> Invocations =>
            ObservationSource.Create<Replacement, TCandidate, ReplacementObservation<TCandidate>>(replacer);

        /// <summary>Records this replacer's successful calls into the instrument, wherever this configuration is placed.</summary>
        public Observed<Replacement, TCandidate, ReplacementObservation<TCandidate>> Observed(IObservationInstrument<ReplacementObservation<TCandidate>> instrument) =>
            new(replacer, instrument);
    }
}
