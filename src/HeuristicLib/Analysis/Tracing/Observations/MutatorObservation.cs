using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public sealed record MutatorObservation<TCandidate, TSearchSpace, TProblem>(
    IMutator<TCandidate> Mutator,
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
        /// <remarks>
        /// The mutator names only its candidate, so the observation names the search space and problem it reads. A run
        /// over types the observation was not written for is reported when the execution graph is built.
        /// </remarks>
        public void Observe<TCandidate, TSearchSpace, TProblem>(
            IMutator<TCandidate> mutator,
            Action<MutatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            builder.Install(new MutatorObservationHook<TCandidate, TSearchSpace, TProblem>(mutator, observe));

        /// <summary>Observes every call to one chosen mutator, for an observer that reads no particular search space or problem.</summary>
        /// <remarks>
        /// An implicitly typed lambda binds here, because the overload naming the search space and problem cannot infer
        /// them from it. The observation fits every run over the candidate.
        /// </remarks>
        public void Observe<TCandidate>(
            IMutator<TCandidate> mutator,
            Action<MutatorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> observe) =>
            builder.Observe<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>(mutator, observe);
    }

    extension<TCandidate, TSearchSpace, TProblem>(TypedObservationBuilder<TCandidate, TSearchSpace, TProblem> builder)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public void Observe(IMutator<TCandidate> mutator, Action<MutatorObservation<TCandidate, TSearchSpace, TProblem>> observe) =>
            builder.Builder.Observe<TCandidate, TSearchSpace, TProblem>(mutator, observe);
    }
}

internal sealed class MutatorObservationHook<TCandidate, TSearchSpace, TProblem>(
    IMutator<TCandidate> mutator,
    Action<MutatorObservation<TCandidate, TSearchSpace, TProblem>> observe) : IExecutionHook
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void Install(ExecutionInstanceResolverBuilder builder) =>
        builder.Decorate(mutator, current => new ObservingMutator<TCandidate, TSearchSpace, TProblem>(mutator, current, observe));
}

internal sealed class ObservingMutator<TCandidate, TSearchSpace, TProblem>(
    IMutator<TCandidate> observedMutator,
    IMutator<TCandidate> childMutator,
    Action<MutatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
    : IMutator<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public bool Fits(ExecutionSignature execution) =>
        ObservationSignature.Fits<TSearchSpace, TProblem>(execution) && execution.Fits(childMutator);

    public IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceResolver resolver)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        ObservationSignature.Require<TSearchSpace, TProblem, TRunSearchSpace, TRunProblem>(this);
        return new Instance<TRunSearchSpace, TRunProblem>(observedMutator, resolver.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(childMutator), observe);
    }

    private sealed class Instance<TRunSearchSpace, TRunProblem>(
        IMutator<TCandidate> observedMutator,
        IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childMutator,
        Action<MutatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
        : IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem>
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random,
            TRunSearchSpace searchSpace, TRunProblem problem)
        {
            var offspring = childMutator.Mutate(parents, random, searchSpace, problem);
            observe(new MutatorObservation<TCandidate, TSearchSpace, TProblem>(
                observedMutator, offspring, parents, (TSearchSpace)(object)searchSpace, (TProblem)(object)problem));
            return offspring;
        }
    }
}
