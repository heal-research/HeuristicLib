using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public sealed record CountingEvaluator<TCandidate, TSearchSpace, TProblem> : ObservableEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ObservationCounter Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, ObservationCounter counter, OperatorCountMetric metric)
        : base(evaluator, new ActionEvaluatorObserver<TCandidate, TSearchSpace, TProblem>(
            (candidates, _, _, _) => counter.IncrementBy(metric == OperatorCountMetric.Calls ? 1 : candidates.Count)))
    {
        Counter = counter;
        Metric = metric;
    }
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
