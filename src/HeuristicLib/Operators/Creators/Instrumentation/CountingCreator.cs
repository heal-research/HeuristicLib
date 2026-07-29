using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public sealed record CountingCreator<TCandidate, TSearchSpace, TProblem> : ObservableCreator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ObservationCounter Counter { get; }
    public OperatorCountMetric Metric { get; }

    public CountingCreator(ICreator<TCandidate, TSearchSpace, TProblem> creator, ObservationCounter counter, OperatorCountMetric metric)
        : base(creator, new ActionCreatorObserver<TCandidate, TSearchSpace, TProblem>(
            (offspring, _, _, _) => counter.IncrementBy(metric == OperatorCountMetric.Calls ? 1 : offspring.Count)))
    {
        Counter = counter;
        Metric = metric;
    }
}

public static class CreatorCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate, TSearchSpace, TProblem> creator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public CountingCreator<TCandidate, TSearchSpace, TProblem> CountCreatorCalls(ObservationCounter counter) => new(creator, counter, OperatorCountMetric.Calls);

        public CountingCreator<TCandidate, TSearchSpace, TProblem> CountCreatorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return creator.CountCreatorCalls(counter);
        }

        public CountingCreator<TCandidate, TSearchSpace, TProblem> CountCreatedCandidates(ObservationCounter counter) => new(creator, counter, OperatorCountMetric.Candidates);

        public CountingCreator<TCandidate, TSearchSpace, TProblem> CountCreatedCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return creator.CountCreatedCandidates(counter);
        }
    }
}
