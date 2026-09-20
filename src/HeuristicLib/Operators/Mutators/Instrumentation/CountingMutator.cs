using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public sealed record CountingMutator<TCandidate> : WrappingMutator<TCandidate>
{
    public ObservationCounter Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingMutator(IMutator<TCandidate> childMutator, ObservationCounter counter, OperatorCountMetric metric)
        : base(childMutator)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childMutator) =>
        new Instance<TRunSearchSpace, TRunProblem>(childMutator, Counter, Metric);

    private sealed class Instance<TSearchSpace, TProblem>(IMutatorInstance<TCandidate, TSearchSpace, TProblem> childMutator, ObservationCounter counter, OperatorCountMetric metric)
        : WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem>(childMutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
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
    public static CountingMutator<TCandidate> Create<TCandidate>(IMutator<TCandidate> childMutator, ObservationCounter counter, OperatorCountMetric metric) =>
        new(childMutator, counter, metric);
}

public static class MutatorCounterExtensions
{
    extension<TCandidate>(IMutator<TCandidate> mutator)
    {
        public CountingMutator<TCandidate> CountCalls(ObservationCounter counter) => new(mutator, counter, OperatorCountMetric.Calls);

        public CountingMutator<TCandidate> CountCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return mutator.CountCalls(counter);
        }

        public CountingMutator<TCandidate> CountCandidates(ObservationCounter counter) => new(mutator, counter, OperatorCountMetric.Candidates);

        public CountingMutator<TCandidate> CountCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return mutator.CountCandidates(counter);
        }
    }
}
