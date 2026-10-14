using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static class CrossoverObservations
{
    extension(AspectBuilder builder)
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
            builder.Install(new CrossoverObservationAspect<TCandidate, TSearchSpace, TProblem>(crossover, observe));

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
}

internal sealed class CrossoverObservationAspect<TCandidate, TSearchSpace, TProblem>(
    ICrossover<TCandidate> crossover,
    Action<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> observe) : Aspect
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected override void Declare(AspectBuilder builder) =>
        builder.Install(crossover, current => new ObservingCrossover<TCandidate, TSearchSpace, TProblem>(crossover, current, observe));
}

internal sealed record ObservingCrossover<TCandidate, TSearchSpace, TProblem> : WrappingCrossover<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private readonly ICrossover<TCandidate> observedCrossover;
    private readonly Action<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> observe;

    public ObservingCrossover(ICrossover<TCandidate> observedCrossover, ICrossover<TCandidate> childCrossover, Action<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> observe)
        : base(childCrossover)
    {
        this.observedCrossover = observedCrossover;
        this.observe = observe;
    }

    public override bool Fits(ExecutionSignature execution) =>
        base.Fits(execution) && ObservationSignature.Fits<TSearchSpace, TProblem>(execution);

    protected override WrapperExecutionFactory<ICrossoverExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>()
    {
        ObservationSignature.Require<TSearchSpace, TProblem, TRunSearchSpace, TRunProblem>(this);
        return inner => new Execution<TRunSearchSpace, TRunProblem>(observedCrossover, inner, observe);
    }

    private sealed class Execution<TRunSearchSpace, TRunProblem>(
        ICrossover<TCandidate> observedCrossover,
        ICrossoverExecution<TCandidate, TRunSearchSpace, TRunProblem> childCrossover,
        Action<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> observe)
        : WrappingCrossoverExecution<TCandidate, TRunSearchSpace, TRunProblem>(childCrossover)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random,
            TRunSearchSpace searchSpace, TRunProblem problem)
        {
            var offspring = ChildCrossover.Cross(parents, random, searchSpace, problem);
            observe(new CrossoverObservation<TCandidate, TSearchSpace, TProblem>(
                observedCrossover, offspring, parents, (TSearchSpace)(object)searchSpace, (TProblem)(object)problem));
            return offspring;
        }
    }
}
