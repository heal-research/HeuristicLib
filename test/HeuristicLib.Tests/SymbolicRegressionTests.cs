using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.NSGA2;
using HEAL.HeuristicLib.Encodings.Trees;
using HEAL.HeuristicLib.Encodings.Trees.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.Encodings.Trees.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Operators.Analyzer.Genealogy;
using HEAL.HeuristicLib.Operators.Creator.SymbolicExpressionTrees;
using HEAL.HeuristicLib.Operators.Crossover.SymbolicExpressionTrees;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Mutator.SymbolicExpressionTrees;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Operators.Terminator;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Random;
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
    var r = new SystemRandomNumberGenerator(AlgorithmRandomSeed);
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
    var problem = CreateTestSymbolicRegressionProblem(constOptIteration: -1);
    var r = new SystemRandomNumberGenerator(AlgorithmRandomSeed);
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
    var problem = CreateTestSymbolicRegressionProblem(constOptIteration: -1);
    var r = new SystemRandomNumberGenerator(AlgorithmRandomSeed);
    var tree = problem.SearchSpace.Grammar.MakeStump(r);
    tree.Root[0].AddSubtree(new Variable().CreateTreeNode("x", 1));
    var y = problem.Evaluate(tree)[0];
    Assert.Equal(Math.Sqrt(66), y, 1.0e-15);
  }

  [Fact]
  public void Add() {
    var problem = CreateTestSymbolicRegressionProblem(constOptIteration: -1);
    var r = new SystemRandomNumberGenerator(AlgorithmRandomSeed);
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
    var r = new SystemRandomNumberGenerator(AlgorithmRandomSeed);
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
    Assert.Equal(0, y, 1.0e-15);
  }

  [Fact]
  public void Creators() {
    var problem = CreateTestSymbolicRegressionProblem(treeLength: 12);
    var creators = new SymbolicExpressionTreeCreator[] { new BalancedTreeCreator(), new ProbabilisticTreeCreator(), };
    var r = new SystemRandomNumberGenerator(AlgorithmRandomSeed);

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

  [Fact]
  public void GeneticAlgorithmExecution() {
    var problem = CreateTestSymbolicRegressionProblem();

    var ga = GeneticAlgorithm.GetBuilder(new ProbabilisticTreeCreator(), new SubtreeCrossover(), CreateSymRegAllMutator());
    ga.PopulationSize = 100;
    ga.MutationRate = 0.05;
    ga.Selector = new TournamentSelector<SymbolicExpressionTree>(3);
    ga.Elites = 1;
    ga.RandomSeed = AlgorithmRandomSeed;
    ga.Terminator = new AfterIterationsTerminator<SymbolicExpressionTree>(100);

    var qualities = BestMedianWorstAnalysis.Analyze(ga);
    var res = ga.Execute(problem);

    Assert.Equal(100, qualities.BestISolutions.Count);
    Assert.Equal(100, res.Population.Solutions.Count);
  }

  [Fact]
  public void GenealogyGraphOnGeneticAlgorithm() {
    var problem = CreateTestSymbolicRegressionProblem();

    var gens = 100;
    var popsize = 10;
    var ga = GeneticAlgorithm.GetBuilder(new ProbabilisticTreeCreator(), new SubtreeCrossover(), CreateSymRegAllMutator());
    ga.PopulationSize = popsize;
    ga.MutationRate = 0.05;
    ga.Selector = new TournamentSelector<SymbolicExpressionTree>(3);
    ga.Elites = 1;
    ga.RandomSeed = AlgorithmRandomSeed;
    ga.Terminator = new AfterIterationsTerminator<SymbolicExpressionTree>(gens);

    var evalQualities = QualityCurveAnalysis.Create(ga);
    var qualities = BestMedianWorstAnalysis.Analyze(ga);
    var genealogy = GenealogyAnalysis.Create(ga);
    var res = ga.Execute(problem);

    Assert.Equal(gens, qualities.BestISolutions.Count);
    Assert.Equal(popsize, res.Population.Solutions.Count);
    var graphViz = genealogy.Graph.ToGraphViz();
    Assert.True(graphViz.Length > 0);
    Assert.Equal(qualities.BestISolutions[^1].Best.ObjectiveVector, evalQualities.CurrentState[^1].best.ObjectiveVector);
  }

  [Fact]
  public void GenealogyGraphOnLocalSearch() {
    var problem = CreateTestSymbolicRegressionProblem();
    var ga = LocalSearch.GetBuilder(new ProbabilisticTreeCreator(), CreateSymRegAllMutator());
    ga.RandomSeed = AlgorithmRandomSeed;
    ga.Terminator = new AfterIterationsTerminator<SymbolicExpressionTree>(100);

    var genealogy = GenealogyAnalysis.Create(ga);
    var res = ga.Execute(problem);
    Assert.Single(res.Population.Solutions);
    var graphViz = genealogy.Graph.ToGraphViz();
    Assert.True(graphViz.Length > 0);
  }

  [Fact]
  public void GenealogyGraphOnNSGA2() {
    var problem = CreateTestSymbolicRegressionProblem(multiObjective: true);
    var symRegAllMutator = CreateSymRegAllMutator();
    const int populationSize = 10;
    const int maximumIterations = 50;
    const double mutationRate = 0.05;
    var nsga2 = Nsga2.GetBuilder(
      new ProbabilisticTreeCreator(),
      new SubtreeCrossover(),
      symRegAllMutator);
    nsga2.PopulationSize = populationSize;
    nsga2.Terminator = new AfterIterationsTerminator<SymbolicExpressionTree>(maximumIterations);
    nsga2.MutationRate = mutationRate;

    QualityCurveAnalysis.Create(nsga2);
    var genealogy = GenealogyAnalysis.Create(nsga2);
    var qualities = BestMedianWorstAnalysis.Analyze(nsga2);

    var res = nsga2.Execute(problem);
    Assert.Equal(maximumIterations, qualities.BestISolutions.Count);
    Assert.Equal(populationSize, res.Population.Solutions.Count);
    var graphViz = genealogy.Graph.ToGraphViz();
    Assert.True(graphViz.Length > 0);
  }

  [Fact]
  public void TestPlayground() {
    const int iterations = 200;
    var i = 0;
    var file = @"TestData\192_vineyard.tsv";
    var res = PythonGenealogyAnalysis.RunSymbolicRegressionConfigurable(file,
      new PythonGenealogyAnalysis.SymRegExperimentParameters {
        Seed = AlgorithmRandomSeed,
        Iterations = iterations
      },
      callback: _ => i++);
    Assert.Equal(iterations, i);
  }

  [Fact]
  public void TestPlayground2() {
    const int iterations = 4;
    var i = 0;
    PythonCorrelationAnalysis.RunCorrelationNsga2((_, _) => { i++; }, iterations, 100);
    Assert.Equal(iterations, i);
  }

  private static SymbolicRegressionProblem CreateTestSymbolicRegressionProblem(int treeLength = 40, bool multiObjective = false, int constOptIteration = 5) {
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
      ParameterOptimizationIterations = constOptIteration
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

  private static MultiMutator<SymbolicExpressionTree, SymbolicExpressionTreeEncoding, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeEncoding>> CreateSymRegAllMutator() {
    return MultiMutator.Create(
      new ChangeNodeTypeManipulation(),
      new FullTreeShaker(),
      new OnePointShaker(),
      new RemoveBranchManipulation(),
      new ReplaceBranchManipulation());
  }
}
