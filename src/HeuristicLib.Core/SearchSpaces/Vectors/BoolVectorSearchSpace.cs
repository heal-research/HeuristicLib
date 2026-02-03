using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Encodings.BoolVector;

public record BoolVectorSearchSpace(int Length) : SearchSpace<BoolVector> {
  public override bool Contains(BoolVector genotype) {
    return genotype.Count == Length;
  }
}
