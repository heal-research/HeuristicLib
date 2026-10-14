using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Instrumentation;

public sealed class CountingCrossoverAdvice<TCandidate> : CrossoverAdvice<TCandidate>
{
    public CountAccumulator Counter { get; }
    public OperatorCountMetric Metric { get; }

    public CountingCrossoverAdvice(CountAccumulator counter, OperatorCountMetric metric)
    {
        Counter = counter;
        Metric = metric;
    }

    public override WrapperExecutionFactory<ICrossoverExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(ICrossover<TCandidate> crossover) =>
        childCrossover => new Execution<TRunSearchSpace, TRunProblem>(childCrossover, Counter, Metric);

    private sealed class Execution<TSearchSpace, TProblem>(ICrossoverExecution<TCandidate, TSearchSpace, TProblem> childCrossover, CountAccumulator counter, OperatorCountMetric metric)
        : WrappingCrossoverExecution<TCandidate, TSearchSpace, TProblem>(childCrossover)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var offspring = ChildCrossover.Cross(parents, random, searchSpace, problem);
            counter.IncrementBy(metric == OperatorCountMetric.Calls ? 1 : offspring.Count);
            return offspring;
        }
    }
}

public sealed record CrossoverCounting<TCandidate>(OperatorCountMetric Metric) : IOperatorCounting<ICrossover<TCandidate>>
{
    public Advice<ICrossover<TCandidate>> CreateAdvice(CountAccumulator counter) =>
        new CountingCrossoverAdvice<TCandidate>(counter, Metric);
}

public static class CrossoverCounterExtensions
{
    extension(Aspect)
    {
        public static Aspect CountCalls<TCandidate>(ICrossover<TCandidate> crossover, CountAccumulator counter) =>
            Aspect.Create(Pointcut.Reference(crossover), new CountingCrossoverAdvice<TCandidate>(counter, OperatorCountMetric.Calls));

        public static Aspect CountCandidates<TCandidate>(ICrossover<TCandidate> crossover, CountAccumulator counter) =>
            Aspect.Create(Pointcut.Reference(crossover), new CountingCrossoverAdvice<TCandidate>(counter, OperatorCountMetric.Candidates));

        public static Aspect CountCandidates<TCandidate>(ICrossover<TCandidate> crossover, out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return Aspect.CountCandidates(crossover, counter);
        }
    }
}
