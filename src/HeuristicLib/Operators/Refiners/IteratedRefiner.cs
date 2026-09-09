using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Applies the child refiner to its own output, so one refinement is repeated as <c>refine(refine(refine(candidate)))</c>.
/// The candidates produced by the final iteration are returned.
/// </summary>
/// <remarks>
/// The child refiner is applied exactly <see cref="Iterations"/> times. There is no early exit when an iteration
/// leaves a candidate unchanged, because candidate equality is not generally meaningful and a fixed iteration count
/// keeps the result reproducible. Each iteration receives its own forked random number generator, so the result does
/// not depend on how many random draws an individual iteration consumes.
/// </remarks>
public sealed record IteratedRefiner<TCandidate>
    : WrappingRefiner<TCandidate>
{
    /// <summary>
    /// Gets the number of times the child refiner is applied to each candidate.
    /// </summary>
    public int Iterations { get; init; }

    public IteratedRefiner(IRefiner<TCandidate> childRefiner, int iterations)
        : base(childRefiner)
    {
        Iterations = iterations;
    }

    protected override IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> childRefiner)
    {
        if (Iterations <= 0)
            throw new InvalidOperationException("Iterations must be positive.");

        return new Instance<TRunSearchSpace, TRunProblem>(childRefiner, Iterations);
    }

    private sealed class Instance<TSearchSpace, TProblem>(IRefinerInstance<TCandidate, TSearchSpace, TProblem> childRefiner, int iterations)
        : WrappingRefinerInstance<TCandidate, TSearchSpace, TProblem>(childRefiner)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var current = candidates;
            for (var i = 0; i < iterations; i++)
            {
                current = ChildRefiner.Refine(current, random.Fork(i), searchSpace, problem);
            }

            return current;
        }
    }
}

public static class IteratedRefiner
{
    public static IteratedRefiner<TCandidate> Create<TCandidate>(IRefiner<TCandidate> childRefiner, int iterations) =>
        new(childRefiner, iterations);
}

public static class IteratedRefinerExtensions
{
    extension<TCandidate>(IRefiner<TCandidate> refiner)
    {
        public IteratedRefiner<TCandidate> AsIterated(int iterations) =>
            new IteratedRefiner<TCandidate>(refiner, iterations);
    }
}
