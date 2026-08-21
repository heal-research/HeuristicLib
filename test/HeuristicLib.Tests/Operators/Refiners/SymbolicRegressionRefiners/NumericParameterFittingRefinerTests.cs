using HEAL.HeuristicLib.Problems.MachineLearning;
using static HEAL.HeuristicLib.Encodings.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Operators.Refiners.SymbolicRegressionRefiners;

public sealed class NumericParameterFittingRefinerTests
{
    [Fact]
    public void RefineCandidate_FitsTheEvolvableConstantsToTheTrainingData()
    {
        var problem = CreateProblem();
        var expression = (Constant(0.25) * Variable("x") + Constant(-0.5)).Build();

        var refined = Refine(new NumericParameterFittingRefiner { MaximumIterations = 100 }, problem, expression);

        Predictions(refined, problem).ShouldBe(Targets, tolerance: 1e-8);
    }

    [Fact]
    public void RefineCandidate_LeavesTheSourceExpressionUnchanged()
    {
        var problem = CreateProblem();
        var expression = (Constant(0.25) * Variable("x") + Constant(-0.5)).Build();

        Refine(new NumericParameterFittingRefiner { MaximumIterations = 100 }, problem, expression);

        expression.RootPoint.Child(0).Child(0).Node.ShouldBeOfType<NumericConstantExpressionNode>().Value.ShouldBe(0.25);
    }

    [Fact]
    public void RefineCandidate_WithoutEvolvableConstants_ReturnsTheSameInstance()
    {
        var problem = CreateProblem();
        var expression = (FixedConstant(2.0) * Variable("x")).Build();

        Refine(new NumericParameterFittingRefiner { MaximumIterations = 100 }, problem, expression).ShouldBeSameAs(expression);
    }

    [Fact]
    public void RefineCandidate_WithZeroIterations_ReturnsTheSameInstance()
    {
        var problem = CreateProblem();
        var expression = (Constant(0.25) * Variable("x")).Build();

        Refine(new NumericParameterFittingRefiner { MaximumIterations = 0 }, problem, expression).ShouldBeSameAs(expression);
    }

    // A stateless refiner seals the execution-instance factory, so a negative iteration count is rejected by the role
    // method rather than at configuration time.
    [Fact]
    public void RefineCandidate_WithNegativeIterations_Throws()
    {
        var problem = CreateProblem();
        var expression = (Constant(0.25) * Variable("x")).Build();

        Should.Throw<ArgumentOutOfRangeException>(() =>
            Refine(new NumericParameterFittingRefiner { MaximumIterations = -1 }, problem, expression));
    }

    [Fact]
    public void MaximumIterations_DefaultsToFive()
    {
        new NumericParameterFittingRefiner().MaximumIterations.ShouldBe(5);
    }

    [Fact]
    public void FittingData_DefaultsToNullMeaningTheProblemTrainingData()
    {
        new NumericParameterFittingRefiner().FittingData.ShouldBeNull();
    }

    [Fact]
    public void Configurations_WithEqualSettings_AreEqual()
    {
        var left = new NumericParameterFittingRefiner { MaximumIterations = 20 };
        var right = new NumericParameterFittingRefiner { MaximumIterations = 20 };

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
        left.ShouldNotBe(new NumericParameterFittingRefiner { MaximumIterations = 21 });
    }

    // A dataset takes part in configuration equality by reference, because comparing it by value would cost a pass
    // over every row on each comparison.
    [Fact]
    public void Configurations_CompareFittingDataByReference()
    {
        var data = CreateProblem().TrainingData;

        new NumericParameterFittingRefiner { FittingData = data }
            .ShouldBe(new NumericParameterFittingRefiner { FittingData = data });

        new NumericParameterFittingRefiner { FittingData = CreateProblem().TrainingData }
            .ShouldNotBe(new NumericParameterFittingRefiner { FittingData = CreateProblem().TrainingData });
    }

