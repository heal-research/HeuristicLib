using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public sealed record CountingCrossover<TCandidate, TSearchSpace, TProblem>
    : WrappingCrossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ObservationCounter Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingCrossover(ICrossover<TCandidate, TSearchSpace, TProblem> childCrossover, ObservationCounter counter, OperatorCountMetric metric)
        : base(childCrossover)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> childCrossover) =>
        new Instance(childCrossover, Counter, Metric);

    private sealed class Instance(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> childCrossover, ObservationCounter counter, OperatorCountMetric metric)
        : WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem>(childCrossover)
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var offspring = ChildCrossover.Cross(parents, random, searchSpace, problem);
            counter.IncrementBy(metric == OperatorCountMetric.Calls ? 1 : offspring.Count);
            return offspring;
        }
    }
}

public static class CountingCrossover
{
    public static CountingCrossover<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> childCrossover, ObservationCounter counter, OperatorCountMetric metric)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childCrossover, counter, metric);
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
