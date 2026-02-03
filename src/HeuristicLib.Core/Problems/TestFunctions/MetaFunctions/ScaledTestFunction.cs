using HEAL.HeuristicLib.Encodings.RealVector;

namespace HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;

public class ScaledTestFunction(double[] inputScaling, double outputScaling, ITestFunction inner)
  : MetaTestFunction(inner) {
  protected readonly double[] InputScaling = inputScaling;
  protected readonly double OutputScaling = outputScaling;

  public override double Evaluate(RealVector solution) => Inner.Evaluate(solution * InputScaling) * OutputScaling;
}

public class ScaledGradientTestFunction(double[] inputScaling, double outputScaling, IGradientTestFunction inner)
  : ScaledTestFunction(inputScaling, outputScaling, inner), IGradientTestFunction {
  public RealVector EvaluateGradient(RealVector solution) {
    return inner.EvaluateGradient(solution * InputScaling) * InputScaling * OutputScaling;
  }
}
