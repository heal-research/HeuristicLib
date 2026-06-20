using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public static class ReplacerCounterExtensions
{
    extension<TG, TS, TP>(IReplacer<TG, TS, TP> replacer)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
    {
        public IReplacer<TG, TS, TP> CountReplacerCalls(ObservationCounter counter)
            => replacer.ObserveWith(_ => counter.IncrementBy(1));

        public IReplacer<TG, TS, TP> CountReplacerCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return replacer.CountReplacerCalls(counter);
        }

        public IReplacer<TG, TS, TP> CountReplacementSolutions(ObservationCounter counter)
            => replacer.ObserveWith(newPopulation => counter.IncrementBy(newPopulation.Count));

        public IReplacer<TG, TS, TP> CountReplacementSolutions(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return replacer.CountReplacementSolutions(counter);
        }
    }
}
