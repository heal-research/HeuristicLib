using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math.Variables;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public static class SymbolicRegressionParameterOptimization {
  public static readonly PearsonR2Evaluator[] Evaluator = [new()];

  public static double OptimizeParameters(ISymbolicDataAnalysisExpressionTreeInterpreter interpreter,
                                          SymbolicExpressionTree tree,
                                          RegressionProblemData problemData,
                                          DataAnalysisProblemData.PartitionType type,
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

    var problemDataPartition = problemData.Partitions[type];
    var (_, count) = problemDataPartition.GetOffsetAndLength(problemData.Dataset.Rows);
    interpreter.GetSymbolicExpressionTreeValues(tree, problemData.Dataset, problemDataPartition);

    var model = new BoundedSymbolicRegressionModel(tree, interpreter, lowerEstimationLimit, upperEstimationLimit); // applyLinearScaling, lowerEstimationLimit, upperEstimationLimit;
    var originalQuality = problemData.Evaluate(model, type, Evaluator)[0];

    counter ??= new EvaluationsCounter();
    var rowEvaluationsCounter = new EvaluationsCounter();

    int retVal;
    var ds = problemData.Dataset;
    var x = new double[count, parameters.Count];
    var row = 0;
    foreach (var r in problemDataPartition.Enumerate()) {
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

    var y = ds.GetDoubleValues(problemData.TargetVariable, problemDataPartition.Enumerate()).ToArray();
    var n = x.GetLength(0);
    var m = x.GetLength(1);
    var k = c.Length;

    var functionCx1Func = CreatePFunc(func);
    var functionCx1Grad = CreatePGrad(funcGrad);

    try {
      alglib.lsfitcreatefg(x, y, c, n, m, k, false, out var state);
      alglib.lsfitsetcond(state, 0.0, maxIterations);
      alglib.lsfitsetxrep(state, iterationCallback != null);
      alglib.lsfitfit(state, functionCx1Func, functionCx1Grad, Xrep, rowEvaluationsCounter);
      alglib.lsfitresults(state, out retVal, out c, out _);
    }
    catch (ArithmeticException) {
      return originalQuality;
    }
    catch (alglib.alglibexception) {
      return originalQuality;
    }

    counter.FunctionEvaluations += rowEvaluationsCounter.FunctionEvaluations / n;
    counter.GradientEvaluations += rowEvaluationsCounter.GradientEvaluations / n;

    //retVal == -7  => parameter optimization failed due to wrong gradient
    //          -8  => optimizer detected  NAN / INF  in  the target
    //                 function and/ or gradient
    if (retVal != -7 && retVal != -8)
      UpdateParameters(tree, c, updateVariableWeights);

    var quality = problemData.Evaluate(model, type, Evaluator)[0];

    var improvement = originalQuality - quality <= 0.001 && !double.IsNaN(quality);

    if (!improvement) {
      UpdateParameters(tree, initialParameters, updateVariableWeights); //reset tree parameters
      return originalQuality;
    }

    if (!updateParametersInTree)
      UpdateParameters(tree, initialParameters, updateVariableWeights); //reset tree parameters
    return quality;

    void Xrep(double[] p, double f, object obj) => iterationCallback?.Invoke(p, f, obj);
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

  private static alglib.ndimensional_pfunc CreatePFunc(TreeToAutoDiffTermConverter.ParametricFunction func) {
    return (double[] c, double[] x, ref double fx, object o) => {
      fx = func(c, x);
      var counter = (EvaluationsCounter)o;
      counter.FunctionEvaluations++;
    };
  }

  private static alglib.ndimensional_pgrad CreatePGrad(TreeToAutoDiffTermConverter.ParametricFunctionGradient func_grad) {
    return (double[] c, double[] x, ref double fx, double[] grad, object o) => {
      var tuple = func_grad(c, x);
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
