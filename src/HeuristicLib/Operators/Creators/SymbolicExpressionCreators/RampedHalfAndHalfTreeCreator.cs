using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

namespace HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionCreators;

public sealed record RampedHalfAndHalfTreeCreator
    : StatelessCreator<ExpressionTree, ExpressionTreeSearchSpace>
{
    public RampedHalfAndHalfTreeCreator(int minimumDepth = 2, int? maximumDepth = null)
    {
        if (minimumDepth <= 0)
            throw new ArgumentOutOfRangeException(nameof(minimumDepth));
        if (maximumDepth is <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumDepth));
        if (maximumDepth is int configuredMaximumDepth && configuredMaximumDepth < minimumDepth)
            throw new ArgumentException("The maximum depth must be greater than or equal to the minimum depth.", nameof(maximumDepth));

        MinimumDepth = minimumDepth;
        MaximumDepth = maximumDepth;
    }

    /// <summary>Gets the first requested depth in the ramp.</summary>
    public int MinimumDepth { get; }

    /// <summary>
    /// Gets the last requested depth, or <see langword="null"/> to use the deepest Full-compatible search-space depth.
    /// An explicit depth range must be feasible within the search-space depth and length limits.
    /// </summary>
    public int? MaximumDepth { get; }

    public override IReadOnlyList<ExpressionTree> Create(int count, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var maximumFeasibleDepth = FullTreeCreation.GetMaximumFeasibleDepth(searchSpace);
        var maximumDepth = MaximumDepth ?? maximumFeasibleDepth;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumDepth, maximumFeasibleDepth);

        var minimumDepth = MinimumDepth;
        if (minimumDepth > maximumDepth)
        {
            if (MinimumDepth == 2 && MaximumDepth is null && maximumDepth == 1)
                minimumDepth = 1;
            else
                throw new ArgumentException($"The configured minimum depth {minimumDepth} exceeds the maximum feasible depth {maximumDepth}.", nameof(searchSpace));
        }

        var depthCount = maximumDepth - minimumDepth + 1;

        var result = new ExpressionTree[count];
        for (var i = 0; i < count; i++)
            result[i] = Create(i, random.Fork(i), searchSpace, minimumDepth, depthCount);

        return result;
    }

    private static ExpressionTree Create(int index, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, int minimumDepth, int depthCount)
    {
        var depth = minimumDepth + index / 2 % depthCount;
        var root = index % 2 == 0
            ? FullTreeCreation.CreateSubtreeAtDepth(random, searchSpace, searchSpace.MaximumLength, depth)
            : GrowTreeCreation.CreateSubtree(random, searchSpace, searchSpace.MaximumLength, depth);

        return new ExpressionTree(root);
    }
}
