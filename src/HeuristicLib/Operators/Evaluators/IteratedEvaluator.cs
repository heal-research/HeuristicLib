using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

/// <summary>
/// Feeds the candidate returned by the child evaluator back into it, so an evaluator that transforms candidates is
/// applied repeatedly. The evaluated candidates of the final iteration are returned.
/// </summary>
public sealed record IteratedEvaluator<TCandidate, TSearchSpace, TProblem>
    : WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    /// <summary>
    /// Gets the number of times the child evaluator is applied to each candidate.
    /// </summary>
    public int Iterations { get; init; }

    public IteratedEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> childEvaluator, int iterations)
        : base(childEvaluator)
    {
        Iterations = iterations;
    }

    protected override WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator)
    {
        if (Iterations <= 0)
            throw new InvalidOperationException("Iterations must be positive.");

        return new Instance(childEvaluator, Iterations);
    }

    private sealed class Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator, int iterations)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(childEvaluator)
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
                evaluatedCandidates = ChildEvaluator.Evaluate(currentCandidates, random.Fork(i), searchSpace, problem);
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
