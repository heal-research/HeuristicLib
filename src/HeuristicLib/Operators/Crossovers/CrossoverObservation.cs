using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>One successful crossover call, with the offspring it returned.</summary>
/// <remarks>
/// The search space and problem are read at the candidate's interfaces, so an observation fits every run over the
/// candidate.
/// </remarks>
public sealed record CrossoverObservation<TCandidate>(
    IOperator<Crossover, TCandidate> Crossover,
    IReadOnlyList<Parents<TCandidate>> Parents,
    IReadOnlyList<TCandidate> Offspring,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem);

file sealed class CrossoverObservationAdvice<TCandidate>(Action<CrossoverObservation<TCandidate>> observe) : CrossoverAdvice<TCandidate>
{
    protected override ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(
        ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> executor, IOperator<Crossover, TCandidate> crossover) =>
        new Executor<TSearchSpace, TProblem>(executor, crossover, observe);

    private sealed class Executor<TSearchSpace, TProblem>(ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IOperator<Crossover, TCandidate> crossover, Action<CrossoverObservation<TCandidate>> observe)
        : ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>, IExecutorControlProvider
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var offspring = executor.Cross(parents, random, searchSpace, problem);
            observe(new CrossoverObservation<TCandidate>(crossover, parents, offspring, searchSpace, problem));
            return offspring;
        }

        public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(executor);
    }
}

public static class CrossoverObservations
{
    extension<TCandidate>(IOperator<Crossover, TCandidate> crossover)
    {
        /// <summary>The observations of this crossover's successful calls.</summary>
        public ObservationSource<CrossoverObservation<TCandidate>> Invocations =>
            ObservationSource.Create<CrossoverObservation<TCandidate>>(crossover, observe => Aspect.Create(Pointcut.Reference(crossover), new CrossoverObservationAdvice<TCandidate>(observe)));
    }
}
