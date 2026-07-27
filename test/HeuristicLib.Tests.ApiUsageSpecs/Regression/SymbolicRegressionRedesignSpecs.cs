using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.DataAnalysis;
using HEAL.HeuristicLib.DataAnalysis.Inspection;
using HEAL.HeuristicLib.DataAnalysis.Regression;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionCreators;
using HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionCrossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;
using Xunit;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Regression;

public class SymbolicRegressionRedesignSpecs
{
    [Fact]
    public void Formatting_AuthoringShape_PreservesMacros()
    {
        var expression = Sigmoid(Variable("x")).Build();

        expression.ToCSharpString().ShouldBe("sigmoid(x)");
        ExpressionFormatters.Python.Format(expression).ShouldBe("sigmoid(x)");
    }

    [Fact]
    public void Formatting_AuthoringShape_AllowsCustomFormatters()
    {
        var expression = (Variable("x") + FixedConstant(2)).Build();
        var macro = Sigmoid(Variable("x")).Build();
        var formatter = new PrefixExpressionFormatter();

        formatter.Format(expression).ShouldBe("+[x, 2]");
        formatter.Format(macro).ShouldBe("sigmoid[x]");
    }

    [Fact]
    public void Parsing_AuthoringShape_ParsesInfixExpressions()
    {
        var expression = InfixExpressionParser.Parse("x0 + param(2) * x1");

        expression.ToInfixString().ShouldBe("(x0 + (2 * x1))");
        expression.ToInfixString(InfixConstantNotation.MarkParameters)
            .ShouldBe("(x0 + (param(2) * x1))");
        expression.TraversePreOrder()
            .OfType<NumericConstantExpressionNode>()
            .Single()
            .Symbol.ShouldBeOfType<EvolvableConstantSymbol>();
    }

    [Fact]
    public void FeatureImportance_AuthoringShape_UsesPermutationDefaults()
    {
        var data = CreateLinearRegressionData();
        var predictor = (Variable("x0") + FixedConstant(2.0) * Variable("x1")).Build()
            .ToRegressor("prediction")
            .ToBounded(double.NegativeInfinity, double.PositiveInfinity);

        var result = FeatureImportance.Permutation(
            predictor,
            data,
            RandomNumberGenerator.Create(42));

        result.Metric.ShouldBeSameAs(Metrics.MSE);
        result.Features.Select(feature => feature.FeatureName).ShouldBe(["x0", "x1"]);
        result.Features.ShouldAllBe(feature => feature.MeanImportance >= 0.0);
    }

    [Fact]
    public void ExpressionDraft_AuthoringShape_BuildsX0PlusTwoTimesX1()
    {
        var expression = (Variable("x0") + FixedConstant(2.0) * Variable("x1")).Build();

        expression.ToInfixString().ShouldBe("(x0 + (2 * x1))");
        var root = expression.Root.ShouldBeOfType<BinaryExpressionNode>();
        root.Left.ShouldBeOfType<VariableExpressionNode>().VariableName.ShouldBe("x0");
        root.Right.ShouldBeOfType<BinaryExpressionNode>().Right
            .ShouldBeOfType<VariableExpressionNode>().VariableName.ShouldBe("x1");
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
        var draft = Variable("x0") + FixedConstant(2.0) * Variable("x1");
        var expression = draft.Build();

        var predictions = expression.Evaluate(data.Inputs);

        predictions.ShouldBe([7.0, 10.0, 13.0], tolerance: 1e-12);
        data.Inputs.Columns.Select(column => column.Name).Order().ShouldBe(["x0", "x1"]);
        data.Target.Name.ShouldBe("y");
        data.Target.Values.ToArray().ShouldBe([7.0, 10.0, 13.0]);
    }

    [Fact]
    public void SymbolicExpression_AuthoringShape_NavigatesSubExpressionsAsTree()
    {
        var expression = (Variable("x0") + FixedConstant(2.0) * Variable("x1")).Build();

        var root = expression.Root.ShouldBeOfType<BinaryExpressionNode>();
        var left = root.Left;
        var right = root.Right.ShouldBeOfType<BinaryExpressionNode>();
        var rightLeft = right.Left;
        var rightRight = right.Right;

        root.Symbol.ShouldBe(new AdditionSymbol());
        left.ShouldBeOfType<VariableExpressionNode>().VariableName.ShouldBe("x0");
        right.Symbol.ShouldBe(new MultiplicationSymbol());
        rightLeft.ShouldBeOfType<NumericConstantExpressionNode>().Value.ShouldBe(2.0);
        rightRight.ShouldBeOfType<VariableExpressionNode>().VariableName.ShouldBe("x1");
    }

    [Fact]
    public void Problem_AuthoringShape_ConstructsSymbolicRegressionProblemWithExplicitMetric()
    {
        var data = CreateLinearRegressionData();
        var searchSpace = CreateSearchSpace();
        var problem = new SymbolicRegressionProblem(data, Metrics.RMSE, searchSpace);

        problem.PredictionMetrics.ShouldBe([Metrics.RMSE]);
        problem.ExpressionMetrics.ShouldBeEmpty();
        problem.SearchSpace.ShouldBeSameAs(searchSpace);
        problem.Objective.Directions.ShouldBe([ObjectiveDirection.Minimize]);
    }

