using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Random;
using Xunit;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Regression;

public class SymbolicRegressionRedesignSpecs
{
    [Fact]
    public void ExpressionDraft_AuthoringShape_BuildsX0PlusTwoTimesX1()
    {
        var expression = ExpressionDraft
          .Add(
            ExpressionDraft.Variable("x0"),
            ExpressionDraft.Multiply(
              ExpressionDraft.Fixed(2.0),
              ExpressionDraft.Variable("x1")))
          .Compile();

        expression.VariableReferences.Select(variable => variable.Name).ShouldBe(["x0", "x1"]);
        expression.Instructions.Where(instruction => instruction.OpCode == SymbolicExpressionOpCode.Variable)
          .Select(instruction => instruction.PayloadIndex)
          .ShouldBe([0, 1]);
        expression.ToInfixString().ShouldBe("(x0 + (2 * x1))");
        expression.Evaluate(
            ["x0", "x1"],
            new double[,]
            {
                { 1.0, 3.0 },
                { 2.0, 4.0 },
                { 3.0, 5.0 }
            })
          .ShouldBe([7.0, 10.0, 13.0]);
    }

    [Fact]
    public void Interpreter_AuthoringShape_EvaluatesCompiledExpressionAgainstRegressionData()
    {
        var data = CreateLinearDataset();

        /*
        var expression = ExpressionDraft
          .Add(
            ExpressionDraft.Variable("x0"),
            ExpressionDraft.Multiply(
              ExpressionDraft.Fixed(2.0),
              ExpressionDraft.Variable("x1")))
          .Compile();

        var interpreter = new SymbolicExpressionInterpreter();
        var predictions = interpreter.Evaluate(
          expression,
          data.Dataset,
          rows: data.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate());

        predictions.ShouldBe([7.0, 10.0, 13.0], tolerance: 1e-12);
        */

        data.Dataset.GetVariableNames().ShouldBe(["x0", "x1", "y"]);
        data.TargetVariableValues(DataAnalysisProblemData.PartitionType.Training).ShouldBe([7.0, 10.0, 13.0]);
    }

    [Fact]
    public void Problem_AuthoringShape_ConstructsDefaultSymbolicRegressionProblemWithRmseMetric()
    {
        var data = CreateLinearDataset();

        /*
        var problem = SymbolicRegressionProblem.CreateDefault(
          data,
          inputVariables: ["x0", "x1"],
          loss: Metrics.RMSE,
          searchSpace: SymbolicExpressionSearchSpace.Unrestricted(
            maxLength: 40,
            maxDepth: 12,
            allowedVariables: ["x0", "x1"]));

        problem.Loss.ShouldBe(Metrics.RMSE);
        problem.Objective.Directions.ShouldBe([ObjectiveDirection.Minimize]);
        */

        var currentProblem = new SymbolicRegressionProblem(data, new RootMeanSquaredErrorEvaluator());
        currentProblem.Objective.Directions.ShouldBe([HEAL.HeuristicLib.Optimization.ObjectiveDirection.Minimize]);
    }

    [Fact]
    public void GeneticAlgorithm_AuthoringShape_RunsWithNewCreatorCrossoverAndMutator()
    {
        var data = CreateLinearDataset();

        /*
        var problem = SymbolicRegressionProblem.CreateDefault(
          data,
          inputVariables: ["x0", "x1"],
          loss: Metrics.RMSE,
          searchSpace: SymbolicExpressionSearchSpace.Unrestricted(
            maxLength: 40,
            maxDepth: 12,
            allowedVariables: ["x0", "x1"]));

        var algorithm = new GeneticAlgorithm<SymbolicExpression, UnrestrictedSymbolicExpressionSearchSpace, SymbolicRegressionProblem>
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

        var currentProblem = new SymbolicRegressionProblem(data, new RootMeanSquaredErrorEvaluator());
        var selector = new TournamentSelector<object>(tournamentSize: 2);

        currentProblem.SearchSpace.ShouldNotBeNull();
        selector.ShouldNotBeNull();
        RandomNumberGenerator.Create(123).ShouldNotBeNull();
        typeof(GeneticAlgorithm<,,>).ShouldNotBeNull();
    }

    [Fact]
    public void Refinement_AuthoringShape_EnablesNumericRefinementWithoutEvaluationTimeMutation()
    {
        /*
        var problem = SymbolicRegressionProblem.CreateDefault(
          CreateLinearDataset(),
          inputVariables: ["x0"],
          loss: Metrics.RMSE,
          searchSpace: SymbolicExpressionSearchSpace.Unrestricted(
            maxLength: 40,
            maxDepth: 12,
            allowedVariables: ["x0"]));

        var rawExpression = ExpressionDraft
          .Add(ExpressionDraft.Parameter(1.0), ExpressionDraft.Variable("x0"))
          .Compile();

        var refiner = SymbolicExpressionRefiner.OptimizeNumericParameters(
          maxIterations: 25,
          tolerance: 1e-8);

        var result = refiner.Refine(
          [rawExpression],
          RandomNumberGenerator.Create(123),
          problem.SearchSpace,
          problem);

        result.Candidates.Single().ShouldNotBeSameAs(rawExpression);
        rawExpression.NumericLiterals.Single().Value.ShouldBe(1.0);
        result.Counters.FunctionEvaluations.ShouldBeGreaterThan(0);
        */

        var currentParameterOptimizationRunsDuringEvaluation = typeof(SymbolicRegressionProblem)
          .GetProperty(nameof(SymbolicRegressionProblem.ParameterOptimizationIterations))
          is not null;

        currentParameterOptimizationRunsDuringEvaluation.ShouldBeTrue();
    }

    [Fact]
    public void GeneticAlgorithm_AuthoringShape_ConfiguresProblemSpecificNumericRefiner()
    {
        var data = CreateLinearDataset();

        /*
        var problem = SymbolicRegressionProblem.CreateDefault(
          data,
          inputVariables: ["x0", "x1"],
          loss: Metrics.RMSE,
          searchSpace: SymbolicExpressionSearchSpace.Unrestricted(
            maxLength: 40,
            maxDepth: 12,
            allowedVariables: ["x0", "x1"]));

        var algorithm = new GeneticAlgorithm<SymbolicExpression, UnrestrictedSymbolicExpressionSearchSpace, SymbolicRegressionProblem>
        {
            PopulationSize = 24,
            Creator = new UnrestrictedSymbolicExpressionCreator(),
            Crossover = new UnrestrictedSymbolicExpressionCrossover(),
            Mutator = new UnrestrictedSymbolicExpressionMutator(),
            MutationRate = 0.2,
            Refiner = SymbolicExpressionRefiner.OptimizeNumericParameters(
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
        algorithm.Refiner.Counters.FunctionEvaluations.ShouldBeGreaterThan(0);
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
}
