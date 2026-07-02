using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public static class CreatorCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate, TSearchSpace, TProblem> creator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ICreator<TCandidate, TSearchSpace, TProblem> CountCreatorCalls(ObservationCounter counter)
            => creator.ObserveWith(_ => counter.IncrementBy(1));

        public ICreator<TCandidate, TSearchSpace, TProblem> CountCreatorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return creator.CountCreatorCalls(counter);
        }

        public ICreator<TCandidate, TSearchSpace, TProblem> CountCreatedCandidates(ObservationCounter counter)
            => creator.ObserveWith(offspring => counter.IncrementBy(offspring.Count));

        public ICreator<TCandidate, TSearchSpace, TProblem> CountCreatedCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return creator.CountCreatedCandidates(counter);
        }
    }
}
