using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

/// <summary>
/// Placeholder symbol Function Invocation and Arguments are not yet implemented
/// </summary>
internal class InvokeFunction(int minimumArity, int maximumArity) : Symbol {
  public override int MinimumArity { get; } = minimumArity;
  public override int MaximumArity { get; } = maximumArity;
}
