using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public record LimitEvaluator<TG, TS, TP>
  : WrappingEvaluator<TG, TS, TP, LimitEvaluator<TG, TS, TP>.ExecutionState>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
    public sealed class ExecutionState
    {
        public ObservationCounter Counter { get; } = new();
    }

    private readonly int maxEvaluations;
    private readonly ObjectiveVector? alternativeValue;
    private readonly bool strict;

    // ToDo: document that strict means in-batch checking
    public LimitEvaluator(IEvaluator<TG, TS, TP> evaluator, int maxEvaluations, ObjectiveVector? alternativeValue, bool strict = false)
      : base(evaluator)
    {
        this.maxEvaluations = maxEvaluations;
        this.alternativeValue = alternativeValue;
        this.strict = strict;
    }

    protected override ExecutionState CreateInitialState() => new();

    protected override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TG> candidates, ExecutionState executionState,
        InnerEvaluate innerEvaluate, IRandomNumberGenerator random, TS searchSpace, TP problem)
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
    extension<TG, TS, TP>(IEvaluator<TG, TS, TP> evaluator) where TS : class, ISearchSpace<TG> where TP : class, IProblem<TG, TS>
    {
        public LimitEvaluator<TG, TS, TP> LimitEvaluations(int maxEvaluations, ObjectiveVector? alternativeValue = null, bool strict = false)
        {
            return new LimitEvaluator<TG, TS, TP>(evaluator, maxEvaluations, alternativeValue, strict);
        }
    }
}
