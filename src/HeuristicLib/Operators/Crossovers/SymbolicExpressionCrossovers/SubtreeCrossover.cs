using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

namespace HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionCrossovers;

public sealed record SubtreeCrossover
    : SingleSolutionCrossover<ExpressionTree, ExpressionTreeSearchSpace>
{
    public SubtreeCrossover(double? internalNodeProbability = null)
    {
        ValidateInternalNodeProbability(internalNodeProbability);
        InternalNodeProbability = internalNodeProbability;
    }

    /// <summary>
    /// Gets the probability of selecting an internal node before selecting uniformly within that category.
    /// A value of <see langword="null"/> selects uniformly among all eligible nodes.
    /// </summary>
    public double? InternalNodeProbability { get; }

    public override ExpressionTree Cross(IParents<ExpressionTree> parents, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace) =>
        Cross(parents.Parent1, parents.Parent2, random, searchSpace, InternalNodeProbability);

    public static ExpressionTree Cross(ExpressionTree parent1, ExpressionTree parent2, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, double? internalNodeProbability = null)
    {
        ValidateInternalNodeProbability(internalNodeProbability);

        var destination = SelectDestination(parent1, random, internalNodeProbability);
        var selectedDonor = SelectDonor(parent1, parent2, destination, random, searchSpace, internalNodeProbability);

        return selectedDonor is null
            ? parent1
            : parent1.Replace(destination, selectedDonor.Node);
    }

    private static ExpressionPoint SelectDestination(ExpressionTree parent, IRandomNumberGenerator random, double? internalNodeProbability)
    {
        if (internalNodeProbability is null)
            return parent.GetPoint(random.NextInt(parent.Length));

        var selectInternalNode = random.NextDouble() < internalNodeProbability.Value;
        return SelectPoint(parent.RootPoint.TraversePreOrder(), selectInternalNode, random)
            ?? SelectPoint(parent.RootPoint.TraversePreOrder(), !selectInternalNode, random)
            ?? throw new InvalidOperationException("The parent expression does not contain a selectable point.");
    }

    private static ExpressionPoint? SelectDonor(ExpressionTree parent1, ExpressionTree parent2, ExpressionPoint destination, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, double? internalNodeProbability)
    {
        if (internalNodeProbability is null)
            return SelectDonor(parent1, parent2, destination, random, searchSpace, internalNode: null);

        var selectInternalNode = random.NextDouble() < internalNodeProbability.Value;
        return SelectDonor(parent1, parent2, destination, random, searchSpace, selectInternalNode)
            ?? SelectDonor(parent1, parent2, destination, random, searchSpace, !selectInternalNode);
    }

    private static ExpressionPoint? SelectDonor(ExpressionTree parent1, ExpressionTree parent2, ExpressionPoint destination, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, bool? internalNode)
    {
        ExpressionPoint? selectedDonor = null;
        var candidateCount = 0;

        foreach (var donor in parent2.RootPoint.TraversePreOrder())
        {
            if (internalNode is not null && (donor.Node.Arity > 0) != internalNode.Value)
                continue;

            var offspringLength = parent1.Length - destination.Node.Length + donor.Node.Length;
            var replacementDepth = destination.Depth + donor.Node.Depth;
            if (offspringLength > searchSpace.MaximumLength || replacementDepth > searchSpace.MaximumDepth)
                continue;

            candidateCount++;
            if (candidateCount > 1 && random.NextInt(candidateCount) != 0)
                continue;

            selectedDonor = donor;
        }

        return selectedDonor;
    }

    private static ExpressionPoint? SelectPoint(IEnumerable<ExpressionPoint> points, bool internalNode, IRandomNumberGenerator random)
    {
        ExpressionPoint? selectedPoint = null;
        var candidateCount = 0;

        foreach (var point in points)
        {
            if ((point.Node.Arity > 0) != internalNode)
                continue;

            candidateCount++;
            if (candidateCount > 1 && random.NextInt(candidateCount) != 0)
                continue;

            selectedPoint = point;
        }

        return selectedPoint;
    }

    private static void ValidateInternalNodeProbability(double? internalNodeProbability)
    {
        if (internalNodeProbability is double probability && (double.IsNaN(probability) || probability is < 0.0 or > 1.0))
            throw new ArgumentOutOfRangeException(nameof(internalNodeProbability));
    }
}
