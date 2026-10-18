using HEAL.HeuristicLib.Execution;
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
    : WrappingOperator<Evaluation, TCandidate>
{
    /// <summary>
    /// Gets the best-known objective vector used as the normalization reference.
    /// </summary>
    public ObjectiveVector BestKnown { get; init; }

    public RelativeQualityZeroBestKnownPolicy ZeroBestKnownPolicy { get; init; } = RelativeQualityZeroBestKnownPolicy.SignedInfinity;

    public RelativeQualityEvaluator(IOperator<Evaluation, TCandidate> childEvaluator, ObjectiveVector bestKnown)
        : base(childEvaluator)
    {
        BestKnown = bestKnown;
    }

    protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(this);

    private sealed class Factory(RelativeQualityEvaluator<TCandidate> source) : OperatorExecutorFactory<Evaluation, TCandidate>
    {
        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
        {
            var childEvaluator = scope.Resolve(source.ChildOperator);
            return new Executor<TRunSearchSpace, TRunProblem>(childEvaluator, source.BestKnown, source.ZeroBestKnownPolicy);
        }
    }

    private sealed class Executor<TSearchSpace, TProblem>(IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> childEvaluator, ObjectiveVector bestKnown, RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy)
        : WrappingEvaluationExecutor<TCandidate, TSearchSpace, TProblem>(childEvaluator)
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
    public static RelativeQualityEvaluator<TCandidate> Create<TCandidate>(IOperator<Evaluation, TCandidate> childEvaluator, ObjectiveVector bestKnown) =>
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
    extension<TCandidate>(IOperator<Evaluation, TCandidate> evaluator)
    {
        public RelativeQualityEvaluator<TCandidate> ScaledToBestKnown(ObjectiveVector bestKnown) =>
            new(evaluator, bestKnown);

        public RelativeQualityEvaluator<TCandidate> ScaledToBestKnown(ObjectiveVector bestKnown, RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy) =>
            new(evaluator, bestKnown) { ZeroBestKnownPolicy = zeroBestKnownPolicy };
    }
}
