using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public static class CrossoverCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> crossover)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ICrossover<TCandidate, TSearchSpace, TProblem> CountCrossoverCalls(ObservationCounter counter)
            => crossover.ObserveWith(_ => counter.IncrementBy(1));

        public ICrossover<TCandidate, TSearchSpace, TProblem> CountCrossoverCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return crossover.CountCrossoverCalls(counter);
        }

        public ICrossover<TCandidate, TSearchSpace, TProblem> CountCrossedGenotypes(ObservationCounter counter)
            => crossover.ObserveWith(offspring => counter.IncrementBy(offspring.Count));

        public ICrossover<TCandidate, TSearchSpace, TProblem> CountCrossedGenotypes(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return crossover.CountCrossedGenotypes(counter);
        }
    }
}
