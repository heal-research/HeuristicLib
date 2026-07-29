using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

namespace HEAL.HeuristicLib.Tests.Problems.DataAnalysis.Regression;

public class TreeToAutoDiffTermConverterTests
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
        var problem = new SymbolicRegressionProblem(problemData, objectives)
        {
            LowerPredictionBound = 0,
            UpperPredictionBound = 100,
            SearchSpace = {
        TreeDepth = treeLength,
        TreeLength = treeLength
      },
            ParameterOptimizationIterations = constOptIteration
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

    private static SymbolicExpressionTree CreateEmptyTree(SymbolicRegressionProblem problem)
    {
        var r = RandomNumberGenerator.Create(AlgorithmRandomSeed);
        return problem.SearchSpace.Grammar.MakeStump(r);
    }

    [Fact]
    public void TryConvert_NumberTree_ReturnsSingleCoefficient()
    {
        var problem = CreateTestSymbolicRegressionProblem();
        var tree = CreateEmptyTree(problem);

        var numberTreeNode = new Number().CreateTreeNode(3.5);
        tree.Root[0].AddSubtree(numberTreeNode);

        var success = TreeToAutoDiffTermConverter.TryConvertToAutoDiff(
          tree,
          makeVariableWeightsVariable: true,
          out var parameters,
          out var initialParameters,
          out var func,
          out var funcGrad);

        success.ShouldBeTrue();
        parameters.ShouldNotBeNull();
        initialParameters.ShouldNotBeNull();
        func.ShouldNotBeNull();
        funcGrad.ShouldNotBeNull();

        parameters.ShouldBeEmpty();
        initialParameters.ShouldHaveSingleItem();
        initialParameters[0].ShouldBe(3.5, 1e-12);

        var y = func([7.0], []);
        y.ShouldBe(7.0, 1e-12);
    }

    [Fact]
    public void TryConvert_VariableWithFixedWeight_CreatesNoOptimizableWeight()
    {
        var problem = CreateTestSymbolicRegressionProblem();
        var tree = CreateEmptyTree(problem);

        var variableTreeNode = new Variable().CreateTreeNode("x", 2.5);
        tree.Root[0].AddSubtree(variableTreeNode);

        var success = TreeToAutoDiffTermConverter.TryConvertToAutoDiff(
          tree,
          makeVariableWeightsVariable: false,
          out var parameters,
          out var initialParameters,
          out var func,
          out var funcGrad);

        success.ShouldBeTrue();
        parameters.ShouldNotBeNull();
        initialParameters.ShouldNotBeNull();
        func.ShouldNotBeNull();
        funcGrad.ShouldNotBeNull();

        parameters.ShouldHaveSingleItem();
        initialParameters.ShouldBeEmpty();

        parameters[0].VariableName.ShouldBe("x");
        parameters[0].VariableValue.ShouldBe(string.Empty);
        parameters[0].Lag.ShouldBe(0);

        var y = func([], [4.0]);
        y.ShouldBe(10.0, 1e-12);
    }

    [Fact]
    public void TryConvert_VariableWithOptimizableWeight_CreatesWeightCoefficient()
    {
        var problem = CreateTestSymbolicRegressionProblem();
        var tree = CreateEmptyTree(problem);

        var variableTreeNode = new Variable().CreateTreeNode("x", 2.5);
        tree.Root[0].AddSubtree(variableTreeNode);

        var success = TreeToAutoDiffTermConverter.TryConvertToAutoDiff(
          tree,
          makeVariableWeightsVariable: true,
          out var parameters,
          out var initialParameters,
          out var func,
          out var funcGrad);

        success.ShouldBeTrue();
        parameters.ShouldNotBeNull();
        initialParameters.ShouldNotBeNull();
        func.ShouldNotBeNull();
        funcGrad.ShouldNotBeNull();

        parameters.ShouldHaveSingleItem();
        initialParameters.ShouldHaveSingleItem();
        initialParameters[0].ShouldBe(2.5, 1e-12);

        var y = func([3.0], [4.0]);
        y.ShouldBe(12.0, 1e-12);
    }

    [Fact]
    public void TryConvert_RepeatedVariableOccurrences_ReuseSingleDataParameter()
    {
        var problem = CreateTestSymbolicRegressionProblem();
        var tree = CreateEmptyTree(problem);

        var add = new SymbolicExpressionTreeNode(new Addition());
        add.AddSubtree(new Variable().CreateTreeNode("x", 2.0));
        add.AddSubtree(new Variable().CreateTreeNode("x", 3.0));
        tree.Root[0].AddSubtree(add);

        var success = TreeToAutoDiffTermConverter.TryConvertToAutoDiff(
          tree,
          makeVariableWeightsVariable: true,
          out var parameters,
          out var initialParameters,
          out var func,
          out var funcGrad);

        success.ShouldBeTrue();
        parameters.ShouldNotBeNull();
        initialParameters.ShouldNotBeNull();
        func.ShouldNotBeNull();
        funcGrad.ShouldNotBeNull();

        parameters.ShouldHaveSingleItem();
        parameters[0].VariableName.ShouldBe("x");
        initialParameters.Length.ShouldBe(2);
        initialParameters[0].ShouldBe(2.0, 1e-12);
        initialParameters[1].ShouldBe(3.0, 1e-12);

        var y = func([2.0, 3.0], [4.0]);
        y.ShouldBe(20.0, 1e-12);
    }

    [Fact]
    public void TryConvert_PowerWithNonNumberExponent_Fails()
    {
        var problem = CreateTestSymbolicRegressionProblem();
        var tree = CreateEmptyTree(problem);

        var power = new SymbolicExpressionTreeNode(new Power());
        power.AddSubtree(new Variable().CreateTreeNode("x", 1.0));
        power.AddSubtree(new Variable().CreateTreeNode("x", 1.0)); // invalid exponent

        tree.Root[0].AddSubtree(power);

        var success = TreeToAutoDiffTermConverter.TryConvertToAutoDiff(
          tree,
          makeVariableWeightsVariable: true,
          out var parameters,
          out var initialParameters,
          out var func,
          out var funcGrad);

        success.ShouldBeFalse();
        parameters.ShouldBeNull();
        initialParameters.ShouldBeNull();
        func.ShouldBeNull();
        funcGrad.ShouldBeNull();
    }

    [Fact]
    public void TryConvert_PowerWithNonIntegerExponent_Fails()
    {
        var problem = CreateTestSymbolicRegressionProblem();
        var tree = CreateEmptyTree(problem);

        var power = new SymbolicExpressionTreeNode(new Power());
        power.AddSubtree(new Variable().CreateTreeNode("x", 1.0));
        power.AddSubtree(new Number().CreateTreeNode(2.5)); // invalid

        tree.Root[0].AddSubtree(power);

        var success = TreeToAutoDiffTermConverter.TryConvertToAutoDiff(
          tree,
          makeVariableWeightsVariable: true,
          out _, out _, out _, out _);

        success.ShouldBeFalse();
    }

    private class DummySymbol() : Symbol(0, 0, 0);

    [Fact]
    public void TryConvert_UnsupportedSymbol_Fails()
    {
        var problem = CreateTestSymbolicRegressionProblem();
        var tree = CreateEmptyTree(problem);

        var dummy = new SymbolicExpressionTreeNode(new DummySymbol());
        tree.Root[0].AddSubtree(dummy);

        var success = TreeToAutoDiffTermConverter.TryConvertToAutoDiff(
          tree,
          makeVariableWeightsVariable: true,
          out _, out _, out _, out _);

        success.ShouldBeFalse();
    }

    [Fact]
    public void IsCompatible_PowerWithNonIntegerExponent_ReturnsTrue_ButConversionFails()
    {
        var problem = CreateTestSymbolicRegressionProblem();
        var tree = CreateEmptyTree(problem);

        var power = new SymbolicExpressionTreeNode(new Power());
        power.AddSubtree(new Variable().CreateTreeNode("x", 1.0));
        power.AddSubtree(new Number().CreateTreeNode(2.5));

        tree.Root[0].AddSubtree(power);

        TreeToAutoDiffTermConverter.IsCompatible(tree).ShouldBeTrue();

        var success = TreeToAutoDiffTermConverter.TryConvertToAutoDiff(
          tree,
          makeVariableWeightsVariable: true,
          out _, out _, out _, out _);

        success.ShouldBeFalse();
    }
}
