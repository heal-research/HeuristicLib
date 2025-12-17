using HEAL.HeuristicLib.Encodings.RealVector;

namespace HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;

public class ShiftedTestFunction(RealVector shiftVector, ITestFunction inner) : MetaTestFunction(inner) {
  protected readonly RealVector ShiftVector = shiftVector;

  public override double Evaluate(RealVector solution) {
    return Inner.Evaluate(solution + ShiftVector);
  }
}

public class ShiftedGradientTestFunction(RealVector shiftVector, IGradientTestFunction inner) : ShiftedTestFunction(shiftVector, inner), IGradientTestFunction {
  public RealVector EvaluateGradient(RealVector solution) => inner.EvaluateGradient(solution + ShiftVector);
}
