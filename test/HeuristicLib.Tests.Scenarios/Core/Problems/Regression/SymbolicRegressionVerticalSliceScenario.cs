using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionCreators;
using HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionCrossovers;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

namespace HEAL.HeuristicLib.Tests.Scenarios.Core.Problems.Regression;

public sealed class SymbolicRegressionVerticalSliceScenario(ITestOutputHelper output)
{
    [Fact]
    public async Task GeneticProgramming_FitsSyntheticDataAndRetainsCompiledBestExpression()
    {
        var data = CreateData();
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 15,
            maximumDepth: 5,
            operations: [Symbols.Addition, Symbols.Subtraction, Symbols.Multiplication],
            variables: ["x0", "x1"],
            constants: [new EvolvableConstantSymbol(), new FixedConstantSymbol(2.0)]);
        var problem = new SymbolicExpressionRegressionProblem(data, Metrics.RMSE, searchSpace);
        var mutator = new ChooseOneMutator<ExpressionTree, ExpressionTreeSearchSpace, SymbolicExpressionRegressionProblem>(
            [new NodeReplacementMutator(), new SubtreeMutator(), new LocalPerturbationMutator()],
            [1.0, 1.0, 1.0]);
        var algorithm = new GeneticAlgorithm<ExpressionTree, ExpressionTreeSearchSpace, SymbolicExpressionRegressionProblem>
        {
            PopulationSize = 80,
            MaximumGenerations = 30,
            Creator = new RampedHalfAndHalfTreeCreator(),
            Crossover = new SubtreeCrossover(),
            Mutator = mutator,
            MutationRate = 0.3,
            Selector = new TournamentSelector<ExpressionTree>(tournamentSize: 3),
            Elites = 1
        };

        var finalState = await algorithm.CompleteAsync(
            problem,
            RandomNumberGenerator.Create(42),
            ct: TestContext.Current.CancellationToken);

        var best = finalState.Population.EvaluatedCandidates
            .OrderBy(solution => solution.ObjectiveVector[0])
            .First();
        var baseline = CalculateConstantMeanBaseline(data.TrainingTarget.Values.Span);
        var compiled = best.Candidate.Compile(optimize: true);
        var firstPredictions = ExpressionInterpreter.Interpret(compiled, data.TrainingInputs);
        var repeatedPredictions = ExpressionInterpreter.Interpret(compiled, data.TrainingInputs);
        var retainedCompiledScore = Metrics.RMSE.Evaluate(firstPredictions, data.TrainingTarget.Values.Span);

        finalState.Population.EvaluatedCandidates.Length.ShouldBe(80);
        finalState.Population.EvaluatedCandidates.All(candidate => searchSpace.Contains(candidate.Candidate)).ShouldBeTrue();
        best.ObjectiveVector[0].ShouldBeLessThan(baseline);
        retainedCompiledScore.ShouldBe(best.ObjectiveVector[0], tolerance: 1e-12);
        repeatedPredictions.ShouldBe(firstPredictions, tolerance: 0.0);

        output.WriteLine($"Best expression: {best.Candidate.ToInfixString()}");
        output.WriteLine($"Training RMSE: {best.ObjectiveVector[0]:G6}");
        output.WriteLine($"Compiled instructions: {compiled.Length}");
    }

    private static RegressionData CreateData()
    {
        const int valuesPerVariable = 11;
        var inputs = new double[valuesPerVariable * valuesPerVariable, 2];
        var target = new double[valuesPerVariable * valuesPerVariable];
        var row = 0;
        for (var x0Index = 0; x0Index < valuesPerVariable; x0Index++)
        {
            var x0 = -2.0 + 0.4 * x0Index;
            for (var x1Index = 0; x1Index < valuesPerVariable; x1Index++)
            {
                var x1 = -2.0 + 0.4 * x1Index;
                inputs[row, 0] = x0;
                inputs[row, 1] = x1;
                target[row] = x0 + 2.0 * x1;
                row++;
            }
        }

        return RegressionData.Training(
            DataFrame.FromMatrix(["x0", "x1"], inputs),
            Series<double>.Create(target, name: "y"));
    }

    private static double CalculateConstantMeanBaseline(ReadOnlySpan<double> target)
    {
        var mean = 0.0;
        foreach (var value in target)
            mean += value;
        mean /= target.Length;

        var squaredError = 0.0;
        foreach (var value in target)
        {
            var error = value - mean;
            squaredError += error * error;
        }

        return Math.Sqrt(squaredError / target.Length);
    }
}
