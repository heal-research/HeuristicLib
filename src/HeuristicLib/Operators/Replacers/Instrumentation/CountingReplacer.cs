using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public sealed record CountingReplacer<TCandidate>
    : WrappingReplacer<TCandidate>
{
    public CountAccumulator Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingReplacer(IReplacer<TCandidate> childReplacer, CountAccumulator counter, OperatorCountMetric metric)
        : base(childReplacer)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override WrapperExecutionFactory<IReplacerExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>() =>
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

public static class CountingReplacer
{
    public static CountingReplacer<TCandidate> Create<TCandidate>(IReplacer<TCandidate> childReplacer, CountAccumulator counter, OperatorCountMetric metric) =>
        new(childReplacer, counter, metric);
}

public static class ReplacerCounterExtensions
{
    extension<TCandidate>(IReplacer<TCandidate> replacer)
    {
        public CountingReplacer<TCandidate> CountCalls(CountAccumulator counter) => new(replacer, counter, OperatorCountMetric.Calls);

        public CountingReplacer<TCandidate> CountCalls(out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return replacer.CountCalls(counter);
        }

        public CountingReplacer<TCandidate> CountCandidates(CountAccumulator counter) =>
            new(replacer, counter, OperatorCountMetric.Candidates);

        public CountingReplacer<TCandidate> CountCandidates(out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return replacer.CountCandidates(counter);
        }
    }
}
