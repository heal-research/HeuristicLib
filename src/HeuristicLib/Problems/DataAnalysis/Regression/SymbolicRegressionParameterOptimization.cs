using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Variables;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public static class SymbolicRegressionParameterOptimization {
  public static readonly PearsonR2Evaluator[] Evaluator = [new()];

  public static double OptimizeParameters(ISymbolicDataAnalysisExpressionTreeInterpreter interpreter,
                                          SymbolicExpressionTree tree,
                                          RegressionProblemData problemData,
                                          IReadOnlyList<int> rows,
                                          int maxIterations,
                                          bool updateVariableWeights = true,
                                          double lowerEstimationLimit = double.MinValue,
                                          double upperEstimationLimit = double.MaxValue,
                                          bool updateParametersInTree = true,
                                          Action<double[], double, object>? iterationCallback = null,
                                          EvaluationsCounter? counter = null) {
    // Numeric parameters in the tree become variables for parameter optimization.
    // Variables in the tree become parameters (fixed values) for parameter optimization.
    // For each parameter (variable in the original tree) we store the 
    // variable name, variable value (for factor vars) and lag as a DataForVariable object.
    // A dictionary is used to find parameters

    if (!TreeToAutoDiffTermConverter.TryConvertToAutoDiff(tree, updateVariableWeights, out var parameters, out var initialParameters, out var func, out var funcGrad))
      throw new NotSupportedException("Could not optimize parameters of symbolic expression tree due to not supported symbols used in the tree.");
    ArgumentNullException.ThrowIfNull(parameters);
    ArgumentNullException.ThrowIfNull(initialParameters);
    ArgumentNullException.ThrowIfNull(func);
    ArgumentNullException.ThrowIfNull(funcGrad);

    if (parameters.Count == 0)
      return 0.0; // constant expressions always have an R� of 0.0 
    var parameterEntries = parameters.ToArray(); // order of entries must be the same for x

    // extract initial parameters

    var c = (double[])initialParameters.Clone();

    interpreter.GetSymbolicExpressionTreeValues(tree, problemData.Dataset, rows);

    //var model = new BoundedSymbolicRegressionModel(tree, interpreter, lowerEstimationLimit, upperEstimationLimit); // applyLinearScaling, lowerEstimationLimit, upperEstimationLimit;
    var model = new SymbolicRegressionModel(tree, interpreter);
    var originalQuality = problemData.Evaluate(model, rows, Evaluator, lowerEstimationLimit, upperEstimationLimit)[0];

    counter ??= new EvaluationsCounter();
    var rowEvaluationsCounter = new EvaluationsCounter();

    var ds = problemData.Dataset;
    var x = new double[rows.Count, parameters.Count];
    var row = 0;
    foreach (var r in rows) {
      var col = 0;
      foreach (var info in parameterEntries) {
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
    var k = c.Length;

    var functionCx1Func = CreatePFunc(func);
    var functionCx1Grad = CreatePGrad(funcGrad);

    double[] cOpt1;
    int retVal;

    double[] cOpt = [];
    ExitCondition status = ExitCondition.None;

    try {
      (cOpt1, retVal) = alglibLM(maxIterations, iterationCallback, x, y, n, m, k, functionCx1Func, functionCx1Grad, rowEvaluationsCounter, c.ToArray());
    }
    catch (ArithmeticException) {
      return originalQuality;
    }
    catch (alglib.alglibexception) {
      return originalQuality;
    }

    try {
      (cOpt, status) = (cOpt1, ExitCondition.None); //MathnetLM(maxIterations, n, y, m, x, func, rowEvaluationsCounter, funcGrad, c.ToArray(), k);
    }
    catch (NonConvergenceException) {
      if (retVal != -7 && retVal != -8)
        throw;
    }
    catch (ArgumentException) {
      if (retVal != -7 && retVal != -8)
        throw;
    }

    if (status is ExitCondition.InvalidValues) {
      return originalQuality;
    }

    var debug = cOpt.Zip(cOpt1, (a, b) =>
      Math.Abs(a - b));

    counter.FunctionEvaluations += rowEvaluationsCounter.FunctionEvaluations / n;
    counter.GradientEvaluations += rowEvaluationsCounter.GradientEvaluations / n;

    //retVal == -7  => parameter optimization failed due to wrong gradient
    //          -8  => optimizer detected  NAN / INF  in  the target
    //                 function and/ or gradient
    if (retVal is -7 or -8) return originalQuality;
    double quality;
    UpdateParameters(tree, cOpt1, updateVariableWeights);
    try {
      quality = problemData.Evaluate(model, rows, Evaluator)[0];
    }
    catch (InvalidOperationException) {
      //this happens when the new parameters produce invalid results (e.g. catastrophic cancellation)
      UpdateParameters(tree, initialParameters, updateVariableWeights);
      return originalQuality;
    }

    UpdateParameters(tree, cOpt, updateVariableWeights);
    var quality2 = problemData.Evaluate(model, rows, Evaluator)[0];

    Optimization.Extensions.CheckDebug(Math.Abs(quality2 - quality) < 1e-3, "Math.Net!= alglib");

    var improvement = originalQuality - quality <= 0.001 && !double.IsNaN(quality);

    if (!improvement) {
      UpdateParameters(tree, initialParameters, updateVariableWeights); //reset tree parameters
      return originalQuality;
    }

    if (!updateParametersInTree)
      UpdateParameters(tree, initialParameters, updateVariableWeights); //reset tree parameters
    return quality;
  }

  private static (double[] c, int retVal) alglibLM(int maxIterations, Action<double[], double, object>? iterationCallback, double[,] x, double[] y, int n, int m, int k, PFunc functionCx1Func, PGrad functionCx1Grad, EvaluationsCounter rowEvaluationsCounter, double[] c) {
    alglib.lsfitcreatefg(x, y, c, n, m, k, false, out var state);
    alglib.lsfitsetcond(state, 0.0, maxIterations);
    alglib.lsfitsetxrep(state, iterationCallback != null);
    alglib.lsfitfit(state,
      new alglib.ndimensional_pfunc(functionCx1Func),
      new alglib.ndimensional_pgrad(functionCx1Grad), Xrep, rowEvaluationsCounter);
    alglib.lsfitresults(state, out int retVal, out c, out _);
    return (c, retVal);
    void Xrep(double[] p, double f, object obj) => iterationCallback?.Invoke(p, f, obj);
  }

  private static (double[] cOpt, ExitCondition status) MathnetLM(int maxIterations, int n, double[] y, int m, double[,] x, TreeToAutoDiffTermConverter.ParametricFunction func, EvaluationsCounter rowEvaluationsCounter, TreeToAutoDiffTermConverter.ParametricFunctionGradient funcGrad, double[] c, int k) {
    #region math.net.numerics
    var xIdx = Vector<double>.Build.Dense(n, i => i);
    var yVec = Vector<double>.Build.DenseOfArray(y);
    var wVec = Vector<double>.Build.Dense(n, 1.0);

    // Model: parameters p -> vector of predicted y_i
    Vector<double> ModelN(Vector<double> p, Vector<double> idx) {
      var pred = Vector<double>.Build.Dense(idx.Count);
      var xRow = new double[m];

      for (int j = 0; j < idx.Count; j++) {
        int i = (int)idx[j]; // observation index

        for (int col = 0; col < m; col++) xRow[col] = x[i, col];

        pred[j] = func(p.ToArray(), xRow);
        rowEvaluationsCounter.FunctionEvaluations++;
      }

      return pred;
    }

    // Jacobian: J[i, q] = ∂f(c, x_i) / ∂c_q
    Matrix<double> Jacobian(Vector<double> p, Vector<double> idx) {
      var J = Matrix<double>.Build.Dense(idx.Count, p.Count);
      var xRow = new double[m];

      for (int j = 0; j < idx.Count; j++) {
        int i = (int)idx[j];

        for (int col = 0; col < m; col++) xRow[col] = x[i, col];

        var (gradArr, _) = funcGrad(p.ToArray(), xRow);

        for (int q = 0; q < p.Count; q++) J[j, q] = gradArr[q];

        rowEvaluationsCounter.GradientEvaluations++;
      }

      return J;
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
    double[] scales = Enumerable.Repeat(1.0, k).ToArray();
    bool[] isFixed = Enumerable.Repeat(false, k).ToArray();

// Configure LM similarly to your ALGLIB maxIterations limit
    var lm = new LevenbergMarquardtMinimizer(
      initialMu: 1e-3,
      gradientTolerance: 1e-12,
      stepTolerance: 1e-12,
      functionTolerance: 1e-12,
      maximumIterations: maxIterations
    );

// Solve
    var result = lm.FindMinimum(objective,
      initialGuess: c, // initialGuess
      scales: scales,
      isFixed: isFixed);

    double[] cOpt = result.MinimizingPoint.ToArray();
    var status = result.ReasonForExit; // similar to retVal
    #endregion

    return (cOpt, status);
  }

  private static void UpdateParameters(SymbolicExpressionTree tree, double[] parameters, bool updateVariableWeights) {
    var i = 0;
    foreach (var node in tree.Root.IterateNodesPrefix()) {
      switch (node) {
        case NumberTreeNode { Parent.Symbol: Power } numberTreeNode when numberTreeNode.Parent[1] == numberTreeNode:
          continue; // exponents in powers are not optimized (see TreeToAutoDiffTermConverter)
        case NumberTreeNode numberTreeNode:
          numberTreeNode.Value = parameters[i++];
          break;
        case VariableTreeNodeBase variableTreeNodeBase when updateVariableWeights:
          variableTreeNodeBase.Weight = parameters[i++];
          break;
        case VariableTreeNodeBase: {
          if (node is FactorVariableTreeNode { Weights: not null } factorVarTreeNode) {
            for (var j = 0; j < factorVarTreeNode.Weights.Length; j++)
              factorVarTreeNode.Weights[j] = parameters[i++];
          }

          break;
        }
      }
    }
  }

  public delegate void PFunc(double[] c, double[] x, ref double fx, object o);

  private static PFunc CreatePFunc(TreeToAutoDiffTermConverter.ParametricFunction func) {
    return (double[] c, double[] x, ref double fx, object o) => {
      fx = func(c, x);
      var counter = (EvaluationsCounter)o;
      counter.FunctionEvaluations++;
    };
  }

  public delegate void PGrad(double[] c, double[] x, ref double fx, double[] grad, object o);

  private static PGrad CreatePGrad(TreeToAutoDiffTermConverter.ParametricFunctionGradient funcGrad) {
    return (double[] c, double[] x, ref double fx, double[] grad, object o) => {
      var tuple = funcGrad(c, x);
      fx = tuple.Item2;
      Array.Copy(tuple.Item1, grad, grad.Length);
      var counter = (EvaluationsCounter)o;
      counter.GradientEvaluations++;
    };
  }

  public static bool CanOptimizeParameters(SymbolicExpressionTree tree) {
    return TreeToAutoDiffTermConverter.IsCompatible(tree);
  }
}
