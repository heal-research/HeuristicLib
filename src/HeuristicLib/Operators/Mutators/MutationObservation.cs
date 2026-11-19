using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>One successful mutation call, with the offspring it returned.</summary>
/// <remarks>
/// The search space and problem are read at the candidate's interfaces, so an observation fits every run over the
/// candidate.
/// </remarks>
public sealed record MutationObservation<TCandidate>(
    IOperator<Mutation, TCandidate> Mutator,
    IReadOnlyList<TCandidate> Parents,
    IReadOnlyList<TCandidate> Offspring,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem)
    : IInvocationObservation<Mutation, TCandidate, MutationObservation<TCandidate>>
{
    static IExecutor IInvocationObservation<Mutation, TCandidate, MutationObservation<TCandidate>>.Wrap<TSearchSpace, TProblem, TSearchState>(
        IExecutor executor, IOperator<Mutation, TCandidate> source, Action<MutationObservation<TCandidate>> record, TProblem problem) =>
        new Executor((IMutationExecutor<TCandidate>)executor, source, record, problem);

    private sealed class Executor(IMutationExecutor<TCandidate> executor,
        IOperator<Mutation, TCandidate> mutator, Action<MutationObservation<TCandidate>> observe, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
        : MutationExecutor<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random)
        {
            var offspring = executor.Mutate(parents, random);
            observe(new MutationObservation<TCandidate>(mutator, parents, offspring, problem.SearchSpace, problem));
            return offspring;
        }
    }
}

public static class MutationObservations
{
    extension<TCandidate>(IOperator<Mutation, TCandidate> mutator)
    {
        /// <summary>The observations of this mutator's successful calls.</summary>
        public InvocationSource<Mutation, TCandidate, MutationObservation<TCandidate>> Invocations =>
            ObservationSource.Create<Mutation, TCandidate, MutationObservation<TCandidate>>(mutator);

        /// <summary>Records this mutator's successful calls into the receiver, wherever this configuration is placed.</summary>
        public Observed<Mutation, TCandidate, MutationObservation<TCandidate>> Observed(IObservationReceiver<MutationObservation<TCandidate>> receiver) =>
            new(mutator, receiver);
    }
}
