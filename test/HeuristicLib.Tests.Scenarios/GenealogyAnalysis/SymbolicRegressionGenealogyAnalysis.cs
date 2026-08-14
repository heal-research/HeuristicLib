using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Analysis.GenealogyAnalysis;
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

namespace HEAL.HeuristicLib.Tests.Scenarios.GenealogyAnalysis;

public class GenealogyGraphTests
{
    private static
        ChooseOneMutator<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace,
            IProblem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>> CreateSymRegAllMutator()
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

    // Note: be cautious here because Levenberg-Marquardt likely caused an endless loop in the past.
    [Fact]
    public void GeneticAlgorithmExecution()
    {
        var problem = CreateTestSymbolicRegressionProblem();

        var builder = GeneticAlgorithm.GetBuilder(new ProbabilisticTreeCreator(), new SubtreeCrossover(),
            CreateSymRegAllMutator());
        builder.PopulationSize = 8;
        builder.MutationRate = 0.05;
        builder.Selector = TournamentSelector.For(problem, tournamentSize: 3);
        builder.Elites = 1;
        //ga.RandomSeed = AlgorithmRandomSeed;
        //builder.Terminator = new AfterIterationsTerminator<SymbolicExpressionTree>(100);
        var ga = builder.Build();
        var interceptor = ga.Interceptor ?? IdentityInterceptor.For(ga);
        ga = ga with
        {
            Interceptor = interceptor,
            MaximumGenerations = 6
        };

        var analysis = Analyzer.BestMedianWorst(ga.Interceptor!);

        var run = ga.CreateRun(problem, RandomNumberGenerator.Create(AlgorithmRandomSeed)).WithAnalyzer(analysis);
        var res = run.Complete(cancellationToken: TestContext.Current.CancellationToken);
        var ares = run.GetResult(analysis);

        ares.Count.ShouldBe(6);
        res.Population.EvaluatedCandidates.Count().ShouldBe(8);
        res.Population.EvaluatedCandidates.All(solution => problem.SearchSpace.Contains(solution.Candidate))
           .ShouldBeTrue();
        res.Population.EvaluatedCandidates.All(solution => solution.ObjectiveVector.Count == 1).ShouldBeTrue();
        res.Population.EvaluatedCandidates.All(solution => double.IsFinite(solution.ObjectiveVector[0])).ShouldBeTrue();
    }

    [Fact]
    public void GenealogyGraphOnGeneticAlgorithm()
    {
        var problem = CreateTestSymbolicRegressionProblem();

        const int gens = 6;
        const int popsize = 6;
        var ga = GeneticAlgorithm.GetBuilder(new ProbabilisticTreeCreator(), new SubtreeCrossover(),
            CreateSymRegAllMutator());
        ga.PopulationSize = popsize;
        ga.MutationRate = 0.05;
        ga.Selector = TournamentSelector.For(problem, tournamentSize: 3);
        ga.Elites = 1;
        //ga.Terminator = new AfterIterationsTerminator<SymbolicExpressionTree>(gens);

        var algorithm = ga.Build();
        var interceptor = algorithm.Interceptor ?? IdentityInterceptor.For(algorithm);
        algorithm = algorithm with
        {
            Interceptor = interceptor,
            MaximumGenerations = gens
        };

        var evalQualities = ExperimentalAnalyzers.QualityCurve(algorithm.Evaluator);
        var qualities = Analyzer.BestMedianWorst(algorithm.Interceptor!);
        var genealogyAnalysis =
            ExperimentalAnalyzers.Genealogy(algorithm.Crossover, algorithm.Mutator, algorithm.Interceptor);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(AlgorithmRandomSeed)).WithAnalyzers(evalQualities, qualities, genealogyAnalysis);
        var res = run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        var qres = run.GetResult(qualities);
        var eres = run.GetResult(evalQualities);
        var gres = run.GetResult(genealogyAnalysis);

