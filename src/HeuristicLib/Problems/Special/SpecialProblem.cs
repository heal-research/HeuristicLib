using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.Special;

// This is an example problem that do not use any of the standard search spaces and needs to define its own operators

public class SpecialProblem(double data) : Problem<SpecialGenotype, SpecialEncoding>(SingleObjective.Maximize, GetEncoding()) {
  public double Data { get; set; } = data;

  public override ObjectiveVector Evaluate(SpecialGenotype solution) {
    return Data + solution.Value;
  }

  private static SpecialEncoding GetEncoding() {
    return new SpecialEncoding();
  }
}
