using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;

public static class LinearScaling {
  public static readonly Number Offset = new();
  public static readonly Number Intercept = new();
  public static readonly Multiplication Multiplication = new() {
    MinimumArity = 2,
    DefaultArity = 2,
    MaximumArity = 2
  };
  public static readonly Addition Add = new() {
    MinimumArity = 2,
    DefaultArity = 2,
    MaximumArity = 2
  };

  public static Symbol AddLinearScaling(this ISymbolicExpressionGrammar grammar) {
    var startSymbol = grammar.StartSymbol;
    var rem = grammar.GetAllowedChildSymbols(startSymbol).ToArray();
    foreach (var child in rem) {
      grammar.RemoveAllowedChildSymbol(startSymbol, child);
    }

    var rem2 = grammar.GetAllowedChildSymbols(startSymbol, 0).ToArray();
    foreach (var child in rem2) grammar.RemoveAllowedChildSymbol(startSymbol, child, 0);

    grammar.AddSymbol(Offset);
    grammar.AddSymbol(Intercept);
    grammar.AddSymbol(Multiplication);
    grammar.AddSymbol(Add);
    grammar.AddAllowedChildSymbol(startSymbol, Add);
    grammar.AddAllowedChildSymbol(Add, Multiplication, 0);
    grammar.AddAllowedChildSymbol(Add, Offset, 1);
    grammar.AddAllowedChildSymbol(Multiplication, Intercept, 1);

    foreach (var symbol in rem.Concat(rem2)) grammar.AddAllowedChildSymbol(Multiplication, symbol, 0);

    return Multiplication;
  }

  extension(Genotypes.Trees.SymbolicExpressionTree tree) {
    public double[] AdjustScalingFactors(double[] predictions, double[] targets) {
      var start = tree.Root[0];
      if (start.SubtreeCount == 0) return predictions;
      var add = start[0];
      if (add.Symbol != Add) return predictions; // not a tree with linear scaling
      var offsetNode = (NumberTreeNode)add[1];
      var interceptNode = (NumberTreeNode)add[0][1];

      var o = offsetNode.Value;
      var b = interceptNode.Value;
      var unscaled = predictions.Select(x => (x - o) / b).ToArray();
      OnlineLinearScalingParameterCalculator.Calculate(unscaled, targets, out var oNew, out var bNew, out var error);
      if (error == OnlineCalculatorError.None) {
        offsetNode.Value = oNew;
        interceptNode.Value = bNew;
      }

      //reuse unscaled array
      for (int i = 0; i < unscaled.Length; i++)
        unscaled[i] = unscaled[i] * bNew + oNew;

      return unscaled;
    }

    public IEnumerable<double> PredictAndAdjustScaling(ISymbolicDataAnalysisExpressionTreeInterpreter interpreter, Dataset dataset, IEnumerable<int> rows, IEnumerable<double> targets) {
      var start = tree.Root[0];
      if (start.SubtreeCount == 0) return interpreter.GetSymbolicExpressionTreeValues(tree, dataset, rows);
      var add = start[0];
      if (add.Symbol != Add) return interpreter.GetSymbolicExpressionTreeValues(tree, dataset, rows); // not a tree with linear scaling
      var offsetNode = (NumberTreeNode)add[1];
      var interceptNode = (NumberTreeNode)add[0][1];
      offsetNode.Value = 0;
      interceptNode.Value = 1;
      var unscaled = interpreter.GetSymbolicExpressionTreeValues(tree, dataset, rows).ToArray();
      OnlineLinearScalingParameterCalculator.Calculate(unscaled, targets, out var oNew, out var bNew, out var error);
      if (error == OnlineCalculatorError.None) {
        offsetNode.Value = oNew;
        interceptNode.Value = bNew;
      }

      //reuse unscaled array
      for (int i = 0; i < unscaled.Length; i++)
        unscaled[i] = unscaled[i] * bNew + oNew;

      return unscaled;
    }
  }
}
