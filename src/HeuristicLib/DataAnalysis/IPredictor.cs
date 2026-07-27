namespace HEAL.HeuristicLib.DataAnalysis;

public interface IPredictor
{
    string PredictionName { get; }
}

public interface IPredictor<TPrediction> : IPredictor
    where TPrediction : notnull
{
    Series<TPrediction> Predict(DataFrame inputs);

    void Predict(DataFrame inputs, Span<TPrediction> destination);
}
