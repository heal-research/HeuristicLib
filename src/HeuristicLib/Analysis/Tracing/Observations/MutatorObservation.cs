using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public sealed record MutatorObservation<TCandidate, TSearchSpace, TProblem>(
    IMutator<TCandidate, TSearchSpace, TProblem> Mutator,
    IReadOnlyList<TCandidate> Offspring,
    IReadOnlyList<TCandidate> Parents,
    TSearchSpace SearchSpace,
    TProblem Problem) : Observation<TProblem>(Problem)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public static class MutatorObservations
{
    extension(ExecutionInstanceResolverBuilder builder)
    {
        /// <summary>Observes every call to one chosen mutator.</summary>
        public void Observe<TCandidate, TSearchSpace, TProblem>(
            IMutator<TCandidate, TSearchSpace, TProblem> mutator,
            Action<MutatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            builder.Install(new MutatorObservationHook<TCandidate, TSearchSpace, TProblem>(mutator, observe));
    }
}

internal sealed class MutatorObservationHook<TCandidate, TSearchSpace, TProblem>(
    IMutator<TCandidate, TSearchSpace, TProblem> mutator,
    Action<MutatorObservation<TCandidate, TSearchSpace, TProblem>> observe) : IExecutionHook
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void Install(ExecutionInstanceResolverBuilder builder) =>
        builder.Decorate(mutator, current => new ObservingMutator<TCandidate, TSearchSpace, TProblem>(mutator, current, observe));
}

internal sealed class ObservingMutator<TCandidate, TSearchSpace, TProblem>(
    IMutator<TCandidate, TSearchSpace, TProblem> observedMutator,
    IMutator<TCandidate, TSearchSpace, TProblem> childMutator,
    Action<MutatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
    : IMutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public IMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceResolver resolver) =>
        new Instance(observedMutator, resolver.Resolve(childMutator), observe);

    private sealed class Instance(
        IMutator<TCandidate, TSearchSpace, TProblem> observedMutator,
        IMutatorInstance<TCandidate, TSearchSpace, TProblem> childMutator,
        Action<MutatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
        : IMutatorInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random,
            TSearchSpace searchSpace, TProblem problem)
        {
            var offspring = childMutator.Mutate(parents, random, searchSpace, problem);
            observe(new MutatorObservation<TCandidate, TSearchSpace, TProblem>(
                observedMutator, offspring, parents, searchSpace, problem));
            return offspring;
        }
    }
}