        qres.Count.ShouldBe(gens);
        res.Population.EvaluatedCandidates.Count.ShouldBe(popsize);
        res.Population.EvaluatedCandidates.All(solution => problem.SearchSpace.Contains(solution.Candidate))
           .ShouldBeTrue();
        res.Population.EvaluatedCandidates.All(solution => solution.ObjectiveVector.Count == 1).ShouldBeTrue();
        var graphViz = gres.ToGraphViz();
        (graphViz.Length > 0).ShouldBeTrue();
        eres.CurrentState[^1].best.ObjectiveVector.ShouldBe(qres[^1].Best.ObjectiveVector);
    }

    // Note: be cautious here because Levenberg-Marquardt likely caused an endless loop in the past.
    [Fact]
    public void GenealogyGraphOnLocalSearch()
    {
        var problem = CreateTestSymbolicRegressionProblem();
        var builder = HillClimber.GetBuilder(new ProbabilisticTreeCreator(), CreateSymRegAllMutator());
        var algorithm = builder.Build();
        var interceptor = algorithm.Interceptor ?? IdentityInterceptor.For(algorithm);
        algorithm = algorithm with { Interceptor = interceptor };
        var genealogy =
            new GenealogyAnalysis<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace,
                IProblem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>,
                SingleSolutionState<SymbolicExpressionTree>>(
                mutator: algorithm.Mutator, interceptor: algorithm.Interceptor);
        var run = algorithm.WithMaxIterations(8).CreateRun(problem, RandomNumberGenerator.Create(AlgorithmRandomSeed)).WithAnalyzer(genealogy);
        var res = run.Complete(cancellationToken: TestContext.Current.CancellationToken);
        var gres = run.GetResult(genealogy);
        res.Population.EvaluatedCandidates.ShouldHaveSingleItem();
        problem.SearchSpace.Contains(res.Population.EvaluatedCandidates.Single().Candidate).ShouldBeTrue();
        res.Population.EvaluatedCandidates.Single().ObjectiveVector.Count.ShouldBe(1);
        double.IsFinite(res.Population.EvaluatedCandidates.Single().ObjectiveVector[0]).ShouldBeTrue();
        var graphViz = gres.ToGraphViz();
        (graphViz.Length > 0).ShouldBeTrue();
    }

    [Fact]
    public void GenealogyGraphOnNSGA2()
    {
        var problem = CreateTestSymbolicRegressionProblem(multiObjective: true);
        var symRegAllMutator = CreateSymRegAllMutator();
        const int populationSize = 6;
        const int maximumIterations = 4;
        const double mutationRate = 0.05;
        var nsga2 = NSGA2.GetBuilder(
            new ProbabilisticTreeCreator(),
            new SubtreeCrossover(),
            symRegAllMutator);
        nsga2.PopulationSize = populationSize;
        nsga2.MutationRate = mutationRate;

        var algorithm = nsga2.Build();
        var interceptor = algorithm.Interceptor ?? IdentityInterceptor.For(algorithm);
        algorithm = algorithm with
        {
            Interceptor = interceptor,
            MaximumGenerations = maximumIterations
        };

        var genealogy = ExperimentalAnalyzers.Genealogy(algorithm.Crossover, algorithm.Mutator, algorithm.Interceptor);
        var qualities = Analyzer.BestMedianWorst(algorithm.Interceptor!);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(AlgorithmRandomSeed)).WithAnalyzers(genealogy, qualities);
        var res = run.Complete(cancellationToken: TestContext.Current.CancellationToken);
        var gres = run.GetResult(genealogy);
        var qres = run.GetResult(qualities);

        qres.Count.ShouldBe(maximumIterations);
        res.Population.EvaluatedCandidates.Count.ShouldBe(populationSize);
        res.Population.EvaluatedCandidates.All(solution => problem.SearchSpace.Contains(solution.Candidate))
           .ShouldBeTrue();
        res.Population.EvaluatedCandidates
           .All(solution => solution.ObjectiveVector.Count == problem.Objective.Directions.Count()).ShouldBeTrue();
        res.Population.EvaluatedCandidates.All(solution => solution.ObjectiveVector.All(double.IsFinite))
           .ShouldBeTrue();
        var graphViz = gres.ToGraphViz();
        (graphViz.Length > 0).ShouldBeTrue();
    }

    private const int AlgorithmRandomSeed = 42;

    public static readonly double[,] Data = new double[,]
    {
        { 0, 10 }, { 1, 10 }, { 2, 10 }, { 3, 10 }, { 4, 10 }, { 5, 10 }, { 6, 10 }, { 7, 10 }, { 8, 10 },
        { 9, 10 }, { 10, 10 }
    };

    private static SymbolicRegressionProblem CreateTestSymbolicRegressionProblem(
        int treeLength = 12, bool multiObjective = false, int constOptIteration = 1)
    {
        var problemData = new RegressionProblemData(new ModifiableDataset(["x", "y"], Data));

        IRegressionEvaluator<SymbolicExpressionTree>[] objectives = multiObjective
            ?
            [
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
            SearchSpace =
            {
                TreeDepth = treeLength,
                TreeLength = treeLength
            },
            ParameterOptimizationIterations = constOptIteration
        };

        var linearScalingRoot = problem.SearchSpace.Grammar.AddLinearScaling();

        var symbols = new Symbol[]
        {
            new Addition(), new Subtraction(), new Multiplication(), new Division(), new Number(), new SquareRoot(),
            new Logarithm(), new Exponential(), new Variable { VariableNames = problemData.InputVariables }
        };

        problem.SearchSpace.Grammar.AddFullyConnectedSymbols(
            linearScalingRoot, symbols);
        return problem;
    }
}
