using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.DataAnalysis;

public interface IEstimator<in TTrainingData, TPredictor>
    where TPredictor : IPredictor
{
    Task<TPredictor> FitAsync(TTrainingData trainingData, IRandomNumberGenerator random, CancellationToken cancellationToken = default);
}

public static class EstimatorExtensions
{
    public static TPredictor Fit<TTrainingData, TPredictor>(this IEstimator<TTrainingData, TPredictor> estimator, TTrainingData trainingData, IRandomNumberGenerator random, CancellationToken cancellationToken = default)
        where TPredictor : IPredictor =>
        estimator.FitAsync(trainingData, random, cancellationToken).GetAwaiter().GetResult();
}
