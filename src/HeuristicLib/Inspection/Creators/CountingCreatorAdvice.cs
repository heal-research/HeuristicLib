using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

public sealed class CountingCreatorAdvice<TCandidate> : CreatorAdvice<TCandidate>
{
    public CountAccumulator Counter { get; }
    public OperatorCountMetric Metric { get; }

    public CountingCreatorAdvice(CountAccumulator counter, OperatorCountMetric metric)
    {
        Counter = counter;
        Metric = metric;
    }

    public override WrapperExecutionFactory<ICreatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(ICreator<TCandidate> creator) =>
        childCreator => new Execution<TRunSearchSpace, TRunProblem>(childCreator, Counter, Metric);

    private sealed class Execution<TSearchSpace, TProblem>(ICreatorExecution<TCandidate, TSearchSpace, TProblem> childCreator, CountAccumulator counter, OperatorCountMetric metric)
        : WrappingCreatorExecution<TCandidate, TSearchSpace, TProblem>(childCreator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var candidates = ChildCreator.Create(count, random, searchSpace, problem);
            counter.IncrementBy(metric == OperatorCountMetric.Calls ? 1 : candidates.Count);
            return candidates;
        }
    }
}

public sealed record CreatorCounting<TCandidate>(OperatorCountMetric Metric) : IOperatorCounting<ICreator<TCandidate>>
{
    public Advice<ICreator<TCandidate>> CreateAdvice(CountAccumulator counter) =>
        new CountingCreatorAdvice<TCandidate>(counter, Metric);
}

public static class CreatorCounterExtensions
{
    extension(Aspect)
    {
        public static Aspect CountCalls<TCandidate>(ICreator<TCandidate> creator, CountAccumulator counter) =>
            Aspect.Create(Pointcut.Reference(creator), new CountingCreatorAdvice<TCandidate>(counter, OperatorCountMetric.Calls));

        public static Aspect CountCandidates<TCandidate>(ICreator<TCandidate> creator, CountAccumulator counter) =>
            Aspect.Create(Pointcut.Reference(creator), new CountingCreatorAdvice<TCandidate>(counter, OperatorCountMetric.Candidates));

        public static Aspect CountCandidates<TCandidate>(ICreator<TCandidate> creator, out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return Aspect.CountCandidates(creator, counter);
        }
    }
}
