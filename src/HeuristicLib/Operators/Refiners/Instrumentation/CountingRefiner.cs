using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

public sealed record CountingRefiner<TCandidate>
    : WrappingRefiner<TCandidate>
{
    public ObservationCounter Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingRefiner(IRefiner<TCandidate> childRefiner, ObservationCounter counter, OperatorCountMetric metric)
        : base(childRefiner)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> childRefiner) =>
        new Instance<TRunSearchSpace, TRunProblem>(childRefiner, Counter, Metric);

    private sealed class Instance<TSearchSpace, TProblem>(IRefinerInstance<TCandidate, TSearchSpace, TProblem> childRefiner, ObservationCounter counter, OperatorCountMetric metric)
        : WrappingRefinerInstance<TCandidate, TSearchSpace, TProblem>(childRefiner)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var refined = ChildRefiner.Refine(candidates, random, searchSpace, problem);
            counter.IncrementBy(metric == OperatorCountMetric.Calls ? 1 : refined.Count);
            return refined;
        }
    }
}

public static class CountingRefiner
{
    public static CountingRefiner<TCandidate> Create<TCandidate>(IRefiner<TCandidate> childRefiner, ObservationCounter counter, OperatorCountMetric metric) =>
        new(childRefiner, counter, metric);
}

public static class RefinerCounterExtensions
{
    extension<TCandidate>(IRefiner<TCandidate> refiner)
    {
        public CountingRefiner<TCandidate> CountCalls(ObservationCounter counter) => new(refiner, counter, OperatorCountMetric.Calls);

        public CountingRefiner<TCandidate> CountCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return refiner.CountCalls(counter);
        }

        public CountingRefiner<TCandidate> CountCandidates(ObservationCounter counter) => new(refiner, counter, OperatorCountMetric.Candidates);

        public CountingRefiner<TCandidate> CountCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return refiner.CountCandidates(counter);
        }
    }
}
