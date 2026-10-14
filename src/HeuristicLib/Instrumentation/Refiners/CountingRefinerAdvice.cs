using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Instrumentation;

public sealed class CountingRefinerAdvice<TCandidate> : RefinerAdvice<TCandidate>
{
    public CountAccumulator Counter { get; }
    public OperatorCountMetric Metric { get; }

    public CountingRefinerAdvice(CountAccumulator counter, OperatorCountMetric metric)
    {
        Counter = counter;
        Metric = metric;
    }

    public override WrapperExecutionFactory<IRefinerExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(IRefiner<TCandidate> refiner) =>
        childRefiner => new Execution<TRunSearchSpace, TRunProblem>(childRefiner, Counter, Metric);

    private sealed class Execution<TSearchSpace, TProblem>(IRefinerExecution<TCandidate, TSearchSpace, TProblem> childRefiner, CountAccumulator counter, OperatorCountMetric metric)
        : WrappingRefinerExecution<TCandidate, TSearchSpace, TProblem>(childRefiner)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var refined = ChildRefiner.Refine(candidates, random, searchSpace, problem);
            counter.IncrementBy(metric == OperatorCountMetric.Calls ? 1 : refined.Count);
            return refined;
        }
    }
}

public sealed record RefinerCounting<TCandidate>(OperatorCountMetric Metric) : IOperatorCounting<IRefiner<TCandidate>>
{
    public Advice<IRefiner<TCandidate>> CreateAdvice(CountAccumulator counter) =>
        new CountingRefinerAdvice<TCandidate>(counter, Metric);
}

public static class RefinerCounterExtensions
{
    extension(Aspect)
    {
        public static Aspect CountCalls<TCandidate>(IRefiner<TCandidate> refiner, CountAccumulator counter) =>
            Aspect.Create(Pointcut.Reference(refiner), new CountingRefinerAdvice<TCandidate>(counter, OperatorCountMetric.Calls));

        public static Aspect CountCandidates<TCandidate>(IRefiner<TCandidate> refiner, CountAccumulator counter) =>
            Aspect.Create(Pointcut.Reference(refiner), new CountingRefinerAdvice<TCandidate>(counter, OperatorCountMetric.Candidates));

        public static Aspect CountCandidates<TCandidate>(IRefiner<TCandidate> refiner, out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return Aspect.CountCandidates(refiner, counter);
        }
    }
}
