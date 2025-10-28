using HEAL.HeuristicLib.Encodings.SymbolicExpression;

namespace HEAL.HeuristicLib.Operators.SymbolicExpression.Formatters;

public interface ISymbolicExpressionTreeStringFormatter {
  string Format(SymbolicExpressionTree symbolicExpressionTree);
}
