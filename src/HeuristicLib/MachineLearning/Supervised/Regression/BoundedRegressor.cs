using System.Numerics.Tensors;
using HEAL.HeuristicLib.Data;

namespace HEAL.HeuristicLib.MachineLearning;

public sealed class BoundedRegressor : IRegressor
{
    public BoundedRegressor(IRegressor regressor, double lowerBound, double upperBound)
    {
        if (double.IsNaN(lowerBound))
            throw new ArgumentOutOfRangeException(nameof(lowerBound));
        if (double.IsNaN(upperBound) || lowerBound > upperBound)
            throw new ArgumentOutOfRangeException(nameof(upperBound));

        Regressor = regressor;
        LowerBound = lowerBound;
        UpperBound = upperBound;
    }

    public IRegressor Regressor { get; }
    public string PredictionName => Regressor.PredictionName;
    public double LowerBound { get; }
    public double UpperBound { get; }

    public Series<double> Predict(DataFrame inputs)
    {
        var values = new double[inputs.RowCount];
        Predict(inputs, values);
        return Series<double>.FromOwnedArray(PredictionName, values);
    }

    public void Predict(DataFrame inputs, Span<double> destination)
    {
        if (destination.Length != inputs.RowCount)
            throw new ArgumentException($"Destination must contain exactly {inputs.RowCount} values but contains {destination.Length}.", nameof(destination));

        Regressor.Predict(inputs, destination);
        TensorPrimitives.Clamp(destination, LowerBound, UpperBound, destination);
    }
}

public static class BoundedRegressorExtensions
{
    extension(IRegressor regressor)
    {
        public BoundedRegressor ToBounded(double lowerBound, double upperBound)
        {
            return new BoundedRegressor(regressor, lowerBound, upperBound);
        }
    }
}
