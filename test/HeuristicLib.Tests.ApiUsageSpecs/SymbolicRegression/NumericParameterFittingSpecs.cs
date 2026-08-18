using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.DataAnalysis;
using HEAL.HeuristicLib.DataAnalysis.Regression;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionCreators;
using HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionCrossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionMutators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Operators.Refiners.SymbolicRegressionRefiners;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.SymbolicRegression;

// Shows how symbolic regression fits the numeric parameters of its candidates.
// Genetic programming searches for the structure of an expression, and is poor at tuning the numeric values
// inside it, because it can only reach them through random perturbation. Numeric parameter fitting solves those
// values directly by nonlinear least squares, so the search can spend its effort on structure. The literature calls
// this parameter identification; PySR and HeuristicLab call it constant optimization.
public class NumericParameterFittingSpecs
{
    // The plain configuration: hand the algorithm a refiner and it fits every candidate's parameters before
    // evaluating it. The fitted expression is kept, which is the Lamarckian arrangement.
    [Fact]
    public void FittingNumericParametersDuringASymbolicRegressionRun()
    {
        var problem = CreateProblem();

        var algorithm = CreateAlgorithm(problem) with
        {
            Refiner = new NumericParameterFittingRefiner { MaximumIterations = 10 }
        };

        var bestError = BestMeanSquaredError(algorithm, problem);

        // The same run without a refiner, to show what the fitting is worth. Both use the same seed, so the search
        // makes the same structural decisions and only the numeric values differ.
        var bestErrorWithoutFitting = BestMeanSquaredError(CreateAlgorithm(problem), problem);

        bestError.ShouldBeLessThan(bestErrorWithoutFitting);
    }

    // Fitting minimizes squared error against the raw targets, which is not necessarily the problem's objective. Wrap
    // the refiner in an improvement check to keep a fitted candidate only when it actually improves that objective.
    // Passing the algorithm's own evaluator to the check makes its two comparison evaluations share that evaluator's
    // execution instance, so they count against the same budget and hit the same cache. Leaving the evaluator unset
    // gives the check a private one, whose evaluations stay invisible to budgets and analysis.
    [Fact]
    public void KeepingAFittedCandidateOnlyWhenItImprovesTheObjective()
    {
        var problem = CreateProblem();
        var evaluator = new ProblemEvaluator<ExpressionTree, ExpressionTreeSearchSpace, SymbolicRegressionProblem>();

        var algorithm = CreateAlgorithm(problem) with
        {
            Evaluator = evaluator,
            Refiner = new NumericParameterFittingRefiner { MaximumIterations = 10 }
                .WithImprovementCheck(evaluator)
        };

        var result = algorithm.Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        result.Population.EvaluatedCandidates.ShouldNotBeEmpty();
    }

    // Fitting every candidate is expensive, so a run may fit only some of them. That needs no setting on the refiner:
    // applying an operator at a rate is ordinary composition, and every refiner has it.
    [Fact]
    public void FittingOnlyAProportionOfTheCandidates()
    {
        var problem = CreateProblem();

        var algorithm = CreateAlgorithm(problem) with
        {
            Refiner = new NumericParameterFittingRefiner().WithRate(0.25)
        };

        var result = algorithm.Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        result.Population.EvaluatedCandidates.ShouldNotBeEmpty();
    }

    // Fitting on a subset of the rows trades accuracy for solver time. Supply the rows explicitly rather than a
    // percentage, so the run records which rows were fitted.
    [Fact]
    public void FittingAgainstASubsetOfTheTrainingRows()
    {
        var problem = CreateProblem();
        var everySecondRow = SubsetOfEverySecondRow(problem.TrainingData);

        var algorithm = CreateAlgorithm(problem) with
        {
            Refiner = new NumericParameterFittingRefiner { FittingData = everySecondRow }
        };

        var result = algorithm.Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        result.Population.EvaluatedCandidates.ShouldNotBeEmpty();
    }

