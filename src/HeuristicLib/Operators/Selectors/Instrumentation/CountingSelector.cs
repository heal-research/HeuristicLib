using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public sealed record CountingSelector<TCandidate>
    : WrappingSelector<TCandidate>
{
    public ObservationCounter Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingSelector(ISelector<TCandidate> childSelector, ObservationCounter counter, OperatorCountMetric metric)
        : base(childSelector)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem> childSelector) =>
        new Instance<TRunSearchSpace, TRunProblem>(childSelector, Counter, Metric);

    private sealed class Instance<TSearchSpace, TProblem>(ISelectorInstance<TCandidate, TSearchSpace, TProblem> childSelector, ObservationCounter counter, OperatorCountMetric metric)
        : WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem>(childSelector)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
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
    public static CountingSelector<TCandidate> Create<TCandidate>(ISelector<TCandidate> childSelector, ObservationCounter counter, OperatorCountMetric metric) =>
        new(childSelector, counter, metric);
}

public static class SelectorCounterExtensions
{
    extension<TCandidate>(ISelector<TCandidate> selector)
    {
        public CountingSelector<TCandidate> CountCalls(ObservationCounter counter) => new(selector, counter, OperatorCountMetric.Calls);

        public CountingSelector<TCandidate> CountCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return selector.CountCalls(counter);
        }

        public CountingSelector<TCandidate> CountCandidates(ObservationCounter counter) => new(selector, counter, OperatorCountMetric.Candidates);

        public CountingSelector<TCandidate> CountCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return selector.CountCandidates(counter);
        }
    }
}
