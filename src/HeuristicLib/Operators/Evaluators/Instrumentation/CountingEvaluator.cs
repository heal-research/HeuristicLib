using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public sealed record CountingEvaluator<TCandidate>
    : WrappingEvaluator<TCandidate>
{
    public ObservationCounter Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingEvaluator(IEvaluator<TCandidate> childEvaluator, ObservationCounter counter, OperatorCountMetric metric)
        : base(childEvaluator)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childEvaluator) =>
        new Instance<TRunSearchSpace, TRunProblem>(childEvaluator, Counter, Metric);

    private sealed class Instance<TSearchSpace, TProblem>(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator, ObservationCounter counter, OperatorCountMetric metric)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(childEvaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
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
    public static CountingEvaluator<TCandidate> Create<TCandidate>(IEvaluator<TCandidate> childEvaluator, ObservationCounter counter, OperatorCountMetric metric) =>
        new(childEvaluator, counter, metric);
}

public static class EvaluatorCounterExtensions
{
    extension<TCandidate>(IEvaluator<TCandidate> evaluator)
    {
        public CountingEvaluator<TCandidate> CountCalls(ObservationCounter counter) => new(evaluator, counter, OperatorCountMetric.Calls);

        public CountingEvaluator<TCandidate> CountCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return evaluator.CountCalls(counter);
        }

        public CountingEvaluator<TCandidate> CountCandidates(ObservationCounter counter) =>
            new(evaluator, counter, OperatorCountMetric.Candidates);

        public CountingEvaluator<TCandidate> CountCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return evaluator.CountCandidates(counter);
        }
    }
}
