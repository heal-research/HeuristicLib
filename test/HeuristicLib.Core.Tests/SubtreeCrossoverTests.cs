using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionTreeCrossovers;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

namespace HEAL.HeuristicLib.Tests;

public class SubtreeCrossoverTests
{
  [Fact]
  public void Cross_DoesNotInsertBranchThatWouldExceedTreeDepth()
  {
    var grammar = new SimpleSymbolicExpressionGrammar();
    var addition = new Addition();
    var variable = new Variable { VariableNames = ["x"] };
    grammar.AddFullyConnectedSymbols(grammar.StartSymbol, [addition, variable]);

    var searchSpace = new SymbolicExpressionTreeSearchSpace(grammar, TreeLength: 20, TreeDepth: 4);

    var parent0 = grammar.MakeStump(new SequenceRandomNumberGenerator());
    var parent0Add = new SymbolicExpressionTreeNode(addition);
    parent0Add.AddSubtree(variable.CreateTreeNode("x", 1.0));
    parent0Add.AddSubtree(variable.CreateTreeNode("x", 1.0));
    parent0.Root[0].AddSubtree(parent0Add);

    var parent1 = grammar.MakeStump(new SequenceRandomNumberGenerator());
    var parent1Add = new SymbolicExpressionTreeNode(addition);
    var nestedAdd = new SymbolicExpressionTreeNode(addition);
    nestedAdd.AddSubtree(variable.CreateTreeNode("x", 1.0));
    nestedAdd.AddSubtree(variable.CreateTreeNode("x", 1.0));
    parent1Add.AddSubtree(nestedAdd);
    parent1Add.AddSubtree(variable.CreateTreeNode("x", 1.0));
    parent1.Root[0].AddSubtree(parent1Add);

    var random = new SequenceRandomNumberGenerator(
      nextDoubles: [0.5, 0.0],
      nextInts: [0, 0]);

    var child = SubtreeCrossover.Cross(
      random,
      parent0,
      parent1,
      internalCrossoverPointProbability: 0.0,
      searchSpace);

    Assert.True(searchSpace.Contains(child));
    Assert.Equal(4, child.Depth);
  }

  private sealed class SequenceRandomNumberGenerator(
    IEnumerable<double>? nextDoubles = null,
    IEnumerable<int>? nextInts = null) : IRandomNumberGenerator
  {
    private readonly Queue<double> doubles = new(nextDoubles ?? []);
    private readonly Queue<int> ints = new(nextInts ?? []);

    public double NextDouble() => doubles.Count > 0 ? doubles.Dequeue() : 0.5;

    public int NextInt() => ints.Count > 0 ? ints.Dequeue() : 0;

    public IRandomNumberGenerator Fork(ulong forkKey) => this;
  }
}
