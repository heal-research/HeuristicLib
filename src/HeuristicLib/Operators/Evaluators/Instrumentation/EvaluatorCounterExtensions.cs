using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public static class EvaluatorCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IEvaluator<TCandidate, TSearchSpace, TProblem> CountEvaluatorCalls(ObservationCounter counter)
            => evaluator.ObserveWith((_, _) => counter.IncrementBy(1));

        public IEvaluator<TCandidate, TSearchSpace, TProblem> CountEvaluatorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return evaluator.CountEvaluatorCalls(counter);
        }

        public IEvaluator<TCandidate, TSearchSpace, TProblem> CountEvaluatedGenotypes(ObservationCounter counter)
            => evaluator.ObserveWith((candidates, _) => counter.IncrementBy(candidates.Count));

        public IEvaluator<TCandidate, TSearchSpace, TProblem> CountEvaluatedGenotypes(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return evaluator.CountEvaluatedGenotypes(counter);
        }
    }
}
