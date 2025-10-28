using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.SymbolicExpression.Mutators;

public sealed class MultiSymbolicExpressionTreeManipulator : SymbolicExpressionTreeManipulator {
  public List<SymbolicExpressionTreeManipulator> SubOperators { get; } = [];

  public override SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeEncoding encoding) {
    return SubOperators.SampleRandom(random).Mutate(parent, random, encoding);
  }
}
