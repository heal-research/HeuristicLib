using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions.ZDT;

public abstract class Zdt : IMultiObjectiveGradientTestFunction
{
  protected Zdt(int dimension)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(dimension, 2);
    Dimension = dimension;
  }

  public int Dimension { get; }
  public double Min => 0;
  public double Max => 1;
  public Objective Objective => MultiObjective.Create(ObjectiveDirection.Minimize, ObjectiveDirection.Minimize);

  public RealVector Evaluate(RealVector solution)
  {
    var g = G(solution);
    var f1 = F1(solution);
    var h = H(f1, g);

    return new RealVector(f1, g * h);
  }

  public RealVector[] EvaluateGradient(RealVector solution)
  {
    var g = G(solution);
    var f1 = F1(solution);
    var f1Grad = F1Gradient(solution);
    var h = H(f1, g);
    var gGrad = GGradient(solution);
    var (dhDf1, dhDg) = HGradient(f1, g);
    var grad = gGrad * (g * dhDg + h);
    grad += f1Grad * (g * dhDf1);

    return [f1Grad, grad];
  }

  protected virtual double F1(RealVector solution) => solution[0];

  protected virtual RealVector F1Gradient(RealVector solution)
  {
    var r = new double[solution.Count];
    r[0] = 1.0;

    return r;
  }

  protected abstract double G(RealVector solution);
  protected abstract RealVector GGradient(RealVector solution);
  protected abstract double H(double f1, double g);
  protected abstract (double dh_df1, double dh_dg) HGradient(double f1, double g);
}
