using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

public sealed class CountingMutatorAdvice<TCandidate> : MutatorAdvice<TCandidate>
{
    public CountAccumulator Counter { get; }
    public OperatorCountMetric Metric { get; }

    public CountingMutatorAdvice(CountAccumulator counter, OperatorCountMetric metric)
    {
        Counter = counter;
        Metric = metric;
    }

    public override WrapperExecutionFactory<IMutatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(IMutator<TCandidate> mutator) =>
        childMutator => new Execution<TRunSearchSpace, TRunProblem>(childMutator, Counter, Metric);

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

public sealed record MutatorCounting<TCandidate>(OperatorCountMetric Metric) : IOperatorCounting<IMutator<TCandidate>>
{
    public Advice<IMutator<TCandidate>> CreateAdvice(CountAccumulator counter) =>
        new CountingMutatorAdvice<TCandidate>(counter, Metric);
}

public static class MutatorCounterExtensions
{
    extension(Aspect)
    {
        /// <summary>Counts every call to one chosen mutator.</summary>
        public static Aspect CountCalls<TCandidate>(IMutator<TCandidate> mutator, CountAccumulator counter) =>
            Aspect.Create(Pointcut.Reference(mutator), new CountingMutatorAdvice<TCandidate>(counter, OperatorCountMetric.Calls));

        /// <summary>Counts the offspring one chosen mutator returns.</summary>
        public static Aspect CountCandidates<TCandidate>(IMutator<TCandidate> mutator, CountAccumulator counter) =>
            Aspect.Create(Pointcut.Reference(mutator), new CountingMutatorAdvice<TCandidate>(counter, OperatorCountMetric.Candidates));

        public static Aspect CountCandidates<TCandidate>(IMutator<TCandidate> mutator, out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return Aspect.CountCandidates(mutator, counter);
        }
    }
}
