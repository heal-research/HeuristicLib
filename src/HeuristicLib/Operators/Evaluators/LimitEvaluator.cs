using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public record LimitEvaluator<TCandidate, TSearchSpace, TProblem>
    : WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private readonly int maxEvaluations;
    private readonly ObjectiveVector? alternativeValue;
    private readonly bool strict;

    /// <param name="strict">Whether the limit must also be respected within a single batch. When false, a batch that starts below the limit is evaluated completely.</param>
    public LimitEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, int maxEvaluations, ObjectiveVector? alternativeValue, bool strict = false)
      : base(evaluator)
    {
        this.maxEvaluations = maxEvaluations;
        this.alternativeValue = alternativeValue;
        this.strict = strict;
    }

    protected override WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateEvaluatorInstance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator) =>
        new Instance(innerEvaluator, maxEvaluations, alternativeValue, strict);

    private sealed class Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator, int maxEvaluations, ObjectiveVector? alternativeValue, bool strict)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(innerEvaluator)
    {
        private readonly ObservationCounter counter = new();

        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var remainingEvaluations = maxEvaluations - counter.CurrentCount;
            var alternative = alternativeValue ?? problem.Objective.Worst;

            if (remainingEvaluations <= 0)
            {
                return Enumerable.Repeat(alternative, candidates.Count).ToArray();
            }

            if (strict && remainingEvaluations < candidates.Count)
            {
                var candidatesToEvaluate = candidates.Take(remainingEvaluations).ToList();
                var candidatesToSkip = candidates.Skip(remainingEvaluations).ToList();
                var evaluated = InnerEvaluator.Evaluate(candidatesToEvaluate, random, searchSpace, problem);
                counter.IncrementBy(candidatesToEvaluate.Count);
                var skipped = Enumerable.Repeat(alternative, candidatesToSkip.Count);

                return evaluated.Concat(skipped).ToArray();
            }

            var result = InnerEvaluator.Evaluate(candidates, random, searchSpace, problem);
            counter.IncrementBy(candidates.Count);
            return result;
        }
    }
}

public static class LimitEvaluatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public LimitEvaluator<TCandidate, TSearchSpace, TProblem> LimitEvaluations(int maxEvaluations, ObjectiveVector? alternativeValue = null, bool strict = false)
        {
            return new(evaluator, maxEvaluations, alternativeValue, strict);
        }
    }
}
