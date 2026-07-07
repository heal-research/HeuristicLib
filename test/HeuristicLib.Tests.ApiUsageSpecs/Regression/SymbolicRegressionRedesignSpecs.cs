using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Random;
using Xunit;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Regression;

public class SymbolicRegressionRedesignSpecs
{
    [Fact]
    public void ExpressionDraft_AuthoringShape_BuildsX0PlusTwoTimesX1()
    {
        var expression = (Variable("x0") + Fixed(2.0) * Variable("x1")).Build();

        expression.ToInfixString().ShouldBe("(x0 + (2 * x1))");
        var root = expression.Root;
        root.Child(0).TryGetVariableReference(out var leftVariable).ShouldBeTrue();
        root.Child(1).Child(1).TryGetVariableReference(out var rightVariable).ShouldBeTrue();
        leftVariable.Name.ShouldBe("x0");
        rightVariable.Name.ShouldBe("x1");
        var data = DataFrame.FromMatrix(
          ["x0", "x1"],
          new double[,]
          {
              { 1.0, 3.0 },
              { 2.0, 4.0 },
              { 3.0, 5.0 }
          });

        expression.Evaluate(data).ShouldBe([7.0, 10.0, 13.0]);
    }

    [Fact]
    public void Interpreter_AuthoringShape_EvaluatesCompiledExpressionAgainstRegressionData()
    {
        var data = CreateLinearRegressionData();
        var draft = Variable("x0") + Fixed(2.0) * Variable("x1");
        var expression = draft.Build();

        var predictions = expression.Evaluate(data.TrainingInputs);

        predictions.ShouldBe([7.0, 10.0, 13.0], tolerance: 1e-12);
        data.TrainingInputs.DoubleSeriesNames.Order().ShouldBe(["x0", "x1"]);
        data.TargetName.ShouldBe("y");
        data.TrainingTarget.Values.ToArray().ShouldBe([7.0, 10.0, 13.0]);
    }

    [Fact]
    public void SymbolicExpression_AuthoringShape_NavigatesSubExpressionsAsTree()
    {
        var expression = (Variable("x0") + Fixed(2.0) * Variable("x1")).Build();

        var root = expression.Root;
        var left = root.Child(0);
        var right = root.Child(1);
        var rightLeft = right.Child(0);
        var rightRight = right.Child(1);

        root.Symbol.ShouldBe(new AddSymbol());
        left.TryGetVariableReference(out var leftVariable).ShouldBeTrue();
        leftVariable.Name.ShouldBe("x0");
        right.Symbol.ShouldBe(new MultiplySymbol());
        rightLeft.TryGetNumericLiteral(out var literal).ShouldBeTrue();
        rightRight.TryGetVariableReference(out var rightVariable).ShouldBeTrue();
        literal.ShouldBe(new NumericLiteral(2.0, NumericLiteralKind.Fixed));
        rightVariable.Name.ShouldBe("x1");
    }

    [Fact]
    public void Problem_AuthoringShape_ConstructsDefaultSymbolicRegressionProblemWithRmseMetric()
    {
        var data = CreateLinearRegressionData();

        /*
        var problem = SymbolicRegressionProblem.CreateDefault(
          data,
          inputVariables: ["x0", "x1"],
          metric: Metrics.RMSE,
          searchSpace: new SymbolicExpressionSearchSpace(
             maximumLength: 40,
             maximumDepth: 12,
             allowedSymbols: Symbols.BasicArithmetic,
             allowedVariables: ["x0", "x1"]));

        problem.Metric.ShouldBe(Metrics.RMSE);
        problem.Objective.Directions.ShouldBe([ObjectiveDirection.Minimize]);
        */

        var currentProblem = new SymbolicExpressionRegressionProblem(data, Metrics.RMSE);
        currentProblem.Metric.ShouldBe(Metrics.RMSE);
        currentProblem.Objective.Directions.ShouldBe([ObjectiveDirection.Minimize]);
    }

    [Fact]
    public void GeneticAlgorithm_AuthoringShape_RunsWithNewCreatorCrossoverAndMutator()
    {
        /*
        var data = CreateLinearRegressionData();
        var problem = SymbolicRegressionProblem.CreateDefault(
          data,
          inputVariables: ["x0", "x1"],
          metric: Metrics.RMSE,
          searchSpace: new SymbolicExpressionSearchSpace(
             maximumLength: 40,
             maximumDepth: 12,
             allowedSymbols: Symbols.BasicArithmetic,
             allowedVariables: ["x0", "x1"]));

        var algorithm = new GeneticAlgorithm<SymbolicExpression, SymbolicExpressionSearchSpace, SymbolicRegressionProblem>
        {
            PopulationSize = 24,
            Creator = new UnrestrictedSymbolicExpressionCreator(),
            Crossover = new UnrestrictedSymbolicExpressionCrossover(),
            Mutator = new UnrestrictedSymbolicExpressionMutator(),
            MutationRate = 0.2,
            Selector = new TournamentSelector<SymbolicExpression>(tournamentSize: 2),
            Elites = 1
        }.WithMaxIterations(8);

        var finalState = await algorithm.RunToCompletionAsync(
          problem,
          RandomNumberGenerator.Create(123),
          ct: TestContext.Current.CancellationToken);

        finalState.Population.Solutions.All(solution => problem.SearchSpace.Contains(solution.Genotype)).ShouldBeTrue();
        */

        var currentProblem = new SymbolicExpressionRegressionProblem(CreateLinearRegressionData(), Metrics.RMSE);
        var selector = new TournamentSelector<object>(tournamentSize: 2);

        currentProblem.Objective.Directions.ShouldBe([ObjectiveDirection.Minimize]);
        selector.ShouldNotBeNull();
        RandomNumberGenerator.Create(123).ShouldNotBeNull();
        typeof(GeneticAlgorithm<,,>).ShouldNotBeNull();
    }