    private static double BestMeanSquaredError(
        GeneticAlgorithm<ExpressionTree, ExpressionTreeSearchSpace, SymbolicRegressionProblem> algorithm,
        SymbolicRegressionProblem problem) =>
        algorithm.Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken)
            .Population.EvaluatedCandidates
            .Min(candidate => candidate.ObjectiveVector[0]);

    private static SymbolicRegressionProblem CreateProblem()
    {
        // y = 2.5 * x + 1.3, which needs both a scale and an offset that the search has to get numerically right.
        var x = Enumerable.Range(0, 40).Select(index => (index - 20) * 0.25).ToArray();
        var y = x.Select(value => (2.5 * value) + 1.3).ToArray();

        var data = new RegressionData(
            new DataFrame([Series<double>.FromOwnedArray("x", x)]),
            Series<double>.FromOwnedArray("y", y));

        var evolvableConstant = new EvolvableConstantSymbol(
            new UniformDoubleDistribution(-5.0, 5.0),
            new AdditiveNumericPerturbation(new NormalDoubleDistribution(0.0, 1.0)));

        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 15,
            maximumDepth: 5,
            operations: [Symbols.Addition, Symbols.Subtraction, Symbols.Multiplication],
            variables: ["x"],
            constants: [evolvableConstant]);

        return new SymbolicRegressionProblem(data, Metrics.MSE, searchSpace);
    }

    // Whether refinement effort counts against the run's evaluation budget is decided by one thing: whether the
    // algorithm and the improvement check hold the same evaluator object. Execution instances resolve by reference, so
    // one object means one counter. Sharing it makes the budget describe total effort including refinement; leaving the
    // check on its own evaluator keeps refinement outside the budget. Neither is a hidden default, and both are here.
    [Fact]
    public void DecidingWhetherRefinementEffortCountsAgainstTheEvaluationBudget()
    {
        var problem = CreateProblem();
        var sharedEvaluator = new ProblemEvaluator<ExpressionTree, ExpressionTreeSearchSpace, SymbolicRegressionProblem>()
            .CountEvaluatedCandidates(out var sharedCounter);
        var algorithmEvaluator = new ProblemEvaluator<ExpressionTree, ExpressionTreeSearchSpace, SymbolicRegressionProblem>()
            .CountEvaluatedCandidates(out var ownCounter);

        var sharing = CreateAlgorithm(problem) with
        {
            Evaluator = sharedEvaluator,
            Refiner = new NumericParameterFittingRefiner().WithImprovementCheck(sharedEvaluator)
        };
        var notSharing = CreateAlgorithm(problem) with
        {
            Evaluator = algorithmEvaluator,
            Refiner = new NumericParameterFittingRefiner().WithImprovementCheck()
        };

        sharing.Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);
        notSharing.Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        // The shared counter saw the algorithm's evaluations plus the two comparison evaluations per refined candidate.
        // The private one saw only the algorithm's, over the very same search.
        sharedCounter.CurrentCount.ShouldBeGreaterThan(ownCounter.CurrentCount);
    }

    private static GeneticAlgorithm<ExpressionTree, ExpressionTreeSearchSpace, SymbolicRegressionProblem> CreateAlgorithm(
        SymbolicRegressionProblem problem) =>
        new()
        {
            PopulationSize = 30,
            MaximumGenerations = 10,
            Creator = new RampedHalfAndHalfTreeCreator(),
            Crossover = new SubtreeCrossover(),
            Mutator = ChooseOneMutator.Create(
                new NodeReplacementMutator(),
                new LocalPerturbationMutator(),
                new SubtreeMutator()),
            MutationRate = 0.25,
            Selector = TournamentSelector.For(problem, tournamentSize: 3),
            Elites = 1
        };

    private static RegressionData SubsetOfEverySecondRow(RegressionData data)
    {
        var x = data.Inputs.Get<double>("x").Values.ToArray().Where((_, index) => index % 2 == 0).ToArray();
        var y = data.Target.Values.ToArray().Where((_, index) => index % 2 == 0).ToArray();

        return new RegressionData(
            new DataFrame([Series<double>.FromOwnedArray("x", x)]),
            Series<double>.FromOwnedArray("y", y));
    }
}
