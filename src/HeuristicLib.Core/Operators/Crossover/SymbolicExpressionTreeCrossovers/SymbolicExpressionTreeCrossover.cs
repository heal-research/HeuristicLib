using HEAL.HeuristicLib.Operators.Crossover;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Crossovers;

/// <summary>
/// A base class for operators that perform a crossover of symbolic expression trees.
/// </summary>
public abstract class SymbolicExpressionTreeCrossover : Crossover<SymbolicExpressionTree, SymbolicExpressionSearchSpace>;
