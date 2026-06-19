using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public static class MutatorCounterExtensions
{
    extension<TG, TS, TP>(IMutator<TG, TS, TP> mutator)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
    {
        public IMutator<TG, TS, TP> CountMutatorCalls(ObservationCounter counter)
            => mutator.ObserveWith(_ => counter.IncrementBy(1));

        public IMutator<TG, TS, TP> CountMutatorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return mutator.CountMutatorCalls(counter);
        }

        public IMutator<TG, TS, TP> CountMutatedGenotypes(ObservationCounter counter)
            => mutator.ObserveWith(offspring => counter.IncrementBy(offspring.Count));

        public IMutator<TG, TS, TP> CountMutatedGenotypes(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return mutator.CountMutatedGenotypes(counter);
        }
    }
}
