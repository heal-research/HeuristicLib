using HEAL.HeuristicLib.Algorithms.ALPS;
using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Grammars;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.SymbolicExpression.Creators;
using HEAL.HeuristicLib.Operators.SymbolicExpression.Crossovers;
using HEAL.HeuristicLib.Operators.SymbolicExpression.Mutators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;
using HEAL.HeuristicLib.Random;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using YamlDotNet.Core.Tokens;

namespace HEAL.HeuristicLib.Tests;

public class SymbolicRegressionTests {
  [Fact]
  public void ConstantTest() {
    var data = new double[,] { { 0, 10 }, { 1, 10 }, { 2, 10 }, { 3, 10 }, { 4, 10 }, { 5, 10 }, { 6, 10 }, { 7, 10 }, { 8, 10 }, { 9, 10 }, { 10, 10 } };
    var problemdata = new RegressionProblemData(new ModifiableDataset(["x", "y"], data), ["x"], "y");
    var problem = new SymbolicRegressionProblem(problemdata, [new RootMeanSquaredErrorEvaluator()]);

    var r = new SystemRandomNumberGenerator();
    var tree = problem.SearchSpace.Grammar.MakeStump(r);
    var startNode = tree.Root.GetSubtree(0);
    var numberNode = (NumberTreeNode)new Number().CreateTreeNode();
    numberNode.Value = 10;
    startNode.AddSubtree(numberNode);

    var x = problem.Evaluate(tree);
    Assert.Equal(0, x[0]);
    numberNode.Value = 11;
    var y = problem.Evaluate(tree);
    Assert.Equal(1, y[0]);

    var tree2 = problem.SearchSpace.Grammar.MakeStump(r);
    startNode = tree2.Root.GetSubtree(0);
    startNode.AddSubtree(new VariableTreeNode(new()) {
      NodeWeight = 1,
      VariableName = "x"
    });

    var y1 = problem.Evaluate(tree);
  }

  [Fact]
  public void VariableTest() {
    var data = new double[,] { { 0, 10 }, { 1, 10 }, { 2, 10 }, { 3, 10 }, { 4, 10 }, { 5, 10 }, { 6, 10 }, { 7, 10 }, { 8, 10 }, { 9, 10 }, { 10, 10 } };
    var problemdata = new RegressionProblemData(new ModifiableDataset(["x", "y"], data), ["x"], "y");
    var problem = new SymbolicRegressionProblem(problemdata, [new RootMeanSquaredErrorEvaluator()]);

    var r = new SystemRandomNumberGenerator();
    var tree = problem.SearchSpace.Grammar.MakeStump(r);
    tree.Root.GetSubtree(0).AddSubtree(new VariableTreeNode(new()) {
      NodeWeight = 1,
      VariableName = "x",
      Weight = 1
    });

    var y = problem.Evaluate(tree)[0];
    Assert.Equal(Math.Sqrt(66), y, 1.0e-15);
  }

  [Fact]
  public void AddTest() {
    var data = new double[,] { { 0, 10 }, { 1, 10 }, { 2, 10 }, { 3, 10 }, { 4, 10 }, { 5, 10 }, { 6, 10 }, { 7, 10 }, { 8, 10 }, { 9, 10 }, { 10, 10 } };
    var problemdata = new RegressionProblemData(new ModifiableDataset(["x", "y"], data), ["x"], "y");
    var problem = new SymbolicRegressionProblem(problemdata, [new RootMeanSquaredErrorEvaluator()]);

    var r = new SystemRandomNumberGenerator();
    var tree = problem.SearchSpace.Grammar.MakeStump(r);

    var varnode = new VariableTreeNode(new()) {
      NodeWeight = 1,
      VariableName = "x",
      Weight = 1
    };
    var numnode = new NumberTreeNode(new Number()) { Value = 1 };

    var add = new SymbolicExpressionTreeNode(new Addition()); //  new Addition().CreateTreeNode();
    add.AddSubtree(varnode);
    add.AddSubtree(numnode);

    tree.Root.GetSubtree(0).AddSubtree(add);

    var y = problem.Evaluate(tree)[0];
    Assert.Equal(Math.Sqrt(51), y, 1.0e-15);
  }

  [Fact]
  public void AddOptTest() {
    var data = new double[,] { { 0, 10 }, { 1, 10 }, { 2, 10 }, { 3, 10 }, { 4, 10 }, { 5, 10 }, { 6, 10 }, { 7, 10 }, { 8, 10 }, { 9, 10 }, { 10, 10 } };
    var problemdata = new RegressionProblemData(new ModifiableDataset(["x", "y"], data), ["x"], "y");
    var problem = new SymbolicRegressionProblem(problemdata, [new RootMeanSquaredErrorEvaluator()]);
    var r = new SystemRandomNumberGenerator();
    var tree = problem.SearchSpace.Grammar.MakeStump(r);
    var varnode = new VariableTreeNode(new()) {
      NodeWeight = 1,
      VariableName = "x",
      Weight = 1
    };
    var numnode = new NumberTreeNode(new Number()) { Value = 1 };
    var add = new SymbolicExpressionTreeNode(new Addition());
    add.AddSubtree(varnode);
    add.AddSubtree(numnode);
    tree.Root.GetSubtree(0).AddSubtree(add);
    SymbolicRegressionParameterOptimizationEvaluator.OptimizeParameters(problem.Interpreter, tree, problemdata, DataAnalysisProblemData.PartitionType.Training, false, 10);
    var y = problem.Evaluate(tree)[0];
    Assert.Equal(0, y, 1.0e-15);
  }

  private class A : ICreator<SymbolicExpressionTree, SymbolicExpressionTreeEncoding> {
    public IReadOnlyList<SymbolicExpressionTree> Create(int count, IRandomNumberGenerator random, SymbolicExpressionTreeEncoding encoding, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeEncoding> problem) => throw new NotImplementedException();

    public IReadOnlyList<SymbolicExpressionTree> Create(int count, IRandomNumberGenerator random, SymbolicExpressionTreeEncoding encoding) => throw new NotImplementedException();
  }

  class Asol { }

  class Aenc : IEncoding<Asol> {
    public bool Contains(Asol genotype) => throw new NotImplementedException();
  }

  [Fact]
  public void AddOptTest2() {
    var data = new double[,] { { 0, 10 }, { 1, 10 }, { 2, 10 }, { 3, 10 }, { 4, 10 }, { 5, 10 }, { 6, 10 }, { 7, 10 }, { 8, 10 }, { 9, 10 }, { 10, 10 } };
    var problemdata = new RegressionProblemData(new ModifiableDataset(["x", "y"], data), ["x"], "y");
    var problem = new SymbolicRegressionProblem(problemdata, [new RootMeanSquaredErrorEvaluator()]);

    var mutators = new SymbolicExpressionTreeManipulator[] { new ChangeNodeTypeManipulation(), new FullTreeShaker(), new OnePointShaker(), new RemoveBranchManipulation(), new ReplaceBranchManipulation() };
    var mutator = MultiMutator.Create(mutators);

    var creator = new BalancedTreeCreator();
    var cross = new SubtreeCrossover();

    var ga = new GeneticAlgorithm<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>(
      100,
      creator,
      cross,
      mutator,
      0.05,
      new TournamentSelector<SymbolicExpressionTree>(3),
      1,
      0, new AfterIterationsTerminator<SymbolicExpressionTree>(100)
    );
  }
}
