using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static class CrossoverObservations
{
    extension(ExecutionInstanceResolverBuilder builder)
    {
        /// <summary>Observes every call to one chosen crossover.</summary>
        /// <remarks>
        /// The crossover names only its candidate, so the observation names the search space and problem it reads. A
        /// run over types the observation was not written for is reported when the execution graph is built.
        /// </remarks>
        public void Observe<TCandidate, TSearchSpace, TProblem>(
            ICrossover<TCandidate> crossover,
            Action<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> observe)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            builder.Install(new CrossoverObservationHook<TCandidate, TSearchSpace, TProblem>(crossover, observe));

        /// <summary>Observes every call to one chosen crossover, for an observer that reads no particular search space or problem.</summary>
        /// <remarks>
        /// An implicitly typed lambda binds here, because the overload naming the search space and problem cannot infer
        /// them from it. The observation fits every run over the candidate.
        /// </remarks>
        public void Observe<TCandidate>(
            ICrossover<TCandidate> crossover,
            Action<CrossoverObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> observe) =>
            builder.Observe<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>(crossover, observe);
    }

    extension<TCandidate, TSearchSpace, TProblem>(TypedObservationBuilder<TCandidate, TSearchSpace, TProblem> builder)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public void Observe(ICrossover<TCandidate> crossover, Action<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> observe) =>
            builder.Builder.Observe<TCandidate, TSearchSpace, TProblem>(crossover, observe);
    }
}

internal sealed class CrossoverObservationHook<TCandidate, TSearchSpace, TProblem>(
    ICrossover<TCandidate> crossover,
    Action<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> observe) : IExecutionHook
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void Install(ExecutionInstanceResolverBuilder builder) =>
        builder.Decorate(crossover, current => new ObservingCrossover<TCandidate, TSearchSpace, TProblem>(crossover, current, observe));
}

internal sealed class ObservingCrossover<TCandidate, TSearchSpace, TProblem>(
    ICrossover<TCandidate> observedCrossover,
    ICrossover<TCandidate> childCrossover,
    Action<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> observe)
    : ICrossover<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public bool Fits(ExecutionSignature execution) =>
        ObservationSignature.Fits<TSearchSpace, TProblem>(execution) && execution.Fits(childCrossover);

    public ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceResolver resolver)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        ObservationSignature.Require<TSearchSpace, TProblem, TRunSearchSpace, TRunProblem>(this);
        return new Instance<TRunSearchSpace, TRunProblem>(observedCrossover, resolver.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(childCrossover), observe);
    }

    private sealed class Instance<TRunSearchSpace, TRunProblem>(
        ICrossover<TCandidate> observedCrossover,
        ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem> childCrossover,
        Action<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> observe)
        : ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem>
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        public IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random,
            TRunSearchSpace searchSpace, TRunProblem problem)
        {
            var offspring = childCrossover.Cross(parents, random, searchSpace, problem);
            observe(new CrossoverObservation<TCandidate, TSearchSpace, TProblem>(
                observedCrossover, offspring, parents, (TSearchSpace)(object)searchSpace, (TProblem)(object)problem));
            return offspring;
        }
    }
}
