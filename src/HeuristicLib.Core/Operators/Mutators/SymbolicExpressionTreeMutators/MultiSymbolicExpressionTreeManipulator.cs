using Generator.Equals;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees;

namespace HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionTreeMutators;

[Equatable]
public sealed partial record MultiSymbolicExpressionTreeManipulator : SymbolicExpressionTreeManipulator
{
  [OrderedEquality] public IReadOnlyList<SymbolicExpressionTreeManipulator> SubOperators { get; } = [];

  public override SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace)
  {
    return SubOperators.SampleRandom(random).Mutate(parent, random, searchSpace);
  }
}