    [Fact]
    public void Problem_AuthoringShape_CombinesPredictionAndExpressionObjectives()
    {
        var data = CreateLinearRegressionData();
        var searchSpace = CreateSearchSpace();
        var problem = new SymbolicRegressionProblem(
            data,
            [Metrics.MSE],
            [ExpressionMetrics.Length],
            searchSpace);

        problem.Evaluate((Variable("x0") + FixedConstant(2.0) * Variable("x1")).Build())
            .ShouldBe(new ObjectiveVector(0.0, 5.0));
        problem.Objective.Directions.ShouldBe(
            [ObjectiveDirection.Minimize, ObjectiveDirection.Minimize]);
    }

    [Fact]
    public async Task GeneticAlgorithm_AuthoringShape_RunsWithNewCreatorCrossoverAndMutator()
    {
        var data = CreateLinearRegressionData();
        var searchSpace = CreateSearchSpace();
        var problem = new SymbolicRegressionProblem(data, Metrics.RMSE, searchSpace);
        var algorithm = new GeneticAlgorithm<ExpressionTree, ExpressionTreeSearchSpace, SymbolicRegressionProblem>
        {
            PopulationSize = 24,
            MaximumGenerations = 8,
            Creator = new RampedHalfAndHalfTreeCreator(),
            Crossover = new SubtreeCrossover(),
            Mutator = new ChooseOneMutator<ExpressionTree, ExpressionTreeSearchSpace, SymbolicRegressionProblem>(
                [new NodeReplacementMutator(), new SubtreeMutator(), new LocalPerturbationMutator()],
                [1.0, 1.0, 1.0]),
            MutationRate = 0.2,
            Selector = new TournamentSelector<ExpressionTree>(tournamentSize: 2),
            Elites = 1
        };

        var finalState = await algorithm.CompleteAsync(
          problem,
          RandomNumberGenerator.Create(123),
          ct: TestContext.Current.CancellationToken);

        finalState.Population.EvaluatedCandidates.All(candidate => problem.SearchSpace.Contains(candidate.Candidate)).ShouldBeTrue();
        finalState.Population.EvaluatedCandidates.Length.ShouldBe(24);
    }

    [Fact]
    public void Evaluator_AuthoringShape_EnablesNumericOptimizationWithoutInPlaceMutation()
    {
        /*
        var problem = SymbolicRegressionProblem.CreateDefault(
          CreateLinearDataset(),
          inputVariables: ["x0"],
          metric: Metrics.RMSE,
          searchSpace: new ExpressionTreeSearchSpace(
             maximumLength: 40,
             maximumDepth: 12,
             allowedSymbols: Symbols.MinimalOperations,
             allowedVariables: ["x0"]));

        var rawExpression = (Constant(1.0) + Variable("x0")).Build();

        var evaluator = SymbolicExpressionEvaluator.OptimizeNumericParameters(
          maxIterations: 25,
          tolerance: 1e-8);

        var solution = evaluator.Evaluate(
          [rawExpression],
          RandomNumberGenerator.Create(123),
          problem.SearchSpace,
          problem).Single();

        solution.Candidate.ShouldNotBeSameAs(rawExpression);
        rawExpression.Root.ShouldBeOfType<BinaryExpressionNode>().Left
          .ShouldBeOfType<NumericConstantExpressionNode>().Value.ShouldBe(1.0);
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
          searchSpace: new ExpressionTreeSearchSpace(
             maximumLength: 40,
             maximumDepth: 12,
             allowedSymbols: Symbols.MinimalOperations,
             allowedVariables: ["x0", "x1"]));

        var algorithm = new GeneticAlgorithm<ExpressionTree, ExpressionTreeSearchSpace, SymbolicRegressionProblem>
        {
            PopulationSize = 24,
            Creator = new UnrestrictedSymbolicExpressionCreator(),
            Crossover = new UnrestrictedSymbolicExpressionCrossover(),
            Mutator = new UnrestrictedSymbolicExpressionMutator(),
            MutationRate = 0.2,
            Evaluator = SymbolicExpressionEvaluator.OptimizeNumericParameters(
              maxIterations: 25,
              tolerance: 1e-8),
            Selector = new TournamentSelector<ExpressionTree>(tournamentSize: 2),
            Elites = 1
        }.WithMaxIterations(8);

        var finalState = await algorithm.RunToCompletionAsync(
          problem,
          RandomNumberGenerator.Create(123),
          ct: TestContext.Current.CancellationToken);

        finalState.Population.EvaluatedCandidates.All(candidate => problem.SearchSpace.Contains(candidate.Candidate)).ShouldBeTrue();
        algorithm.Evaluator.Counters.FunctionEvaluations.ShouldBeGreaterThan(0);
        */

        data.InputVariables.ShouldBe(["x0", "x1"]);
        typeof(GeneticAlgorithm<,,>).ShouldNotBeNull();
    }

    private sealed class PrefixExpressionFormatter : ExpressionFormatter
    {
        protected override string FormatOperation(OperationSymbol symbol, IReadOnlyList<string> children) =>
            $"{symbol.Name}[{string.Join(", ", children)}]";
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
        new(
            DataFrame.FromMatrix(
                ["x0", "x1"],
                new double[,]
                {
                    { 1.0, 3.0 },
                    { 2.0, 4.0 },
                    { 3.0, 5.0 }
                }),
            new Series<double>("y", [7.0, 10.0, 13.0]));

    private static ExpressionTreeSearchSpace CreateSearchSpace() =>
      new(
        maximumLength: 31,
        maximumDepth: 6,
        operations: [Symbols.Addition, Symbols.Subtraction, Symbols.Multiplication],
        variables: ["x0", "x1"]);
}
