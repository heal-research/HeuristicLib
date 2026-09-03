using HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;
using HEAL.HeuristicLib.Problems.MachineLearning.Legacy;
using LegacySymbolicRegressionProblem = HEAL.HeuristicLib.Problems.MachineLearning.Legacy.SymbolicRegressionProblem;

namespace HEAL.HeuristicLib.Tests.Problems.MachineLearning.Legacy.Regression;

public sealed class RegressionProblemTests
{
    [Fact]
    public void Constructor_DerivesPredictionBoundsFromTrainingTargetStatistics()
    {
        var dataset = Dataset.FromRowData(
            ["x", "y"],
            new[,]
            {
                { 0.0, 1.0 },
                { 0.0, 3.0 },
                { 0.0, 5.0 }
            });
        var problemData = new RegressionProblemData(dataset, "y", trainingRange: ..3);
        IRegressionEvaluator<SymbolicExpressionTree>[] evaluators = [new MeanSquaredErrorCalculator()];

        var problem = new LegacySymbolicRegressionProblem(problemData, evaluators);

        problem.UpperPredictionBound.ShouldBe(43.0);
        problem.LowerPredictionBound.ShouldBe(-37.0);
    }
}
