using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Mutators;

public sealed class MultiSymbolicExpressionTreeManipulator : SymbolicExpressionTreeManipulator {
  public List<SymbolicExpressionTreeManipulator> SubOperators { get; } = [];

  public override SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionSearchSpace encoding) {
    return SubOperators.SampleRandom(random).Mutate(parent, random, encoding);
  }
}
