using HEAL.HeuristicLib.Encodings.Trees;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Mutator.SymbolicExpressionTrees;

public sealed class MultiSymbolicExpressionTreeManipulator : SymbolicExpressionTreeManipulator {
  public List<SymbolicExpressionTreeManipulator> SubOperators { get; } = [];

  public override SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeEncoding encoding) {
    return SubOperators.SampleRandom(random).Mutate(parent, random, encoding);
  }
}
