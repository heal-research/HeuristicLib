using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public sealed record CountingCreator<TCandidate, TSearchSpace, TProblem>
    : WrappingCreator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ObservationCounter Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingCreator(ICreator<TCandidate, TSearchSpace, TProblem> childCreator, ObservationCounter counter, OperatorCountMetric metric)
        : base(childCreator)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ICreatorInstance<TCandidate, TSearchSpace, TProblem> childCreator) =>
        new Instance(childCreator, Counter, Metric);

    private sealed class Instance(ICreatorInstance<TCandidate, TSearchSpace, TProblem> childCreator, ObservationCounter counter, OperatorCountMetric metric)
        : WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem>(childCreator)
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
    public static CountingCreator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate, TSearchSpace, TProblem> childCreator, ObservationCounter counter, OperatorCountMetric metric)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childCreator, counter, metric);
}

public static class CreatorCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate, TSearchSpace, TProblem> creator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public CountingCreator<TCandidate, TSearchSpace, TProblem> CountCreatorCalls(ObservationCounter counter) => new(creator, counter, OperatorCountMetric.Calls);

        public CountingCreator<TCandidate, TSearchSpace, TProblem> CountCreatorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return creator.CountCreatorCalls(counter);
        }

        public CountingCreator<TCandidate, TSearchSpace, TProblem> CountCreatedCandidates(ObservationCounter counter) => new(creator, counter, OperatorCountMetric.Candidates);

        public CountingCreator<TCandidate, TSearchSpace, TProblem> CountCreatedCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return creator.CountCreatedCandidates(counter);
        }
    }
}
