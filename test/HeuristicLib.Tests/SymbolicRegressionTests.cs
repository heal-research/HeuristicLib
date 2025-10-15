using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Grammars;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.SymbolicExpression.Creators;
using HEAL.HeuristicLib.Operators.SymbolicExpression.Crossovers;
using HEAL.HeuristicLib.Operators.SymbolicExpression.Mutators;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Tests;

public class SymbolicRegressionTests {
  public static readonly double[,] Data = new double[,] { { 0, 10 }, { 1, 10 }, { 2, 10 }, { 3, 10 }, { 4, 10 }, { 5, 10 }, { 6, 10 }, { 7, 10 }, { 8, 10 }, { 9, 10 }, { 10, 10 } };

  [Fact]
  public void ConstantTest() {
    var problemData = new RegressionProblemData(new ModifiableDataset(["x", "y"], Data), ["x"], "y");
    var problem = new SymbolicRegressionProblem(problemData, [new RootMeanSquaredErrorEvaluator()]);

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
    startNode.AddSubtree(new VariableTreeNode(new Variable()) {
      NodeWeight = 1,
      VariableName = "x"
    });

    _ = problem.Evaluate(tree);
  }

  [Fact]
  public void VariableTest() {
    var problemData = new RegressionProblemData(new ModifiableDataset(["x", "y"], Data), ["x"], "y");
    var problem = new SymbolicRegressionProblem(problemData, [new RootMeanSquaredErrorEvaluator()]);

    var r = new SystemRandomNumberGenerator();
    var tree = problem.SearchSpace.Grammar.MakeStump(r);
    tree.Root.GetSubtree(0).AddSubtree(new VariableTreeNode(new Variable()) {
      NodeWeight = 1,
      VariableName = "x",
      Weight = 1
    });

    var y = problem.Evaluate(tree)[0];
    Assert.Equal(Math.Sqrt(66), y, 1.0e-15);
  }

  [Fact]
  public void AddTest() {
    var problemData = new RegressionProblemData(new ModifiableDataset(["x", "y"], Data), ["x"], "y");
    var problem = new SymbolicRegressionProblem(problemData, [new RootMeanSquaredErrorEvaluator()]);

    var r = new SystemRandomNumberGenerator();
    var tree = problem.SearchSpace.Grammar.MakeStump(r);

    var variableTreeNode = new VariableTreeNode(new Variable()) {
      NodeWeight = 1,
      VariableName = "x",
      Weight = 1
    };
    var numberTreeNode = new NumberTreeNode(new Number()) { Value = 1 };

    var add = new SymbolicExpressionTreeNode(new Addition()); //  new Addition().CreateTreeNode();
    add.AddSubtree(variableTreeNode);
    add.AddSubtree(numberTreeNode);

    tree.Root.GetSubtree(0).AddSubtree(add);

    var y = problem.Evaluate(tree)[0];
    Assert.Equal(Math.Sqrt(51), y, 1.0e-15);
  }

  [Fact]
  public void AddOptTest() {
    var problemData = new RegressionProblemData(new ModifiableDataset(["x", "y"], Data), ["x"], "y");
    var problem = new SymbolicRegressionProblem(problemData, [new RootMeanSquaredErrorEvaluator()]);
    var r = new SystemRandomNumberGenerator();
    var tree = problem.SearchSpace.Grammar.MakeStump(r);
    var variableTreeNode = new VariableTreeNode(new Variable()) {
      NodeWeight = 1,
      VariableName = "x",
      Weight = 1
    };
    var numberTreeNode = new NumberTreeNode(new Number()) { Value = 1 };
    var add = new SymbolicExpressionTreeNode(new Addition());
    add.AddSubtree(variableTreeNode);
    add.AddSubtree(numberTreeNode);
    tree.Root.GetSubtree(0).AddSubtree(add);
    SymbolicRegressionParameterOptimization.OptimizeParameters(problem.Interpreter, tree, problemData, DataAnalysisProblemData.PartitionType.Training, false, 10);
    var y = problem.Evaluate(tree)[0];
    Assert.Equal(0, y, 1.0e-15);
  }

