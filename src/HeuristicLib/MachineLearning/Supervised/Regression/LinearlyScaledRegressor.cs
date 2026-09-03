using HEAL.HeuristicLib.Data;

namespace HEAL.HeuristicLib.MachineLearning;

public sealed class LinearlyScaledRegressor : IRegressor
{
    public LinearlyScaledRegressor(IRegressor regressor, LinearScalingParameters parameters)
    {
        Regressor = regressor;
        Parameters = parameters;
    }

    public IRegressor Regressor { get; }
    public LinearScalingParameters Parameters { get; }
    public string PredictionName => Regressor.PredictionName;

    public static LinearlyScaledRegressor Fit(IRegressor regressor, RegressionData data)
    {
        var predictions = new double[data.RowCount];
        regressor.Predict(data.Inputs, predictions);
        var parameters = LinearScaling.Fit(predictions, data.Target.Values.Span);
        return new LinearlyScaledRegressor(regressor, parameters);
    }

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
        LinearScaling.Apply(destination, Parameters, destination);
    }
}

public static class LinearlyScaledRegressorExtensions
{
    extension(IRegressor regressor)
    {
        public LinearlyScaledRegressor FitLinearScaling(RegressionData data)
        {
            return LinearlyScaledRegressor.Fit(regressor, data);
        }
    }
}
