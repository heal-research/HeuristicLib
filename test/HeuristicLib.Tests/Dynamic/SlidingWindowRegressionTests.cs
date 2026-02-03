using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.Dynamic.SlidingWindowRegression;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Dynamic;

public class SlidingWindowRegressionTests {
  public static Dataset GetDataset() => new ModifiableDataset(["x1", "x2", "y"], new[,] { { 1.0, 2.0, 3.0 }, { 4.0, 5.0, 6.0 }, { 7.0, 8.0, 9.0 }, { 10.0, 11.0, 12.0 }, { 13.0, 14.0, 15.0 }, { 16.0, 17.0, 18.0 }, { 19.0, 20.0, 21.0 }, { 22.0, 23.0, 24.0 }, { 25.0, 26.0, 27.0 }, { 28.0, 29.0, 30.0 } });

  public static RegressionProblemData GetProblemData(Dataset dataset) => new(
    dataset,
    targetVariable: dataset.DoubleVariables.Last(),
    trainingRange: ..dataset.Rows);

  public class SpyEvaluator : RegressionEvaluator {
    public override ObjectiveDirection Direction => ObjectiveDirection.Minimize;

    public List<double[]> LastPredictions = [];

    public override double Evaluate(IEnumerable<double> predictedValues, IEnumerable<double> trueValues) {
      LastPredictions.Add(predictedValues.ToArray());
      var d = LastPredictions[^1];
      var res = d[0].GetHashCode();
      for (int i = 1; i < d.Length; i++) {
        res = HashCode.Combine(res, d[i]);
      }

      return res;
    }
  }

  private static SymbolicExpressionTree MakeVariableTree(SymbolicExpressionTreeEncoding enc, string varName) {
    var tree = enc.Grammar.MakeStump(NoRandomGenerator.Instance);
    tree.Root[0].AddSubtree(new Variable().CreateTreeNode(varName, 1));
    return tree;
  }

  [Fact]
  public void WindowRows_WithinPartition_NoWrap() {
    var data = GetProblemData(GetDataset());
    var spy = new SpyEvaluator();
    var inner = new SymbolicRegressionProblem(data, SymbolicRegressionProblem.GetDefaultEncoding(data.InputVariables), spy);
    var p = new SlidingWindowSymbolicRegressionProblem(inner, windowStart: 2, windowLength: 4, stepSize: 1);
    var tree = MakeVariableTree(p.SearchSpace, "x1");
    _ = p.Evaluate(tree, NoRandomGenerator.Instance)[0];
    Assert.Equal([7.0, 10, 13.0, 16.0], spy.LastPredictions[0]);
  }

  [Fact]
  public void WindowRows_WrapsRoundRobin() {
    var data = GetProblemData(GetDataset());
    var spy = new SpyEvaluator();
    var inner = new SymbolicRegressionProblem(data, SymbolicRegressionProblem.GetDefaultEncoding(data.InputVariables), spy);
    var p = new SlidingWindowSymbolicRegressionProblem(inner, windowStart: 8, windowLength: 5, stepSize: 1);
    var tree = MakeVariableTree(p.SearchSpace, "x1");
    _ = p.Evaluate(tree, NoRandomGenerator.Instance)[0];
    Assert.Equal([25.0, 28.0, 1.0, 4.0, 7.0], spy.LastPredictions[0]);
  }

  [Fact]
  public void Update_AdvancesWindow_ByStepSize() {
    var data = GetProblemData(GetDataset());
    var spy = new SpyEvaluator();
    var inner = new SymbolicRegressionProblem(data, SymbolicRegressionProblem.GetDefaultEncoding(data.InputVariables), spy);
    var p = new SlidingWindowSymbolicRegressionProblem(inner, windowStart: 1, windowLength: 4, stepSize: 3);
    var tree = MakeVariableTree(p.SearchSpace, "x1");
    _ = p.Evaluate(tree, NoRandomGenerator.Instance)[0];
    Assert.Equal([4.0, 7.0, 10.0, 13.0], spy.LastPredictions[0]);
    p.UpdateOnce();
    _ = p.Evaluate(tree, NoRandomGenerator.Instance)[0];
    Assert.Equal([13.0, 16.0, 19.0, 22.0], spy.LastPredictions[1]);
  }

  [Fact]
  public void WindowLength_IsRespected() {
    var data = GetProblemData(GetDataset());
    var spy = new SpyEvaluator();
    var inner = new SymbolicRegressionProblem(data, SymbolicRegressionProblem.GetDefaultEncoding(data.InputVariables), spy);
    var p = new SlidingWindowSymbolicRegressionProblem(inner, windowStart: 9, windowLength: 7, stepSize: 1);
    var tree = MakeVariableTree(p.SearchSpace, "x1");
    _ = p.Evaluate(tree, NoRandomGenerator.Instance)[0];
    Assert.Equal(7, spy.LastPredictions[0].Length);
  }
}
