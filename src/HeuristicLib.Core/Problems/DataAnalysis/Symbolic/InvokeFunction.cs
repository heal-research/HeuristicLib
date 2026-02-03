using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

/// <summary>
///   Placeholder symbol Function Invocation and Arguments are not yet implemented
/// </summary>
internal class InvokeFunction(int minimumArity, int maximumArity) : Symbol(minimumArity, minimumArity, maximumArity);
