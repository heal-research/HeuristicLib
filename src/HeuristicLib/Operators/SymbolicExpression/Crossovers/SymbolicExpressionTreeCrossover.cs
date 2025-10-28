using HEAL.HeuristicLib.Encodings.SymbolicExpression;

namespace HEAL.HeuristicLib.Operators.SymbolicExpression.Crossovers;

/// <summary>
/// A base class for operators that perform a crossover of symbolic expression trees.
/// </summary>
public abstract class SymbolicExpressionTreeCrossover : Crossover<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>;
