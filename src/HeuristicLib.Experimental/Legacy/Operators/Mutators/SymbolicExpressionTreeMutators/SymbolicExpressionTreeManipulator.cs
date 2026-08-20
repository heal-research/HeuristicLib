using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees;

namespace HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionTreeMutators;

public abstract record SymbolicExpressionTreeManipulator : SingleCandidateMutator<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>
{
    public abstract SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace);

    public sealed override SymbolicExpressionTree MutateCandidate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace) =>
        Mutate(parent, random, searchSpace);
}
