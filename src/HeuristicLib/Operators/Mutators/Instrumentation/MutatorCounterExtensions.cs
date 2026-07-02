using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public static class MutatorCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate, TSearchSpace, TProblem> mutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IMutator<TCandidate, TSearchSpace, TProblem> CountMutatorCalls(ObservationCounter counter)
            => mutator.ObserveWith(_ => counter.IncrementBy(1));

        public IMutator<TCandidate, TSearchSpace, TProblem> CountMutatorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return mutator.CountMutatorCalls(counter);
        }

        public IMutator<TCandidate, TSearchSpace, TProblem> CountMutatedCandidates(ObservationCounter counter)
            => mutator.ObserveWith(offspring => counter.IncrementBy(offspring.Count));

        public IMutator<TCandidate, TSearchSpace, TProblem> CountMutatedCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return mutator.CountMutatedCandidates(counter);
        }
    }
}
