using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

namespace HEAL.HeuristicLib.Tests.Problems.DataAnalysis.Regression;

public class SymbolicRegressionParameterOptimizationTests
{
  private const int AlgorithmRandomSeed = 42;
  public static readonly double[,] Data = new double[,] { { 0, 10 }, { 1, 10 }, { 2, 10 }, { 3, 10 }, { 4, 10 }, { 5, 10 }, { 6, 10 }, { 7, 10 }, { 8, 10 }, { 9, 10 }, { 10, 10 } };

  // Note: Some converter tests build trees that are not fully grammar-conform.
  // This is intentional: TreeToAutoDiffTermConverter operates on the realized
  // symbolic expression tree structure and does not depend on grammar validity.
  private static SymbolicRegressionProblem CreateTestSymbolicRegressionProblem(int treeLength = 40, bool multiObjective = false, int constOptIteration = 5)
  {
    var problemData = new RegressionProblemData(new ModifiableDataset(["x", "y"], Data));

    IRegressionEvaluator<SymbolicExpressionTree>[] objectives = multiObjective
      ? [
        new MaxAbsoluteErrorEvaluator(),
        new MeanAbsoluteErrorEvaluator(),
        new MeanLogErrorEvaluator(),
        new MeanRelativeErrorEvaluator(),
        new MeanSquaredErrorCalculator(),
        new NormalizedMeanSquaredErrorEvaluator(),
        new NumberOfVariablesEvaluator(),
        new PearsonR2Evaluator(),
        new RootMeanSquaredErrorEvaluator(),
        new TreeComplexityEvaluator(),
        new TreeLengthEvaluator()
      ]
      : [new RootMeanSquaredErrorEvaluator()];
    var problem = new SymbolicRegressionProblem(problemData, objectives) {
      LowerPredictionBound = -100,
      UpperPredictionBound = 100,
      SearchSpace = {
        TreeDepth = treeLength,
        TreeLength = treeLength
      },
      ParameterOptimizationIterations = constOptIteration
    };
    return problem;
  }

  private static SymbolicRegressionProblem CreateTestSymbolicRegressionProblem(
    double[,] data,
    string[]? variableNames = null,
    int treeLength = 40,
    bool multiObjective = false,
    int constOptIteration = 5)
  {
    variableNames ??= ["x", "y"];

    var dataF = new ModifiableDataset(variableNames, data);
    var trange = new Range(0, dataF.Rows);
    var problemData = new RegressionProblemData(dataF, variableNames[^1], variableNames[..^1], trange);

    IRegressionEvaluator<SymbolicExpressionTree>[] objectives = multiObjective
      ? [
        new MaxAbsoluteErrorEvaluator(),
        new MeanAbsoluteErrorEvaluator(),
        new MeanLogErrorEvaluator(),
        new MeanRelativeErrorEvaluator(),
        new MeanSquaredErrorCalculator(),
        new NormalizedMeanSquaredErrorEvaluator(),
        new NumberOfVariablesEvaluator(),
        new PearsonR2Evaluator(),
        new RootMeanSquaredErrorEvaluator(),
        new TreeComplexityEvaluator(),
        new TreeLengthEvaluator()
      ]
      : [new RootMeanSquaredErrorEvaluator()];

    return new SymbolicRegressionProblem(problemData, objectives) {
      LowerPredictionBound = -100,
      UpperPredictionBound = 100,
      SearchSpace = {
        TreeDepth = treeLength,
        TreeLength = treeLength
      },
      ParameterOptimizationIterations = constOptIteration
    };
  }

  private static int[] TrainingRows(SymbolicRegressionProblem problem) =>
    problem.ProblemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate().ToArray();

  private static SymbolicExpressionTree CreateEmptyTree(SymbolicRegressionProblem problem)
  {
    var r = RandomNumberGenerator.Create(AlgorithmRandomSeed);
    return problem.SearchSpace.Grammar.MakeStump(r);
  }

  [Fact]
  public void OptimizeParameters_ConstantTree_ReturnsZero()
  {
    var problem = CreateTestSymbolicRegressionProblem();
    var tree = CreateEmptyTree(problem);
    tree.Root[0].AddSubtree(new Number().CreateTreeNode(42.0));

    var quality = SymbolicRegressionParameterOptimization.OptimizeParameters(
      problem.Interpreter,
      tree,
      problem.ProblemData,
      problem.ProblemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate().ToArray(),
      10);

    Assert.Equal(0.0, quality, 12);
  }

