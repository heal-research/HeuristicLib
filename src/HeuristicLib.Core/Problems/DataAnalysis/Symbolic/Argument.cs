using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

/// <summary>
/// Placeholder symbol Function Invocation and Arguments are not yet implemented
/// </summary>
internal class Argument(int minimumArity, int maximumArity) : Symbol(minimumArity, minimumArity, maximumArity);
