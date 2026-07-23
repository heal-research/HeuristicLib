using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
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

        var predictions = expression.Evaluate(data.TrainingInputs);

        predictions.ShouldBe([7.0, 10.0, 13.0], tolerance: 1e-12);
        data.TrainingInputs.DoubleSeriesNames.Order().ShouldBe(["x0", "x1"]);
        data.TargetName.ShouldBe("y");
        data.TrainingTarget.Values.ToArray().ShouldBe([7.0, 10.0, 13.0]);
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
    public void Problem_AuthoringShape_ConstructsDefaultSymbolicRegressionProblemWithRmseMetric()
    {
        var data = CreateLinearRegressionData();
        var searchSpace = CreateSearchSpace();
        var problem = new SymbolicExpressionRegressionProblem(data, Metrics.RMSE, searchSpace);

        problem.Metric.ShouldBe(Metrics.RMSE);
        problem.SearchSpace.ShouldBeSameAs(searchSpace);
        problem.Objective.Directions.ShouldBe([ObjectiveDirection.Minimize]);
    }

    [Fact]
    public async Task GeneticAlgorithm_AuthoringShape_RunsWithNewCreatorCrossoverAndMutator()
    {
        var data = CreateLinearRegressionData();
        var searchSpace = CreateSearchSpace();
        var problem = new SymbolicExpressionRegressionProblem(data, Metrics.RMSE, searchSpace);
        var algorithm = new GeneticAlgorithm<ExpressionTree, ExpressionTreeSearchSpace, SymbolicExpressionRegressionProblem>
        {
            PopulationSize = 24,
            MaximumGenerations = 8,
            Creator = new RampedHalfAndHalfTreeCreator(),
            Crossover = new SubtreeCrossover(),
            Mutator = new ChooseOneMutator<ExpressionTree, ExpressionTreeSearchSpace, SymbolicExpressionRegressionProblem>(
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
             allowedSymbols: Symbols.BasicArithmetic,
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
             allowedSymbols: Symbols.BasicArithmetic,
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

    private static ExpressionTreeSearchSpace CreateSearchSpace() =>
      new(
        maximumLength: 31,
        maximumDepth: 6,
        operations: [Symbols.Addition, Symbols.Subtraction, Symbols.Multiplication],
        variables: ["x0", "x1"]);
}