  [Fact]
  public void OptimizeParameters_UpdateParametersInTreeFalse_RestoresTreeParameters()
  {
    var problem = CreateTestSymbolicRegressionProblem();
    var tree = CreateEmptyTree(problem);

    var variableTreeNode = new Variable().CreateTreeNode("x", 1.0);
    var numberTreeNode = new Number().CreateTreeNode(1.0);
    var add = new SymbolicExpressionTreeNode(new Addition());
    add.AddSubtree(variableTreeNode);
    add.AddSubtree(numberTreeNode);
    tree.Root[0].AddSubtree(add);

    var originalWeight = variableTreeNode.Weight;
    var originalConstant = numberTreeNode.Value;

    var quality = SymbolicRegressionParameterOptimization.OptimizeParameters(
      problem.Interpreter,
      tree,
      problem.ProblemData,
      problem.ProblemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate().ToArray(),
      20,
      updateParametersInTree: false);

    Assert.Equal(1.0, variableTreeNode.Weight, 12);
    Assert.Equal(1.0, numberTreeNode.Value, 12);
    Assert.True(quality >= 0.0);
  }

  [Fact]
  public void OptimizeParameters_UpdateParametersInTreeTrue_UpdatesTreeParameters()
  {
    var problem = CreateTestSymbolicRegressionProblem();
    var tree = CreateEmptyTree(problem);

    var variableTreeNode = new Variable().CreateTreeNode("x", 1.0);
    var numberTreeNode = new Number().CreateTreeNode(1.0);
    var add = new SymbolicExpressionTreeNode(new Addition());
    add.AddSubtree(variableTreeNode);
    add.AddSubtree(numberTreeNode);
    tree.Root[0].AddSubtree(add);

    var quality = SymbolicRegressionParameterOptimization.OptimizeParameters(
      problem.Interpreter,
      tree,
      problem.ProblemData,
      problem.ProblemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate().ToArray(),
      20,
      updateParametersInTree: true);

    Assert.NotEqual(1.0, variableTreeNode.Weight);
    Assert.NotEqual(1.0, numberTreeNode.Value);
    Assert.True(quality >= 0.0);
  }

  [Fact]
  public void OptimizeParameters_DoesNotReturnWorseQuality()
  {
    var problem = CreateTestSymbolicRegressionProblem();
    var tree = CreateEmptyTree(problem);

    var variableTreeNode = new Variable().CreateTreeNode("x", 1.0);
    var numberTreeNode = new Number().CreateTreeNode(1.0);
    var add = new SymbolicExpressionTreeNode(new Addition());
    add.AddSubtree(variableTreeNode);
    add.AddSubtree(numberTreeNode);
    tree.Root[0].AddSubtree(add);

    var model = new SymbolicRegressionModel(tree, problem.Interpreter);
    var rows = problem.ProblemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate().ToArray();

    var originalQuality = problem.ProblemData.Evaluate(
      model,
      rows,
      [new PearsonR2Evaluator()],
      double.MinValue,
      double.MaxValue)[0];

    var optimizedQuality = SymbolicRegressionParameterOptimization.OptimizeParameters(
      problem.Interpreter,
      tree,
      problem.ProblemData,
      rows,
      20);

    Assert.True(optimizedQuality >= originalQuality);
  }

  private static readonly double[,] ConstantTargetData = {
    { 0, 10 }, { 1, 10 }, { 2, 10 }, { 3, 10 }, { 4, 10 },
    { 5, 10 }, { 6, 10 }, { 7, 10 }, { 8, 10 }, { 9, 10 }, { 10, 10 }
  };

  [Fact]
  public void OptimizeParameters_NumberOnlyTree_DoesNotOptimizeAndReturnsZero()
  {
    var problem = CreateTestSymbolicRegressionProblem(ConstantTargetData);
    var tree = CreateEmptyTree(problem);
    var numberTreeNode = new Number().CreateTreeNode(1.0);
    tree.Root[0].AddSubtree(numberTreeNode);

    var quality = SymbolicRegressionParameterOptimization.OptimizeParameters(
      problem.Interpreter, tree, problem.ProblemData, TrainingRows(problem), 20);

    Assert.Equal(0.0, quality, 12);
    Assert.Equal(1.0, numberTreeNode.Value, 12);
  }

  [Fact]
  public void OptimizeParameters_XPlusConstant_FitsConstantTarget()
  {
    var problem = CreateTestSymbolicRegressionProblem(ConstantTargetData);
    var tree = CreateEmptyTree(problem);

    var xNode = new Variable().CreateTreeNode("x", 1.0);
    var cNode = new Number().CreateTreeNode(1.0);
    var add = new SymbolicExpressionTreeNode(new Addition());
    add.AddSubtree(xNode);
    add.AddSubtree(cNode);
    tree.Root[0].AddSubtree(add);

    var quality = SymbolicRegressionParameterOptimization.OptimizeParameters(
      problem.Interpreter, tree, problem.ProblemData, TrainingRows(problem), 20, updateParametersInTree: true);

    var y = problem.Evaluate(tree)[0];

    Assert.Equal(0.0, y, 1e-8);
    Assert.True(quality == 0);
  }

