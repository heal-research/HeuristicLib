using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Analyzers;
using HEAL.HeuristicLib.Execution;
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
  private static MultiMutator<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>> CreateSymRegAllMutator()
  {
    var symRegAllMutator = MultiMutator.Create(
      new ChangeNodeTypeManipulation(),
      new FullTreeShaker(),
      new OnePointShaker(),
      new RemoveBranchManipulation(),
      new ReplaceBranchManipulation()
    );

    return symRegAllMutator;
  }

  [Fact]
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

    //var qualities = BestMedianWorstAnalysis<>.Analyze(ga);
    var analysis = new BestMedianWorstAnalysis<SymbolicExpressionTree>();
    var interceptor = ga.Interceptor ?? new IdentityInterceptor<SymbolicExpressionTree, PopulationState<SymbolicExpressionTree>>();

    var wrappedInterceptor = interceptor.ObserveWith(analysis);

    ga = ga with { Interceptor = wrappedInterceptor };

    var algorithmInstance = ga.WithMaxIterations(100).CreateExecutionInstance(out var registry);
    var res = algorithmInstance.RunToCompletion(problem, RandomNumberGenerator.Create(AlgorithmRandomSeed), ct: TestContext.Current.CancellationToken);
    var ares = (BestMedianWorstAnalysis<SymbolicExpressionTree>.Instance)analysis.RetrieveAnalysis(registry);

    Assert.Equal(100, ares.BestSolutions.Count);
    Assert.Equal(100, res.Population.Solutions.Count());
  }

  [Fact]
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

    var evalQualities = new QualityCurveAnalysis<SymbolicExpressionTree>();
    var qualities = new BestMedianWorstAnalysis<SymbolicExpressionTree>();
    ga.AttachObserver(evalQualities);
    ga.AttachObserver(qualities);
    var genealogyAnalysis = new GenealogyAnalysis<SymbolicExpressionTree>();
    ga.AttachObserver(genealogyAnalysis);

    var res = ga.Build()
                .WithMaxIterations(gens)
                .CreateExecutionInstance(out var registry)
                .RunToCompletion(problem, RandomNumberGenerator.Create(AlgorithmRandomSeed), null, CancellationToken.None);

    var qres = (BestMedianWorstAnalysis<SymbolicExpressionTree>.Instance)qualities.RetrieveAnalysis(registry);
    var eres = (QualityCurveAnalysis<SymbolicExpressionTree>.Instance)evalQualities.RetrieveAnalysis(registry);
    var gres = (GenealogyAnalysis<SymbolicExpressionTree>.Instance)genealogyAnalysis.RetrieveAnalysis(registry);

    Assert.Equal(gens, qres.BestSolutions.Count);
    Assert.Equal(popsize, res.Population.Solutions.Length);
    var graphViz = gres.Graph.ToGraphViz();
    Assert.True(graphViz.Length > 0);
    Assert.Equal(qres.BestSolutions[^1].Best.ObjectiveVector, eres.CurrentState[^1].best.ObjectiveVector);
  }

  [Fact]
  public void GenealogyGraphOnLocalSearch()
  {
    var problem = CreateTestSymbolicRegressionProblem();
    var ga = HillClimber.GetBuilder(new ProbabilisticTreeCreator(), CreateSymRegAllMutator());
    //ga.RandomSeed = AlgorithmRandomSeed;
    var genealogy = new GenealogyAnalysis<SymbolicExpressionTree>();
    ga.AttachObserver(genealogy);
    var ai = ga.Build().WithMaxIterations(100).CreateExecutionInstance(out var registry);
    var res = ai.RunToCompletion(problem, RandomNumberGenerator.Create(AlgorithmRandomSeed), null, CancellationToken.None);
    var gres = (GenealogyAnalysis<SymbolicExpressionTree>.Instance)genealogy.RetrieveAnalysis(registry);
    Assert.Single(res.Population.Solutions);
    var graphViz = gres.Graph.ToGraphViz();
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
    nsga2.MutationRate = mutationRate;

    var genealogy = new GenealogyAnalysis<SymbolicExpressionTree>();
    nsga2.AttachObserver(genealogy);
    var qualities = new BestMedianWorstAnalysis<SymbolicExpressionTree>();
    nsga2.AttachObserver(qualities);

    var res = nsga2.Build().WithMaxIterations(maximumIterations).CreateExecutionInstance(out var registry).RunToCompletion(problem, RandomNumberGenerator.Create(AlgorithmRandomSeed), ct: TestContext.Current.CancellationToken);
    var gres = (GenealogyAnalysis<SymbolicExpressionTree>.Instance)genealogy.RetrieveAnalysis(registry);
    var qres = (BestMedianWorstAnalysis<SymbolicExpressionTree>.Instance)qualities.RetrieveAnalysis(registry);

    Assert.Equal(maximumIterations, qres.BestSolutions.Count);
    Assert.Equal(populationSize, res.Population.Solutions.Length);
    var graphViz = gres.Graph.ToGraphViz();
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
