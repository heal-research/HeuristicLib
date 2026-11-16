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
        new Executor<TSearchSpace, TProblem>((IMutationExecutor<TCandidate>)executor, source, record, problem);

    private sealed class Executor<TSearchSpace, TProblem>(IMutationExecutor<TCandidate> executor,
        IOperator<Mutation, TCandidate> mutator, Action<MutationObservation<TCandidate>> observe, TProblem problem)
        : MutationExecutor<TCandidate, TSearchSpace, TProblem>(problem), IExecutorControlProvider
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random)
        {
            var offspring = executor.Mutate(parents, random);
            observe(new MutationObservation<TCandidate>(mutator, parents, offspring, SearchSpace, Problem));
            return offspring;
        }

        public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(executor);
    }
}

public static class MutationObservations
{
    extension<TCandidate>(IOperator<Mutation, TCandidate> mutator)
    {
        /// <summary>The observations of this mutator's successful calls.</summary>
        public InvocationSource<Mutation, TCandidate, MutationObservation<TCandidate>> Invocations =>
            ObservationSource.Create<Mutation, TCandidate, MutationObservation<TCandidate>>(mutator);

        /// <summary>Records this mutator's successful calls into the instrument, wherever this configuration is placed.</summary>
        public Observed<Mutation, TCandidate, MutationObservation<TCandidate>> Observed(IObservationInstrument<MutationObservation<TCandidate>> instrument) =>
            new(mutator, instrument);
    }
}
