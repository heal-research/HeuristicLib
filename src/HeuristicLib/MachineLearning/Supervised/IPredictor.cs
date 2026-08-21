using HEAL.HeuristicLib.Data;
namespace HEAL.HeuristicLib.MachineLearning;

public interface IPredictor<TPrediction> : IPredictor
    where TPrediction : notnull
{
    Series<TPrediction> Predict(DataFrame inputs);

    void Predict(DataFrame inputs, Span<TPrediction> destination);
}
