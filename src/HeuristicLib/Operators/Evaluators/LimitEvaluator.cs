using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public record LimitEvaluator<TCandidate, TSearchSpace, TProblem>
  : WrappingEvaluator<TCandidate, TSearchSpace, TProblem, LimitEvaluator<TCandidate, TSearchSpace, TProblem>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public sealed class ExecutionState
    {
        public ObservationCounter Counter { get; } = new();
    }

    private readonly int maxEvaluations;
    private readonly ObjectiveVector? alternativeValue;
    private readonly bool strict;

    // ToDo: document that strict means in-batch checking
    public LimitEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, int maxEvaluations, ObjectiveVector? alternativeValue, bool strict = false)
      : base(evaluator)
    {
        this.maxEvaluations = maxEvaluations;
        this.alternativeValue = alternativeValue;
        this.strict = strict;
    }

    protected override ExecutionState CreateInitialState() => new();

    protected override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, ExecutionState executionState,
        InnerEvaluate innerEvaluate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
        var remainingEvaluations = maxEvaluations - executionState.Counter.CurrentCount;

        var alternative = alternativeValue ?? problem.Objective.Worst;

        if (remainingEvaluations <= 0)
        {
            return Enumerable.Repeat(alternative, candidates.Count).ToArray();
        }

        if (strict && remainingEvaluations < candidates.Count)
        {
            var candidatesToEvaluate = candidates.Take(remainingEvaluations).ToList();
            var candidatesToSkip = candidates.Skip(remainingEvaluations).ToList();

            var evaluated = innerEvaluate(candidatesToEvaluate, random, searchSpace, problem);
            executionState.Counter.IncrementBy(candidatesToEvaluate.Count);
            var skipped = Enumerable.Repeat(alternative, candidatesToSkip.Count);

            return evaluated.Concat(skipped).ToArray();
        }

        var result = innerEvaluate(candidates, random, searchSpace, problem);
        executionState.Counter.IncrementBy(candidates.Count);
        return result;
    }
}

public static class LimitEvaluatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator) where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public LimitEvaluator<TCandidate, TSearchSpace, TProblem> LimitEvaluations(int maxEvaluations, ObjectiveVector? alternativeValue = null, bool strict = false)
        {
            return new LimitEvaluator<TCandidate, TSearchSpace, TProblem>(evaluator, maxEvaluations, alternativeValue, strict);
        }
    }
}
