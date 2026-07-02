using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public static class ReplacerCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate, TSearchSpace, TProblem> replacer)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReplacer<TCandidate, TSearchSpace, TProblem> CountReplacerCalls(ObservationCounter counter)
            => replacer.ObserveWith(_ => counter.IncrementBy(1));

        public IReplacer<TCandidate, TSearchSpace, TProblem> CountReplacerCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return replacer.CountReplacerCalls(counter);
        }

        public IReplacer<TCandidate, TSearchSpace, TProblem> CountReplacementSolutions(ObservationCounter counter)
            => replacer.ObserveWith(newPopulation => counter.IncrementBy(newPopulation.Count));

        public IReplacer<TCandidate, TSearchSpace, TProblem> CountReplacementSolutions(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return replacer.CountReplacementSolutions(counter);
        }
    }
}
