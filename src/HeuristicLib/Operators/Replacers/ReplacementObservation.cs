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
/// candidate. Read the bound objective through <c>Problem.Objective</c>.
/// </remarks>
public sealed record ReplacementObservation<TCandidate>(
    IOperator<Replacement, TCandidate> Replacer,
    ValueArray<EvaluatedCandidate<TCandidate>> PreviousPopulation,
    ValueArray<EvaluatedCandidate<TCandidate>> OffspringPopulation,
    int Count,
    ValueArray<EvaluatedCandidate<TCandidate>> Replaced,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem)
    : IInvocationObservation<Replacement, TCandidate, ReplacementObservation<TCandidate>>
{
    static IExecutor IInvocationObservation<Replacement, TCandidate, ReplacementObservation<TCandidate>>.Wrap<TSearchState>(
        IExecutor executor, IOperator<Replacement, TCandidate> source, Action<ReplacementObservation<TCandidate>> record,
        IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        new Executor((IReplacementExecutor<TCandidate>)executor, source, record, problem);

    private sealed class Executor(IReplacementExecutor<TCandidate> executor,
        IOperator<Replacement, TCandidate> replacer, Action<ReplacementObservation<TCandidate>> observe, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
        : ReplacementExecutor<TCandidate>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, int count, IRandomNumberGenerator random)
        {
            var replaced = executor.Replace(previousPopulation, offspringPopulation, count, random);
            observe(new ReplacementObservation<TCandidate>(replacer, previousPopulation.ToValueArray(), offspringPopulation.ToValueArray(), count, replaced.ToValueArray(),
                problem.SearchSpace, problem));
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

        /// <summary>Records this replacer's successful calls into the receiver, wherever this configuration is placed.</summary>
        public Observed<Replacement, TCandidate, ReplacementObservation<TCandidate>> Observed(IObservationReceiver<ReplacementObservation<TCandidate>> receiver) =>
            new(replacer, receiver);
    }
}
