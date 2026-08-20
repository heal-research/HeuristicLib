using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Legacy;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Legacy.Evaluators;
using LegacySymbolicRegressionProblem = HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Legacy.SymbolicRegressionProblem;

namespace HEAL.HeuristicLib.Tests.Problems.DataAnalysis.Regression;

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
        var problemData = new RegressionProblemData(dataset, "y", trainingRange: 0..3);
        IRegressionEvaluator<SymbolicExpressionTree>[] evaluators = [new MeanSquaredErrorCalculator()];

        var problem = new LegacySymbolicRegressionProblem(problemData, evaluators);

        problem.UpperPredictionBound.ShouldBe(43.0);
        problem.LowerPredictionBound.ShouldBe(-37.0);
    }
}