    [Fact]
    public void FittingData_WhenSupplied_FitsToItInsteadOfTheProblemTrainingData()
    {
        var problem = CreateProblem();
        var subset = new RegressionData(
            new DataFrame([Series<double>.FromOwnedArray("x", [1.0, 2.0])]),
            Series<double>.FromOwnedArray("y", [5.0, 10.0]));
        var expression = (Constant(0.25) * Variable("x") + Constant(-0.5)).Build();

        var refiner = new NumericParameterFittingRefiner { MaximumIterations = 100, FittingData = subset };
        var refined = Refine(refiner, problem, expression);

        // Fitted to y = 5x on the subset, so it reproduces the subset and not the problem's y = 2x + 3.
        ExpressionInterpreter.Interpret(refined.Compile(optimize: false), subset.Inputs)
            .ShouldBe([5.0, 10.0], tolerance: 1e-8);
        Predictions(refined, problem).Zip(Targets).ShouldContain(pair => Math.Abs(pair.First - pair.Second) > 1e-8);
    }

    [Fact]
    public void Refine_FitsEveryCandidateInTheBatchIndependently()
    {
        var problem = CreateProblem();
        ExpressionTree[] candidates =
        [
            (Constant(0.25) * Variable("x") + Constant(-0.5)).Build(),
            (Constant(-3.0) * Variable("x") + Constant(9.0)).Build()
        ];

        var refined = new NumericParameterFittingRefiner { MaximumIterations = 100 }
            .Refine(candidates, RandomNumberGenerator.Create(42), problem.SearchSpace, problem);

        refined.Count.ShouldBe(2);
        foreach (var expression in refined)
        {
            Predictions(expression, problem).ShouldBe(Targets, tolerance: 1e-8);
        }
    }

    // Linear scaling already makes the unfitted expression score perfectly, so the fit cannot improve the objective.
    [Fact]
    public void RefineCandidate_DoesNotConsultTheProblemObjective()
    {
        var problem = CreateProblem(useLinearScaling: true);
        var expression = (Constant(0.25) * Variable("x") + Constant(-0.5)).Build();

        var refined = Refine(new NumericParameterFittingRefiner { MaximumIterations = 100 }, problem, expression);

        problem.Evaluate(expression)[0].ShouldBe(0.0, tolerance: 1e-12);
        problem.Evaluate(refined)[0].ShouldBe(0.0, tolerance: 1e-12);
        refined.ShouldNotBeSameAs(expression);
    }

    [Fact]
    public void UnderAnImprovementCheck_ANonImprovingFitIsRejected()
    {
        var problem = CreateProblem(useLinearScaling: true);
        var expression = (Constant(0.25) * Variable("x") + Constant(-0.5)).Build();

        var instance = new NumericParameterFittingRefiner { MaximumIterations = 100 }
            .WithImprovementCheck()
            .CreateExecutionInstance(new ExecutionInstanceRegistry());

        var refined = instance.Refine([expression], RandomNumberGenerator.Create(42), problem.SearchSpace, problem);

        refined.ShouldHaveSingleItem().ShouldBeSameAs(expression);
    }

    [Fact]
    public void UnderAnImprovementCheck_AnImprovingFitIsAccepted()
    {
        var problem = CreateProblem();
        var expression = (Constant(0.25) * Variable("x") + Constant(-0.5)).Build();

        var instance = new NumericParameterFittingRefiner { MaximumIterations = 100 }
            .WithImprovementCheck()
            .CreateExecutionInstance(new ExecutionInstanceRegistry());

        var refined = instance.Refine([expression], RandomNumberGenerator.Create(42), problem.SearchSpace, problem);

        Predictions(refined.ShouldHaveSingleItem(), problem).ShouldBe(Targets, tolerance: 1e-8);
    }