  [Fact]
  public void CreatorsTest() {
    var problemData = new RegressionProblemData(new ModifiableDataset(["x", "y"], Data), ["x"], "y");
    var problem = new SymbolicRegressionProblem(problemData, [new RootMeanSquaredErrorEvaluator()]) {
      LowerPredictionBound = 0,
      UpperPredictionBound = 20,
      SearchSpace = {
        TreeDepth = 10,
        TreeLength = 10
      }
    };
    problem.SearchSpace.Grammar.AddFullyConnectedSymbols([new Addition(), new Subtraction(), new Multiplication(), new Division(), new Number(), new SquareRoot(), new Logarithm(), new Variable { VariableNames = problemData.InputVariables }]);

    var creators = new SymbolicExpressionTreeCreator[] { new BalancedTreeCreator(), new GrowTreeCreator(), new ProbabilisticTreeCreator(), };

    //TODO these creators often create invalid trees (ignore tree length)
    //new FullTreeCreator()
    //new RampedHalfAndHalfTreeCreator() 

    foreach (var c in creators) {
      var tree = c.Create(new SystemRandomNumberGenerator(0), problem.SearchSpace);
      Assert.True(problem.SearchSpace.Contains(tree));
      _ = problem.Evaluate(tree);
    }
  }

  [Fact]
  public void AlgTest() {
    var problemData = new RegressionProblemData(new ModifiableDataset(["x", "y"], Data), ["x"], "y");
    var problem = new SymbolicRegressionProblem(problemData, [new RootMeanSquaredErrorEvaluator()]) {
      LowerPredictionBound = 0,
      UpperPredictionBound = 20,
      SearchSpace = {
        TreeDepth = 40,
        TreeLength = 40
      }
    };
    problem.SearchSpace.Grammar.AddFullyConnectedSymbols([new Addition(), new Subtraction(), new Multiplication(), new Division(), new Number(), new SquareRoot(), new Logarithm(), new Variable { VariableNames = problemData.InputVariables }]);

    var qualities = new BestMedianWorstInterceptor<SymbolicExpressionTree>();
    var ga = AlgorithmFactory.GeneticAlgorithm(100,
      new ProbabilisticTreeCreator(),
      new SubtreeCrossover(),
      MultiMutator.Create([new ChangeNodeTypeManipulation(), new FullTreeShaker(), new OnePointShaker(), new RemoveBranchManipulation(), new ReplaceBranchManipulation()]), //
      0.05,
      new TournamentSelector<SymbolicExpressionTree>(3),
      1,
      0,
      new AfterIterationsTerminator<SymbolicExpressionTree>(100),
      MultiInterceptor.Build([qualities])
    );
    var res = ga.Execute(problem);
  }

  [Fact]
  public void GenealogyGraphTest() {
    var problemData = new RegressionProblemData(new ModifiableDataset(["x", "y"], Data), ["x"], "y");
    var problem = new SymbolicRegressionProblem(problemData, [new RootMeanSquaredErrorEvaluator()]) {
      LowerPredictionBound = 0,
      UpperPredictionBound = 20,
      SearchSpace = {
        TreeDepth = 40,
        TreeLength = 40
      }
    };
    problem.SearchSpace.Grammar.AddFullyConnectedSymbols([new Addition(), new Subtraction(), new Multiplication(), new Division(), new Number(), new SquareRoot(), new Logarithm(), new Variable { VariableNames = problemData.InputVariables }]);

    var qualities = new BestMedianWorstInterceptor<SymbolicExpressionTree>();
    var graph = new GenealogyGraph<SymbolicExpressionTree>(ReferenceEqualityComparer.Instance);
    var graphAnalyzer = graph.GetInterceptor();
    graphAnalyzer.SaveSpace = true;
    var graphCrossover = graph.WrapCrossover(new SubtreeCrossover());
    var graphMutator = graph.WrapMutator(
      MultiMutator.Create(
        [new ChangeNodeTypeManipulation(), new FullTreeShaker(), new OnePointShaker(), new RemoveBranchManipulation(), new ReplaceBranchManipulation()]));

    var values = new List<List<double>>();
    var extractor = FuncInterceptor.Build<SymbolicExpressionTree, PopulationIterationResult<SymbolicExpressionTree>>(
      (_, _) => {
        if (graph.Nodes.Count < 2) return;
        var line = new List<double>();
        values.Add(line);
        line.AddRange(graph.Nodes[^2].Values
                           .Where(x => x.Layer == 0)
                           .OrderBy(x => x.Rank)
                           .Select(node => node.Children.Count > 0
                             ? node.GetAllDescendants()
                                   .Where(x => x.Rank >= 0)
                                   .Average(x => x.Rank)
                             : Double.NaN)
        );
      });

    var ga = AlgorithmFactory.GeneticAlgorithm(10,
      new ProbabilisticTreeCreator(),
      graphCrossover,
      graphMutator, //
      0.05,
      new RandomSelector<SymbolicExpressionTree>(),
      1,
      0,
      new AfterIterationsTerminator<SymbolicExpressionTree>(30),
      MultiInterceptor.Build([qualities, graphAnalyzer, extractor])
    );
    var res = ga.Execute(problem);

    var best = qualities.CurrentState.Select(x => x.best).ToArray();
    var graphViz = graph.ToGraphViz();
  }
}