  private static readonly double[,] DoubleXData = {
    { 0, 0 }, { 1, 2 }, { 2, 4 }, { 3, 6 }, { 4, 8 }, { 5, 10 }
  };

  [Fact]
  public void OptimizeParameters_VariableWeight_OptimizesLinearScaling()
  {
    var problem = CreateTestSymbolicRegressionProblem(DoubleXData);
    var tree = CreateEmptyTree(problem);

    var xNode = new Variable().CreateTreeNode("x", 1.0);
    tree.Root[0].AddSubtree(xNode);

    var quality = SymbolicRegressionParameterOptimization.OptimizeParameters(
      problem.Interpreter, tree, problem.ProblemData, TrainingRows(problem), 20, updateParametersInTree: true);

    Assert.InRange(xNode.Weight, 1.9, 2.1);
    Assert.True(quality > 0.99);
  }

  private static readonly double[,] ShiftedData = {
    { 0, 5 }, { 1, 6 }, { 2, 7 }, { 3, 8 }, { 4, 9 }, { 5, 10 }
  };

  [Fact]
  public void OptimizeParameters_NumberNode_OptimizesIntercept()
  {
    var problem = CreateTestSymbolicRegressionProblem(ShiftedData);
    var tree = CreateEmptyTree(problem);

    var xNode = new Variable().CreateTreeNode("x", -1.0);
    var cNode = new Number().CreateTreeNode(1.0);
    var add = new SymbolicExpressionTreeNode(new Addition());
    add.AddSubtree(xNode);
    add.AddSubtree(cNode);
    tree.Root[0].AddSubtree(add);

    var quality = SymbolicRegressionParameterOptimization.OptimizeParameters(
      problem.Interpreter, tree, problem.ProblemData, TrainingRows(problem), 20, updateParametersInTree: true);

    Assert.InRange(cNode.Value, 4.9, 5.1);
    Assert.InRange(xNode.Weight, 0.9, 1.1);
    Assert.True(quality > 0.99);
  }

  [Fact]
  public void OptimizeParameters_UpdateVariableWeightsFalse_DoesNotChangeVariableWeight()
  {
    var problem = CreateTestSymbolicRegressionProblem(DoubleXData);
    var tree = CreateEmptyTree(problem);

    var xNode = new Variable().CreateTreeNode("x", 1.0);
    tree.Root[0].AddSubtree(xNode);

    var quality = SymbolicRegressionParameterOptimization.OptimizeParameters(
      problem.Interpreter,
      tree,
      problem.ProblemData,
      TrainingRows(problem),
      20,
      updateVariableWeights: false,
      updateParametersInTree: true);

    Assert.Equal(1.0, xNode.Weight, 12);
  }

  [Fact]
  public void OptimizeParameters_UpdateVariableWeightsFalse_StillOptimizesNumberConstants()
  {
    var problem = CreateTestSymbolicRegressionProblem(ShiftedData);
    var tree = CreateEmptyTree(problem);

    var xNode = new Variable().CreateTreeNode("x", 1.0);
    var cNode = new Number().CreateTreeNode(1.0);
    var add = new SymbolicExpressionTreeNode(new Addition());
    add.AddSubtree(xNode);
    add.AddSubtree(cNode);
    tree.Root[0].AddSubtree(add);

    var quality = SymbolicRegressionParameterOptimization.OptimizeParameters(
      problem.Interpreter,
      tree,
      problem.ProblemData,
      TrainingRows(problem),
      20,
      updateVariableWeights: false,
      updateParametersInTree: true);

    Assert.Equal(1.0, xNode.Weight, 12);
    Assert.InRange(cNode.Value, 4.9, 5.1);
    Assert.True(quality > 0.99);
  }

  private static readonly double[,] CubicNoisyData = {
    { -3, -18.65 },
    { -2, -9.10 },
    { -1, 0.45 },
    { 0, 4.10 },
    { 1, 3.55 },
    { 2, 2.90 },
    { 3, 5.35 }
  };

