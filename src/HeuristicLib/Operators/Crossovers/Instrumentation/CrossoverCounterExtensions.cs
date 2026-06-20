using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public static class CrossoverCounterExtensions
{
    extension<TG, TS, TP>(ICrossover<TG, TS, TP> crossover)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
    {
        public ICrossover<TG, TS, TP> CountCrossoverCalls(ObservationCounter counter)
            => crossover.ObserveWith(_ => counter.IncrementBy(1));

        public ICrossover<TG, TS, TP> CountCrossoverCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return crossover.CountCrossoverCalls(counter);
        }

        public ICrossover<TG, TS, TP> CountCrossedGenotypes(ObservationCounter counter)
            => crossover.ObserveWith(offspring => counter.IncrementBy(offspring.Count));

        public ICrossover<TG, TS, TP> CountCrossedGenotypes(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return crossover.CountCrossedGenotypes(counter);
        }
    }
}
