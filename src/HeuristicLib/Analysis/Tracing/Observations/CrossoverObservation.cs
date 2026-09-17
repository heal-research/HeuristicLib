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
        public void Observe<TCandidate, TSearchSpace, TProblem>(
            ICrossover<TCandidate, TSearchSpace, TProblem> crossover,
            Action<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> observe)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            builder.Install(new CrossoverObservationHook<TCandidate, TSearchSpace, TProblem>(crossover, observe));
    }
}

internal sealed class CrossoverObservationHook<TCandidate, TSearchSpace, TProblem>(
    ICrossover<TCandidate, TSearchSpace, TProblem> crossover,
    Action<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> observe) : IExecutionHook
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void Install(ExecutionInstanceResolverBuilder builder) =>
        builder.Decorate(crossover, current => new ObservingCrossover<TCandidate, TSearchSpace, TProblem>(crossover, current, observe));
}

internal sealed class ObservingCrossover<TCandidate, TSearchSpace, TProblem>(
    ICrossover<TCandidate, TSearchSpace, TProblem> observedCrossover,
    ICrossover<TCandidate, TSearchSpace, TProblem> childCrossover,
    Action<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> observe)
    : ICrossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ICrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceResolver resolver) =>
        new Instance(observedCrossover, resolver.Resolve(childCrossover), observe);

    private sealed class Instance(
        ICrossover<TCandidate, TSearchSpace, TProblem> observedCrossover,
        ICrossoverInstance<TCandidate, TSearchSpace, TProblem> childCrossover,
        Action<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> observe)
        : ICrossoverInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random,
            TSearchSpace searchSpace, TProblem problem)
        {
            var offspring = childCrossover.Cross(parents, random, searchSpace, problem);
            observe(new CrossoverObservation<TCandidate, TSearchSpace, TProblem>(
                observedCrossover, offspring, parents, searchSpace, problem));
            return offspring;
        }
    }
}
