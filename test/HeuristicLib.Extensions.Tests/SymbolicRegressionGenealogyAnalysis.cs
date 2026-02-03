using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Analyzers.Genealogy;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionTreeCreators;
using HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionTreeCrossovers;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

namespace HEAL.HeuristicLib.Extensions.Tests;

public class UnitTest1
{
  [Fact]
  public void GeneticAlgorithmExecution()
  {
    var problem = CreateTestSymbolicRegressionProblem();

    var ga = GeneticAlgorithm.GetBuilder(new ProbabilisticTreeCreator(), new SubtreeCrossover(), CreateSymRegAllMutator());
    ga.PopulationSize = 100;
    ga.MutationRate = 0.05;
    ga.Selector = new TournamentSelector<SymbolicExpressionTree>(3);
    ga.Elites = 1;
    //ga.RandomSeed = AlgorithmRandomSeed;
    ga.Terminator = new AfterIterationsTerminator<SymbolicExpressionTree>(100);

    var qualities = BestMedianWorstAnalysis.Analyze(ga);
    var res = ga.Build().RunToCompletion(problem, RandomNumberGenerator.Create(AlgorithmRandomSeed));

    Assert.Equal(100, qualities.BestISolutions.Count);
    Assert.Equal(100, res.Population.Solutions.Count);
  }
  
  
  [Fact]
  public void GenealogyGraphOnGeneticAlgorithm()
  {
    var problem = CreateTestSymbolicRegressionProblem();

    var gens = 100;
    var popsize = 10;
    var ga = GeneticAlgorithm.GetBuilder(new ProbabilisticTreeCreator(), new SubtreeCrossover(), CreateSymRegAllMutator());
    ga.PopulationSize = popsize;
    ga.MutationRate = 0.05;
    ga.Selector = new TournamentSelector<SymbolicExpressionTree>(3);
    ga.Elites = 1;
    //ga.RandomSeed = AlgorithmRandomSeed;
    ga.Terminator = new AfterIterationsTerminator<SymbolicExpressionTree>(gens);

    var evalQualities = QualityCurveAnalysis.Create(ga);
    var qualities = BestMedianWorstAnalysis.Analyze(ga);
    var genealogy = GenealogyAnalysis.Create(ga);
    var res = ga.Build().RunToCompletion(problem, RandomNumberGenerator.Create(AlgorithmRandomSeed));

    Assert.Equal(gens, qualities.BestISolutions.Count);
    Assert.Equal(popsize, res.Population.Solutions.Count);
    var graphViz = genealogy.Graph.ToGraphViz();
    Assert.True(graphViz.Length > 0);
    Assert.Equal(qualities.BestISolutions[^1].Best.ObjectiveVector, evalQualities.CurrentState[^1].best.ObjectiveVector);
  }
  
  [Fact]
  public void GenealogyGraphOnLocalSearch()
  {
    var problem = CreateTestSymbolicRegressionProblem();
    var ga = HillClimber.GetBuilder(new ProbabilisticTreeCreator(), CreateSymRegAllMutator());
    //ga.RandomSeed = AlgorithmRandomSeed;
    ga.Terminator = new AfterIterationsTerminator<SymbolicExpressionTree>(100);

    var genealogy = GenealogyAnalysis.Create(ga);
    var res = ga.Build().RunToCompletion(problem, RandomNumberGenerator.Create(AlgorithmRandomSeed));
    Assert.Single(res.Population.Solutions);
    var graphViz = genealogy.Graph.ToGraphViz();
    Assert.True(graphViz.Length > 0);
  }

  [Fact]
  public void GenealogyGraphOnNSGA2()
  {
    var problem = CreateTestSymbolicRegressionProblem(multiObjective: true);
    var symRegAllMutator = CreateSymRegAllMutator();
    const int populationSize = 10;
    const int maximumIterations = 50;
    const double mutationRate = 0.05;
    var nsga2 = NSGA2.GetBuilder(
    new ProbabilisticTreeCreator(),
    new SubtreeCrossover(),
    symRegAllMutator);
    nsga2.PopulationSize = populationSize;
    nsga2.Terminator = new AfterIterationsTerminator<SymbolicExpressionTree>(maximumIterations);
    nsga2.MutationRate = mutationRate;

    QualityCurveAnalysis.Create(nsga2);
    var genealogy = GenealogyAnalysis.Create(nsga2);
    var qualities = BestMedianWorstAnalysis.Analyze(nsga2);

    var res = nsga2.Build().RunToCompletion(problem, RandomNumberGenerator.Create(AlgorithmRandomSeed));
    Assert.Equal(maximumIterations, qualities.BestISolutions.Count);
    Assert.Equal(populationSize, res.Population.Solutions.Count);
    var graphViz = genealogy.Graph.ToGraphViz();
    Assert.True(graphViz.Length > 0);
  }
  
  private const int AlgorithmRandomSeed = 42;
  
  public static readonly double[,] Data = new double[,] { { 0, 10 }, { 1, 10 }, { 2, 10 }, { 3, 10 }, { 4, 10 }, { 5, 10 }, { 6, 10 }, { 7, 10 }, { 8, 10 }, { 9, 10 }, { 10, 10 } };
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
}
