using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public enum RelativeQualityZeroBestKnownPolicy
{
    SignedInfinity,
    Difference,
    NaN
}

/// <summary>
/// Normalizes the objective vectors produced by the child evaluator against a fixed best-known objective vector.
/// </summary>
public sealed record RelativeQualityEvaluator<TCandidate>
    : WrappingEvaluator<TCandidate>
{
    /// <summary>
    /// Gets the best-known objective vector used as the normalization reference.
    /// </summary>
    public ObjectiveVector BestKnown { get; init; }

    /// <summary>
    /// Gets the policy applied when a best-known objective value is zero.
    /// </summary>
    public RelativeQualityZeroBestKnownPolicy ZeroBestKnownPolicy { get; init; } = RelativeQualityZeroBestKnownPolicy.SignedInfinity;

    public RelativeQualityEvaluator(IEvaluator<TCandidate> childEvaluator, ObjectiveVector bestKnown)
        : base(childEvaluator)
    {
        BestKnown = bestKnown;
    }

    protected override IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childEvaluator) =>
        new Instance<TRunSearchSpace, TRunProblem>(childEvaluator, BestKnown, ZeroBestKnownPolicy);

    private sealed class Instance<TSearchSpace, TProblem>(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator, ObjectiveVector bestKnown, RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(childEvaluator)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            ChildEvaluator.Evaluate(candidates, random, searchSpace, problem)
                .Select(objectiveVector => RelativeQuality.Normalize(objectiveVector, bestKnown, zeroBestKnownPolicy))
                .ToArray();
    }
}

public static class RelativeQualityEvaluator
{
    public static RelativeQualityEvaluator<TCandidate> Create<TCandidate>(IEvaluator<TCandidate> childEvaluator, ObjectiveVector bestKnown) =>
        new(childEvaluator, bestKnown);
}

public static class RelativeQuality
{
    private const double ZeroTolerance = 1e-12;

    public static ObjectiveVector Normalize(ObjectiveVector objective, ObjectiveVector bestKnown, RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy = RelativeQualityZeroBestKnownPolicy.SignedInfinity)
    {
        if (objective.Count != bestKnown.Count)
            throw new ArgumentException("Objective vector and best-known vector must have the same length.", nameof(bestKnown));

        var relativeValues = new double[objective.Count];
        for (var i = 0; i < relativeValues.Length; i++)
        {
            relativeValues[i] = Normalize(objective[i], bestKnown[i], zeroBestKnownPolicy);
        }

        return new ObjectiveVector(relativeValues);
    }

    private static double Normalize(double observed, double bestKnown, RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy)
    {
        if (Math.Abs(bestKnown) > ZeroTolerance)
            return (observed - bestKnown) / Math.Abs(bestKnown);

        if (Math.Abs(observed) <= ZeroTolerance)
            return 0.0;

        return zeroBestKnownPolicy switch
        {
            RelativeQualityZeroBestKnownPolicy.SignedInfinity => Math.CopySign(double.PositiveInfinity, observed),
            RelativeQualityZeroBestKnownPolicy.Difference => observed,
            RelativeQualityZeroBestKnownPolicy.NaN => double.NaN,
            _ => throw new InvalidOperationException($"Unsupported zero best-known policy: {zeroBestKnownPolicy}.")
        };
    }
}

public static class RelativeQualityEvaluatorExtensions
{
    extension<TCandidate>(IEvaluator<TCandidate> evaluator)
    {
        public RelativeQualityEvaluator<TCandidate> WithRelativeQuality(ObjectiveVector bestKnown) =>
            new(evaluator, bestKnown);

        public RelativeQualityEvaluator<TCandidate> WithRelativeQuality(ObjectiveVector bestKnown, RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy) =>
            new(evaluator, bestKnown) { ZeroBestKnownPolicy = zeroBestKnownPolicy };
    }
}
