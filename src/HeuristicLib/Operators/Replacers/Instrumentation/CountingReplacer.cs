using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public sealed record CountingReplacer<TCandidate>
    : WrappingReplacer<TCandidate>
{
    public ObservationCounter Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingReplacer(IReplacer<TCandidate> childReplacer, ObservationCounter counter, OperatorCountMetric metric)
        : base(childReplacer)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override IReplacerInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IReplacerInstance<TCandidate, TRunSearchSpace, TRunProblem> childReplacer) =>
        new Instance<TRunSearchSpace, TRunProblem>(childReplacer, Counter, Metric);

    private sealed class Instance<TSearchSpace, TProblem>(IReplacerInstance<TCandidate, TSearchSpace, TProblem> childReplacer, ObservationCounter counter, OperatorCountMetric metric)
        : WrappingReplacerInstance<TCandidate, TSearchSpace, TProblem>(childReplacer)
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
    public static CountingReplacer<TCandidate> Create<TCandidate>(IReplacer<TCandidate> childReplacer, ObservationCounter counter, OperatorCountMetric metric) =>
        new(childReplacer, counter, metric);
}

public static class ReplacerCounterExtensions
{
    extension<TCandidate>(IReplacer<TCandidate> replacer)
    {
        public CountingReplacer<TCandidate> CountCalls(ObservationCounter counter) => new(replacer, counter, OperatorCountMetric.Calls);

        public CountingReplacer<TCandidate> CountCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return replacer.CountCalls(counter);
        }

        public CountingReplacer<TCandidate> CountCandidates(ObservationCounter counter) =>
            new(replacer, counter, OperatorCountMetric.Candidates);

        public CountingReplacer<TCandidate> CountCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return replacer.CountCandidates(counter);
        }
    }
}
