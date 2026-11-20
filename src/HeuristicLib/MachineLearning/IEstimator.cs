using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.MachineLearning;

public interface IEstimator<in TTrainingData, TPredictor>
    where TPredictor : IPredictor
{
    Task<TPredictor> FitAsync(TTrainingData trainingData, IRandomNumberGenerator random, CancellationToken cancellationToken = default);
}

public static class EstimatorExtensions
{
    extension<TTrainingData, TPredictor>(IEstimator<TTrainingData, TPredictor> estimator)
        where TPredictor : IPredictor
    {
        public TPredictor Fit(TTrainingData trainingData, IRandomNumberGenerator random, CancellationToken cancellationToken = default) =>
            estimator.FitAsync(trainingData, random, cancellationToken).GetAwaiter().GetResult();
    }
}
