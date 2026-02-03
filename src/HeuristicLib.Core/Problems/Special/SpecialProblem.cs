using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Special;

// This is an example problem that do not use any of the standard search spaces and needs to define its own operators

public class SpecialProblem(double data) : Problem<SpecialGenotype, SpecialSearchSpace>(SingleObjective.Maximize, GetEncoding()) {
  public double Data { get; set; } = data;

  public override ObjectiveVector Evaluate(SpecialGenotype solution, IRandomNumberGenerator random) {
    return Data + solution.Value;
  }

  private static SpecialSearchSpace GetEncoding() {
    return new SpecialSearchSpace();
  }
}
