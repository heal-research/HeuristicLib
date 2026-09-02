using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record LimitEvaluator<TCandidate>
    : WrappingEvaluator<TCandidate>
{
    public int MaxEvaluations { get; init; }

    /// <summary>
    /// Gets the objective vector returned for candidates that are not evaluated because the limit has been reached.
    /// A <see langword="null"/> value uses <see cref="ObjectiveDirections.Worst"/> from the problem.
    /// </summary>
    public ObjectiveVector? FallbackObjectiveVector { get; init; }

    /// <summary>
    /// Gets whether <see cref="MaxEvaluations"/> is enforced within a batch that would cross the limit.
    /// When <see langword="false"/>, a batch that starts below the limit is evaluated completely.
    /// </summary>
    public bool EnforceLimitWithinBatch { get; init; }

    public LimitEvaluator(IEvaluator<TCandidate> childEvaluator, int maxEvaluations)
        : base(childEvaluator)
    {
        MaxEvaluations = maxEvaluations;
    }

    protected override IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childEvaluator) =>
        new Instance<TRunSearchSpace, TRunProblem>(childEvaluator, MaxEvaluations, FallbackObjectiveVector, EnforceLimitWithinBatch);

    private sealed class Instance<TSearchSpace, TProblem>(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator, int maxEvaluations, ObjectiveVector? fallbackObjectiveVector, bool enforceLimitWithinBatch)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(childEvaluator)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly ObservationCounter counter = new();

        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var remainingEvaluations = maxEvaluations - counter.CurrentCount;
            var fallback = fallbackObjectiveVector ?? problem.Objective.Worst;

            if (remainingEvaluations <= 0)
            {
                return Enumerable.Repeat(fallback, candidates.Count).ToArray();
            }

            if (enforceLimitWithinBatch && remainingEvaluations < candidates.Count)
            {
                var candidatesToEvaluate = candidates.Take(remainingEvaluations).ToList();
                var candidatesToSkip = candidates.Skip(remainingEvaluations).ToList();
                var evaluated = ChildEvaluator.Evaluate(candidatesToEvaluate, random, searchSpace, problem);
                counter.IncrementBy(candidatesToEvaluate.Count);
                var skipped = Enumerable.Repeat(fallback, candidatesToSkip.Count);

                return evaluated.Concat(skipped).ToArray();
            }

            var result = ChildEvaluator.Evaluate(candidates, random, searchSpace, problem);
            counter.IncrementBy(candidates.Count);
            return result;
        }
    }
}

public static class LimitEvaluatorExtensions
{
    extension<TCandidate>(IEvaluator<TCandidate> evaluator)
    {
        public LimitEvaluator<TCandidate> LimitEvaluations(int maxEvaluations, ObjectiveVector? fallbackObjectiveVector = null, bool enforceLimitWithinBatch = false)
        {
            return new(evaluator, maxEvaluations) { FallbackObjectiveVector = fallbackObjectiveVector, EnforceLimitWithinBatch = enforceLimitWithinBatch };
        }
    }
}
