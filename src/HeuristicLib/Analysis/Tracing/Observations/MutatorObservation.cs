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
    extension(ResolutionScopeBuilder builder)
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
            builder.Install(new MutatorObservationModule<TCandidate, TSearchSpace, TProblem>(mutator, observe));

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
}

internal sealed class MutatorObservationModule<TCandidate, TSearchSpace, TProblem>(
    IMutator<TCandidate> mutator,
    Action<MutatorObservation<TCandidate, TSearchSpace, TProblem>> observe) : IExecutionModule
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void Install(ResolutionScopeBuilder builder) =>
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

    public IMutatorExecution<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ResolutionScope scope)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        ObservationSignature.Require<TSearchSpace, TProblem, TRunSearchSpace, TRunProblem>(this);
        return new Execution<TRunSearchSpace, TRunProblem>(observedMutator, scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(childMutator), observe);
    }

    private sealed class Execution<TRunSearchSpace, TRunProblem>(
        IMutator<TCandidate> observedMutator,
        IMutatorExecution<TCandidate, TRunSearchSpace, TRunProblem> childMutator,
        Action<MutatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
        : IMutatorExecution<TCandidate, TRunSearchSpace, TRunProblem>
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
