using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

public sealed record FullTreeCreator
    : SingleCandidateCreator<ExpressionTree, ExpressionTreeSearchSpace>
{
    /// <summary>
    /// Gets the exact tree depth, or <see langword="null"/> to use the deepest depth contained by the search space.
    /// An explicit depth must be positive and feasible within the search-space depth and length limits.
    /// </summary>
    public int? Depth { get; init; }

    public override ExpressionTree CreateCandidate(IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace) =>
        FullTreeCreation.Create(random, searchSpace, Depth);
}

public static class FullTreeCreation
{
    public static ExpressionTree Create(IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, int? depth = null)
    {
        if (depth is <= 0)
            throw new InvalidOperationException("The configured depth must be positive.");

        var effectiveDepth = depth ?? GetMaximumFeasibleDepth(searchSpace);
        var root = CreateSubtreeAtDepth(random, searchSpace, searchSpace.MaximumLength, effectiveDepth);

        return new ExpressionTree(root);
    }

    public static ExpressionNode CreateSubtreeAtDepth(IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, int maximumLength, int depth)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumLength, searchSpace.MaximumLength);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(depth);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(depth, searchSpace.MaximumDepth);

        var minimumLength = GetMinimumLength(searchSpace, depth);
        if (minimumLength > maximumLength)
            throw new ArgumentException($"A full tree of depth {depth} requires at least {minimumLength} nodes, but only {maximumLength} are available.", nameof(maximumLength));

        return CreateNode(random, searchSpace, maximumLength, depth);
    }

    internal static int GetMaximumFeasibleDepth(ExpressionTreeSearchSpace searchSpace)
    {
        var depth = 1;
        while (depth < searchSpace.MaximumDepth && GetMinimumLength(searchSpace, depth + 1) <= searchSpace.MaximumLength)
            depth++;

        return depth;
    }

    private static ExpressionNode CreateNode(IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, int maximumLength, int depth)
    {
        if (depth == 1)
            return searchSpace.SelectSymbol(0, random).CreateNode(random);

        var minimumChildLength = GetMinimumLength(searchSpace, depth - 1);
        var maximumArity = (maximumLength - 1) / minimumChildLength;
        var symbol = searchSpace.SelectSymbol(1, maximumArity, random);
        var children = ImmutableArray.CreateBuilder<ExpressionNode>(symbol.Arity);
        var availableLength = maximumLength - 1;
        for (var i = 0; i < symbol.Arity; i++)
        {
            var remainingChildren = symbol.Arity - i - 1;
            var maximumChildLength = availableLength - remainingChildren * minimumChildLength;
            var childLength = maximumChildLength == minimumChildLength
                ? minimumChildLength
                : random.NextInt(minimumChildLength, maximumChildLength, inclusiveHigh: true);
            var child = CreateNode(random, searchSpace, childLength, depth - 1);
            children.Add(child);
            availableLength -= child.Length;
        }

        return symbol.CreateNode(random, children.MoveToImmutable());
    }

    private static int GetMinimumLength(ExpressionTreeSearchSpace searchSpace, int depth)
    {
        if (depth == 1)
            return 1;

        var minimumArity = searchSpace.Symbols
            .Where(symbol => symbol.Arity > 0)
            .Select(symbol => symbol.Arity)
            .DefaultIfEmpty(int.MaxValue)
            .Min();
        if (minimumArity == int.MaxValue)
            return int.MaxValue;

        var length = 1;
        for (var level = 1; level < depth; level++)
        {
            var nextLength = 1L + (long)minimumArity * length;
            if (nextLength > int.MaxValue)
                return int.MaxValue;

            length = (int)nextLength;
        }

        return length;
    }
}
