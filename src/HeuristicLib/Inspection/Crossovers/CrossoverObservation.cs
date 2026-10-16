using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>
/// The inputs and outputs captured after one crossover call.
/// </summary>
public sealed record CrossoverObservation<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate> Crossover, IReadOnlyList<TCandidate> Offspring, IReadOnlyList<Parents<TCandidate>> Parents, TSearchSpace SearchSpace, TProblem Problem)
    : Observation<TProblem>(Problem)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public static class CrossoverObservations
{
    extension(Aspect)
    {
        /// <summary>Observes every call to one chosen crossover.</summary>
        /// <remarks>
        /// The crossover names only its candidate, so the observation names the search space and problem it reads. A
        /// run over types the observation was not written for is reported when the execution graph is built.
        /// </remarks>
        public static Aspect Observe<TCandidate, TSearchSpace, TProblem>(
            ICrossover<TCandidate> crossover,
            Action<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> observe)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            Aspect.Create(Pointcut.Reference(crossover), new CrossoverObservationAdvice<TCandidate, TSearchSpace, TProblem>(observe));

        /// <summary>Observes every call to one chosen crossover, for an observer that reads no particular search space or problem.</summary>
        /// <remarks>
        /// An implicitly typed lambda binds here, because the overload naming the search space and problem cannot infer
        /// them from it. The observation fits every run over the candidate.
        /// </remarks>
        public static Aspect Observe<TCandidate>(
            ICrossover<TCandidate> crossover,
            Action<CrossoverObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> observe) =>
            Aspect.Observe<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>(crossover, observe);
    }
}

public sealed class CrossoverObservationAdvice<TCandidate, TSearchSpace, TProblem>(
    Action<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> observe) : CrossoverAdvice<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public override WrapperExecutorFactory<ICrossoverExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(ICrossover<TCandidate> crossover)
    {
        ObservationSignature.Require<TSearchSpace, TProblem, TRunSearchSpace, TRunProblem>(this);
        return inner => new Executor<TRunSearchSpace, TRunProblem>(crossover, inner, observe);
    }

    private sealed class Executor<TRunSearchSpace, TRunProblem>(
        ICrossover<TCandidate> observedCrossover,
        ICrossoverExecutor<TCandidate, TRunSearchSpace, TRunProblem> childCrossover,
        Action<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> observe)
        : WrappingCrossoverExecutor<TCandidate, TRunSearchSpace, TRunProblem>(childCrossover)
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
