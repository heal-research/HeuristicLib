using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

public sealed record SubtreeCrossover
    : SingleCandidateCrossover<ExpressionTree, ExpressionTreeSearchSpace>
{
    /// <summary>
    /// Gets the probability of selecting an internal node before selecting uniformly within that category.
    /// A value of <see langword="null"/> selects uniformly among all eligible nodes.
    /// </summary>
    /// <remarks>
    /// A non-null value is used as a threshold against a random draw in <c>[0, 1)</c> and is retained as configured.
    /// A value at or below zero, negative infinity and <see cref="double.NaN"/> never select an internal node; a value
    /// at or above one and positive infinity always do.
    /// </remarks>
    public double? InternalNodeProbability { get; init; }

    public override ExpressionTree CrossParents(Parents<ExpressionTree> parents, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace) =>
        Cross(parents.Parent1, parents.Parent2, random, searchSpace, InternalNodeProbability);

    public static ExpressionTree Cross(ExpressionTree parent1, ExpressionTree parent2, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, double? internalNodeProbability = null)
    {
        var destination = SelectDestination(parent1, random, internalNodeProbability);
        var selectedDonor = SelectDonor(parent1, parent2, destination, random, searchSpace, internalNodeProbability);

        return selectedDonor is null
            ? parent1
            : parent1.Replace(destination, selectedDonor);
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

    private static ExpressionNode? SelectDonor(ExpressionTree parent1, ExpressionTree parent2, ExpressionPoint destination, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, double? internalNodeProbability)
    {
        if (internalNodeProbability is null)
            return SelectDonor(parent1, parent2, destination, random, searchSpace, internalNode: null);

        var selectInternalNode = random.NextDouble() < internalNodeProbability.Value;
        return SelectDonor(parent1, parent2, destination, random, searchSpace, selectInternalNode)
            ?? SelectDonor(parent1, parent2, destination, random, searchSpace, !selectInternalNode);
    }

    private static ExpressionNode? SelectDonor(ExpressionTree parent1, ExpressionTree parent2, ExpressionPoint destination, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, bool? internalNode)
    {
        // Hoisted because the eligibility test runs once per node on each of the two walks, and these do not change.
        var lengthWithoutDestination = parent1.Length - destination.Node.Length;
        var destinationDepth = destination.Depth;
        var maximumLength = searchSpace.MaximumLength;
        var maximumDepth = searchSpace.MaximumDepth;

        var eligibleCount = CountEligible(parent2.Root);
        if (eligibleCount == 0)
            return null;

        // A single eligible donor is not a choice, so it costs no randomness.
        var selectedIndex = eligibleCount == 1 ? 0 : random.NextInt(eligibleCount);
        return FindEligible(parent2.Root, ref selectedIndex);

        int CountEligible(ExpressionNode donor)
        {
            var count = IsEligible(donor) ? 1 : 0;
            for (var i = 0; i < donor.Arity; i++)
                count += CountEligible(donor.GetChild(i));

            return count;
        }

        ExpressionNode? FindEligible(ExpressionNode donor, ref int remaining)
        {
            if (IsEligible(donor) && remaining-- == 0)
                return donor;

            for (var i = 0; i < donor.Arity; i++)
            {
                var found = FindEligible(donor.GetChild(i), ref remaining);
                if (found is not null)
                    return found;
            }

            return null;
        }

        bool IsEligible(ExpressionNode donor)
        {
            if (internalNode is not null && (donor.Arity > 0) != internalNode.Value)
                return false;

            return lengthWithoutDestination + donor.Length <= maximumLength
                   && destinationDepth + donor.Depth <= maximumDepth;
        }
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
}
