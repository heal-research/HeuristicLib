using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.SearchSpaces.Vectors;

public record BoolVectorSearchSpace(int Length) : SearchSpace<BoolVector> {
  public override bool Contains(BoolVector genotype) {
    return genotype.Count == Length;
  }
}
