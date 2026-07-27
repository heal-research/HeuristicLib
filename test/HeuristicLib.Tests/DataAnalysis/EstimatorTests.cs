using HEAL.HeuristicLib.DataAnalysis;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.DataAnalysis;

public sealed class EstimatorTests
{
    [Fact]
    public void Fit_ReturnsThePredictorProducedByFitAsync()
    {
        var predictor = new TestPredictor();
        var estimator = new TestEstimator(predictor);
        var random = RandomNumberGenerator.Create(42);

        var result = estimator.Fit(
            "training data",
            random,
            TestContext.Current.CancellationToken);

        result.ShouldBeSameAs(predictor);
        estimator.TrainingData.ShouldBe("training data");
        estimator.Random.ShouldBeSameAs(random);
    }

    private sealed class TestEstimator(TestPredictor predictor)
        : IEstimator<string, TestPredictor>
    {
        public string? TrainingData { get; private set; }
        public IRandomNumberGenerator? Random { get; private set; }

        public Task<TestPredictor> FitAsync(
            string trainingData,
            IRandomNumberGenerator random,
            CancellationToken cancellationToken = default)
        {
            TrainingData = trainingData;
            Random = random;
            return Task.FromResult(predictor);
        }
    }

    private sealed class TestPredictor : IPredictor<double>
    {
        public string PredictionName => "prediction";

        public Series<double> Predict(DataFrame inputs) =>
            new(PredictionName, Enumerable.Repeat(0.0, inputs.RowCount));

        public void Predict(DataFrame inputs, Span<double> destination)
        {
            if (destination.Length != inputs.RowCount)
                throw new ArgumentException(nameof(destination));

            destination.Clear();
        }
    }
}
