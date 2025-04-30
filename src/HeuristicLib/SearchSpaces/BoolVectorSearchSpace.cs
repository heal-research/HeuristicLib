using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.SearchSpaces;

public record class BoolVectorSearchSpace : SearchSpace<BoolVector> {
  public int Length { get; }

  public BoolVectorSearchSpace(int length) {
    Length = length;
  }

  public override bool Contains(BoolVector genotype) {
    return genotype.Count == Length;
  }
}
