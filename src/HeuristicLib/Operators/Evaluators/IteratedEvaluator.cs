using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public record IteratedEvaluator<TCandidate, TSearchSpace, TProblem>
    : WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private readonly int iterations;

    public IteratedEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, int iterations)
        : base(evaluator)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);
        this.iterations = iterations;
    }

    protected override WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateEvaluatorInstance(
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator) =>
        new Instance(innerEvaluator, iterations);

    private sealed class Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator, int iterations)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(innerEvaluator)
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Evaluate(
            IReadOnlyList<TCandidate> candidates,
            IRandomNumberGenerator random,
            TSearchSpace searchSpace,
            TProblem problem)
        {
            var currentCandidates = candidates;
            IReadOnlyList<EvaluatedCandidate<TCandidate>> evaluatedCandidates = [];

            for (var i = 0; i < iterations; i++)
            {
                evaluatedCandidates = InnerEvaluator.Evaluate(currentCandidates, random.Fork(i), searchSpace, problem);
                currentCandidates = evaluatedCandidates.Select(evaluated => evaluated.Candidate).ToArray();
            }

            return evaluatedCandidates;
        }
    }
}

public static class IteratedEvaluator
{
    public static IteratedEvaluator<TCandidate, TSearchSpace, TProblem> AsIterated<TCandidate, TSearchSpace, TProblem>(
        this IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
        int iterations)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        => new(evaluator, iterations);
}
