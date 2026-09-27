using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public sealed record CountingCreator<TCandidate>
    : WrappingCreator<TCandidate>
{
    public CountAccumulator Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingCreator(ICreator<TCandidate> childCreator, CountAccumulator counter, OperatorCountMetric metric)
        : base(childCreator)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override ICreatorExecution<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ICreatorExecution<TCandidate, TRunSearchSpace, TRunProblem> childCreator) =>
        new Execution<TRunSearchSpace, TRunProblem>(childCreator, Counter, Metric);

    private sealed class Execution<TSearchSpace, TProblem>(ICreatorExecution<TCandidate, TSearchSpace, TProblem> childCreator, CountAccumulator counter, OperatorCountMetric metric)
        : WrappingCreatorExecution<TCandidate, TSearchSpace, TProblem>(childCreator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var candidates = ChildCreator.Create(count, random, searchSpace, problem);
            counter.IncrementBy(metric == OperatorCountMetric.Calls ? 1 : candidates.Count);
            return candidates;
        }
    }
}

public static class CountingCreator
{
    public static CountingCreator<TCandidate> Create<TCandidate>(ICreator<TCandidate> childCreator, CountAccumulator counter, OperatorCountMetric metric) =>
        new(childCreator, counter, metric);
}

public static class CreatorCounterExtensions
{
    extension<TCandidate>(ICreator<TCandidate> creator)
    {
        public CountingCreator<TCandidate> CountCalls(CountAccumulator counter) => new(creator, counter, OperatorCountMetric.Calls);

        public CountingCreator<TCandidate> CountCalls(out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return creator.CountCalls(counter);
        }

        public CountingCreator<TCandidate> CountCandidates(CountAccumulator counter) => new(creator, counter, OperatorCountMetric.Candidates);

        public CountingCreator<TCandidate> CountCandidates(out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return creator.CountCandidates(counter);
        }
    }
}
