using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public sealed record CountingCrossover<TCandidate>
    : WrappingCrossover<TCandidate>
{
    public ObservationCounter Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingCrossover(ICrossover<TCandidate> childCrossover, ObservationCounter counter, OperatorCountMetric metric)
        : base(childCrossover)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem> childCrossover) =>
        new Instance<TRunSearchSpace, TRunProblem>(childCrossover, Counter, Metric);

    private sealed class Instance<TSearchSpace, TProblem>(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> childCrossover, ObservationCounter counter, OperatorCountMetric metric)
        : WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem>(childCrossover)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
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
    public static CountingCrossover<TCandidate> Create<TCandidate>(ICrossover<TCandidate> childCrossover, ObservationCounter counter, OperatorCountMetric metric)
 =>
        new(childCrossover, counter, metric);
}

public static class CrossoverCounterExtensions
{
    extension<TCandidate>(ICrossover<TCandidate> crossover)
    {
        public CountingCrossover<TCandidate> CountCrossoverCalls(ObservationCounter counter) => new(crossover, counter, OperatorCountMetric.Calls);

        public CountingCrossover<TCandidate> CountCrossoverCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return crossover.CountCrossoverCalls(counter);
        }

        public CountingCrossover<TCandidate> CountCrossedCandidates(ObservationCounter counter) => new(crossover, counter, OperatorCountMetric.Candidates);

        public CountingCrossover<TCandidate> CountCrossedCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return crossover.CountCrossedCandidates(counter);
        }
    }
}
