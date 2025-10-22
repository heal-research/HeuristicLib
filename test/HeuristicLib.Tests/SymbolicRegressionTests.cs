using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.SymbolicExpression.Creators;
using HEAL.HeuristicLib.Operators.SymbolicExpression.Crossovers;
using HEAL.HeuristicLib.Operators.SymbolicExpression.Mutators;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Tests;

public class SymbolicRegressionTests {
  public static readonly double[,] Data = new double[,] { { 0, 10 }, { 1, 10 }, { 2, 10 }, { 3, 10 }, { 4, 10 }, { 5, 10 }, { 6, 10 }, { 7, 10 }, { 8, 10 }, { 9, 10 }, { 10, 10 } };

  [Fact]
  public void ConstantTest() {
    var problem = CreateTestSymbolicRegressionProblem();

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
    var problem = CreateTestSymbolicRegressionProblem();

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
    var problem = CreateTestSymbolicRegressionProblem();

    var r = new SystemRandomNumberGenerator();
    var tree = problem.SearchSpace.Grammar.MakeStump(r);

    var variableTreeNode = new VariableTreeNode(new Variable()) {
      NodeWeight = 1,
      VariableName = "x",
      Weight = 1
    };
    var numberTreeNode = new NumberTreeNode(new Number()) { Value = 1 };
    var add = new Addition().CreateTreeNode(); //  
    add.AddSubtree(variableTreeNode);
    add.AddSubtree(numberTreeNode);
    tree.Root.GetSubtree(0).AddSubtree(add);

    var y = problem.Evaluate(tree)[0];
    Assert.Equal(Math.Sqrt(51), y, 1.0e-15);
  }

  [Fact]
  public void AddOptTest() {
    var problem = CreateTestSymbolicRegressionProblem();
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
    SymbolicRegressionParameterOptimization.OptimizeParameters(problem.Interpreter, tree, problem.ProblemData, DataAnalysisProblemData.PartitionType.Training, false, 10);
    var y = problem.Evaluate(tree)[0];
    Assert.Equal(0, y, 1.0e-15);
  }

  [Fact]
  public void CreatorsTest() {
    var problem = CreateTestSymbolicRegressionProblem();
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
  public void GeneticAlgorithmTest() {
    var problem = CreateTestSymbolicRegressionProblem();

    var qualities = new BestMedianWorstAnalyzer<SymbolicExpressionTree>();

    var ga = AlgorithmFactory.GeneticAlgorithm(100,
      new ProbabilisticTreeCreator(),
      new SubtreeCrossover(),
      CreateSymRegAllMutator(), //
      0.05,
      new TournamentSelector<SymbolicExpressionTree>(3),
      1,
      0,
      new AfterIterationsTerminator<SymbolicExpressionTree>(100),
      null,
      qualities
    );

    var res = ga.Execute(problem);

    Assert.Equal(100, qualities.CurrentState.Count);
    Assert.Equal(100, res.Population.Solutions.Count);
  }

  [Fact]
  public void GenealogyGraphTestGeneticAlgorithm() {
    var problem = CreateTestSymbolicRegressionProblem();
    var qualities = new BestMedianWorstAnalyzer<SymbolicExpressionTree>();
    var graph = new GenealogyGraph<SymbolicExpressionTree>(ReferenceEqualityComparer.Instance);
    var graphAnalyzer = graph.GetInterceptor();
    graphAnalyzer.SaveSpace = true;
    var graphCrossover = graph.WrapCrossover(new SubtreeCrossover());
    var graphMutator = graph.WrapMutator(CreateSymRegAllMutator());

    var ga = AlgorithmFactory.GeneticAlgorithm(10,
      new ProbabilisticTreeCreator(),
      graphCrossover,
      graphMutator, //
      0.05,
      new RandomSelector<SymbolicExpressionTree>(),
      1,
      0,
      new AfterIterationsTerminator<SymbolicExpressionTree>(30), null,
      qualities,
      graphAnalyzer
    );
    var res = ga.Execute(problem);
    Assert.Equal(30, qualities.CurrentState.Count);
    Assert.Equal(10, res.Population.Solutions.Count);

    _ = qualities.CurrentState.Select(x => x.best).ToArray();
    var graphViz = graph.ToGraphViz();
  }

  [Fact]
  public void TestPlayground() {
    var iterations = 4;
    var bp = new BernhardPlayground();
    var i = 0;
    bp.GenealogyGraphGeneticAlgorithm("D:\\Projekte\\HCAI\\HL\\datasets\\extracted - Kopie\\192_vineyard.tsv", _ => i++, 10, iterations);
    Assert.Equal(iterations, i);
  }

  [Fact]
  public void GenealogyGraphTestLocalSearch() {
    var problem = CreateTestSymbolicRegressionProblem();
    var qualities = new BestMedianWorstAnalyzer<SymbolicExpressionTree>();
    var graph = new GenealogyGraph<SymbolicExpressionTree>(ReferenceEqualityComparer.Instance);
    var graphAnalyzer = graph.GetInterceptor();
    graphAnalyzer.SaveSpace = false;
    var symRegAllMutator = CreateSymRegAllMutator();
    var graphMutator = graph.WrapMutator(symRegAllMutator);
    var ls = AlgorithmFactory.LocalSearch(
      new ProbabilisticTreeCreator(),
      graphMutator,
      new AfterIterationsTerminator<SymbolicExpressionTree>(50),
      0,
      10,
      5,
      LocalSearchDirection.FirstImprovement,
      null,
      qualities,
      graphAnalyzer);

    var res = ls.Execute(problem);
    Assert.Single(res.Population.Solutions);
    _ = qualities.CurrentState.Select(x => x.best).ToArray();
    var graphViz = graph.ToGraphViz();
  }

  private static SymbolicRegressionProblem CreateTestSymbolicRegressionProblem() {
    var problemData = new RegressionProblemData(new ModifiableDataset(["x", "y"], Data), ["x"], "y");
    var problem = new SymbolicRegressionProblem(problemData, [new RootMeanSquaredErrorEvaluator()]) {
      LowerPredictionBound = 0,
      UpperPredictionBound = 100,
      SearchSpace = {
        TreeDepth = 40,
        TreeLength = 40
      }
    };

    var aroot = problem.SearchSpace.Grammar.AddLinearScaling();
    problem.SearchSpace.Grammar.AddFullyConnectedSymbols([new Addition(), new Subtraction(), new Multiplication(), new Division(), new Number(), new SquareRoot(), new Logarithm(), new Variable { VariableNames = problemData.InputVariables }], aroot);
    return problem;
  }

  private static MultiMutator<SymbolicExpressionTree, SymbolicExpressionTreeEncoding, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>> CreateSymRegAllMutator() {
    var symRegAllMutator = MultiMutator.Create(
      new ChangeNodeTypeManipulation(),
      new FullTreeShaker(),
      new OnePointShaker(),
      new RemoveBranchManipulation(),
      new ReplaceBranchManipulation());
    return symRegAllMutator;
  }
}
