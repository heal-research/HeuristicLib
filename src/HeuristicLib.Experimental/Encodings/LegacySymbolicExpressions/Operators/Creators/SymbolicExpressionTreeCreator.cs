using HEAL.HeuristicLib.Operators.Creators;

namespace HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

public abstract record SymbolicExpressionTreeCreator
    : SingleCandidateCreator<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>;
