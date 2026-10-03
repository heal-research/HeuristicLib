using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

public sealed record CountingRefiner<TCandidate>
    : WrappingRefiner<TCandidate>
{
    public CountAccumulator Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingRefiner(IRefiner<TCandidate> childRefiner, CountAccumulator counter, OperatorCountMetric metric)
        : base(childRefiner)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override WrapperExecutionFactory<IRefinerExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>() =>
        childRefiner => new Execution<TRunSearchSpace, TRunProblem>(childRefiner, Counter, Metric);

    private sealed class Execution<TSearchSpace, TProblem>(IRefinerExecution<TCandidate, TSearchSpace, TProblem> childRefiner, CountAccumulator counter, OperatorCountMetric metric)
        : WrappingRefinerExecution<TCandidate, TSearchSpace, TProblem>(childRefiner)
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
    public static CountingRefiner<TCandidate> Create<TCandidate>(IRefiner<TCandidate> childRefiner, CountAccumulator counter, OperatorCountMetric metric) =>
        new(childRefiner, counter, metric);
}

public static class RefinerCounterExtensions
{
    extension<TCandidate>(IRefiner<TCandidate> refiner)
    {
        public CountingRefiner<TCandidate> CountCalls(CountAccumulator counter) => new(refiner, counter, OperatorCountMetric.Calls);

        public CountingRefiner<TCandidate> CountCalls(out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return refiner.CountCalls(counter);
        }

        public CountingRefiner<TCandidate> CountCandidates(CountAccumulator counter) => new(refiner, counter, OperatorCountMetric.Candidates);

        public CountingRefiner<TCandidate> CountCandidates(out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return refiner.CountCandidates(counter);
        }
    }
}
