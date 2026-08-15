using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

public sealed record CountingRefiner<TCandidate, TSearchSpace, TProblem>
    : WrappingRefiner<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ObservationCounter Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingRefiner(IRefiner<TCandidate, TSearchSpace, TProblem> childRefiner, ObservationCounter counter, OperatorCountMetric metric)
        : base(childRefiner)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override WrappingRefinerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IRefinerInstance<TCandidate, TSearchSpace, TProblem> childRefiner) =>
        new Instance(childRefiner, Counter, Metric);

    private sealed class Instance(IRefinerInstance<TCandidate, TSearchSpace, TProblem> childRefiner, ObservationCounter counter, OperatorCountMetric metric)
        : WrappingRefinerInstance<TCandidate, TSearchSpace, TProblem>(childRefiner)
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
    public static CountingRefiner<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate, TSearchSpace, TProblem> childRefiner, ObservationCounter counter, OperatorCountMetric metric)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childRefiner, counter, metric);
}

public static class RefinerCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate, TSearchSpace, TProblem> refiner)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public CountingRefiner<TCandidate, TSearchSpace, TProblem> CountRefinerCalls(ObservationCounter counter) => new(refiner, counter, OperatorCountMetric.Calls);

        public CountingRefiner<TCandidate, TSearchSpace, TProblem> CountRefinerCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return refiner.CountRefinerCalls(counter);
        }

        public CountingRefiner<TCandidate, TSearchSpace, TProblem> CountRefinedCandidates(ObservationCounter counter) => new(refiner, counter, OperatorCountMetric.Candidates);

        public CountingRefiner<TCandidate, TSearchSpace, TProblem> CountRefinedCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return refiner.CountRefinedCandidates(counter);
        }
    }
}
