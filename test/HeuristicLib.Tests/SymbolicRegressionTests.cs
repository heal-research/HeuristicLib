using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Grammars;
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
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.PythonInterOptScripts;

namespace HEAL.HeuristicLib.Tests;

public class SymbolicRegressionTests {
  public static readonly double[,] Data = new double[,] { { 0, 10 }, { 1, 10 }, { 2, 10 }, { 3, 10 }, { 4, 10 }, { 5, 10 }, { 6, 10 }, { 7, 10 }, { 8, 10 }, { 9, 10 }, { 10, 10 } };
  private const int AlgorithmRandomSeed = 42;

  [Fact]
  public void MultiObjectiveConstant() {
    var problem = CreateTestSymbolicRegressionProblem(multiObjective: true);
    var r = new SystemRandomNumberGenerator();
    var tree = problem.SearchSpace.Grammar.MakeStump(r);
    var numberNode = new Number().CreateTreeNode(10);
    tree.Root[0].AddSubtree(numberNode);
    var res = problem.Evaluate(tree);
    Assert.Equal([0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 3], res);
    const ObjectiveDirection min = ObjectiveDirection.Minimize;
    const ObjectiveDirection max = ObjectiveDirection.Maximize;
    Assert.Equal([min, min, min, min, min, min, min, max, min, min, min], problem.Objective.Directions);
  }

  [Fact]
  public void Constant() {
    var problem = CreateTestSymbolicRegressionProblem();
    var r = new SystemRandomNumberGenerator();
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

  [Fact]
  public void Variable() {
    var problem = CreateTestSymbolicRegressionProblem();
    var r = new SystemRandomNumberGenerator();
    var tree = problem.SearchSpace.Grammar.MakeStump(r);
    tree.Root[0].AddSubtree(new Variable().CreateTreeNode("x", 1));
    var y = problem.Evaluate(tree)[0];
    Assert.Equal(Math.Sqrt(66), y, 1.0e-15);
  }

  [Fact]
  public void Add() {
    var problem = CreateTestSymbolicRegressionProblem();
    var r = new SystemRandomNumberGenerator();
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

  [Fact]
  public void AddOpt() {
    var problem = CreateTestSymbolicRegressionProblem();
    var r = new SystemRandomNumberGenerator();
    var tree = problem.SearchSpace.Grammar.MakeStump(r);
    var variableTreeNode = new Variable().CreateTreeNode("x", 1);
    var numberTreeNode = new Number().CreateTreeNode(1);
    var add = new SymbolicExpressionTreeNode(new Addition());
    add.AddSubtree(variableTreeNode);
    add.AddSubtree(numberTreeNode);
    tree.Root[0].AddSubtree(add);
    SymbolicRegressionParameterOptimization.OptimizeParameters(problem.Interpreter, tree, problem.ProblemData, DataAnalysisProblemData.PartitionType.Training, 10);
    var y = problem.Evaluate(tree)[0];
    Assert.Equal(0, y, 1.0e-15);
  }

  [Fact]
  public void Creators() {
    var problem = CreateTestSymbolicRegressionProblem();
    var creators = new SymbolicExpressionTreeCreator[] { new BalancedTreeCreator(), new GrowTreeCreator(), new ProbabilisticTreeCreator(), };
    //TODO these creators often create invalid trees (ignore tree length)
    //new FullTreeCreator()
    //new RampedHalfAndHalfTreeCreator() 
    foreach (var c in creators) {
      var tree = c.Create(new SystemRandomNumberGenerator(AlgorithmRandomSeed), problem.SearchSpace);
      Assert.True(problem.SearchSpace.Contains(tree));
      _ = problem.Evaluate(tree);
    }
  }

  [Fact]
  public void GeneticAlgorithmExecution() {
    var problem = CreateTestSymbolicRegressionProblem();

    var qualities = new BestMedianWorstAnalyzer<SymbolicExpressionTree>();

    var ga = AlgorithmFactory.GeneticAlgorithm(100,
      new ProbabilisticTreeCreator(),
      new SubtreeCrossover(),
      CreateSymRegAllMutator(), //
      0.05,
      new TournamentSelector<SymbolicExpressionTree>(3),
      problem.CreateEvaluator(),
      1,
      AlgorithmRandomSeed,
      new AfterIterationsTerminator<SymbolicExpressionTree>(100),
      null,
      qualities
    );

    var res = ga.Execute(problem);

    Assert.Equal(100, qualities.CurrentState.Count);
    Assert.Equal(100, res.Population.Solutions.Count);
  }

  [Fact]
  public void GenealogyGraphOnGeneticAlgorithm() {
    var problem = CreateTestSymbolicRegressionProblem();
    var symRegAllMutator = CreateSymRegAllMutator();
    var subtreeCrossover = new SubtreeCrossover();

    var qualities = new BestMedianWorstAnalyzer<SymbolicExpressionTree>();
    var graph = AddGenealogyGraph(subtreeCrossover, symRegAllMutator, out var graphAnalyzer, out var graphCrossover, out var graphMutator);
    var evalQualities = AddQualityCurveTracking(problem.CreateEvaluator(), out var evaluator);

    const int populationSize = 10;
    const double mutationRate = 0.05;
    const int maximumIterations = 30;
    var ga = AlgorithmFactory.GeneticAlgorithm(populationSize,
      new ProbabilisticTreeCreator(),
      graphCrossover,
      graphMutator,
      mutationRate,
      new RandomSelector<SymbolicExpressionTree>(),
      evaluator,
      1,
      AlgorithmRandomSeed,
      new AfterIterationsTerminator<SymbolicExpressionTree>(maximumIterations), null,
      qualities,
      graphAnalyzer
    );
    var res = ga.Execute(problem);

    Assert.Equal(maximumIterations, qualities.CurrentState.Count);
    Assert.Equal(populationSize, res.Population.Solutions.Count);
    var graphViz = graph.ToGraphViz();
    Assert.True(graphViz.Length > 0);
    Assert.Equal(qualities.CurrentState[^1].best.ObjectiveVector, evalQualities.CurrentState[^1].best.ObjectiveVector);
  }

  private static QualityCurveTracker<T> AddQualityCurveTracking<T, T1, T2>(IEvaluator<T, T1, T2> problem, out QualityCurveEvaluationWrapper<T, T1, T2> trackingProblem) where T1 : class, IEncoding<T> where T2 : IProblem<T, T1> {
    var evalQualities = new QualityCurveTracker<T>();
    trackingProblem = evalQualities.WrapEvaluator(problem);
    return evalQualities;
  }

  [Fact]
  public void GenealogyGraphOnLocalSearch() {
    var problem = CreateTestSymbolicRegressionProblem();
    var symRegAllMutator = CreateSymRegAllMutator();
    var qualities = new BestMedianWorstAnalyzer<SymbolicExpressionTree>();

    var graph = AddGenealogyGraph(new SubtreeCrossover(), symRegAllMutator, out var graphAnalyzer, out _, out var graphMutator);
    var evalQualities = AddQualityCurveTracking(problem.CreateEvaluator(), out var evaluator);

    var ls = AlgorithmFactory.LocalSearch(
      new ProbabilisticTreeCreator(),
      graphMutator,
      new AfterIterationsTerminator<SymbolicExpressionTree>(50),
      evaluator,
      AlgorithmRandomSeed,
      10,
      5,
      LocalSearchDirection.FirstImprovement,
      null,
      qualities,
      graphAnalyzer);

    var res = ls.Execute(problem);
    Assert.Single(res.Population.Solutions);
    var graphViz = graph.ToGraphViz();
    Assert.True(graphViz.Length > 0);
  }

  [Fact]
  public void GenealogyGraphOnNSGA2() {
    var problem = CreateTestSymbolicRegressionProblem(multiObjective: true);
    var symRegAllMutator = CreateSymRegAllMutator();
    var qualities = new BestMedianWorstAnalyzer<SymbolicExpressionTree>();
    var graph = AddGenealogyGraph(new SubtreeCrossover(), symRegAllMutator, out var graphAnalyzer, out var crossover, out var graphMutator);
    var evalQualities = AddQualityCurveTracking(problem.CreateEvaluator(), out var evaluator);

    const int populationSize = 10;
    const int maximumIterations = 50;
    const double mutationRate = 0.05;
    var ls = AlgorithmFactory.NSGA2(
      new ProbabilisticTreeCreator(),
      crossover,
      graphMutator,
      new RandomSelector<SymbolicExpressionTree>(),
      new AfterIterationsTerminator<SymbolicExpressionTree>(maximumIterations),
      evaluator,
      AlgorithmRandomSeed,
      populationSize,
      mutationRate,
      true,
      null,
      qualities,
      graphAnalyzer);

    var res = ls.Execute(problem);
    Assert.Equal(maximumIterations, qualities.CurrentState.Count);
    Assert.Equal(populationSize, res.Population.Solutions.Count);
    var graphViz = graph.ToGraphViz();
    Assert.True(graphViz.Length > 0);
  }

  [Fact]
  public void TestPlayground() {
    const int iterations = 4;
    var bp = new GenealogyAnalysis();
    var i = 0;
    bp.GenealogyGraphGeneticAlgorithm("TestData\\192_vineyard.tsv", _ => i++, AlgorithmRandomSeed, 10, iterations);
    Assert.Equal(iterations, i);
  }

  private static SymbolicRegressionProblem CreateTestSymbolicRegressionProblem(int treeLength = 40, bool multiObjective = false) {
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
      }
    };

    var linearScalingRoot = problem.SearchSpace.Grammar.AddLinearScaling();
    problem.SearchSpace.Grammar.AddFullyConnectedSymbols(
      linearScalingRoot,
      new Addition(),
      new Subtraction(),
      new Multiplication(),
      new Division(),
      new Number(),
      new SquareRoot(),
      new Logarithm(),
      new Exponential(),
      new Variable { VariableNames = problemData.InputVariables });
    return problem;
  }

