using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class CombinedTestFunction : IMultiObjectiveTestFunction {
  public CombinedTestFunction(params IEnumerable<ITestFunction> functions) {
    Functions = functions.ToArray();
    Dimension = Functions.Select(f => f.Dimension).Distinct().Single();
    Min = Functions.Select(f => f.Min).Max();
    Max = Functions.Select(f => f.Max).Min();
    Objective = MultiObjective.Create(Functions.Select(f => f.Objective).ToArray());
  }

  public ITestFunction[] Functions { get; }
  public int Dimension { get; }
  public double Min { get; }
  public double Max { get; }
  public Objective Objective { get; }

  public RealVector Evaluate(RealVector solution) => new(Functions.Select(x => x.Evaluate(solution)));
}

public class CombinedGradientTestFunction : CombinedTestFunction, IMultiObjectiveGradientTestFunction {
  private readonly IGradientTestFunction[] functions;

  public CombinedGradientTestFunction(params IReadOnlyCollection<IGradientTestFunction> functions) : base(functions) {
    this.functions = functions.ToArray();
  }

  public RealVector[] EvaluateGradient(RealVector solution) {
    return functions.Select(x => x.EvaluateGradient(solution)).ToArray();
  }
}
