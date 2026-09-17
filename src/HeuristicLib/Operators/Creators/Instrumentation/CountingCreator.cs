using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public sealed record CountingCreator<TCandidate>
    : WrappingCreator<TCandidate>
{
    public ObservationCounter Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingCreator(ICreator<TCandidate> childCreator, ObservationCounter counter, OperatorCountMetric metric)
        : base(childCreator)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override ICreatorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ICreatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childCreator) =>
        new Instance<TRunSearchSpace, TRunProblem>(childCreator, Counter, Metric);

    private sealed class Instance<TSearchSpace, TProblem>(ICreatorInstance<TCandidate, TSearchSpace, TProblem> childCreator, ObservationCounter counter, OperatorCountMetric metric)
        : WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem>(childCreator)
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
    public static CountingCreator<TCandidate> Create<TCandidate>(ICreator<TCandidate> childCreator, ObservationCounter counter, OperatorCountMetric metric) =>
        new(childCreator, counter, metric);
}

public static class CreatorCounterExtensions
{
    extension<TCandidate>(ICreator<TCandidate> creator)
    {
        public CountingCreator<TCandidate> CountCalls(ObservationCounter counter) => new(creator, counter, OperatorCountMetric.Calls);

        public CountingCreator<TCandidate> CountCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return creator.CountCalls(counter);
        }

        public CountingCreator<TCandidate> CountCandidates(ObservationCounter counter) => new(creator, counter, OperatorCountMetric.Candidates);

        public CountingCreator<TCandidate> CountCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return creator.CountCandidates(counter);
        }
    }
}
