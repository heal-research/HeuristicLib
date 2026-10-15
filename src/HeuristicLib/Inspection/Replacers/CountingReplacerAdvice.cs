using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

public sealed class CountingReplacerAdvice<TCandidate> : ReplacerAdvice<TCandidate>
{
    public CountAccumulator Counter { get; }
    public OperatorCountMetric Metric { get; }

    public CountingReplacerAdvice(CountAccumulator counter, OperatorCountMetric metric)
    {
        Counter = counter;
        Metric = metric;
    }

    public override WrapperExecutionFactory<IReplacerExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(IReplacer<TCandidate> replacer) =>
        childReplacer => new Execution<TRunSearchSpace, TRunProblem>(childReplacer, Counter, Metric);

    private sealed class Execution<TSearchSpace, TProblem>(IReplacerExecution<TCandidate, TSearchSpace, TProblem> childReplacer, CountAccumulator counter, OperatorCountMetric metric)
        : WrappingReplacerExecution<TCandidate, TSearchSpace, TProblem>(childReplacer)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var replacements = ChildReplacer.Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
            counter.IncrementBy(metric == OperatorCountMetric.Calls ? 1 : replacements.Count);
            return replacements;
        }
    }
}

public sealed record ReplacerCounting<TCandidate>(OperatorCountMetric Metric) : IOperatorCounting<IReplacer<TCandidate>>
{
    public Advice<IReplacer<TCandidate>> CreateAdvice(CountAccumulator counter) =>
        new CountingReplacerAdvice<TCandidate>(counter, Metric);
}

public static class ReplacerCounterExtensions
{
    extension(Aspect)
    {
        public static Aspect CountCalls<TCandidate>(IReplacer<TCandidate> replacer, CountAccumulator counter) =>
            Aspect.Create(Pointcut.Reference(replacer), new CountingReplacerAdvice<TCandidate>(counter, OperatorCountMetric.Calls));

        public static Aspect CountCandidates<TCandidate>(IReplacer<TCandidate> replacer, CountAccumulator counter) =>
            Aspect.Create(Pointcut.Reference(replacer), new CountingReplacerAdvice<TCandidate>(counter, OperatorCountMetric.Candidates));

        public static Aspect CountCandidates<TCandidate>(IReplacer<TCandidate> replacer, out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return Aspect.CountCandidates(replacer, counter);
        }
    }
}