  private static GenealogyGraph<SymbolicExpressionTree> AddGenealogyGraph(SubtreeCrossover subtreeCrossover, MultiMutator<SymbolicExpressionTree, SymbolicExpressionTreeEncoding, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>> symRegAllMutator, out GenealogyGraphAnalyzer<SymbolicExpressionTree> graphAnalyzer, out GenealogyGraphCrossover<SymbolicExpressionTree, SymbolicExpressionTreeEncoding, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>> graphCrossover, out GenealogyGraphMutator<SymbolicExpressionTree, SymbolicExpressionTreeEncoding, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>> graphMutator) {
    var graph = new GenealogyGraph<SymbolicExpressionTree>(ReferenceEqualityComparer.Instance);
    graphAnalyzer = graph.GetInterceptor();
    graphAnalyzer.SaveSpace = false;
    graphCrossover = graph.WrapCrossover(subtreeCrossover);
    graphMutator = graph.WrapMutator(symRegAllMutator);
    return graph;
  }

  private static MultiMutator<SymbolicExpressionTree, SymbolicExpressionTreeEncoding, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>> CreateSymRegAllMutator() {
    return MultiMutator.Create(
      new ChangeNodeTypeManipulation(),
      new FullTreeShaker(),
      new OnePointShaker(),
      new RemoveBranchManipulation(),
      new ReplaceBranchManipulation());
  }
}
