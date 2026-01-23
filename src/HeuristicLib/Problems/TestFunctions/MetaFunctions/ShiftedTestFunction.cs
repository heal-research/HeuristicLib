using HEAL.HeuristicLib.Encodings.RealVector;

namespace HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;

public class ShiftedTestFunction(RealVector shiftVector, ITestFunction inner) : MetaTestFunction(inner) {
  protected readonly RealVector ShiftVector = shiftVector;

  public override double Evaluate(RealVector solution) {
    return Inner.Evaluate(solution + ShiftVector);
  }
}

public class ShiftedGradientTestFunction(RealVector shiftVector, IGradientTestFunction inner) : ShiftedTestFunction(shiftVector, inner), IGradientTestFunction {
  protected readonly IGradientTestFunction GradientInner = inner;

  public RealVector EvaluateGradient(RealVector solution)
    => GradientInner.EvaluateGradient(solution + ShiftVector);
}
