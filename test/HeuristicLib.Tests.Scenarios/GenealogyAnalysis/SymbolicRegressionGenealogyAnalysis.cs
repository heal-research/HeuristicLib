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
        ga = ga with
        {
            MaximumGenerations = 6
        };

        var analysis = Analyzer.TraceBestMedianWorst(ga);

        var run = ga.CreateRun(problem, RandomNumberGenerator.Create(AlgorithmRandomSeed), analysis);
        var res = run.Complete(cancellationToken: TestContext.Current.CancellationToken);
        var ares = analysis.Snapshot();

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
        algorithm = algorithm with
        {
            MaximumGenerations = gens
        };

        var evalQualities = Analyzer.TraceBestQuality(algorithm.Evaluator);
        var qualities = Analyzer.TraceBestMedianWorst(algorithm);
        var genealogyAnalysis =
            Analyzer.Genealogy(algorithm.Crossover, algorithm.Mutator, algorithm);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(AlgorithmRandomSeed), evalQualities, qualities, genealogyAnalysis);
        var res = run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        var qres = qualities.Snapshot();
        var eres = evalQualities.Snapshot();
        var gres = genealogyAnalysis.Graph;

        qres.Count.ShouldBe(gens);
        res.Population.EvaluatedCandidates.Count.ShouldBe(popsize);
        res.Population.EvaluatedCandidates.All(solution => problem.SearchSpace.Contains(solution.Candidate))
           .ShouldBeTrue();
        res.Population.EvaluatedCandidates.All(solution => solution.ObjectiveVector.Count == 1).ShouldBeTrue();
        var graphViz = gres.ToGraphViz();
        (graphViz.Length > 0).ShouldBeTrue();
        eres[^1].Value.ObjectiveVector.ShouldBe(qres[^1].Value.Best.ObjectiveVector);
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
        var genealogy =
            new GenealogyAnalysis<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace,
                IProblem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>,
                SingleSolutionState<SymbolicExpressionTree>>(
                mutators: [algorithm.Mutator], algorithms: [algorithm]);
        var run = algorithm.WithMaxIterations(8).CreateRun(problem, RandomNumberGenerator.Create(AlgorithmRandomSeed), genealogy);
        var res = run.Complete(cancellationToken: TestContext.Current.CancellationToken);
        var gres = genealogy.Graph;
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
        algorithm = algorithm with
        {
            MaximumGenerations = maximumIterations
        };

        var genealogy = Analyzer.Genealogy(algorithm.Crossover, algorithm.Mutator, algorithm);
        var qualities = Analyzer.TraceBestMedianWorst(algorithm);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(AlgorithmRandomSeed), genealogy, qualities);
        var res = run.Complete(cancellationToken: TestContext.Current.CancellationToken);
        var gres = genealogy.Graph;
        var qres = qualities.Snapshot();

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
