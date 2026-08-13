using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public sealed record CountingSelector<TCandidate, TSearchSpace, TProblem>
    : WrappingSelector<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ObservationCounter Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingSelector(ISelector<TCandidate, TSearchSpace, TProblem> childSelector, ObservationCounter counter, OperatorCountMetric metric)
        : base(childSelector)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ISelectorInstance<TCandidate, TSearchSpace, TProblem> childSelector) =>
        new Instance(childSelector, Counter, Metric);

    private sealed class Instance(ISelectorInstance<TCandidate, TSearchSpace, TProblem> childSelector, ObservationCounter counter, OperatorCountMetric metric)
        : WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem>(childSelector)
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var selected = ChildSelector.Select(population, objective, count, random, searchSpace, problem);
            counter.IncrementBy(metric == OperatorCountMetric.Calls ? 1 : selected.Count);
            return selected;
        }
    }
}

public static class CountingSelector
{
    public static CountingSelector<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate, TSearchSpace, TProblem> childSelector, ObservationCounter counter, OperatorCountMetric metric)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childSelector, counter, metric);
}

public static class SelectorCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate, TSearchSpace, TProblem> selector)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public CountingSelector<TCandidate, TSearchSpace, TProblem> CountSelectorCalls(ObservationCounter counter) => new(selector, counter, OperatorCountMetric.Calls);

        public CountingSelector<TCandidate, TSearchSpace, TProblem> CountSelectorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return selector.CountSelectorCalls(counter);
        }

        public CountingSelector<TCandidate, TSearchSpace, TProblem> CountSelectedCandidates(ObservationCounter counter) => new(selector, counter, OperatorCountMetric.Candidates);

        public CountingSelector<TCandidate, TSearchSpace, TProblem> CountSelectedCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return selector.CountSelectedCandidates(counter);
        }
    }
}
