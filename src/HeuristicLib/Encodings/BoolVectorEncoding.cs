using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Encodings;

public record BoolVectorEncoding(int Length) : Encoding<BoolVector> {
  public override bool Contains(BoolVector genotype) {
    return genotype.Count == Length;
  }
}
