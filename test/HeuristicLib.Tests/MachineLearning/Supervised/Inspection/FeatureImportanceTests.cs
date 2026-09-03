namespace HEAL.HeuristicLib.Tests.MachineLearning.Inspection;

public sealed class FeatureImportanceTests
{
    [Fact]
    public void Permutation_UsesRegressionDefaults()
    {
        var (predictor, data) = CreateRegressionFixture();

        var result = FeatureImportance.Permutation(
            predictor,
            data,
            RandomNumberGenerator.Create(42));

        result.Metric.ShouldBeSameAs(Metrics.MSE);
        result.BaselineMetricValue.ShouldBe(0.0);
        result.Features.Select(feature => feature.FeatureName).ShouldBe(["x1", "x2"]);
        result.Features.ShouldAllBe(feature => feature.ImportanceValues.Length == 5);
    }

    [Fact]
    public void Permutation_DelegatesToPermutationPerturbation()
    {
        var (predictor, data) = CreateRegressionFixture();

        var shortcutResult = FeatureImportance.Permutation(
            predictor,
            data,
            RandomNumberGenerator.Create(42),
            Metrics.MSE,
            repetitions: 3);
        var explicitResult = FeatureImportance.Perturbation(
            predictor,
            data,
            RandomNumberGenerator.Create(42),
            Metrics.MSE,
            FeaturePerturbations.Permutation,
            repetitions: 3);

        shortcutResult.BaselineMetricValue.ShouldBe(explicitResult.BaselineMetricValue);
        shortcutResult.Features.Select(feature => feature.PerturbedMetricValues)
            .ShouldBe(explicitResult.Features.Select(feature => feature.PerturbedMetricValues));
        shortcutResult.Features.Select(feature => feature.ImportanceValues)
            .ShouldBe(explicitResult.Features.Select(feature => feature.ImportanceValues));
    }

    [Fact]
    public void Perturbation_MeanRanksTheMoreInfluentialFeatureHigher()
    {
        var (predictor, data) = CreateRegressionFixture();

        var result = FeatureImportance.Perturbation(
            predictor,
            data,
            RandomNumberGenerator.Create(42),
            Metrics.MSE,
            FeaturePerturbations.Mean,
            repetitions: 1);

        result.Features[0].MeanImportance.ShouldBe(5.0, tolerance: 1e-12);
        result.Features[1].MeanImportance.ShouldBe(1.25, tolerance: 1e-12);
        data.Inputs.Get<double>("x1").Values.ToArray().ShouldBe([0.0, 1.0, 2.0, 3.0]);
        data.Inputs.Get<double>("x2").Values.ToArray().ShouldBe([3.0, 2.0, 1.0, 0.0]);
    }

    [Fact]
    public void Perturbation_MaximizationMetricStillReportsPositiveDegradation()
    {
        var (predictor, data) = CreateRegressionFixture();

        var result = FeatureImportance.Perturbation(
            predictor,
            data,
            RandomNumberGenerator.Create(42),
            Metrics.R2,
            FeaturePerturbations.Mean,
            repetitions: 1,
            featureNames: ["x1"]);

        result.BaselineMetricValue.ShouldBe(1.0, tolerance: 1e-12);
        result.Features[0].MeanImportance.ShouldBe(4.0, tolerance: 1e-12);
    }

    [Fact]
    public void Perturbation_MultipleMetricsReuseEachPrediction()
    {
        var (predictor, data) = CreateRegressionFixture();
        var countingPredictor = new CountingRegressor(predictor);

        var results = FeatureImportance.Perturbation(
            countingPredictor,
            data,
            RandomNumberGenerator.Create(42),
            [Metrics.MSE, Metrics.MAE],
            FeaturePerturbations.Mean,
            repetitions: 3);

        countingPredictor.PredictionCount.ShouldBe(7);
        results.Length.ShouldBe(2);
        results[0].Metric.ShouldBeSameAs(Metrics.MSE);
        results[1].Metric.ShouldBeSameAs(Metrics.MAE);
        results.ShouldAllBe(result => result.Features.All(feature => feature.ImportanceValues.Length == 3));
    }

