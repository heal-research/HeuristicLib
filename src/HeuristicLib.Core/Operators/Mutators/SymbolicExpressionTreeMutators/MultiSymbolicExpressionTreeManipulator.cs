using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees;

namespace HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionTreeMutators;

public sealed class MultiSymbolicExpressionTreeManipulator : SymbolicExpressionTreeManipulator
{
  public List<SymbolicExpressionTreeManipulator> SubOperators { get; } = [];

  public override SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace) => SubOperators.SampleRandom(random).Mutate(parent, random, searchSpace);
}
