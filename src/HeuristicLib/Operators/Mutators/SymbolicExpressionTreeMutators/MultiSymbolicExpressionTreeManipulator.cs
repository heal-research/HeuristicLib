using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees;

namespace HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionTreeMutators;

public sealed record MultiSymbolicExpressionTreeManipulator : SymbolicExpressionTreeManipulator
{
    public ValueArray<SymbolicExpressionTreeManipulator> SubOperators { get; init; }

    public override SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace)
    {
        return SubOperators.SampleRandom(random).Mutate(parent, random, searchSpace);
    }
}
