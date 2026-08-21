using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

public abstract record SymbolicExpressionTreeManipulator : SingleCandidateMutator<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>
{
    public abstract SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace);

    public sealed override SymbolicExpressionTree MutateCandidate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace) =>
        Mutate(parent, random, searchSpace);
}
