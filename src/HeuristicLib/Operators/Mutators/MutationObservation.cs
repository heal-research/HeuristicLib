using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
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
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem);

file sealed class MutationObservationAdvice<TCandidate>(Action<MutationObservation<TCandidate>> observe) : MutatorAdvice<TCandidate>
{
    protected override IMutationExecutor<TCandidate, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(
        IMutationExecutor<TCandidate, TSearchSpace, TProblem> executor, IOperator<Mutation, TCandidate> mutator) =>
        new Executor<TSearchSpace, TProblem>(executor, mutator, observe);

    private sealed class Executor<TSearchSpace, TProblem>(IMutationExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IOperator<Mutation, TCandidate> mutator, Action<MutationObservation<TCandidate>> observe)
        : IMutationExecutor<TCandidate, TSearchSpace, TProblem>, IExecutorControlProvider
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var offspring = executor.Mutate(parents, random, searchSpace, problem);
            observe(new MutationObservation<TCandidate>(mutator, parents, offspring, searchSpace, problem));
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
        public ObservationSource<MutationObservation<TCandidate>> Calls =>
            new(mutator, observe => Aspect.Create(Pointcut.Reference(mutator), new MutationObservationAdvice<TCandidate>(observe)));
    }
}
