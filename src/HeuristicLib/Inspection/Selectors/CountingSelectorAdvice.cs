using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

public sealed class CountingSelectorAdvice<TCandidate> : SelectorAdvice<TCandidate>
{
    public CountAccumulator Counter { get; }
    public OperatorCountMetric Metric { get; }

    public CountingSelectorAdvice(CountAccumulator counter, OperatorCountMetric metric)
    {
        Counter = counter;
        Metric = metric;
    }

    public override WrapperExecutionFactory<ISelectorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(ISelector<TCandidate> selector) =>
        childSelector => new Execution<TRunSearchSpace, TRunProblem>(childSelector, Counter, Metric);

    private sealed class Execution<TSearchSpace, TProblem>(ISelectorExecution<TCandidate, TSearchSpace, TProblem> childSelector, CountAccumulator counter, OperatorCountMetric metric)
        : WrappingSelectorExecution<TCandidate, TSearchSpace, TProblem>(childSelector)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var selected = ChildSelector.Select(population, objective, count, random, searchSpace, problem);
            counter.IncrementBy(metric == OperatorCountMetric.Calls ? 1 : selected.Count);
            return selected;
        }
    }
}

public sealed record SelectorCounting<TCandidate>(OperatorCountMetric Metric) : IOperatorCounting<ISelector<TCandidate>>
{
    public Advice<ISelector<TCandidate>> CreateAdvice(CountAccumulator counter) =>
        new CountingSelectorAdvice<TCandidate>(counter, Metric);
}

public static class SelectorCounterExtensions
{
    extension(Aspect)
    {
        public static Aspect CountCalls<TCandidate>(ISelector<TCandidate> selector, CountAccumulator counter) =>
            Aspect.Create(Pointcut.Reference(selector), new CountingSelectorAdvice<TCandidate>(counter, OperatorCountMetric.Calls));

        public static Aspect CountCandidates<TCandidate>(ISelector<TCandidate> selector, CountAccumulator counter) =>
            Aspect.Create(Pointcut.Reference(selector), new CountingSelectorAdvice<TCandidate>(counter, OperatorCountMetric.Candidates));

        public static Aspect CountCandidates<TCandidate>(ISelector<TCandidate> selector, out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return Aspect.CountCandidates(selector, counter);
        }
    }
}
