using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Encodings.Vectors;

public record BoolVectorEncoding(int Length) : Encoding<BoolVector> {
  public override bool Contains(BoolVector genotype) {
    return genotype.Count == Length;
  }
}
