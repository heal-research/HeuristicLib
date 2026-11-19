using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Crossovers;
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
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem)
    : IInvocationObservation<Crossover, TCandidate, CrossoverObservation<TCandidate>>
{
    static IExecutor IInvocationObservation<Crossover, TCandidate, CrossoverObservation<TCandidate>>.Wrap<TSearchSpace, TProblem, TSearchState>(
        IExecutor executor, IOperator<Crossover, TCandidate> source, Action<CrossoverObservation<TCandidate>> record, TProblem problem) =>
        new Executor((ICrossoverExecutor<TCandidate>)executor, source, record, problem);

    private sealed class Executor(ICrossoverExecutor<TCandidate> executor,
        IOperator<Crossover, TCandidate> crossover, Action<CrossoverObservation<TCandidate>> observe, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
        : CrossoverExecutor<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random)
        {
            var offspring = executor.Cross(parents, random);
            observe(new CrossoverObservation<TCandidate>(crossover, parents, offspring, problem.SearchSpace, problem));
            return offspring;
        }
    }
}

public static class CrossoverObservations
{
    extension<TCandidate>(IOperator<Crossover, TCandidate> crossover)
    {
        /// <summary>The observations of this crossover's successful calls.</summary>
        public InvocationSource<Crossover, TCandidate, CrossoverObservation<TCandidate>> Invocations =>
            ObservationSource.Create<Crossover, TCandidate, CrossoverObservation<TCandidate>>(crossover);

        /// <summary>Records this crossover's successful calls into the receiver, wherever this configuration is placed.</summary>
        public Observed<Crossover, TCandidate, CrossoverObservation<TCandidate>> Observed(IObservationReceiver<CrossoverObservation<TCandidate>> receiver) =>
            new(crossover, receiver);
    }
}
