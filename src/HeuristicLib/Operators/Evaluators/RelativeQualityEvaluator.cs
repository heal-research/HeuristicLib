using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public enum RelativeQualityZeroBestKnownPolicy
{
    SignedInfinity,
    Difference,
    NaN
}

public record RelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem>
    : WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private readonly ObjectiveVector bestKnown;
    private readonly RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy;

    public RelativeQualityEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
                                    ObjectiveVector bestKnown,
                                    RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy =
                                        RelativeQualityZeroBestKnownPolicy.SignedInfinity)
        : base(evaluator)
    {
        this.bestKnown = bestKnown;
        this.zeroBestKnownPolicy = zeroBestKnownPolicy;
    }

    protected override WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateEvaluatorInstance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator) =>
        new Instance(innerEvaluator, bestKnown, zeroBestKnownPolicy);

    private sealed class Instance(
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator,
        ObjectiveVector bestKnown,
        RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(innerEvaluator)
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates,
                                                                IRandomNumberGenerator random,
                                                                TSearchSpace searchSpace,
                                                                TProblem problem)
            => InnerEvaluator.Evaluate(candidates, random, searchSpace, problem)
                             .Select(objective => RelativeQuality.Normalize(objective, bestKnown, zeroBestKnownPolicy))
                             .ToArray();
    }
}

public static class RelativeQuality
{
    private const double ZeroTolerance = 1e-12;

    public static ObjectiveVector Normalize(ObjectiveVector objective,
                                            ObjectiveVector bestKnown,
                                            RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy =
                                                RelativeQualityZeroBestKnownPolicy.SignedInfinity)
    {
        if (objective.Count != bestKnown.Count)
        {
            throw new ArgumentException("Objective vector and best-known vector must have the same length.", nameof(bestKnown));
        }

        var relativeValues = new double[objective.Count];
        for (var i = 0; i < relativeValues.Length; i++)
        {
            relativeValues[i] = Normalize(objective[i], bestKnown[i], zeroBestKnownPolicy);
        }

        return new ObjectiveVector(relativeValues);
    }

    private static double Normalize(double observed,
                                    double bestKnown,
                                    RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy)
    {
        if (Math.Abs(bestKnown) > ZeroTolerance)
        {
            return (observed - bestKnown) / Math.Abs(bestKnown);
        }

        if (Math.Abs(observed) <= ZeroTolerance)
        {
            return 0.0;
        }

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
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public RelativeQualityEvaluator<TCandidate, TSearchSpace, TProblem> WithRelativeQuality(
            ObjectiveVector bestKnown,
            RelativeQualityZeroBestKnownPolicy zeroBestKnownPolicy =
                RelativeQualityZeroBestKnownPolicy.SignedInfinity)
            => new(evaluator, bestKnown, zeroBestKnownPolicy);
    }
}
