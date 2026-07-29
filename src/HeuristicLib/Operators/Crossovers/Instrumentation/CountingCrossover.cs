using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public sealed record CountingCrossover<TCandidate, TSearchSpace, TProblem> : ObservableCrossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ObservationCounter Counter { get; }
    public OperatorCountMetric Metric { get; }

    public CountingCrossover(ICrossover<TCandidate, TSearchSpace, TProblem> crossover, ObservationCounter counter, OperatorCountMetric metric)
        : base(crossover, new ActionCrossoverObserver<TCandidate, TSearchSpace, TProblem>(
            (offspring, _, _, _) => counter.IncrementBy(metric == OperatorCountMetric.Calls ? 1 : offspring.Count)))
    {
        Counter = counter;
        Metric = metric;
    }
}

public static class CrossoverCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> crossover)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public CountingCrossover<TCandidate, TSearchSpace, TProblem> CountCrossoverCalls(ObservationCounter counter) => new(crossover, counter, OperatorCountMetric.Calls);

        public CountingCrossover<TCandidate, TSearchSpace, TProblem> CountCrossoverCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return crossover.CountCrossoverCalls(counter);
        }

        public CountingCrossover<TCandidate, TSearchSpace, TProblem> CountCrossedCandidates(ObservationCounter counter) => new(crossover, counter, OperatorCountMetric.Candidates);

        public CountingCrossover<TCandidate, TSearchSpace, TProblem> CountCrossedCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return crossover.CountCrossedCandidates(counter);
        }
    }
}
