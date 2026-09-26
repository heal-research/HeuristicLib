using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public sealed record CountingCrossover<TCandidate>
    : WrappingCrossover<TCandidate>
{
    public CountAccumulator Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingCrossover(ICrossover<TCandidate> childCrossover, CountAccumulator counter, OperatorCountMetric metric)
        : base(childCrossover)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem> childCrossover) =>
        new Instance<TRunSearchSpace, TRunProblem>(childCrossover, Counter, Metric);

    private sealed class Instance<TSearchSpace, TProblem>(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> childCrossover, CountAccumulator counter, OperatorCountMetric metric)
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
    public static CountingCrossover<TCandidate> Create<TCandidate>(ICrossover<TCandidate> childCrossover, CountAccumulator counter, OperatorCountMetric metric) =>
        new(childCrossover, counter, metric);
}

public static class CrossoverCounterExtensions
{
    extension<TCandidate>(ICrossover<TCandidate> crossover)
    {
        public CountingCrossover<TCandidate> CountCalls(CountAccumulator counter) => new(crossover, counter, OperatorCountMetric.Calls);

        public CountingCrossover<TCandidate> CountCalls(out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return crossover.CountCalls(counter);
        }

        public CountingCrossover<TCandidate> CountCandidates(CountAccumulator counter) => new(crossover, counter, OperatorCountMetric.Candidates);

        public CountingCrossover<TCandidate> CountCandidates(out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return crossover.CountCandidates(counter);
        }
    }
}
