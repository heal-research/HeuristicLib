using HEAL.HeuristicLib.Data;
using HEAL.HeuristicLib.Encodings.SymbolicExpressions;
using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.Dynamic.SlidingWindowRegression;
using HEAL.HeuristicLib.Problems.MachineLearning;
using static HEAL.HeuristicLib.Encodings.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.MachineLearning;

public sealed class DynamicRegressionQualitySpecs
{
    [Fact]
    public void Evaluator_AuthoringShape_UsesTheCurrentGroundTruthRegressorAsReference()
    {
        var data = new RegressionData(
            DataFrame.FromMatrix(
                ["x"],
                new double[,]
                {
                    { 0.0 },
                    { 1.0 },
                    { 2.0 }
                }),
            new Series<double>("y", [0.1, 1.1, 2.1]));
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 15,
            maximumDepth: 4,
            operations: Symbols.MinimalOperations,
            variables: ["x"]);
        var problem = new SlidingWindowSymbolicRegressionProblem(
            new SymbolicRegressionProblem(data, Metrics.RMSE, searchSpace),
            windowLength: 3,
            stepSize: 1);
        var groundTruthProvider = new FixedGroundTruthProvider(Variable("x").Build().ToRegressor());
        var evaluator = new ProblemEvaluator<ExpressionTree, ExpressionTreeSearchSpace,
                SlidingWindowSymbolicRegressionProblem>()
            .WithDynamicRelativeRegressionQuality(problem, groundTruthProvider);

        evaluator.SourceProblem.ShouldBeSameAs(problem);
        evaluator.GroundTruthProvider.ShouldBeSameAs(groundTruthProvider);
    }

    private sealed class FixedGroundTruthProvider(IRegressor groundTruth)
        : IGroundTruthRegressorProvider<SlidingWindowSymbolicRegressionProblem>
    {
        public IRegressor GetGroundTruth(SlidingWindowSymbolicRegressionProblem problem) => groundTruth;
    }
}
