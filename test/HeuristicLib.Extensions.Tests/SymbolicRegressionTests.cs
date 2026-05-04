using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionTreeCreators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

namespace HEAL.HeuristicLib.Extensions.Tests;

public class SymbolicRegressionTests
{
  private const int AlgorithmRandomSeed = 42;
  public static readonly double[,] Data = new double[,] { { 0, 10 }, { 1, 10 }, { 2, 10 }, { 3, 10 }, { 4, 10 }, { 5, 10 }, { 6, 10 }, { 7, 10 }, { 8, 10 }, { 9, 10 }, { 10, 10 } };

  public static IEnumerable<TheoryDataRow<ISymbolicDataAnalysisExpressionTreeInterpreter>> InterpreterParameters => [
    new(new SymbolicDataAnalysisExpressionTreeInterpreter()),
    new(new SymbolicDataAnalysisExpressionTreeBatchInterpreter())
  ];

  private static SymbolicRegressionProblem CreateTestSymbolicRegressionProblem(ISymbolicDataAnalysisExpressionTreeInterpreter interpreter, int treeLength = 40, bool multiObjective = false, int constOptIteration = 5)
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
      LowerPredictionBound = 0,
      UpperPredictionBound = 100,
      SearchSpace = {
        TreeDepth = treeLength,
        TreeLength = treeLength
      },
      ParameterOptimizationIterations = constOptIteration,
      Interpreter = interpreter
    };

    var linearScalingRoot = problem.SearchSpace.Grammar.AddLinearScaling();
    var symbols = new Symbol[] {
      new Addition(),
      new Subtraction(),
      new Multiplication(),
      new Division(),
      new Number(),
      new SquareRoot(),
      new Logarithm(),
      new Exponential(),
      new Variable { VariableNames = problemData.InputVariables }
    };

    problem.SearchSpace.Grammar.AddFullyConnectedSymbols(linearScalingRoot, symbols);
    return problem;
  }

  [Theory]
  [MemberData(nameof(InterpreterParameters))]
  public void MultiObjectiveConstant(ISymbolicDataAnalysisExpressionTreeInterpreter interpreter)
  {
    var problem = CreateTestSymbolicRegressionProblem(interpreter, multiObjective: true);
    var r = RandomNumberGenerator.Create(AlgorithmRandomSeed);
    var tree = problem.SearchSpace.Grammar.MakeStump(r);
    var numberNode = new Number().CreateTreeNode(10);
    tree.Root[0].AddSubtree(numberNode);
    var res = problem.Evaluate(tree);
    Assert.Equal([0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 3], res);
    const ObjectiveDirection min = ObjectiveDirection.Minimize;
    const ObjectiveDirection max = ObjectiveDirection.Maximize;
    Assert.Equal([min, min, min, min, min, min, min, max, min, min, min], problem.Objective.Directions);
  }

  [Theory]
  [MemberData(nameof(InterpreterParameters))]
  public void Constant(ISymbolicDataAnalysisExpressionTreeInterpreter interpreter)
  {
    var problem = CreateTestSymbolicRegressionProblem(interpreter, constOptIteration: -1);
    var r = RandomNumberGenerator.Create(AlgorithmRandomSeed);
    var tree = problem.SearchSpace.Grammar.MakeStump(r);
    var startNode = tree.Root[0];
    var numberNode = new Number().CreateTreeNode(10);
    startNode.AddSubtree(numberNode);
    var x = problem.Evaluate(tree);
    Assert.Equal(0, x[0]);
    numberNode.Value = 11;
    var y = problem.Evaluate(tree);
    Assert.Equal(1, y[0]);
    var tree2 = problem.SearchSpace.Grammar.MakeStump(r);
    startNode = tree2.Root[0];
    startNode.AddSubtree(new Variable().CreateTreeNode("x", 1));
    _ = problem.Evaluate(tree2);
  }

  [Theory]
  [MemberData(nameof(InterpreterParameters))]
  public void Variable(ISymbolicDataAnalysisExpressionTreeInterpreter interpreter)
  {
    var problem = CreateTestSymbolicRegressionProblem(interpreter, constOptIteration: -1);
    var r = RandomNumberGenerator.Create(AlgorithmRandomSeed);
    var tree = problem.SearchSpace.Grammar.MakeStump(r);
    tree.Root[0].AddSubtree(new Variable().CreateTreeNode("x", 1));
    var y = problem.Evaluate(tree)[0];
    Assert.Equal(Math.Sqrt(66), y, 1.0e-15);
  }

  [Theory]
  [MemberData(nameof(InterpreterParameters))]
  public void Add(ISymbolicDataAnalysisExpressionTreeInterpreter interpreter)
  {
    var problem = CreateTestSymbolicRegressionProblem(interpreter, constOptIteration: -1);
    var r = RandomNumberGenerator.Create(AlgorithmRandomSeed);
    var tree = problem.SearchSpace.Grammar.MakeStump(r);
    var variableTreeNode = new Variable().CreateTreeNode("x", 1);
    var numberTreeNode = new Number().CreateTreeNode(1);
    var add = new Addition().CreateTreeNode();
    add.AddSubtree(variableTreeNode);
    add.AddSubtree(numberTreeNode);
    tree.Root[0].AddSubtree(add);

    var y = problem.Evaluate(tree)[0];
    Assert.Equal(Math.Sqrt(51), y, 1.0e-15);
  }

  [Theory]
  [MemberData(nameof(InterpreterParameters))]
  public void AddOpt(ISymbolicDataAnalysisExpressionTreeInterpreter interpreter)
  {
    var problem = CreateTestSymbolicRegressionProblem(interpreter);
    var r = RandomNumberGenerator.Create(AlgorithmRandomSeed);
    var tree = problem.SearchSpace.Grammar.MakeStump(r);
    var variableTreeNode = new Variable().CreateTreeNode("x", 1);
    var numberTreeNode = new Number().CreateTreeNode(1);
    var add = new SymbolicExpressionTreeNode(new Addition());
    add.AddSubtree(variableTreeNode);
    add.AddSubtree(numberTreeNode);
    tree.Root[0].AddSubtree(add);
    SymbolicRegressionParameterOptimization.OptimizeParameters(problem.Interpreter, tree, problem.ProblemData,
      problem.ProblemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate().ToArray(), 10);
    var y = problem.Evaluate(tree)[0];
    Assert.Equal(0, y, 1.0e-8);
  }

  [Theory]
  [MemberData(nameof(InterpreterParameters))]
  public void Creators(ISymbolicDataAnalysisExpressionTreeInterpreter interpreter)
  {
    var problem = CreateTestSymbolicRegressionProblem(interpreter, 12);
    var creators = new SymbolicExpressionTreeCreator[] { new BalancedTreeCreator(), new ProbabilisticTreeCreator() };
    var r = RandomNumberGenerator.Create(AlgorithmRandomSeed);

    foreach (var c in creators) {
      var tree = c.Create(r, problem.SearchSpace);
      Assert.True(problem.SearchSpace.Contains(tree));
      _ = problem.Evaluate(tree);
    }

    //these creators often create invalid trees (ignore tree length)
    var invalidCreators = new SymbolicExpressionTreeCreator[] { new FullTreeCreator(), new RampedHalfAndHalfTreeCreator(), new GrowTreeCreator() };
    foreach (var c in invalidCreators) {
      var tree = c.Create(r, problem.SearchSpace);
      Assert.True(problem.SearchSpace.TreeDepth >= tree.Depth);
      _ = problem.Evaluate(tree);
    }
  }
}