    [Fact]
    public void Perturbation_SupportsGenericPredictionAndTargetTypes()
    {
        var inputs = DataFrame.FromMatrix(
            ["x"],
            new[,]
            {
                { 0.0 },
                { 1.0 },
                { 2.0 },
                { 3.0 }
            });
        var data = new TestSupervisedData<int>(inputs, new Series<int>("class", [0, 0, 1, 1]));

        var result = FeatureImportance.Perturbation(
            new ThresholdPredictor(),
            data,
            RandomNumberGenerator.Create(42),
            new AccuracyMetric(),
            FeaturePerturbations.Mean,
            repetitions: 1);

        result.BaselineMetricValue.ShouldBe(1.0);
        result.Features[0].MeanImportance.ShouldBe(0.5);
    }

    [Fact]
    public void Perturbation_UsesOnlyExplicitlySelectedNumericFeatures()
    {
        var inputs = new DataFrame(
        [
            new Series<double>("x1", [0.0, 1.0, 2.0, 3.0]),
            new Series<string>("label", ["a", "b", "c", "d"]),
            new Series<double>("x2", [3.0, 2.0, 1.0, 0.0])
        ]);
        var data = new RegressionData(inputs, new Series<double>("target", [3.0, 4.0, 5.0, 6.0]));

        var result = FeatureImportance.Perturbation(
            new WeightedSumRegressor(),
            data,
            RandomNumberGenerator.Create(42),
            Metrics.MSE,
            FeaturePerturbations.Mean,
            repetitions: 1,
            featureNames: ["x2"]);

        result.Features.Single().FeatureName.ShouldBe("x2");
    }

    [Fact]
    public void Perturbation_RejectsInvalidRequests()
    {
        var (predictor, data) = CreateRegressionFixture();
        var random = RandomNumberGenerator.Create(42);

        Should.Throw<ArgumentOutOfRangeException>(() => FeatureImportance.Perturbation(
            predictor, data, random, Metrics.MSE, FeaturePerturbations.Mean, repetitions: 0));
        Should.Throw<ArgumentException>(() => FeatureImportance.Perturbation(
            predictor, data, random, [], FeaturePerturbations.Mean));
        Should.Throw<ArgumentException>(() => FeatureImportance.Perturbation(
            predictor, data, random, Metrics.MSE, FeaturePerturbations.Mean, featureNames: ["x1", "x1"]));
        Should.Throw<KeyNotFoundException>(() => FeatureImportance.Perturbation(
            predictor, data, random, Metrics.MSE, FeaturePerturbations.Mean, featureNames: ["missing"]));
    }

    [Fact]
    public void PerturbationsProduceTheExpectedReplacementValues()
    {
        var random = RandomNumberGenerator.Create(42);
        var values = new[] { 1.0, 2.0, 8.0, 9.0 };
        var destination = new double[values.Length];

        FeaturePerturbations.Mean.Apply(values, destination, random);
        destination.ShouldBe([5.0, 5.0, 5.0, 5.0]);

        FeaturePerturbations.Median.Apply(values, destination, random);
        destination.ShouldBe([5.0, 5.0, 5.0, 5.0]);

        FeaturePerturbations.Resampling(new UniformDoubleDistribution(12.0, 12.0))
            .Apply(values, destination, random);
        destination.ShouldBe([12.0, 12.0, 12.0, 12.0]);

        FeaturePerturbations.Resampling()
            .Apply([5.0, 5.0, 5.0, 5.0], destination, random);
        destination.ShouldBe([5.0, 5.0, 5.0, 5.0]);

        FeaturePerturbations.Permutation.Apply(values, destination, random);
        destination.Order().ShouldBe(values.Order());
    }

