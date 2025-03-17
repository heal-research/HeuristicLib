#if false

namespace HEAL.HeuristicLib._ProofOfConcept;

public interface IEstimator<out TModel, in TFeatures, in TTarget> 
  where TModel : IPredictor<TFeatures, TTarget> {
  TModel Fit(TFeatures[] features, TTarget[] targets);
}

public interface IPredictor<in TFeatures, out TTarget> {
  TTarget[] Predict(TFeatures[] features);
}

public abstract class EstimatorBase<TModel, TFeature, TTarget> 
  : IEstimator<TModel, TFeature, TTarget>
  where TModel : IPredictor<TFeature, TTarget> {
  public abstract TModel Fit(TFeature[] features, TTarget[] targets);
}

public class LinearRegression : EstimatorBase<LinearRegressionModel, double[], double> {
  public LinearRegression(double learningRate = 0.01, int maxIterations = 1000, double tolerance = 1e-4, bool fitIntercept = true) {
    LearningRate = learningRate;
    MaxIterations = maxIterations;
    Tolerance = tolerance;
    FitIntercept = fitIntercept;
  }
  
  public double LearningRate { get; }
  public int MaxIterations { get; }
  public double Tolerance { get; }
  public bool FitIntercept { get; }

  public override LinearRegressionModel Fit(double[][] features, double[] targets) {
    return FitIntercept switch {
      true => new LinearRegressionModel([1.0, 2.0], intercept: 3.0),
      false => new LinearRegressionModel([2.0, 3.0], intercept: 0)
    };
  }
}

public class LinearRegressionModel : IPredictor<double[], double> {
  public LinearRegressionModel(double[] coefficients, double intercept) {
    Coefficients = coefficients;
    Intercept = intercept;
  }

  public double[] Coefficients { get; }
  public double Intercept { get; }

  public double[] Predict(double[][] features) {
    double[] predictions = new double[features.Length];
    for (int i = 0; i < features.Length; i++) {
      predictions[i] = PredictSingle(features[i]);
    }
    return predictions;
  }
  
  private double PredictSingle(double[] features) {
    double result = Intercept;
    for (int i = 0; i < Coefficients.Length; i++) {
      result += Coefficients[i] * features[i];
    }
    return result;
  }
}


public static class EstimatorsExample {
  public static void Execute() {
    // Create with parameters in constructor
    var estimator = new LinearRegression(
      learningRate: 0.05,
      maxIterations: 2000,
      tolerance: 1e-6,
      fitIntercept: true
    );
        
    // Train the model with sample data
    double[][] X = [
      [1, 2, 3],
      [4, 5, 6]
    ];
    double[] y = [10, 20];
        
    var model = estimator.Fit(X, y);
        
    // Use the model for prediction
    double[] predictions = model.Predict(X);
    for (int i = 0; i < predictions.Length; i++) {
      Console.WriteLine($"Predicted: {predictions[i]}, Actual: {y[i]}");
    }
  }
}

#endif
