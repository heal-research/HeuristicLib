using HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

namespace HEAL.HeuristicLib.Problems.MachineLearning.Legacy;

public interface ISymbolicDataAnalysisExpressionTreeInterpreter
{
    IEnumerable<double> GetSymbolicExpressionTreeValues(SymbolicExpressionTree tree, Dataset dataset, Range partition) =>
      GetSymbolicExpressionTreeValues(tree, dataset, partition.Enumerate());

    IEnumerable<double> GetSymbolicExpressionTreeValues(SymbolicExpressionTree tree, Dataset dataset, IEnumerable<int> rows);
}
