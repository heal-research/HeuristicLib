using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public static class CreatorCounterExtensions
{
    extension<TG, TS, TP>(ICreator<TG, TS, TP> creator)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
    {
        public ICreator<TG, TS, TP> CountCreatorCalls(ObservationCounter counter)
            => creator.ObserveWith(_ => counter.IncrementBy(1));

        public ICreator<TG, TS, TP> CountCreatorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return creator.CountCreatorCalls(counter);
        }

        public ICreator<TG, TS, TP> CountCreatedGenotypes(ObservationCounter counter)
            => creator.ObserveWith(offspring => counter.IncrementBy(offspring.Count));

        public ICreator<TG, TS, TP> CountCreatedGenotypes(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return creator.CountCreatedGenotypes(counter);
        }
    }
}
