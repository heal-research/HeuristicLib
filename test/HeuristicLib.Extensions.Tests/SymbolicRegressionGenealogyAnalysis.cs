using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.GenealogyAnalysis;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionTreeCreators;
using HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionTreeCrossovers;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionTreeMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.SearchSpaces.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.States;
using RandomNumberGenerator = HEAL.HeuristicLib.Random.RandomNumberGenerator;

namespace HEAL.HeuristicLib.Extensions.Tests;

public class GenealogyGraphTests
{
  private static ChooseOneMutator<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>> CreateSymRegAllMutator()
  {
    var symRegAllMutator = ChooseOneMutator.Create(
      new ChangeNodeTypeManipulation(),
      new FullTreeShaker(),
      new OnePointShaker(),
      new RemoveBranchManipulation(),
      new ReplaceBranchManipulation()
    );

    return symRegAllMutator;
  }

  //[Fact(Skip = "Currently disabled because the LevenbergMarquardtMinimizer seems to loop endlessly in this test.")]
  [Fact(Explicit = true)]
  public void GeneticAlgorithmExecution()
  {
    var problem = CreateTestSymbolicRegressionProblem();

    var builder = GeneticAlgorithm.GetBuilder(new ProbabilisticTreeCreator(), new SubtreeCrossover(), CreateSymRegAllMutator());
    builder.PopulationSize = 100;
    builder.MutationRate = 0.05;
    builder.Selector = new TournamentSelector<SymbolicExpressionTree>(3);
    builder.Elites = 1;
    //ga.RandomSeed = AlgorithmRandomSeed;
    //builder.Terminator = new AfterIterationsTerminator<SymbolicExpressionTree>(100);
    var ga = builder.Build();
    var interceptor = ga.Interceptor ?? new IdentityInterceptor<SymbolicExpressionTree, PopulationState<SymbolicExpressionTree>>();
    ga = ga with { Interceptor = interceptor };

    var analysis = new BestMedianWorstAnalysis<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>, PopulationState<SymbolicExpressionTree>>(ga, ga.Interceptor!);

    var run = ga.WithMaxIterations(100).CreateRun(problem, analysis);
    var res = run.RunToCompletion(RandomNumberGenerator.Create(AlgorithmRandomSeed), cancellationToken: TestContext.Current.CancellationToken);
    var ares = run.GetAnalyzerResult(analysis).State;

    Assert.Equal(100, ares.Count);
    Assert.Equal(100, res.Population.Solutions.Count());
  }

  [Fact(Explicit = true)]
  public void GenealogyGraphOnGeneticAlgorithm()
  {
    var problem = CreateTestSymbolicRegressionProblem();

    const int gens = 100;
    const int popsize = 10;
    var ga = GeneticAlgorithm.GetBuilder(new ProbabilisticTreeCreator(), new SubtreeCrossover(), CreateSymRegAllMutator());
    ga.PopulationSize = popsize;
    ga.MutationRate = 0.05;
    ga.Selector = new TournamentSelector<SymbolicExpressionTree>(3);
    ga.Elites = 1;
    //ga.Terminator = new AfterIterationsTerminator<SymbolicExpressionTree>(gens);

    var algorithm = ga.Build();
    var interceptor = algorithm.Interceptor ?? new IdentityInterceptor<SymbolicExpressionTree, PopulationState<SymbolicExpressionTree>>();
    algorithm = algorithm with { Interceptor = interceptor };

    var evalQualities = new QualityCurveAnalysis<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>, PopulationState<SymbolicExpressionTree>>(algorithm, algorithm.Evaluator);
    var qualities = new BestMedianWorstAnalysis<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>, PopulationState<SymbolicExpressionTree>>(algorithm, algorithm.Interceptor!);
    var genealogyAnalysis = new GenealogyAnalysis<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>, PopulationState<SymbolicExpressionTree>>(algorithm, algorithm.Crossover, algorithm.Mutator, algorithm.Interceptor);

    var run = algorithm.WithMaxIterations(gens).CreateRun(problem, evalQualities, qualities, genealogyAnalysis);
    var res = run.RunToCompletion(RandomNumberGenerator.Create(AlgorithmRandomSeed), null, CancellationToken.None);

    var qres = run.GetAnalyzerResult(qualities).State;
    var eres = run.GetAnalyzerResult(evalQualities).State;
    var gres = run.GetAnalyzerResult(genealogyAnalysis).State;

    Assert.Equal(gens, qres.Count);
    Assert.Equal(popsize, res.Population.Solutions.Length);
    var graphViz = gres.ToGraphViz();
    Assert.True(graphViz.Length > 0);
    Assert.Equal(qres[^1].Best.ObjectiveVector, eres.CurrentState[^1].best.ObjectiveVector);
  }

  [Fact(Explicit = true, Skip = "Currently disabled because the LevenbergMarquardtMinimizer seems to loop endlessly in this test.")]
  public void GenealogyGraphOnLocalSearch()
  {
    var problem = CreateTestSymbolicRegressionProblem();
    var builder = HillClimber.GetBuilder(new ProbabilisticTreeCreator(), CreateSymRegAllMutator());
    var algorithm = builder.Build();
    var interceptor = algorithm.Interceptor ?? new IdentityInterceptor<SymbolicExpressionTree, SingleSolutionState<SymbolicExpressionTree>>();
    algorithm = algorithm with { Interceptor = interceptor };
    var genealogy = new GenealogyAnalysis<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>, SingleSolutionState<SymbolicExpressionTree>>(
      algorithm, mutator: algorithm.Mutator, interceptor: algorithm.Interceptor);
    var run = algorithm.WithMaxIterations(100).CreateRun(problem, genealogy);
    var res = run.RunToCompletion(RandomNumberGenerator.Create(AlgorithmRandomSeed), null, CancellationToken.None);
    var gres = run.GetAnalyzerResult(genealogy).State;
    Assert.Single(res.Population.Solutions);
    var graphViz = gres.ToGraphViz();
    Assert.True(graphViz.Length > 0);
  }

  [Fact(Explicit = true)]
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
    nsga2.MutationRate = mutationRate;

    var algorithm = nsga2.Build();
    var interceptor = algorithm.Interceptor ?? new IdentityInterceptor<SymbolicExpressionTree, PopulationState<SymbolicExpressionTree>>();
    algorithm = algorithm with { Interceptor = interceptor };

    var genealogy = new GenealogyAnalysis<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>, PopulationState<SymbolicExpressionTree>>(algorithm, algorithm.Crossover, algorithm.Mutator, algorithm.Interceptor);
    var qualities = new BestMedianWorstAnalysis<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>, PopulationState<SymbolicExpressionTree>>(algorithm, algorithm.Interceptor!);

    var run = algorithm.WithMaxIterations(maximumIterations).CreateRun(problem, genealogy, qualities);
    var res = run.RunToCompletion(RandomNumberGenerator.Create(AlgorithmRandomSeed), cancellationToken: TestContext.Current.CancellationToken);
    var gres = run.GetAnalyzerResult(genealogy).State;
    var qres = run.GetAnalyzerResult(qualities).State;

    Assert.Equal(maximumIterations, qres.Count);
    Assert.Equal(populationSize, res.Population.Solutions.Length);
    var graphViz = gres.ToGraphViz();
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

    problem.SearchSpace.Grammar.AddFullyConnectedSymbols(
      linearScalingRoot, symbols);
    return problem;
  }
}
