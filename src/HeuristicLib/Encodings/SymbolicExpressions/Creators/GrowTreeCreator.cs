using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

public sealed record GrowTreeCreator
    : SingleCandidateCreator<ExpressionTree, ExpressionTreeSearchSpace>
{
    /// <summary>
    /// Gets the maximum tree depth, or <see langword="null"/> to use the search-space maximum.
    /// An explicit maximum must be positive and must not exceed the search-space depth limit.
    /// </summary>
    public int? MaximumDepth { get; init; }

    public override ExpressionTree CreateCandidate(IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace) =>
        GrowTreeCreation.Create(random, searchSpace, MaximumDepth);
}

public static class GrowTreeCreation
{
    public static ExpressionTree Create(IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, int? maximumDepth = null)
    {
        var effectiveMaximumDepth = maximumDepth ?? searchSpace.MaximumDepth;
        var root = CreateSubtree(random, searchSpace, searchSpace.MaximumLength, effectiveMaximumDepth);
        return new ExpressionTree(root);
    }

    public static ExpressionNode CreateSubtree(IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, int maximumLength, int maximumDepth)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumLength, searchSpace.MaximumLength);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumDepth);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumDepth, searchSpace.MaximumDepth);

        return CreateNode(random, searchSpace, maximumLength, maximumDepth);
    }

    private static ExpressionNode CreateNode(IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, int maximumLength, int maximumDepth)
    {
        var maximumArity = maximumDepth == 1 ? 0 : maximumLength - 1;
        var symbol = searchSpace.SelectSymbol(0, maximumArity, random);
        if (symbol.Arity == 0)
            return symbol.CreateNode(random);

        var children = ImmutableArray.CreateBuilder<ExpressionNode>(symbol.Arity);
        var availableLength = maximumLength - 1;
        for (var i = 0; i < symbol.Arity; i++)
        {
            var remainingChildren = symbol.Arity - i - 1;
            var maximumChildLength = availableLength - remainingChildren;
            var childLength = maximumChildLength == 1
                ? 1
                : random.NextInt(1, maximumChildLength, inclusiveHigh: true);
            var child = CreateNode(random, searchSpace, childLength, maximumDepth - 1);
            children.Add(child);
            availableLength -= child.Length;
        }

        return symbol.CreateNode(random, children.MoveToImmutable());
    }
}