    [Fact]
    public void Resampling_NegativeStandardDeviationMirrorsAroundTheMean()
    {
        var positive = new double[1];
        FeaturePerturbations.Resampling(new NormalDoubleDistribution(0.0, 1.0))
            .Apply([1.0], positive, RandomNumberGenerator.Create(42));

        var mirrored = new double[1];
        FeaturePerturbations.Resampling(new NormalDoubleDistribution(0.0, -1.0))
            .Apply([1.0], mirrored, RandomNumberGenerator.Create(42));

        mirrored[0].ShouldBe(-positive[0]);
    }

    private static (WeightedSumRegressor Predictor, RegressionData Data) CreateRegressionFixture()
    {
        var inputs = DataFrame.FromMatrix(
            ["x1", "x2"],
            new[,]
            {
                { 0.0, 3.0 },
                { 1.0, 2.0 },
                { 2.0, 1.0 },
                { 3.0, 0.0 }
            });
        var target = new Series<double>("target", [3.0, 4.0, 5.0, 6.0]);
        return (new WeightedSumRegressor(), new RegressionData(inputs, target));
    }

    private sealed class WeightedSumRegressor : IRegressor
    {
        public string PredictionName => "prediction";

        public Series<double> Predict(DataFrame inputs)
        {
            var predictions = new double[inputs.RowCount];
            Predict(inputs, predictions);
            return Series<double>.FromOwnedArray(PredictionName, predictions);
        }

        public void Predict(DataFrame inputs, Span<double> destination)
        {
            if (destination.Length != inputs.RowCount)
                throw new ArgumentException("Destination length must match the input row count.", nameof(destination));

            var x1 = inputs.Get<double>("x1").Values.Span;
            var x2 = inputs.Get<double>("x2").Values.Span;
            for (var i = 0; i < destination.Length; i++)
                destination[i] = 2.0 * x1[i] + x2[i];
        }
    }

    private sealed class CountingRegressor(IRegressor inner) : IRegressor
    {
        public int PredictionCount { get; private set; }
        public string PredictionName => inner.PredictionName;

        public Series<double> Predict(DataFrame inputs)
        {
            PredictionCount++;
            return inner.Predict(inputs);
        }

        public void Predict(DataFrame inputs, Span<double> destination)
        {
            PredictionCount++;
            inner.Predict(inputs, destination);
        }
    }

    private sealed class TestSupervisedData<T>(DataFrame inputs, Series<T> target)
        : SupervisedData<T>(inputs, target)
        where T : notnull;

    private sealed class ThresholdPredictor : IPredictor<int>
    {
        public string PredictionName => "class";

        public Series<int> Predict(DataFrame inputs)
        {
            var predictions = new int[inputs.RowCount];
            Predict(inputs, predictions);
            return Series<int>.FromOwnedArray(PredictionName, predictions);
        }

        public void Predict(DataFrame inputs, Span<int> destination)
        {
            if (destination.Length != inputs.RowCount)
                throw new ArgumentException("Destination length must match the input row count.", nameof(destination));

            var values = inputs.Get<double>("x").Values.Span;
            for (var i = 0; i < destination.Length; i++)
                destination[i] = values[i] >= 1.5 ? 1 : 0;
        }
    }

    private sealed class AccuracyMetric : IPredictionMetric<int>
    {
        public ObjectiveDirection Direction => ObjectiveDirection.Maximize;

        public double Evaluate(ReadOnlySpan<int> predictedValues, ReadOnlySpan<int> targetValues)
        {
            if (predictedValues.Length != targetValues.Length)
                throw new ArgumentException("Prediction and target lengths must match.", nameof(targetValues));

            var correct = 0;
            for (var i = 0; i < predictedValues.Length; i++)
            {
                if (predictedValues[i] == targetValues[i])
                    correct++;
            }

            return (double)correct / predictedValues.Length;
        }
    }
}
