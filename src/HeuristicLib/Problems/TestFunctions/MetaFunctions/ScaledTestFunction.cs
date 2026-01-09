using HEAL.HeuristicLib.Encodings.RealVector;

namespace HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;

public class ScaledTestFunction : MetaTestFunction {
  private readonly double[] inputScaling;
  private readonly double outputScaling;

  public ScaledTestFunction(double[] inputScaling, double outputScaling, ITestFunction inner) : base(inner) {
    this.inputScaling = inputScaling;
    this.outputScaling = outputScaling;
  }

  public override double Evaluate(RealVector solution) {
    return Inner.Evaluate(solution * inputScaling) * outputScaling;
  }
}