  [Fact]
  public void OptimizeParameters_CubicPolynomialWithNoise_ImprovesFit()
  {
    var problem = CreateTestSymbolicRegressionProblem(CubicNoisyData, constOptIteration: -1);
    var tree = CreateEmptyTree(problem);

    // x
    var xNode1 = new Variable().CreateTreeNode("x", 1.0);
    var xNode2 = new Variable().CreateTreeNode("x", 1.0);
    var xNode3 = new Variable().CreateTreeNode("x", 1.0);

    // x^2 = x * x
    var x2 = new SymbolicExpressionTreeNode(new Multiplication());
    x2.AddSubtree(xNode1);
    x2.AddSubtree(xNode2);

    // x^3 = (x * x) * x
    var x3 = new SymbolicExpressionTreeNode(new Multiplication());
    x3.AddSubtree(x2);
    x3.AddSubtree(xNode3);

    // a * x^3
    var term1 = new SymbolicExpressionTreeNode(new Multiplication());
    var aNode = new Number().CreateTreeNode(1.0);
    term1.AddSubtree(aNode);
    term1.AddSubtree(x3);

    // b * x^2
    var term2 = new SymbolicExpressionTreeNode(new Multiplication());
    var bNode = new Number().CreateTreeNode(1.0);

    // IMPORTANT: need fresh x nodes (tree nodes can't have multiple parents)
    var xNode4 = new Variable().CreateTreeNode("x", 1.0);
    var xNode5 = new Variable().CreateTreeNode("x", 1.0);
    var x2_b = new SymbolicExpressionTreeNode(new Multiplication());
    x2_b.AddSubtree(xNode4);
    x2_b.AddSubtree(xNode5);

    term2.AddSubtree(bNode);
    term2.AddSubtree(x2_b);

    // c * x
    var term3 = new SymbolicExpressionTreeNode(new Multiplication());
    var cNode = new Number().CreateTreeNode(1.0);
    term3.AddSubtree(cNode);
    term3.AddSubtree(new Variable().CreateTreeNode("x", 1.0));

    // constant d
    var dNode = new Number().CreateTreeNode(1.0);

    // sum everything
    var rootAdd = new SymbolicExpressionTreeNode(new Addition());
    rootAdd.AddSubtree(term1);
    rootAdd.AddSubtree(term2);
    rootAdd.AddSubtree(term3);
    rootAdd.AddSubtree(dNode);

    tree.Root[0].AddSubtree(rootAdd);

    var rows = TrainingRows(problem);
    var rmseBefore = problem.Evaluate(tree)[0];

    var quality = SymbolicRegressionParameterOptimization.OptimizeParameters(
      problem.Interpreter,
      tree,
      problem.ProblemData,
      rows,
      50,
      updateVariableWeights: false,
      updateParametersInTree: true);

    var rmseAfter = problem.Evaluate(tree)[0];

    Assert.True(rmseAfter < rmseBefore);
    Assert.True(rmseAfter < 1.04); // broad, robust threshold np is 1.0388867668257762
    Assert.True(quality > 0.9);
  }

  private static readonly double[,] MultivariateRelevantX1Data = {
    { 0, 10, 1 },
    { 1, 7, 4 },
    { 2, 4, 7 },
    { 3, 9, 10 },
    { 4, 3, 13 },
    { 5, 8, 16 }
  };

  [Fact]
  public void OptimizeParameters_MultivariateProblem_FindsRelevantVariable()
  {
    var problem = CreateTestSymbolicRegressionProblem(
      MultivariateRelevantX1Data,
      ["x1", "x2", "y"], constOptIteration: -1);
    var tree = CreateEmptyTree(problem);

    var term1 = new SymbolicExpressionTreeNode(new Multiplication());
    var x1Node = new Variable().CreateTreeNode("x1", 1.0);
    term1.AddSubtree(new Number().CreateTreeNode(1.0));
    term1.AddSubtree(x1Node);

    var term2 = new SymbolicExpressionTreeNode(new Multiplication());
    var x2Node = new Variable().CreateTreeNode("x2", 1.0);
    term2.AddSubtree(new Number().CreateTreeNode(1.0));
    term2.AddSubtree(x2Node);

    var biasNode = new Number().CreateTreeNode(1.0);

    var add = new SymbolicExpressionTreeNode(new Addition());
    add.AddSubtree(term1);
    add.AddSubtree(term2);
    add.AddSubtree(biasNode);

    tree.Root[0].AddSubtree(add);

    var rows = TrainingRows(problem);
    var rmseBefore = problem.Evaluate(tree)[0];

    var quality = SymbolicRegressionParameterOptimization.OptimizeParameters(
      problem.Interpreter,
      tree,
      problem.ProblemData,
      rows,
      50,
      updateVariableWeights: false,
      updateParametersInTree: true);

    var rmseAfter = problem.Evaluate(tree)[0];

    var x1Coeff = ((NumberTreeNode)term1[0]).Value;
    var x2Coeff = ((NumberTreeNode)term2[0]).Value;
    var bias = biasNode.Value;

    Assert.True(rmseAfter < rmseBefore);
    Assert.True(rmseAfter < 1e-6);
    Assert.InRange(x1Coeff, 2.7, 3.1);
    Assert.InRange(x2Coeff, -0.1, 0.1);
    Assert.InRange(bias, 0.9, 1.5);
    Assert.True(quality > 0.99);
  }
}
