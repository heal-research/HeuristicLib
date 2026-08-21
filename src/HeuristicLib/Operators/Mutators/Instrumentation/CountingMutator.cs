using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public sealed record CountingMutator<TCandidate, TSearchSpace, TProblem>
    : WrappingMutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ObservationCounter Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingMutator(IMutator<TCandidate, TSearchSpace, TProblem> childMutator, ObservationCounter counter, OperatorCountMetric metric)
        : base(childMutator)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IMutatorInstance<TCandidate, TSearchSpace, TProblem> childMutator) =>
        new Instance(childMutator, Counter, Metric);

    private sealed class Instance(IMutatorInstance<TCandidate, TSearchSpace, TProblem> childMutator, ObservationCounter counter, OperatorCountMetric metric)
        : WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem>(childMutator)
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var offspring = ChildMutator.Mutate(parents, random, searchSpace, problem);
            counter.IncrementBy(metric == OperatorCountMetric.Calls ? 1 : offspring.Count);
            return offspring;
        }
    }
}

public static class CountingMutator
{
    public static CountingMutator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate, TSearchSpace, TProblem> childMutator, ObservationCounter counter, OperatorCountMetric metric)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childMutator, counter, metric);
}

public static class MutatorCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate, TSearchSpace, TProblem> mutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public CountingMutator<TCandidate, TSearchSpace, TProblem> CountMutatorCalls(ObservationCounter counter) => new(mutator, counter, OperatorCountMetric.Calls);

        public CountingMutator<TCandidate, TSearchSpace, TProblem> CountMutatorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return mutator.CountMutatorCalls(counter);
        }

        public CountingMutator<TCandidate, TSearchSpace, TProblem> CountMutatedCandidates(ObservationCounter counter) => new(mutator, counter, OperatorCountMetric.Candidates);

        public CountingMutator<TCandidate, TSearchSpace, TProblem> CountMutatedCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return mutator.CountMutatedCandidates(counter);
        }
    }
}
