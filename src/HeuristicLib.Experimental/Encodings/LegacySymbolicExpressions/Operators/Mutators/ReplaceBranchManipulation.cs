using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

public sealed record ReplaceBranchManipulation : SymbolicExpressionTreeManipulator
{
    private const int MaxTries = 100;

    public override SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace)
    {
        return Mutate(random, parent, searchSpace);
    }

    public static SymbolicExpressionTree Mutate(IRandomNumberGenerator random, SymbolicExpressionTree symbolicExpressionTree, SymbolicExpressionTreeSearchSpace searchSpace)
    {
        var allowedSymbols = new List<Symbol>();
        SymbolicExpressionTreeNode parent;
        int childIndex;
        int maxLength;
        int maxDepth;
        // repeat until a fitting parent and child are found (MAX_TRIES times)
        var tries = 0;

        var childTree = new SymbolicExpressionTree(symbolicExpressionTree);
        do
        {
            parent = childTree.Root.IterateNodesPrefix().Skip(1).Where(n => n.SubtreeCount > 0).SampleRandom(random);
            childIndex = random.NextInt(parent.SubtreeCount);
            var child = parent[childIndex];
            maxLength = searchSpace.TreeLength - childTree.Length + child.GetLength();
            maxDepth = searchSpace.TreeDepth - childTree.Depth + child.GetDepth();

            allowedSymbols.Clear();
            allowedSymbols.AddRange(searchSpace.Grammar.GetAllowedChildSymbols(parent.Symbol, childIndex)
              .Where(symbol => symbol != child.Symbol
                && symbol.InitialFrequency > 0
                && searchSpace.Grammar.GetMinimumExpressionDepth(symbol) + 1 <= maxDepth
                && searchSpace.Grammar.GetMinimumExpressionLength(symbol) <= maxLength));

            tries++;
        } while (tries < MaxTries && allowedSymbols.Count == 0);

        if (tries < MaxTries)
        {
            var weights = allowedSymbols.Select(s => s.InitialFrequency).ToList();
            var seedSymbol = allowedSymbols.SampleProportional(random, 1, weights).First();

            // replace the old node with the new node
            var seedNode = seedSymbol.CreateTreeNode();
            if (seedNode.HasLocalParameters)
            {
                seedNode.ResetLocalParameters(random);
            }

            parent.RemoveSubtree(childIndex);
            parent.InsertSubtree(childIndex, seedNode);
            ProbabilisticTreeCreator.Ptc2(random, seedNode, maxDepth, maxLength, searchSpace);
            return childTree;
        }

        return symbolicExpressionTree;
    }
}
