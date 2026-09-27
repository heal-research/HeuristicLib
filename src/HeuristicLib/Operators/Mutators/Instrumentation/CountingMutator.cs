using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public sealed record CountingMutator<TCandidate> : WrappingMutator<TCandidate>
{
    public CountAccumulator Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingMutator(IMutator<TCandidate> childMutator, CountAccumulator counter, OperatorCountMetric metric)
        : base(childMutator)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override IMutatorExecution<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IMutatorExecution<TCandidate, TRunSearchSpace, TRunProblem> childMutator) =>
        new Execution<TRunSearchSpace, TRunProblem>(childMutator, Counter, Metric);

    private sealed class Execution<TSearchSpace, TProblem>(IMutatorExecution<TCandidate, TSearchSpace, TProblem> childMutator, CountAccumulator counter, OperatorCountMetric metric)
        : WrappingMutatorExecution<TCandidate, TSearchSpace, TProblem>(childMutator)
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
    public static CountingMutator<TCandidate> Create<TCandidate>(IMutator<TCandidate> childMutator, CountAccumulator counter, OperatorCountMetric metric) =>
        new(childMutator, counter, metric);
}

public static class MutatorCounterExtensions
{
    extension<TCandidate>(IMutator<TCandidate> mutator)
    {
        public CountingMutator<TCandidate> CountCalls(CountAccumulator counter) => new(mutator, counter, OperatorCountMetric.Calls);

        public CountingMutator<TCandidate> CountCalls(out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return mutator.CountCalls(counter);
        }

        public CountingMutator<TCandidate> CountCandidates(CountAccumulator counter) => new(mutator, counter, OperatorCountMetric.Candidates);

        public CountingMutator<TCandidate> CountCandidates(out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return mutator.CountCandidates(counter);
        }
    }
}
