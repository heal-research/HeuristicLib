using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;

public class ShiftedTestFunction : MetaTestFunction
{
  private readonly RealVector shiftVector;

  public ShiftedTestFunction(RealVector shiftVector, ITestFunction inner)
    : base(inner) => this.shiftVector = shiftVector;

  public override double Evaluate(RealVector solution)
  {
    var shiftedISolution = solution + shiftVector;
    return Inner.Evaluate(shiftedISolution);
  }
}
