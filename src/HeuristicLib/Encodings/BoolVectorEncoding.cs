using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Encodings;

public record class BoolVectorEncoding : Encoding<BoolVector> {
  public int Length { get; }

  public BoolVectorEncoding(int length) {
    Length = length;
  }

  public override bool Contains(BoolVector genotype) {
    return genotype.Count == Length;
  }
}
