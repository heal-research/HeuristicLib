using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Variables;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public static class SymbolicRegressionParameterOptimization
{
  private static readonly PearsonR2Evaluator[] Evaluator = [new()];

  public static double OptimizeParameters(ISymbolicDataAnalysisExpressionTreeInterpreter interpreter,
                                          SymbolicExpressionTree tree,
                                          RegressionProblemData problemData,
                                          IReadOnlyList<int> rows,
                                          int maxIterations,
                                          bool updateVariableWeights = true,
                                          double lowerEstimationLimit = double.MinValue,
                                          double upperEstimationLimit = double.MaxValue,
                                          bool updateParametersInTree = true,
                                          EvaluationsCounter? counter = null)
  {
    // Numeric parameters in the tree become variables for parameter optimization.
    // Variables in the tree become parameters (fixed values) for parameter optimization.
    // For each parameter (variable in the original tree) we store the 
    // variable name, variable value (for factor vars) and lag as a DataForVariable object.
    // A dictionary is used to find parameters
    if (!TreeToAutoDiffTermConverter.TryConvertToAutoDiff(tree, updateVariableWeights, out var parameters, out var initialParameters, out var func, out var funcGrad)) {
      throw new NotSupportedException("Could not optimize parameters of symbolic expression tree due to not supported symbols used in the tree.");
    }

    ArgumentNullException.ThrowIfNull(parameters);
    ArgumentNullException.ThrowIfNull(initialParameters);
    ArgumentNullException.ThrowIfNull(func);
    ArgumentNullException.ThrowIfNull(funcGrad);

    if (parameters.Count == 0)
      return 0.0; // constant expressions always have an R� of 0.0 

    var initialParametersClone = (double[])initialParameters.Clone();
    var model = new SymbolicRegressionModel(tree, interpreter);
    var originalQuality = problemData.Evaluate(model, rows, Evaluator, lowerEstimationLimit, upperEstimationLimit)[0];
    counter ??= new EvaluationsCounter();
    var rowEvaluationsCounter = new EvaluationsCounter();

    var ds = problemData.Dataset;
    var x = new double[rows.Count, parameters.Count];
    var row = 0;
    foreach (var r in rows) {
      var col = 0;
      foreach (var info in parameters) {
        if (ds.VariableHasType<double>(info.VariableName)) {
          x[row, col] = ds.GetDoubleValue(info.VariableName, r + info.Lag);
        } else if (ds.VariableHasType<string>(info.VariableName)) {
          x[row, col] = ds.GetStringValue(info.VariableName, r) == info.VariableValue ? 1 : 0;
        } else {
          throw new InvalidProgramException("found a variable of unknown type");
        }

        col++;
      }

      row++;
    }

    var y = ds.GetDoubleValues(problemData.TargetVariable, rows).ToArray();
    var n = x.GetLength(0);
    var m = x.GetLength(1);
    var k = initialParametersClone.Length;

    double[] cOpt = [];
    var status = ExitCondition.None;
    bool success = true;

    try {
      (cOpt, status) = MathNetLevenbergMarquardt(maxIterations, n, y, m, x, func, rowEvaluationsCounter, funcGrad, initialParametersClone, k);
    } catch (NonConvergenceException) {
      success = false;
    } catch (ArgumentException) {
      success = false;
    }

    counter.FunctionEvaluations += rowEvaluationsCounter.FunctionEvaluations / n;
    counter.GradientEvaluations += rowEvaluationsCounter.GradientEvaluations / n;

    if (status is ExitCondition.InvalidValues || !success)
      return originalQuality;

    UpdateParameters(tree, cOpt, updateVariableWeights);

    double quality;
    try {
      quality = problemData.Evaluate(model, rows, Evaluator)[0];
    } catch (InvalidOperationException) { // this happens when the new parameters produce invalid results (e.g. catastrophic cancellation)
      success = false;
      quality = 0;
    }

    //tree got worse
    if (!success || double.IsNaN(quality) || quality - originalQuality < -0.001) {
      UpdateParameters(tree, initialParameters, updateVariableWeights); // reset tree parameters
      return originalQuality;
    }

    if (!updateParametersInTree)
      UpdateParameters(tree, initialParameters, updateVariableWeights); // reset tree parameters
    return quality;
  }

  private static void UpdateParameters(SymbolicExpressionTree tree, double[] parameters, bool updateVariableWeights)
  {
    var i = 0;
    foreach (var node in tree.Root.IterateNodesPrefix()) {
      switch (node) {
        case NumberTreeNode { Parent.Symbol: Power } numberTreeNode when (numberTreeNode.Parent?[1] ?? null) == numberTreeNode:
          continue; // exponents in powers are not optimized (see TreeToAutoDiffTermConverter)
        case NumberTreeNode numberTreeNode:
          numberTreeNode.Value = parameters[i++];

          break;
        case VariableTreeNodeBase variableTreeNodeBase when updateVariableWeights:
          variableTreeNodeBase.Weight = parameters[i++];

          break;
        case VariableTreeNodeBase: {
          if (node is FactorVariableTreeNode { Weights: not null } factorVarTreeNode) {
            for (var j = 0; j < factorVarTreeNode.Weights.Length; j++) {
              factorVarTreeNode.Weights[j] = parameters[i++];
            }
          }

          break;
        }
      }
    }
  }

  public static bool CanOptimizeParameters(SymbolicExpressionTree tree) => TreeToAutoDiffTermConverter.IsCompatible(tree);

  private static (double[] cOpt, ExitCondition status) MathNetLevenbergMarquardt(int maxIterations, int n, double[] y, int m, double[,] x,
                                                                                 TreeToAutoDiffTermConverter.CompiledModel func,
                                                                                 EvaluationsCounter rowEvaluationsCounter,
                                                                                 TreeToAutoDiffTermConverter.CompiledModelGradient funcGrad,
                                                                                 double[] c, int k)
  {
    var xIdx = Vector<double>.Build.Dense(n, init: i => i);
    var yVec = Vector<double>.Build.DenseOfArray(y);
    var wVec = Vector<double>.Build.Dense(n, 1.0);

    // Model: parameters p -> vector of predicted y_i
    Vector<double> ModelN(Vector<double> p, Vector<double> idx)
    {
      var predicted = Vector<double>.Build.Dense(idx.Count);
      var xRow = new double[m];
      var pArr = p.ToArray();

      for (var j = 0; j < idx.Count; j++) {
        var i = (int)idx[j];

        for (var col = 0; col < m; col++) {
          xRow[col] = x[i, col];
        }

        double value;
        try {
          value = func(pArr, xRow);
        } catch (Exception ex) {
          throw new InvalidModelEvaluationException(
            $"func failed at row {i}.", ex);
        }

        if (double.IsNaN(value) || double.IsInfinity(value)) {
          throw new InvalidModelEvaluationException(
            $"func returned non-finite value {value} at row {i}.");
        }

        predicted[j] = value;
        rowEvaluationsCounter.FunctionEvaluations++;
      }

      return predicted;
    }

    // Jacobian: J[i, q] = ∂f(c, x_i) / ∂c_q
    Matrix<double> Jacobian(Vector<double> p, Vector<double> idx)
    {
      var J = Matrix<double>.Build.Dense(idx.Count, p.Count);
      var xRow = new double[m];
      var pArr = p.ToArray();

      for (var j = 0; j < idx.Count; j++) {
        var i = (int)idx[j];

        for (var col = 0; col < m; col++) {
          xRow[col] = x[i, col];
        }

        double[] gradArr;
        try {
          (gradArr, _) = funcGrad(pArr, xRow);
        } catch (Exception ex) {
          throw new InvalidModelEvaluationException(
            $"funcGrad failed at row {i}.", ex);
        }

        if (gradArr == null || gradArr.Length != p.Count) {
          throw new InvalidModelEvaluationException(
            $"funcGrad returned invalid gradient length at row {i}: got {gradArr?.Length}, expected {p.Count}.");
        }

        for (var q = 0; q < p.Count; q++) {
          var g = gradArr[q];
          if (double.IsNaN(g) || double.IsInfinity(g)) {
            throw new InvalidModelEvaluationException(
              $"funcGrad returned non-finite value {g} at row {i}, parameter {q}.");
          }

          J[j, q] = g;
        }

        rowEvaluationsCounter.GradientEvaluations++;
      }

      return JacobianLooksToxic(J, out var reason) ? throw new InvalidModelEvaluationException(reason) : J;
    }

    var objective =
      ObjectiveFunction.NonlinearModel(
        ModelN,
        Jacobian,
        xIdx, // "X" (here just indices; real X is in closure)
        yVec, // observed Y
        wVec // weights
      );

    // Bounds/scales: “no bounds, all free”
    var scales = Enumerable.Repeat(1.0, k).ToArray();
    var isFixed = Enumerable.Repeat(false, k).ToArray();

    // Configure LM
    var lm = new LevenbergMarquardtMinimizer(
      1e-3,
      1e-12,
      1e-12,
      1e-12,
      maxIterations
    );

    // Solve
    try {
      var result = lm.FindMinimum(
        objective,
        c,
        scales: scales,
        isFixed: isFixed);

      return (result.MinimizingPoint.ToArray(), result.ReasonForExit);
    } catch (InvalidModelEvaluationException) {
      return (c.ToArray(), ExitCondition.InvalidValues);
    } catch (ArithmeticException) {
      return (c.ToArray(), ExitCondition.InvalidValues);
    }
  }

  private static bool JacobianLooksToxic(Matrix<double> jacobian, out string reason)
  {
    reason = "";

    double maxAbs = 0.0;
    double minAbsNonZero = double.PositiveInfinity;

    for (int i = 0; i < jacobian.RowCount; i++) {
      for (int j = 0; j < jacobian.ColumnCount; j++) {
        var a = Math.Abs(jacobian[i, j]);
        if ((double.IsNaN(a) || double.IsInfinity(a))) {
          reason = $"Non-finite Jacobian entry at ({i},{j})";
          return true;
        }

        if (a > maxAbs)
          maxAbs = a;
        if (a > 0.0 && a < minAbsNonZero)
          minAbsNonZero = a;
      }
    }

    if (maxAbs > 1e100) {
      reason = $"Jacobian magnitude too large: {maxAbs:E3}";
      return true;
    }

    if ((minAbsNonZero >= double.PositiveInfinity) || (maxAbs / minAbsNonZero <= 1e50)) {
      return false;
    }

    reason = $"Jacobian dynamic range too large: {maxAbs / minAbsNonZero:E3}";
    return true;
  }
}

public sealed class InvalidModelEvaluationException : Exception
{
  public InvalidModelEvaluationException(string message, Exception e) : base(message, e) { }
  public InvalidModelEvaluationException(string message) : base(message) { }
}