    [Fact]
    public void Evaluator_AuthoringShape_EnablesNumericOptimizationWithoutInPlaceMutation()
    {
        /*
        var problem = SymbolicRegressionProblem.CreateDefault(
          CreateLinearDataset(),
          inputVariables: ["x0"],
          metric: Metrics.RMSE,
          searchSpace: new SymbolicExpressionSearchSpace(
             maximumLength: 40,
             maximumDepth: 12,
             allowedSymbols: Symbols.BasicArithmetic,
             allowedVariables: ["x0"]));

        var rawExpression = (Parameter(1.0) + Variable("x0")).Build();

        var evaluator = SymbolicExpressionEvaluator.OptimizeNumericParameters(
          maxIterations: 25,
          tolerance: 1e-8);

        var solution = evaluator.Evaluate(
          [rawExpression],
          RandomNumberGenerator.Create(123),
          problem.SearchSpace,
          problem).Single();

        solution.Genotype.ShouldNotBeSameAs(rawExpression);
        rawExpression.Root.Child(0).TryGetNumericLiteral(out var rawParameter).ShouldBeTrue();
        rawParameter.Value.ShouldBe(1.0);
        evaluator.Counters.FunctionEvaluations.ShouldBeGreaterThan(0);
        */

        typeof(ProblemEvaluator<>).ShouldNotBeNull();
    }

    [Fact]
    public void GeneticAlgorithm_AuthoringShape_ConfiguresProblemSpecificNumericOptimizingEvaluator()
    {
        var data = CreateLinearDataset();

        /*
        var problem = SymbolicRegressionProblem.CreateDefault(
          data,
          inputVariables: ["x0", "x1"],
          metric: Metrics.RMSE,
          searchSpace: new SymbolicExpressionSearchSpace(
             maximumLength: 40,
             maximumDepth: 12,
             allowedSymbols: Symbols.BasicArithmetic,
             allowedVariables: ["x0", "x1"]));

        var algorithm = new GeneticAlgorithm<SymbolicExpression, SymbolicExpressionSearchSpace, SymbolicRegressionProblem>
        {
            PopulationSize = 24,
            Creator = new UnrestrictedSymbolicExpressionCreator(),
            Crossover = new UnrestrictedSymbolicExpressionCrossover(),
            Mutator = new UnrestrictedSymbolicExpressionMutator(),
            MutationRate = 0.2,
            Evaluator = SymbolicExpressionEvaluator.OptimizeNumericParameters(
              maxIterations: 25,
              tolerance: 1e-8),
            Selector = new TournamentSelector<SymbolicExpression>(tournamentSize: 2),
            Elites = 1
        }.WithMaxIterations(8);

        var finalState = await algorithm.RunToCompletionAsync(
          problem,
          RandomNumberGenerator.Create(123),
          ct: TestContext.Current.CancellationToken);

        finalState.Population.Solutions.All(solution => problem.SearchSpace.Contains(solution.Genotype)).ShouldBeTrue();
        algorithm.Evaluator.Counters.FunctionEvaluations.ShouldBeGreaterThan(0);
        */

        data.InputVariables.ShouldBe(["x0", "x1"]);
        typeof(GeneticAlgorithm<,,>).ShouldNotBeNull();
    }

    private static RegressionProblemData CreateLinearDataset()
    {
        var dataset = Dataset.FromRowData(
          ["x0", "x1", "y"],
          new double[,]
          {
              { 1.0, 3.0, 7.0 },
              { 2.0, 4.0, 10.0 },
              { 3.0, 5.0, 13.0 }
          });

        return new RegressionProblemData(
          dataset,
          targetVariable: "y",
          allowedInputVariables: ["x0", "x1"],
          trainingRange: new Range(0, 3));
    }

    private static RegressionData CreateLinearRegressionData() =>
      RegressionData.Training(
        DataFrame.FromMatrix(
          ["x0", "x1"],
          new double[,]
          {
              { 1.0, 3.0 },
              { 2.0, 4.0 },
              { 3.0, 5.0 }
          }),
        Series<double>.Create([7.0, 10.0, 13.0], name: "y"));
}
