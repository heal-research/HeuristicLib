using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Encodings;

namespace HEAL.HeuristicLib.Problems;

public class MultiObjectiveTestFunctionProblem : ProblemBase<RealVector> {

  public enum FunctionType {
    ZDT1,
    ZDT2
  }
  
  private readonly FunctionType functionType;
  
  public MultiObjectiveTestFunctionProblem(FunctionType functionType) 
    : base(MultiObjective.Create([ObjectiveDirection.Minimize, ObjectiveDirection.Minimize])) {
    this.functionType = functionType;
  }
  public override Fitness Evaluate(RealVector solution) {
    return functionType switch {
      FunctionType.ZDT1 => ZDT1(solution),
      FunctionType.ZDT2 => ZDT2(solution),
      _ => throw new NotImplementedException()
    };
  }

  public static double[] ZDT1(RealVector r) {
    double g = 0;
    for (int i = 1; i < r.Count; i++) g += r[i];
    g = 1.0 + 9.0 * g / (r.Count - 1);
    double f0 = r[0];
    double f1 = g * (1.0 - Math.Sqrt(r[0] / g));
    return [f0, f1];
  }

  public static double[] ZDT2(RealVector r) {
    double g = 0;
    for (int i = 1; i < r.Count; i++) g += r[i];
    g = 1.0 + 9.0 * g / (r.Count - 1);
    double d = r[0] / g;
    double f0 = r[0];
    double f1 = g * (1.0 - d * d);
    return [f0, f1];
  }
}
