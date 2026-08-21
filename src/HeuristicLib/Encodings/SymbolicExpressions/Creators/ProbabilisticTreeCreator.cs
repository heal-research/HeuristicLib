using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

/// <summary>
/// Creates expression trees using Luke's Probabilistic Tree Creation 2 (PTC2) algorithm.
/// </summary>
public sealed record ProbabilisticTreeCreator
    : SingleCandidateCreator<ExpressionTree, ExpressionTreeSearchSpace>
{
    /// <summary>
    /// Gets the requested-length distribution, or <see langword="null"/> to sample uniformly
    /// from the search-space length range.
    /// </summary>
    public IDistribution<int>? RequestedLengthDistribution { get; init; }

    /// <summary>
    /// Creates a tree whose requested length is sampled from <see cref="RequestedLengthDistribution"/>.
    /// When no distribution is supplied, the requested length is sampled uniformly from the search-space
    /// length range.
    /// </summary>
    public override ExpressionTree CreateCandidate(IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace)
    {
        var requestedLength = RequestedLengthDistribution?.Sample(random);
        return ProbabilisticTreeCreation.Create(random, searchSpace, requestedLength);
    }
}

public static class ProbabilisticTreeCreation
{
    public static ExpressionTree Create(IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, int? requestedLength = null)
    {
        var root = CreateSubtree(random, searchSpace, searchSpace.MaximumLength, searchSpace.MaximumDepth, requestedLength);

        return new ExpressionTree(root);
    }

    public static ExpressionNode CreateSubtree(IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, int maximumLength, int maximumDepth, int? requestedLength = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumLength, searchSpace.MaximumLength);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumDepth);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumDepth, searchSpace.MaximumDepth);

        var effectiveRequestedLength = requestedLength ?? random.NextInt(1, maximumLength, inclusiveHigh: true);
        if (effectiveRequestedLength <= 0 || effectiveRequestedLength > maximumLength)
            throw new ArgumentOutOfRangeException(nameof(requestedLength));

        var builder = new ExpressionTreeBuilder();
        var root = builder.Root;
        if (effectiveRequestedLength == 1 || maximumDepth == 1 || !HasSymbolWithArity(searchSpace, 1, maximumLength - 1))
        {
            FillWithTerminal(builder, root, random, searchSpace);
            return builder.Build(random);
        }

        var rootChildren = Expand(builder, root, random, searchSpace, maximumLength - 1);
        var extensionPoints = new List<ExtensionPoint>(rootChildren.Length);
        AddChildren(extensionPoints, rootChildren, depth: 2);

        // Every open position will eventually become at least one terminal node.
        var projectedLength = 1 + extensionPoints.Count;
        while (extensionPoints.Count > 0 && projectedLength < effectiveRequestedLength)
        {
            var extension = TakeRandom(extensionPoints, random);
            var maximumArity = maximumLength - projectedLength;

            if (extension.Depth >= maximumDepth || maximumArity == 0 || !HasSymbolWithArity(searchSpace, 1, maximumArity))
            {
                FillWithTerminal(builder, extension.Position, random, searchSpace);
                continue;
            }

            var children = Expand(builder, extension.Position, random, searchSpace, maximumArity);
            projectedLength += children.Length;
            AddChildren(extensionPoints, children, extension.Depth + 1);
        }

        while (extensionPoints.Count > 0)
        {
            var extension = TakeRandom(extensionPoints, random);
            FillWithTerminal(builder, extension.Position, random, searchSpace);
        }

        return builder.Build(random);
    }

    private static ExpressionTreeBuilder.Position[] Expand(ExpressionTreeBuilder builder, ExpressionTreeBuilder.Position position, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, int maximumArity)
    {
        var symbol = searchSpace.SelectSymbol(1, maximumArity, random);
        return builder.Expand(position, symbol);
    }

    private static void FillWithTerminal(ExpressionTreeBuilder builder, ExpressionTreeBuilder.Position position, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace)
    {
        var terminal = searchSpace.SelectSymbol(0, random).CreateNode(random);
        builder.CompleteWithTerminal(position, terminal);
    }

    private static void AddChildren(List<ExtensionPoint> extensionPoints, ExpressionTreeBuilder.Position[] children, int depth)
    {
        for (var i = 0; i < children.Length; i++)
            extensionPoints.Add(new ExtensionPoint(children[i], depth));
    }

    private static ExtensionPoint TakeRandom(List<ExtensionPoint> extensionPoints, IRandomNumberGenerator random)
    {
        var index = random.NextInt(extensionPoints.Count);
        var extension = extensionPoints[index];
        var lastIndex = extensionPoints.Count - 1;
        extensionPoints[index] = extensionPoints[lastIndex];
        extensionPoints.RemoveAt(lastIndex);
        return extension;
    }

    private static bool HasSymbolWithArity(ExpressionTreeSearchSpace searchSpace, int minimumArity, int maximumArity)
    {
        if (maximumArity < minimumArity)
            return false;

        return searchSpace.Symbols.Any(symbol => symbol.Arity >= minimumArity && symbol.Arity <= maximumArity);
    }

    private readonly record struct ExtensionPoint(ExpressionTreeBuilder.Position Position, int Depth);
}
