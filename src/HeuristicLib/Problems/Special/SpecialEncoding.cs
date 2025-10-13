using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.Special;

public record SpecialEncoding : Encoding<SpecialGenotype> {
  public override bool Contains(SpecialGenotype genotype) {
    return true;
  }
}
