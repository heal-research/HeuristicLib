using HEAL.HeuristicLib.Data;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.Dynamic.Regression;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Problems.Dynamic.Regression;

public sealed class DynamicRelativeQualityRegressionEvaluatorTests
{
    [Fact]
    public void Evaluator_UsesGroundTruthLossAndRefreshesItAfterEpochChange()
    {
        var problem = new TestDynamicRegressionProblem();
        var evaluator = new ProblemEvaluator<RealVector, BoundedRealVectorSearchSpace, TestDynamicRegressionProblem>()
            .WithDynamicRelativeRegressionQuality(problem, new ZeroGroundTruthProvider());
        var instance = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestDynamicRegressionProblem>(evaluator);
        var candidate = new RealVector(2.0);

        var before = instance.Evaluate([candidate], TestRandoms.NoRandom, problem.SearchSpace, problem)[0];
        problem.UpdateOnce();
        var after = instance.Evaluate([candidate], TestRandoms.NoRandom, problem.SearchSpace, problem)[0];

        before[0].ShouldBe(1.0, 1e-12);
        after[0].ShouldBe(-0.5, 1e-12);
    }

    [Fact]
    public void Evaluator_RejectsDifferentProblemInstance()
    {
        var problem = new TestDynamicRegressionProblem();
        var otherProblem = new TestDynamicRegressionProblem();
        var evaluator = new ProblemEvaluator<RealVector, BoundedRealVectorSearchSpace, TestDynamicRegressionProblem>()
            .WithDynamicRelativeRegressionQuality(problem, new ZeroGroundTruthProvider());
        var instance = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestDynamicRegressionProblem>(evaluator);

        Should.Throw<InvalidOperationException>(() =>
            instance.Evaluate([new RealVector(2.0)], TestRandoms.NoRandom, otherProblem.SearchSpace, otherProblem));
    }

    private sealed class TestDynamicRegressionProblem()
        : DynamicRegressionProblem<TestDynamicRegressionProblem, RealVector, BoundedRealVectorSearchSpace>(
            new ObjectiveDirections(
                [ObjectiveDirection.Minimize],
                new SingleObjectiveComparer(ObjectiveDirection.Minimize)),
            new BoundedRealVectorSearchSpace(1, double.NegativeInfinity, double.PositiveInfinity),
            TestRandoms.NoRandom)
    {
        private static readonly ImmutableArray<IRegressionMetric> PredictionMetricSet =
            [HEAL.HeuristicLib.MachineLearning.Metrics.MSE];
        private RegressionData currentData = CreateData(1.0);

        public override RegressionData CurrentData => currentData;
        public override ImmutableArray<IRegressionMetric> PredictionMetrics => PredictionMetricSet;

        protected override ObjectiveVector Evaluate(
            RealVector candidate,
            IRandomNumberGenerator random,
            int epoch) =>
            new(candidate[0]);

        protected override void Update() => currentData = CreateData(2.0);

        private static RegressionData CreateData(double target)
        {
            var inputs = new DataFrame([Series<double>.FromOwnedArray("x", [0.0, 1.0])]);
            return new RegressionData(inputs, Series<double>.FromOwnedArray("y", [target, target]));
        }
    }

    private sealed class ZeroGroundTruthProvider : IGroundTruthRegressorProvider<TestDynamicRegressionProblem>
    {
        public IRegressor GetGroundTruth(TestDynamicRegressionProblem problem) => ZeroRegressor.Instance;
    }

    private sealed class ZeroRegressor : IRegressor
    {
        public static ZeroRegressor Instance { get; } = new();
        public string PredictionName => "prediction";

        public Series<double> Predict(DataFrame inputs) =>
            Series<double>.FromOwnedArray("prediction", new double[inputs.RowCount]);

        public void Predict(DataFrame inputs, Span<double> destination) => destination.Clear();
    }
}
