using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public static class EvaluatorCounterExtensions
{
    extension<TG, TS, TP>(IEvaluator<TG, TS, TP> evaluator)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
    {
        public IEvaluator<TG, TS, TP> CountEvaluatorCalls(ObservationCounter counter)
            => evaluator.ObserveWith((_, _) => counter.IncrementBy(1));

        public IEvaluator<TG, TS, TP> CountEvaluatorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return evaluator.CountEvaluatorCalls(counter);
        }

        public IEvaluator<TG, TS, TP> CountEvaluatedGenotypes(ObservationCounter counter)
            => evaluator.ObserveWith((genotypes, _) => counter.IncrementBy(genotypes.Count));

        public IEvaluator<TG, TS, TP> CountEvaluatedGenotypes(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return evaluator.CountEvaluatedGenotypes(counter);
        }
    }
}
