using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public sealed record CountingReplacer<TCandidate, TSearchSpace, TProblem>
    : WrappingReplacer<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ObservationCounter Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingReplacer(IReplacer<TCandidate, TSearchSpace, TProblem> childReplacer, ObservationCounter counter, OperatorCountMetric metric)
        : base(childReplacer)
    {
        Counter = counter;
        Metric = metric;
    }

    protected override WrappingReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IReplacerInstance<TCandidate, TSearchSpace, TProblem> childReplacer) =>
        new Instance(childReplacer, Counter, Metric);

    private sealed class Instance(IReplacerInstance<TCandidate, TSearchSpace, TProblem> childReplacer, ObservationCounter counter, OperatorCountMetric metric)
        : WrappingReplacerInstance<TCandidate, TSearchSpace, TProblem>(childReplacer)
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
    public static CountingReplacer<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate, TSearchSpace, TProblem> childReplacer, ObservationCounter counter, OperatorCountMetric metric)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childReplacer, counter, metric);
}

public static class ReplacerCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate, TSearchSpace, TProblem> replacer)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public CountingReplacer<TCandidate, TSearchSpace, TProblem> CountReplacerCalls(ObservationCounter counter) => new(replacer, counter, OperatorCountMetric.Calls);

        public CountingReplacer<TCandidate, TSearchSpace, TProblem> CountReplacerCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return replacer.CountReplacerCalls(counter);
        }

        public CountingReplacer<TCandidate, TSearchSpace, TProblem> CountReplacementCandidates(ObservationCounter counter) =>
            new(replacer, counter, OperatorCountMetric.Candidates);

        public CountingReplacer<TCandidate, TSearchSpace, TProblem> CountReplacementCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return replacer.CountReplacementCandidates(counter);
        }
    }
}
