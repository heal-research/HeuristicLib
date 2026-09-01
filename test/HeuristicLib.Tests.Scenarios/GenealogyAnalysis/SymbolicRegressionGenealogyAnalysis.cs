using HEAL.HeuristicLib.Analysis.GenealogyAnalysis;
using HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.MachineLearning.Legacy;
using RandomNumberGenerator = HEAL.HeuristicLib.Random.RandomNumberGenerator;
using SymbolicRegressionProblem = HEAL.HeuristicLib.Problems.MachineLearning.Legacy.SymbolicRegressionProblem;

namespace HEAL.HeuristicLib.Tests.Scenarios.GenealogyAnalysis;

public class GenealogyGraphTests
{
    private static
        ChooseOneMutator<SymbolicExpressionTree> CreateSymRegAllMutator()
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

        //ga.RandomSeed = AlgorithmRandomSeed;
        var ga = new GeneticAlgorithm<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>>
        {
            Creator = new ProbabilisticTreeCreator(),
            Crossover = new SubtreeCrossover(),
            Mutator = CreateSymRegAllMutator(),
            Selector = TournamentSelector.For(problem, tournamentSize: 3),
            PopulationSize = 8,
            MutationRate = 0.05,
            Elites = 1
        };
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
        var algorithm = new GeneticAlgorithm<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>>
        {
            Creator = new ProbabilisticTreeCreator(),
            Crossover = new SubtreeCrossover(),
            Mutator = CreateSymRegAllMutator(),
            Selector = TournamentSelector.For(problem, tournamentSize: 3),
            PopulationSize = popsize,
            MutationRate = 0.05,
            Elites = 1
        };
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
        var algorithm = new HillClimber<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>>
        {
            Creator = new ProbabilisticTreeCreator(),
            Mutator = CreateSymRegAllMutator()
        };
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
        var algorithm = new NSGA2<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, IProblem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>>
        {
            Creator = new ProbabilisticTreeCreator(),
            Crossover = new SubtreeCrossover(),
            Mutator = symRegAllMutator,
            PopulationSize = populationSize,
            MutationRate = mutationRate
        };
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
