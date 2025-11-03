using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

public interface ISymbolicDataAnalysisExpressionTreeInterpreter {
  IEnumerable<double> GetSymbolicExpressionTreeValues(SymbolicExpressionTree tree, Dataset dataset, Range partition) =>
    GetSymbolicExpressionTreeValues(tree, dataset, partition.Enumerate());

  IEnumerable<double> GetSymbolicExpressionTreeValues(SymbolicExpressionTree tree, Dataset dataset, IEnumerable<int> rows);
}
