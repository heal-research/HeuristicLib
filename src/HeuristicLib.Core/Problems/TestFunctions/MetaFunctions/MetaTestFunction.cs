using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;

public abstract class MetaTestFunction : ITestFunction
{
  protected readonly ITestFunction Inner;
  protected MetaTestFunction(ITestFunction inner) => Inner = inner;
  public virtual int Dimension => Inner.Dimension;
  public double Min => Inner.Min;
  public double Max => Inner.Max;
  public ObjectiveDirection Objective => Inner.Objective;

  public abstract double Evaluate(RealVector solution);
}