    // A solve that does not converge describes one candidate, so only that candidate is left as it was.
    [Fact]
    public void Refine_WhenOneCandidateCannotBeSolved_LeavesItAloneAndStillFitsTheOthers()
    {
        var problem = CreateProblem();
        var unsolvable = (FixedConstant(1.0) / Constant(0.0)).Build();
        var fittable = (Constant(0.25) * Variable("x") + Constant(-0.5)).Build();

        var refined = new NumericParameterFittingRefiner { MaximumIterations = 100 }
            .CreateExecutionInstance(new ExecutionInstanceRegistry())
            .Refine([unsolvable, fittable], RandomNumberGenerator.Create(42), problem.SearchSpace, problem);

        refined[0].ShouldBeSameAs(unsolvable);
        Predictions(refined[1], problem).ShouldBe(Targets, tolerance: 1e-8);
    }

    // An unbound variable describes the configuration rather than the candidate, so every affected candidate would
    // fail the same way.
    [Fact]
    public void Refine_WithAVariableTheFittingDataDoesNotSupply_Throws()
    {
        var problem = CreateProblem();
        var expression = (Constant(0.25) * Variable("missing") + Constant(-0.5)).Build();

        Should.Throw<ArgumentException>(() => Refine(new NumericParameterFittingRefiner { MaximumIterations = 100 }, problem, expression))
            .Message.ShouldContain("missing");
    }

    [Fact]
    public void InARun_AVariableTheFittingDataDoesNotSupply_PropagatesOutOfTheAlgorithm()
    {
        // Evolvable constants, so every candidate actually reaches the solver instead of taking the identity shortcut.
        var problem = new SymbolicRegressionProblem(
            new RegressionData(
                new DataFrame([Series<double>.FromOwnedArray("x", [.. Inputs])]),
                Series<double>.FromOwnedArray("y", [.. Targets])),
            Metrics.MSE,
            new ExpressionTreeSearchSpace(
                maximumLength: 15,
                maximumDepth: 4,
                operations: Symbols.MinimalOperations,
                variables: ["x"],
                constants: [new EvolvableConstantSymbol()]));
        // The refiner fits against data that has no "x" column, so every candidate using the variable fails alike.
        var fittingData = new RegressionData(
            new DataFrame([Series<double>.FromOwnedArray("other", [.. Inputs])]),
            Series<double>.FromOwnedArray("y", [.. Targets]));
        var algorithm = new GeneticAlgorithm<ExpressionTree, ExpressionTreeSearchSpace, SymbolicRegressionProblem>
        {
            PopulationSize = 4,
            Creator = new GrowTreeCreator(),
            Crossover = new SubtreeCrossover(),
            Mutator = new SubtreeMutator(),
            Selector = RandomSelector.For(problem),
            MaximumGenerations = 2,
            Refiner = new NumericParameterFittingRefiner { MaximumIterations = 5, FittingData = fittingData }
        };

        Should.Throw<ArgumentException>(() => algorithm.Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken));
    }

    private static readonly double[] Inputs = [-2.0, -1.0, 0.0, 1.0, 2.0];
    private static readonly double[] Targets = [-1.0, 1.0, 3.0, 5.0, 7.0];

    private static ExpressionTree Refine(NumericParameterFittingRefiner refiner, SymbolicRegressionProblem problem, ExpressionTree expression) =>
        refiner.RefineCandidate(expression, RandomNumberGenerator.Create(42), problem.SearchSpace, problem);

    private static double[] Predictions(ExpressionTree expression, SymbolicRegressionProblem problem) =>
        ExpressionInterpreter.Interpret(expression.Compile(optimize: false), problem.TrainingData.Inputs);

    private static SymbolicRegressionProblem CreateProblem(bool useLinearScaling = false) =>
        new(
            new RegressionData(
                new DataFrame([Series<double>.FromOwnedArray("x", [.. Inputs])]),
                Series<double>.FromOwnedArray("y", [.. Targets])),
            Metrics.MSE,
            new ExpressionTreeSearchSpace(
                maximumLength: 31,
                maximumDepth: 6,
                operations: Symbols.MinimalOperations,
                variables: ["x"],
                constants: [new FixedConstantSymbol(2.0)]),
            useLinearScaling);
}
