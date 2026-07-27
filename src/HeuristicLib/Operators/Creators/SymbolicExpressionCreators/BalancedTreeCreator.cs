using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

namespace HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionCreators;

/// <summary>
/// Creates target-length expression trees by expanding pending positions breadth-first.
/// </summary>
public sealed record BalancedTreeCreator
    : SingleSolutionCreator<ExpressionTree, ExpressionTreeSearchSpace>
{
    public BalancedTreeCreator(double irregularity = 0.0, IDistribution<int>? requestedLengthDistribution = null)
    {
        if (double.IsNaN(irregularity) || irregularity is < 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(irregularity));

        Irregularity = irregularity;
        RequestedLengthDistribution = requestedLengthDistribution;
    }

    /// <summary>
    /// Gets the probability of allowing terminals before breadth-first expansion reaches
    /// the requested length. Zero produces the most regular shape.
    /// </summary>
    public double Irregularity { get; }

    /// <summary>
    /// Gets the requested-length distribution, or <see langword="null"/> to sample uniformly
    /// from the search-space length range.
    /// </summary>
    public IDistribution<int>? RequestedLengthDistribution { get; }

    public override ExpressionTree Create(IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace)
    {
        var requestedLength = RequestedLengthDistribution?.Sample(random);
        return BalancedTreeCreation.Create(random, searchSpace, requestedLength, Irregularity);
    }
}

public static class BalancedTreeCreation
{
    public static ExpressionTree Create(IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, int? requestedLength = null, double irregularity = 0.0)
    {
        var root = CreateSubtree(random, searchSpace, searchSpace.MaximumLength, searchSpace.MaximumDepth, requestedLength, irregularity);

        return new ExpressionTree(root);
    }

    public static ExpressionNode CreateSubtree(IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, int maximumLength, int maximumDepth, int? requestedLength = null, double irregularity = 0.0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumLength, searchSpace.MaximumLength);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumDepth);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumDepth, searchSpace.MaximumDepth);
        if (double.IsNaN(irregularity) || irregularity is < 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(irregularity));

        var effectiveRequestedLength = requestedLength ?? random.NextInt(1, maximumLength, inclusiveHigh: true);
        if (effectiveRequestedLength <= 0 || effectiveRequestedLength > maximumLength)
            throw new ArgumentOutOfRangeException(nameof(requestedLength));

        var builder = new ExpressionTreeBuilder();
        var currentLevel = new List<ExpressionTreeBuilder.Position> { builder.Root };
        var nextLevel = new List<ExpressionTreeBuilder.Position>();
        var depth = 1;

        // Pending positions already account for one terminal in the eventual tree.
        var projectedLength = 1;
        while (currentLevel.Count > 0)
        {
            var position = TakeRandom(currentLevel, random);
            var maximumArity = maximumLength - projectedLength;

            if (projectedLength >= effectiveRequestedLength || depth >= maximumDepth || maximumArity == 0 || !HasSymbolWithArity(searchSpace, 1, maximumArity))
            {
                FillWithTerminal(builder, position, random, searchSpace);
            }
            else
            {
                var hasOtherExpandablePosition = currentLevel.Count > 0 || (depth + 1 < maximumDepth && nextLevel.Count > 0);
                var allowTerminal = irregularity > 0.0 && hasOtherExpandablePosition && random.NextDouble() < irregularity;
                var symbol = searchSpace.SelectSymbol(allowTerminal ? 0 : 1, maximumArity, random);

                if (symbol.Arity == 0)
                {
                    builder.CompleteWithTerminal(position, symbol.CreateNode(random));
                }
                else
                {
                    var children = builder.Expand(position, symbol);
                    projectedLength += children.Length;
                    nextLevel.AddRange(children);
                }
            }

            if (currentLevel.Count != 0)
                continue;

            currentLevel = nextLevel;
            nextLevel = [];
            depth++;
        }

        return builder.Build(random);
    }

    private static ExpressionTreeBuilder.Position TakeRandom(List<ExpressionTreeBuilder.Position> positions, IRandomNumberGenerator random)
    {
        var index = random.NextInt(positions.Count);
        var position = positions[index];
        var lastIndex = positions.Count - 1;
        positions[index] = positions[lastIndex];
        positions.RemoveAt(lastIndex);
        return position;
    }

    private static void FillWithTerminal(ExpressionTreeBuilder builder, ExpressionTreeBuilder.Position position, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace)
    {
        var terminal = searchSpace.SelectSymbol(0, random).CreateNode(random);
        builder.CompleteWithTerminal(position, terminal);
    }

    private static bool HasSymbolWithArity(ExpressionTreeSearchSpace searchSpace, int minimumArity, int maximumArity)
    {
        if (maximumArity < minimumArity)
            return false;

        return searchSpace.Symbols.Any(symbol => symbol.Arity >= minimumArity && symbol.Arity <= maximumArity);
    }
}
