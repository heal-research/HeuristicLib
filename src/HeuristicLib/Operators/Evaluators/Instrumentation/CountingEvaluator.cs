using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public sealed record CountingEvaluator<TCandidate, TSearchSpace, TProblem>
    : WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ObservationCounter Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> childEvaluator, ObservationCounter counter, OperatorCountMetric metric)
        : base(childEvaluator)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator) =>
        new Instance(childEvaluator, Counter, Metric);

    private sealed class Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator, ObservationCounter counter, OperatorCountMetric metric)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(childEvaluator)
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var objectives = ChildEvaluator.Evaluate(candidates, random, searchSpace, problem);
            counter.IncrementBy(metric == OperatorCountMetric.Calls ? 1 : candidates.Count);
            return objectives;
        }
    }
}

public static class CountingEvaluator
{
    public static CountingEvaluator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> childEvaluator, ObservationCounter counter, OperatorCountMetric metric)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childEvaluator, counter, metric);
}

public static class EvaluatorCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public CountingEvaluator<TCandidate, TSearchSpace, TProblem> CountEvaluatorCalls(ObservationCounter counter) => new(evaluator, counter, OperatorCountMetric.Calls);

        public CountingEvaluator<TCandidate, TSearchSpace, TProblem> CountEvaluatorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return evaluator.CountEvaluatorCalls(counter);
        }

        public CountingEvaluator<TCandidate, TSearchSpace, TProblem> CountEvaluatedCandidates(ObservationCounter counter) =>
            new(evaluator, counter, OperatorCountMetric.Candidates);

        public CountingEvaluator<TCandidate, TSearchSpace, TProblem> CountEvaluatedCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return evaluator.CountEvaluatedCandidates(counter);
        }
    }
}
