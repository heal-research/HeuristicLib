using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

/// <summary>
/// Applies the child refiner to its own output, so one refinement is repeated as <c>refine(refine(refine(candidate)))</c>.
/// The candidates produced by the final iteration are returned.
/// </summary>
/// <remarks>
/// The child refiner is applied exactly <see cref="Iterations"/> times. There is no early exit when an iteration
/// leaves a candidate unchanged, because candidate equality is not generally meaningful and a fixed iteration count
/// keeps the result reproducible. Each iteration receives its own forked random number generator, so the result does
/// not depend on how many random draws an individual iteration consumes.
/// <para>
/// Iterating an accepted refinement and accepting an iterated refinement are different searches. Wrapping an
/// improvement-checking refiner in this refiner keeps every round that improved, while wrapping this refiner in an
/// improvement-checking refiner accepts or rejects the final result once.
/// </para>
/// </remarks>
public sealed record IteratedRefiner<TCandidate, TSearchSpace, TProblem>
    : WrappingRefiner<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    /// <summary>
    /// Gets the number of times the child refiner is applied to each candidate.
    /// </summary>
    public int Iterations { get; init; }

    public IteratedRefiner(IRefiner<TCandidate, TSearchSpace, TProblem> childRefiner, int iterations)
        : base(childRefiner)
    {
        Iterations = iterations;
    }

    protected override WrappingRefinerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IRefinerInstance<TCandidate, TSearchSpace, TProblem> childRefiner)
    {
        if (Iterations <= 0)
            throw new InvalidOperationException("Iterations must be positive.");

        return new Instance(childRefiner, Iterations);
    }

    private sealed class Instance(IRefinerInstance<TCandidate, TSearchSpace, TProblem> childRefiner, int iterations)
        : WrappingRefinerInstance<TCandidate, TSearchSpace, TProblem>(childRefiner)
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
    public static IteratedRefiner<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate, TSearchSpace, TProblem> childRefiner, int iterations)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childRefiner, iterations);
}

public static class IteratedRefinerExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate, TSearchSpace, TProblem> refiner)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IteratedRefiner<TCandidate, TSearchSpace, TProblem> AsIterated(int iterations) =>
            new IteratedRefiner<TCandidate, TSearchSpace, TProblem>(refiner, iterations);
    }
}
